using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Jubatus.Client
{
    public class TinyMsgPackRPC : IRPC
    {
        int _maxConnectionsPerServer;
        int _maxPoolSize;
        int _maxRequestsPerConnection;
        TimeSpan _disconnectTime;
        Thread _idleCheckThread = null;
        bool _idleCheckActive = false;

        Dictionary<EndPoint, EndPointInfo> _map = new Dictionary<EndPoint,EndPointInfo> ();

        public TinyMsgPackRPC (int maxConnectionsPerServer, int maxPoolSize, int maxRequestsPerConnection, TimeSpan disconnectTime)
        {
            _maxConnectionsPerServer = maxConnectionsPerServer;
            _maxPoolSize = maxPoolSize;
            _maxRequestsPerConnection = maxRequestsPerConnection;
            _disconnectTime = disconnectTime;
            if (_disconnectTime > TimeSpan.Zero) {
                _idleCheckThread = new Thread (CheckIdleConnection);
                _idleCheckActive = true;
                _idleCheckThread.Start ();
            }
        }

        public object Invoke (EndPoint remote, string name, params object[] args)
        {
            EndPointInfo info = GetEndPointInfo(remote);
            object ret = null;
            info.Execute(delegate(Connection c) {
                ret = c.Invoke (remote, name, args);
            });
            return ret;
        }

        void CheckIdleConnection ()
        {
            List<Connection> timeouts = new List<Connection> ();
            List<EndPoint> keys = new List<EndPoint> ();
            TimeSpan halfTime = TimeSpan.FromTicks (_disconnectTime.Ticks / 2);
            while (_idleCheckActive) {
                DateTime startTime = DateTime.Now;
                lock (_map) {
                    keys.AddRange (_map.Keys);
                }
                for (int i = 0; i < keys.Count; i ++) {
                    EndPointInfo info;
                    lock (_map) {
                        if (!_map.TryGetValue (keys[i], out info))
                            continue;
                    }
                    info.RemoveIdleConnection (_disconnectTime, timeouts);
                }

                for (int i = 0; i < timeouts.Count; i ++) {
                    timeouts[i].Dispose ();
                }
                keys.Clear ();
                timeouts.Clear ();

                TimeSpan sleepTime = halfTime - (DateTime.Now - startTime);
                if (sleepTime > TimeSpan.Zero) {
                    try {
                        Thread.Sleep (sleepTime);
                    } catch (ThreadInterruptedException) {}
                }
            }
        }

        public void Dispose()
        {
            if (_idleCheckActive) {
                _idleCheckActive = false;
                _idleCheckThread.Interrupt ();
                try {
                    if (_idleCheckThread.IsAlive)
                        _idleCheckThread.Join ();
                } catch {}
            }
        }

        EndPointInfo GetEndPointInfo (EndPoint remote)
        {
            EndPointInfo info;
            bool usePool = (_idleCheckThread != null);
            lock (_map) {
                if (!_map.TryGetValue (remote, out info)) {
                    info = new EndPointInfo (remote, _maxConnectionsPerServer, _maxRequestsPerConnection, usePool);
                    _map.Add (remote, info);
                }
            }
            return info;
        }

        sealed class EndPointInfo
        {
            EndPoint _remote;
            int _maxConn, _maxReq;
            bool _useConnectionPool;
            Queue<Connection> _freeQueue; // TODO: Replace Queue -> Stack

            public EndPointInfo (EndPoint remote, int maxConnections, int maxRequests, bool useConnectionPool)
            {
                _remote = remote;
                _maxConn = maxConnections;
                _maxReq = maxRequests;
                _useConnectionPool = useConnectionPool;
                Semaphore = new SemaphoreSlim (maxConnections);
                _freeQueue = new Queue<Connection> (maxConnections);
            }

            SemaphoreSlim Semaphore { get; set; }

            public void Execute (Action<Connection> act)
            {
                Connection c = null;
                Semaphore.Wait ();
                try {
                    lock (_freeQueue) {
                        c = (_freeQueue.Count > 0 ? _freeQueue.Dequeue () : new Connection (_remote));
                    }
                    if (!c.Connected)
                        c.Connect ();
                    act (c);
                } catch {
                    c.Dispose ();
                    c = null;
                } finally {
                    if (c != null) {
                        if (_useConnectionPool && c.HandledRequests < _maxReq) {
                            c.LastRequestHandledTime = DateTime.Now;
                            lock (_freeQueue) {
                                _freeQueue.Enqueue (c);
                            }
                        } else {
                            c.Dispose ();
                            c = null;
                        }
                    }
                    Semaphore.Release ();
                }
            }

            public void RemoveIdleConnection (TimeSpan idleTimeout, IList<Connection> timeouts)
            {
                lock (_freeQueue) {
                    while (_freeQueue.Count > 0) {
                        if (DateTime.Now.Subtract (_freeQueue.Peek().LastRequestHandledTime) >= idleTimeout) {
                            timeouts.Add (_freeQueue.Dequeue ());
                        } else {
                            break;
                        }
                    }
                }
            }
        }

        sealed class Connection : IRPC, IDisposable
        {
            static MsgPack.ObjectPacker _packer = new MsgPack.ObjectPacker ();
            static MsgPack.BoxingPacker _unpacker = new MsgPack.BoxingPacker ();
            Socket _sock = null;
            Stream _strm = null;

            public Connection (EndPoint ep)
            {
                RemoteEndPoint = ep;
                Connected = false;
                LastRequestHandledTime = DateTime.Now;
            }

            public int HandledRequests { get; private set; }
            public bool Connected { get; private set; }
            public EndPoint RemoteEndPoint { get; private set; }
            public DateTime LastRequestHandledTime { get; set; }

            public void Connect ()
            {
                if (Connected)
                    return;
                _sock = new Socket(RemoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _sock.Connect (RemoteEndPoint);
                _strm = new BufferedStream (new NetworkStream (_sock, false));
                Connected = true;
                Console.WriteLine ("Connect to {0}", RemoteEndPoint);
            }

            public void Dispose ()
            {
                if (_sock == null || !Connected)
                    return;
                Console.WriteLine ("Disconnect from {0}", RemoteEndPoint);
                try {
                    _sock.Close ();
                } catch {}
            }

            public object Invoke (EndPoint remote, string name, params object[] args)
            {
                HandledRequests ++;
                int msg_id = (int)(DateTime.Now.Ticks % 65535);
                _packer.Pack(_strm, new object[] { 0, msg_id, name, args });
                _strm.Flush ();
                object[] response = (object[])_unpacker.Unpack (_strm);
                if (response.Length != 4 || ((int)response[0]) != 1)
                    throw new Exception ();
                if (response[2] != null)
                    throw new Exception ("err_code=" + response[2].ToString ());
                return response[3];
            }
        }
    }
}
