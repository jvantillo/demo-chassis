using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JP.Demo.Chassis.SharedCode.Kafka
{
    public class KafkaSender<T>
        : IDisposable
        where T : class, new()
    {
        private readonly ILogger logger;
        private readonly KafkaConfig kafkaOptions;

        private IProducer<Null, T> producer;
        private ISchemaRegistryClient schemaRegistryClient;
        private readonly object myLock = new object();

        public KafkaSender(ILogger<KafkaSender<T>> logger, IOptions<KafkaConfig> kafkaOptions)
        {
            this.logger = logger;
            this.kafkaOptions = kafkaOptions.Value;
        }

        private void SetupProducer()
        {
            if (producer != null)
            {
                return;
            }

            lock (myLock)
            {
                Dispose();
            }

            var config = new ProducerConfig { BootstrapServers = kafkaOptions.BootstrapServerUri };
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = kafkaOptions.SchemaRegistryUri };
            var jsonSerializerConfig = new JsonSerializerConfig();

            schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);
            producer = new ProducerBuilder<Null, T>(config)
                .SetValueSerializer(new JsonSerializer<T>(schemaRegistryClient, jsonSerializerConfig))
                .Build();
        }

        public void Dispose()
        {
            if (producer != null)
            {
                producer.Dispose();
                producer = null;
            }

            if (schemaRegistryClient != null)
            {
                schemaRegistryClient.Dispose();
                schemaRegistryClient = null;
            }
        }

        public async Task<bool> SendToBusWithoutRetries(T rq, string target, List<Tuple<string, byte[]>> headers = null)
        {
            SetupProducer();

            try
            {
                logger.LogInformation("Trying to deliver message..");
                var newMessage = new Message<Null, T> {Value = rq, Headers = new Headers()};
                if (headers != null)
                {
                    foreach (var tuple in headers)
                    {
                        newMessage.Headers.Add(tuple.Item1, tuple.Item2);
                    }
                }

                var deliverTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var deliveryResult = await producer.ProduceAsync(target, newMessage, deliverTokenSource.Token);
                logger.LogInformation($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");
                return true;
            }
            catch (ProduceException<Null, string> e)
            {
                logger.LogInformation($"Delivery failed: {e.Error.Reason}");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError("Fatal failure upon sending message: " + ex, ex);
                return false;
            }
        }
    }
}
