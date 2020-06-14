using Newtonsoft.Json;

namespace JP.Demo.Chassis.SharedCode.Schemas
{
    public class TransactionRequest : IRequestIdentifier
    {
        [JsonRequired]
        [JsonProperty("request_id")]
        public string RequestId { get; set; }

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
