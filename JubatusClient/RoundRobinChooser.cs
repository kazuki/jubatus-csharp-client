using System.Net;
using System.Threading;

namespace Jubatus.Client
{
    public class RoundRobinChooser : IServerChooser
    {
        EndPoint[] _endpoints;
        int _counter = 0;

        public RoundRobinChooser (EndPoint[] endpoints)
        {
            _endpoints = endpoints;
        }

        public EndPoint Choose()
        {
            int idx = Interlocked.Increment (ref _counter) % _endpoints.Length;
            return _endpoints[idx];
        }
    }
}
