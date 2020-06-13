using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JP.Demo.Chassis.TransactionProducerDirect
{
    public class ProducerWorker : BackgroundService
    {
        private readonly ILogger<ProducerWorker> logger;
        private readonly Random rand = new Random();
        private readonly KafkaConfig kafkaOptions;

        public ProducerWorker(ILogger<ProducerWorker> logger, IOptions<KafkaConfig> kafkaOptions)
        {
            this.kafkaOptions = kafkaOptions.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Beginning direct transaction producer");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProduceTransactions(stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogInformation("Failure in producer: " + e.Message, e);
                    await Task.Delay(1000);
                }
            }

            logger.LogInformation("Stopping direct transaction producer");
        }

        private async Task ProduceTransactions(CancellationToken stoppingToken)
        {
            var config = new ProducerConfig { BootstrapServers = kafkaOptions.BootstrapServerUri };
            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = kafkaOptions.SchemaRegistryUri
            };
            var jsonSerializerConfig = new JsonSerializerConfig();

            // If serializers are not specified, default serializers from
            // `Confluent.Kafka.Serializers` will be automatically used where
            // available. Note: by default strings are encoded as UTF8.
            using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);
            using var producer = new ProducerBuilder<Null, TransactionRequest>(config)
                .SetValueSerializer(new JsonSerializer<TransactionRequest>(schemaRegistry, jsonSerializerConfig))
                .Build();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Trying to deliver message..");
                    var newMessage = new Message<Null, TransactionRequest>
                    {
                        Value = GenerateNewRandomTransaction()
                    };

                    var deliverTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    var deliveryResult = await producer.ProduceAsync("transaction-requests", newMessage, deliverTokenSource.Token);
                    logger.LogInformation($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");
                    await Task.Delay(2000, stoppingToken);
                }
                catch (ProduceException<Null, string> e)
                {
                    logger.LogInformation($"Delivery failed: {e.Error.Reason}");
                }
            }
        }

        private TransactionRequest GenerateNewRandomTransaction()
        {
            return new TransactionRequest
            {
                RequestId = "DIRECT-" + rand.Next(Int32.MaxValue),
                Amount = rand.Next(1, 100000),
                From = "BANK-A-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0'),
                To = "BANK-B-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0')
            };
        }
    }
}
