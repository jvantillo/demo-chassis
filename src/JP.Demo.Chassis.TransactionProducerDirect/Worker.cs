using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace JP.Demo.Chassis.TransactionProducerDirect
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly Random rand = new Random();

        public Worker(ILogger<Worker> logger)
        {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Beginning direct transaction producer");
            try
            {
                await ProduceTransactions(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogInformation("Failure in producer: " + e.Message, e);
                throw;
            }
            logger.LogInformation("Stopping direct transaction producer");
        }

        private async Task ProduceTransactions(CancellationToken stoppingToken)
        {
            var config = new ProducerConfig { BootstrapServers = "broker:29092" };
            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = "http://schema-registry:8081"
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
                    var deliveryResult = await producer.ProduceAsync("transaction-requests", newMessage, stoppingToken);
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
                Amount = rand.Next(1, 100000),
                From = "BANK-A-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0'),
                To = "BANK-B-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0')
            };
        }
    }
}
