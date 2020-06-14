using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JP.Demo.Chassis.TransactionService
{
    public class ConsumerWorker : BackgroundService
    {
        private readonly ILogger<ConsumerWorker> logger;
        private readonly KafkaSender<TransactionReply> replySender;
        private readonly KafkaSender<TransactionCreated> createdSender;
        private readonly KafkaConsumer<TransactionRequest> requestConsumer;

        public ConsumerWorker(ILogger<ConsumerWorker> logger,
            KafkaSender<TransactionReply> replySender,
            KafkaSender<TransactionCreated> createdSender,
            KafkaConsumer<TransactionRequest> requestConsumer)
        {
            this.logger = logger;
            this.replySender = replySender;
            this.createdSender = createdSender;
            this.requestConsumer = requestConsumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();       // Avoid blocking entirely, see https://github.com/dotnet/runtime/issues/36063

            await requestConsumer.Consume(stoppingToken,
                "TransactionReply",
                "transactionservice",
                "transaction-requests", async cr =>
                {
                    await ProcessMessage(cr);
                });
        }

        private async Task ProcessMessage(ConsumeResult<Ignore, TransactionRequest> cr)
        {
            var rq = cr.Message.Value;
            logger.LogInformation($"Consumed message '{rq}' (amount: {rq.AmountCents})'.");

            var replyGroupId = cr.Message.Headers.SingleOrDefault(p => p.Key == "reply-group-id");

            // We are going to pretend that we do some processing here and then send out two events:
            // * Reply to the Request that it has been processesed
            // * Publish the fact that the transaction has been created onto the bus

            // This is obviously not a real world implementation. We're sending two messages independently, without transactions.
            // We're also not concerned about commits and idempotency upon reprocessing
            // Also, if we are doing other work, such as databases, things can get complex because you will have multiple transactions, each
            // of which can fail independently.

            // Do our 'processing'
            var reply = new TransactionReply {RequestId = rq.RequestId, Status = $"Transaction of value {rq.AmountCents} has been processed"};
            var replyHeaders = new List<Tuple<string, byte[]>>();
            if (replyGroupId != null)
            {
                replyHeaders.Add(new Tuple<string, byte[]>("reply-group-id", replyGroupId.GetValueBytes()));
            }

            var createdEvent = new TransactionCreated
            {
                AmountCents = rq.AmountCents, FromAccount = rq.FromAccount, ToAccount = rq.ToAccount,
                CreatedAt = DateTime.UtcNow,
                TransactionId = Guid.NewGuid().ToString("B")
            };

            await createdSender.SendToBusWithoutRetries(createdEvent, "transactions");
            await replySender.SendToBusWithoutRetries(reply, "transaction-replies", replyHeaders);
        }
    }
}
