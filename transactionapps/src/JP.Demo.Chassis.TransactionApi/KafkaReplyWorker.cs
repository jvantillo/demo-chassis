using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Kafka.Tracing;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing;

namespace JP.Demo.Chassis.TransactionApi
{
    public class KafkaReplyWorker : BackgroundService
    {
        private readonly ILogger<KafkaReplyWorker> logger;
        private readonly KafkaRequestReplyGroup replyGroup;
        private readonly RequestReplyImplementation reqRep;
        private readonly ITracer tracer;
        private readonly KafkaConfig kafkaOptions;
        private readonly string myUniqueConsumerGroup;
        private static readonly Random rand = new Random();

        public KafkaReplyWorker(ILogger<KafkaReplyWorker> logger,
            IOptions<KafkaConfig> kafkaOptions,
            KafkaRequestReplyGroup replyGroup,
            RequestReplyImplementation reqRep,
            ITracer tracer)
        {
            this.logger = logger;
            this.replyGroup = replyGroup;
            this.reqRep = reqRep;
            this.tracer = tracer;
            this.kafkaOptions = kafkaOptions.Value;

            // Assign a unique consumer group name
            myUniqueConsumerGroup = "RPL-" + RandomString(16);
            // note: technically we could generate a duplicate or old ID. This would have nasty effects. Use time + pid or something to make them unique
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting transaction response consumer");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Run(() => ConsumeFromKafka(stoppingToken));
                }
                catch (Exception e)
                {
                    logger.LogInformation("Failure in response consumer: " + e.Message, e);
                    await Task.Delay(1000);
                }
            }

            logger.LogInformation("Stopping transaction response consumer");
        }

        private void ConsumeFromKafka(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                GroupId = myUniqueConsumerGroup,
                BootstrapServers = kafkaOptions.BootstrapServerUri,
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Latest
            };
            var jsonSerializerConfig = new JsonSerializerConfig();

            using var c = new ConsumerBuilder<Ignore, TransactionReply>(config)
                .SetValueDeserializer(new JsonDeserializer<TransactionReply>(jsonSerializerConfig).AsSyncOverAsync())
                .Build();
            c.Subscribe("transaction-replies");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = c.Consume(stoppingToken);

                        // Set up our distributed trace
                        var spanBuilder = KafkaTracingHelper.ObtainConsumerSpanBuilder(tracer, cr.Message.Headers, "Process TransactionReply");
                        using var s = spanBuilder.StartActive(true);

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
                    }
                    catch (ConsumeException e)
                    {
                        logger.LogInformation($"Error occured: {e.Error.Reason}");
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("Stopping topic consumption, cancellation requested");
                        c.Close();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ensure the consumer leaves the group cleanly and final offsets are committed.
                c.Close();
            }
        }
    }
}
