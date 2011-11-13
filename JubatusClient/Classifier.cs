using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Jubatus.Client
{
    public class Classifier : Accessor
    {
        public Classifier (IRPC rpc, IServerChooser chooser, string name)
            : base (rpc, chooser, name) {}

        protected override string ImplementationName {
            get { return "classifier"; }
        }

        public void SetConfig (Config cfg)
        {
            CheckReponse (_rpc.Invoke(_chooser.Choose(), "set_config", _name, cfg.ToRPCObject()));
        }

        public object GetConfig()
        {
            return CheckReponse(_rpc.Invoke(_chooser.Choose(), "get_config", _name));
        }

        public object GetStatus()
        {
            return CheckReponse(_rpc.Invoke(_chooser.Choose(), "get_status", _name));
        }

        public void Train (IList<KeyValuePair<string, Datum>> data)
        {
            object[][] rpc_data = new object[data.Count][];
            for (int i = 0; i < rpc_data.Length; i ++)
                rpc_data[i] = new object[] {data[i].Key, data[i].Value.ToRPCObject ()};
            CheckReponse (_rpc.Invoke (_chooser.Choose(), "train", _name, rpc_data));
        }

        public EstimateResults[] Classify(Datum data)
        {
            return Classify (new Datum[] {data})[0];
        }

        public EstimateResults[][] Classify(IList<Datum> data)
        {
            object[] rpc_data = new object[data.Count];
            for (int i = 0; i < rpc_data.Length; i ++)
                rpc_data[i] = data[i].ToRPCObject();
            try {
                object[] results = (object[])CheckReponse(_rpc.Invoke(_chooser.Choose(), "classify", _name, rpc_data));
                EstimateResults[][] estimates = new EstimateResults[results.Length][];
                for (int i = 0; i < estimates.Length; i ++) {
                    object[] objs = (object[])results[i];
                    estimates[i] = new EstimateResults[objs.Length];
                    for (int j = 0; j < estimates[i].Length; j ++) {
                        object[] tmp = (object[])objs[j];
                        estimates[i][j] = new EstimateResults (Encoding.UTF8.GetString((byte[])tmp[0]), (double)tmp[1]);
                    }
                }
                return estimates;
            } catch (JubatusClientExeption) {
                throw;
            } catch {
                throw new JubatusClientExeption ("unknown reuslt object format");
            }
        }
    }
}
