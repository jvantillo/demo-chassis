using Newtonsoft.Json;

namespace JP.Demo.Chassis.SharedCode.Schemas
{
    public class TransactionReply
    {
        /// <summary>
        /// Note: this is the ID of the request we are replying to. Not the message ID or something
        /// </summary>
        [JsonRequired]
        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonRequired]
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
