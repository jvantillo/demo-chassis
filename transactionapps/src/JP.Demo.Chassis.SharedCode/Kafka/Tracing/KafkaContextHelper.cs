using System;
using System.Collections.Generic;
using System.Text;

namespace JP.Demo.Chassis.SharedCode.Kafka.Tracing
{
    public static class KafkaTracingContextHelper
    {
        public static byte[] Encode(IDictionary<string, string> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pair in dict)
            {
                sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Key)));
                sb.Append("$");
                sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Value)));
                sb.Append("$");
            }

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public static IDictionary<string, string> Decode(byte[] enc)
        {
            var splitted = Encoding.ASCII.GetString(enc).Split(new[] {'$'}, StringSplitOptions.RemoveEmptyEntries);
            var map = new Dictionary<string, string>();
            for (int i = 0; i < splitted.Length; i+=2)
            {
                string key = Encoding.UTF8.GetString(Convert.FromBase64String(splitted[i]));
                string val = Encoding.UTF8.GetString(Convert.FromBase64String(splitted[i + 1]));
                map[key] = val;
            }

            return map;
        }
    }
}
