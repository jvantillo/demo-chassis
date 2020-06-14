using System;
using Newtonsoft.Json;

namespace JP.Demo.Chassis.SharedCode.Schemas
{
    public class TransactionCreated
    {
        [JsonRequired]
        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonRequired]
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonRequired]
        [JsonProperty("from_account")]
        public string FromAccount { get; set; }

        [JsonRequired]
        [JsonProperty("to_account")]
        public string ToAccount { get; set; }

        [JsonRequired]
        [JsonProperty("amount_cents")]
        public int AmountCents { get; set; }
    }
}
