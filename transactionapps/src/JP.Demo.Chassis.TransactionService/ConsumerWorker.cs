using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

namespace JP.Demo.Chassis.TransactionService
{
    public class ConsumerWorker : BackgroundService
    {
        private readonly ILogger<ConsumerWorker> logger;
        private readonly KafkaSender<TransactionReply> replySender;
        private readonly KafkaSender<TransactionCreated> createdSender;
        private readonly ITracer tracer;
        private readonly KafkaConfig kafkaOptions;

        public ConsumerWorker(ILogger<ConsumerWorker> logger, IOptions<KafkaConfig> kafkaOptions,
            KafkaSender<TransactionReply> replySender, KafkaSender<TransactionCreated> createdSender,
            ITracer tracer)
        {
            this.kafkaOptions = kafkaOptions.Value;
            this.logger = logger;
            this.replySender = replySender;
            this.createdSender = createdSender;
            this.tracer = tracer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting transaction service");
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ConsumeFromKafka(stoppingToken);
                    }
                    catch (Exception e)
                    {
                        logger.LogInformation("Failure in transaction consumer: " + e.Message, e);
                        await Task.Delay(1000);
                    }
                }
            }
            logger.LogInformation("Stopping transaction service");
        }

        private async Task ConsumeFromKafka(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                GroupId = "transaction-ingest",
                BootstrapServers = kafkaOptions.BootstrapServerUri,
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Latest
            };
            var jsonSerializerConfig = new JsonSerializerConfig();

            using var c = new ConsumerBuilder<Ignore, TransactionRequest>(config)
                .SetValueDeserializer(new JsonDeserializer<TransactionRequest>(jsonSerializerConfig).AsSyncOverAsync())
                .Build();
            c.Subscribe("transaction-requests");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = c.Consume(stoppingToken);

                        // Set up our distributed trace
                        var spanBuilder = KafkaTracingHelper.ObtainConsumerSpanBuilder(tracer, cr.Message.Headers, "Process TransactionRequest");
                        using var s = spanBuilder.StartActive(true);

                        var rq = cr.Message.Value;
                        logger.LogInformation($"Consumed message '{rq}' (amount: {rq.Amount}) at: '{cr.TopicPartitionOffset}'.");

                        var replyGroupId = cr.Message.Headers.SingleOrDefault(p => p.Key == "reply-group-id");

                        // We are going to pretend that we do some processing here and then send out two events:
                        // * Reply to the Request that it has been processes
                        // * Publish the fact that the transaction has been created onto the bus

                        // This is obviously not a real world implementation. We're sending two messages independently, without transactions.
                        // We're also not concerned about commits and idempotency upon reprocessing
                        // Also, if we are doing other work, such as databases, things can get complex because you will have multiple transactions, each
                        // of which can fail independently.
                        
                        // Do our 'processing'
                        var reply = new TransactionReply {RequestId = rq.RequestId, Status = $"Transaction of value {rq.Amount} has been processed"};
                        var replyHeaders = new List<Tuple<string, byte[]>>();
                        if (replyGroupId != null)
                        {
                            replyHeaders.Add(new Tuple<string, byte[]>("reply-group-id", replyGroupId.GetValueBytes()));
                        }

                        var createdEvent = new TransactionCreated { Amount = rq.Amount, From = rq.From, To = rq.To, CreatedAt = DateTime.UtcNow };

                        await createdSender.SendToBusWithoutRetries(createdEvent, "transactions");
                        await replySender.SendToBusWithoutRetries(reply, "transaction-replies", replyHeaders);
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
