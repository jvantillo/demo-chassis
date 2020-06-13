using System;
using System.Linq;

namespace JP.Demo.Chassis.TransactionApi
{
    public class KafkaRequestReplyGroup
    {
        public string MyUniqueConsumerGroup { get; private set; }

        private static readonly Random rand = new Random();

        public KafkaRequestReplyGroup()
        {
            // Assign a unique consumer group name
            MyUniqueConsumerGroup = "RPL-" + RandomString(16);
            // note: technically we could generate a duplicate or old ID. This would have nasty effects. Use time + pid or something to make them unique
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
        }
    }
}
