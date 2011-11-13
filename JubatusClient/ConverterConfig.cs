using System.Collections.Generic;

using param_t = System.Collections.Generic.Dictionary<string, string>;

namespace Jubatus.Client
{
    public class ConverterConfig
    {
        public Dictionary<string, param_t> string_filter_types;
        public List<FilterRule> string_filter_rules;

        public Dictionary<string, param_t> num_filter_types;
        public List<FilterRule> num_filter_rules;

        public Dictionary<string, param_t> string_types;
        public List<StringRule> string_rules;

        public Dictionary<string, param_t> num_types;
        public List<NumRule> num_rules;

        public ConverterConfig ()
        {
            string_filter_types = new Dictionary<string,param_t> ();
            string_filter_rules = new List<FilterRule> ();
            num_filter_types = new Dictionary<string, param_t>();
            num_filter_rules = new List<FilterRule>();
            string_types = new Dictionary<string, param_t>();
            string_rules = new List<StringRule>();
            num_types = new Dictionary<string, param_t>();
            num_rules = new List<NumRule>();
        }

        public object ToRPCObject ()
        {
            return new object[] {
                string_filter_types,
                ToRPCObject(string_filter_rules),
                num_filter_types,
                ToRPCObject(num_filter_rules),
                string_types,
                ToRPCObject(string_rules),
                num_types,
                ToRPCObject(num_rules)
            };
        }

        static object ToRPCObject (IList<FilterRule> list)
        {
            object[] items = new object[list.Count];
            for (int i = 0; i < items.Length; i ++)
                items[i] = new object[] {
                    list[i].key, list[i].type, list[i].suffix
                };
            return items;
        }

        static object ToRPCObject(IList<StringRule> list)
        {
            object[] items = new object[list.Count];
            for (int i = 0; i < items.Length; i++)
                items[i] = new object[] {
                    list[i].key, list[i].type, list[i].sample_weight, list[i].global_weight
                };
            return items;
        }

        static object ToRPCObject(IList<NumRule> list)
        {
            object[] items = new object[list.Count];
            for (int i = 0; i < items.Length; i++)
                items[i] = new object[] {
                    list[i].key, list[i].type
                };
            return items;
        }
    }
}
