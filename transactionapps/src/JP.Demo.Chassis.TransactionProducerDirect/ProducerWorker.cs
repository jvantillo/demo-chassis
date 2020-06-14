using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JP.Demo.Chassis.TransactionProducerDirect
{
    public class ProducerWorker : BackgroundService
    {
        private readonly ILogger<ProducerWorker> logger;
        private readonly KafkaSender<TransactionRequest> sender;
        private readonly Random rand = new Random();

        public ProducerWorker(ILogger<ProducerWorker> logger, KafkaSender<TransactionRequest> sender)
        {
            this.logger = logger;
            this.sender = sender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Beginning direct transaction producer");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var rq = GenerateNewRandomTransaction();
                    var rqHeaders = new List<Tuple<string, byte[]>> { new Tuple<string, byte[]>("reply-group-id", Encoding.ASCII.GetBytes("irrelevant-for-me")) };
                    await sender.SendToBusWithoutRetries(rq, "transaction-requests", rqHeaders);

                    await Task.Delay(250, stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogInformation("Failure in producer: " + e.Message, e);
                    await Task.Delay(1000);
                }
            }

            logger.LogInformation("Stopping direct transaction producer");
        }

        private TransactionRequest GenerateNewRandomTransaction()
        {
            return new TransactionRequest
            {
                RequestId = "DIRECT-" + rand.Next(Int32.MaxValue),
                AmountCents = rand.Next(1, 100000),
                FromAccount = "BANK-A-" + rand.Next(1, 10).ToString().PadLeft(8, '0'),
                ToAccount = "BANK-B-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0')
            };
        }
    }
}
