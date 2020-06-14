using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JP.Demo.Chassis.HighValueNotification
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly KafkaConsumer<TransactionCreated> consumer;

        public Worker(ILogger<Worker> logger, KafkaConsumer<TransactionCreated> consumer)
        {
            this.logger = logger;
            this.consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield(); // Avoid blocking entirely, see https://github.com/dotnet/runtime/issues/36063

            await consumer.Consume(stoppingToken,
                "TransactionCreated",
                "highvaluenotification",
                "TRANSACTIONS_HIGHVALUE",
                async cr =>
                {
                    await ProcessMessage(cr);
                });
        }

        private Task ProcessMessage(ConsumeResult<Ignore, TransactionCreated> cr)
        {
            var rq = cr.Message.Value;
            logger.LogInformation($"High value transaction by '{rq.FromAccount}' (amount: {rq.AmountCents})'.");
            return Task.CompletedTask;
        }
    }
}
