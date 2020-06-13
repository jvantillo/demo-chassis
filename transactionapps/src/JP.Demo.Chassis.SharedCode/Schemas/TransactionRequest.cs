﻿using Newtonsoft.Json;

namespace JP.Demo.Chassis.SharedCode.Schemas
{
    public class TransactionRequest
    {
        [JsonRequired]
        [JsonProperty("requestid")]
        public string RequestId { get; set; }

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
