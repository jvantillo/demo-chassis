using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JP.Demo.Chassis.TransactionApi
{
    public class KafkaReplyWorker : BackgroundService
    {
        private readonly ILogger<KafkaReplyWorker> logger;
        private readonly KafkaRequestReplyGroup replyGroup;
        private readonly RequestReplyImplementation<TransactionRequest, TransactionReply> reqRep;
        private readonly KafkaConsumer<TransactionReply> consumer;

        public KafkaReplyWorker(ILogger<KafkaReplyWorker> logger,
            KafkaRequestReplyGroup replyGroup,
            RequestReplyImplementation<TransactionRequest, TransactionReply> reqRep,
            KafkaConsumer<TransactionReply> consumer)
        {
            this.logger = logger;
            this.replyGroup = replyGroup;
            this.reqRep = reqRep;
            this.consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();       // Avoid blocking entirely, see https://github.com/dotnet/runtime/issues/36063

            await consumer.Consume(stoppingToken, "TransactionReply", replyGroup.MyUniqueConsumerGroup, "transaction-replies", async msg =>
            {
                await ProcessMessage(msg);
            });
        }

        private Task ProcessMessage(ConsumeResult<Ignore, TransactionReply> cr)
        {
            var groupIdHeader = cr.Message.Headers.SingleOrDefault(p => p.Key == "reply-group-id");
            if (groupIdHeader == null || replyGroup.MyUniqueConsumerGroup != Encoding.ASCII.GetString(groupIdHeader.GetValueBytes()))
            {
                // Not for us
                logger.LogInformation("Discarding reply message, not for this reply group");
            }
            else
            {
                logger.LogInformation($"Consumed message '{cr.Message.Value}' (ReqID: {cr.Message.Value.RequestId} STATUS: {cr.Message.Value.Status}) at: '{cr.TopicPartitionOffset}'.");
                // Now we need to bring this to the RequestReplyImplementation so that we can process it there.
                // We are just directly injecting it into the Singleton of that class. Obviously, better constructs exist :)
                reqRep.ProcessReply(cr.Message.Value);
            }

            return Task.CompletedTask;
        }
    }
}
