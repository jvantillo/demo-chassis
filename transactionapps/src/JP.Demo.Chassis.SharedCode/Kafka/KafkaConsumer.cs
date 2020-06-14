using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using JP.Demo.Chassis.SharedCode.Kafka.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing;

namespace JP.Demo.Chassis.SharedCode.Kafka
{
    public class KafkaConsumer<T> where T : class, new()
    {
        private readonly ILogger<KafkaConsumer<T>> logger;
        private readonly ITracer tracer;
        private readonly KafkaConfig kafkaOptions;

        public KafkaConsumer(ILogger<KafkaConsumer<T>> logger,
            ITracer tracer,
            IOptions<KafkaConfig> kafkaOptions)
        {
            this.logger = logger;
            this.tracer = tracer;
            this.kafkaOptions = kafkaOptions.Value;
        }

        public async Task Consume(CancellationToken stoppingToken,
            string consumerDisplayName,
            string consumerGroupId,
            string sourceTopic,
            Func<ConsumeResult<Ignore, T>, Task> processingCallback)
        {
            logger.LogInformation($"Starting {consumerDisplayName} consumer");
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ConsumeFromKafka(stoppingToken, consumerDisplayName, consumerGroupId, sourceTopic, processingCallback);
                    }
                    catch (Exception e)
                    {
                        logger.LogInformation($"Failure in {consumerDisplayName} consumer: {e.Message}", e);
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            logger.LogInformation($"Stopping {consumerDisplayName} consumer");
        }

        private async Task ConsumeFromKafka(CancellationToken stoppingToken,
            string consumerDisplayName,
            string consumerGroupId,
            string sourceTopic,
            Func<ConsumeResult<Ignore, T>, Task> processingCallback)
        {
            var config = new ConsumerConfig
            {
                GroupId = consumerGroupId,
                BootstrapServers = kafkaOptions.BootstrapServerUri,
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Latest
            };
            var jsonSerializerConfig = new JsonSerializerConfig();

            using var c = new ConsumerBuilder<Ignore, T>(config)
                .SetValueDeserializer(new JsonDeserializer<T>(jsonSerializerConfig).AsSyncOverAsync())
                .Build();
            c.Subscribe(sourceTopic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = c.Consume(stoppingToken);

                        // Set up our distributed trace
                        var spanBuilder = KafkaTracingHelper.ObtainConsumerSpanBuilder(tracer, cr.Message.Headers, $"Consume message in {consumerDisplayName}");
                        using var s = spanBuilder.StartActive(true);

                        await processingCallback(cr);
                    }
                    catch (ConsumeException e)
                    {
                        logger.LogInformation($"Error in consumer {consumerDisplayName} occured: {e.Error.Reason}");
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation($"Stopping topic consumption for consumer {consumerDisplayName}, cancellation requested");
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
