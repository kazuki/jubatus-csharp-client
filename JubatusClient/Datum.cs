using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jubatus.Client
{
    public class Datum
    {
        public List<KeyValuePair<string,string>> string_values;
        public List<KeyValuePair<string,double>> num_values;

        public Datum ()
        {
            string_values = new List<KeyValuePair<string,string>> ();
            num_values = new List<KeyValuePair<string,double>> ();
        }

        public object ToRPCObject ()
        {
            object[][] strs = new object[string_values.Count][];
            object[][] nums = new object[num_values.Count][];
            for(int i = 0; i < strs.Length; i ++)
                strs[i] = new object[] {string_values[i].Key, string_values[i].Value};
            for (int i = 0; i < nums.Length; i++)
                nums[i] = new object[] { num_values[i].Key, num_values[i].Value };
            return new object[] { strs, nums };
        }
    }
}
