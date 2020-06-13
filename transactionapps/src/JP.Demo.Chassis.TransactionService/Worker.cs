using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JP.Demo.Chassis.TransactionService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;

        public Worker(ILogger<Worker> logger)
        {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting transaction service");
            await Task.Run(() => ConsumeFromKafka(stoppingToken));
            logger.LogInformation("Stopping transaction service");
        }

        private void ConsumeFromKafka(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                GroupId = "transaction-ingest",
                BootstrapServers = "broker:29092",
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
                        logger.LogInformation($"Consumed message '{cr.Message.Value}' (amount: {cr.Message.Value.Amount}) at: '{cr.TopicPartitionOffset}'.");
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
