using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using JP.Demo.Chassis.SharedCode.Kafka.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Propagation;

namespace JP.Demo.Chassis.SharedCode.Kafka
{
    public class KafkaSender<T>
        : IDisposable
        where T : class, new()
    {
        private readonly ILogger logger;
        private readonly ITracer tracer;
        private readonly KafkaConfig kafkaOptions;

        private IProducer<Null, T> producer;
        private ISchemaRegistryClient schemaRegistryClient;
        private readonly object myLock = new object();

        public KafkaSender(ILogger<KafkaSender<T>> logger, IOptions<KafkaConfig> kafkaOptions, ITracer tracer)
        {
            this.logger = logger;
            this.tracer = tracer;
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
            var builder = tracer.BuildSpan("Send message to Kafka");
            using var s = builder.StartActive(true);
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

                // Propagate our tracing information
                IDictionary<string, string> dict = new Dictionary<string, string>();
                tracer.Inject(tracer.ActiveSpan.Context, BuiltinFormats.TextMap, new TextMapInjectAdapter(dict));
                var contextForKafka = KafkaTracingContextHelper.Encode(dict);
                newMessage.Headers.Add("tracing-id", contextForKafka);

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
