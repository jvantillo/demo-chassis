using System;
using Newtonsoft.Json;

namespace JP.Demo.Chassis.SharedCode.Schemas
{
    public class TransactionCreated
    {
        [JsonRequired]
        [JsonProperty("createdat")]
        public DateTime CreatedAt { get; set; }

        [JsonRequired]
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonRequired]
        [JsonProperty("to")]
        public string To { get; set; }

        [JsonRequired]
        [JsonProperty("amount")]
        public int Amount { get; set; }
    }
}
