using System;
using System.Text;

namespace Jubatus.Client
{
    public abstract class Accessor
    {
        protected IRPC _rpc;
        protected IServerChooser _chooser;
        protected string _name;

        protected Accessor (IRPC rpc, IServerChooser chooser, string name)
        {
            _rpc = rpc;
            _chooser = chooser;
            _name = name;
        }

        public void Load(string id)
        {
            CheckReponse(_rpc.Invoke(_chooser.Choose(), "load", _name, ImplementationName, id));
        }

        public void Save (string id)
        {
            CheckReponse(_rpc.Invoke(_chooser.Choose(), "save", _name, ImplementationName, id));
        }

        protected abstract string ImplementationName { get; }

        protected object CheckReponse (object o)
        {
            try {
                object[] response = (object[])o;
                if (response.Length != 3)
                    throw new FormatException ();
                if ((bool)response[0])
                    return response[1];
                throw new JubatusClientExeption (Encoding.UTF8.GetString((byte[])response[2]));
            } catch (JubatusClientExeption) {
                throw;
            } catch {
                throw new JubatusClientExeption ("unknown response message format");
            }
        }
    }
}
