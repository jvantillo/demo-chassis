using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.Logging;

namespace JP.Demo.Chassis.TransactionApi
{
    public class RequestReplyImplementation
    {
        private readonly KafkaSender<TransactionRequest> sender;
        private readonly ILogger<RequestReplyImplementation> logger;
        private readonly KafkaRequestReplyGroup replyGroup;
        private readonly ConcurrentDictionary<string, TransactionReply> pendingRequests;

        public RequestReplyImplementation(KafkaSender<TransactionRequest> sender,
            ILogger<RequestReplyImplementation> logger,
            KafkaRequestReplyGroup replyGroup)
        {
            this.sender = sender;
            this.logger = logger;
            this.replyGroup = replyGroup;

            // Note: we could leak memory over time here. Not a problem for this demo
            pendingRequests = new ConcurrentDictionary<string, TransactionReply>();
        }

        public async Task<TransactionReply> RequestTransaction(TransactionRequest rq)
        {
            // Generate our request ID. Could be used for idempotency.
            // Should be provided by our calling client so that also that part can be made idempotent
            var requestId = "API-" + Guid.NewGuid().ToString("B");
            rq.RequestId = requestId;

            // Set up receiving our response
            pendingRequests[requestId] = null;

            // Note: you can carry over information in headers like this. An example would be to put the reply topic in a header,
            // so that you can let consumers know where to expect back the reply. Or add the correlation IDs in the headers.
            // For example, this would be the place where you want to think of distributed tracing. In this example, we could carry over
            // our Jaeger correlation ID.

            // Specifically, we are going to carry over our reply group ID, which the consumer must communicate back to use
            var rqHeaders = new List<Tuple<string, byte[]>>
            {
                new Tuple<string, byte[]>("reply-group-id", Encoding.ASCII.GetBytes(replyGroup.MyUniqueConsumerGroup))
            };

            // Send our request
            var sendSuccess = await sender.SendToBusWithoutRetries(rq, "transaction-requests", rqHeaders);
            if (!sendSuccess)
            {
                return null;
            }

            // Wait for response and return it.
            try
            {
                using var ts = new CancellationTokenSource();
                ts.CancelAfter(TimeSpan.FromSeconds(5));
                await WaitForReplyById(requestId, ts.Token);
            }
            catch (TaskCanceledException ex)
            {
                // If we fail, return null
                logger.LogError("Task cancelled waiting for response", ex);
                return null;
            }

            if (pendingRequests.TryRemove(requestId, out var reply))
            {
                return reply;
            }

            // we failed for some weird reason
            return null;
        }

        private async Task WaitForReplyById(string requestId, CancellationToken cancelToken)
        {
            // small busy loop waiting for replies. Should do this more event driven :)
            logger.LogInformation($"Waiting for {requestId}");
            while (!cancelToken.IsCancellationRequested)
            {
                await Task.Delay(50);
                if (pendingRequests[requestId] != null)
                {
                    return;
                }
            }
        }

        public void ProcessReply(TransactionReply messageValue)
        {
            // again, possibility for memory leak. Don't care for demo
            pendingRequests[messageValue.RequestId] = messageValue;
        }
    }
}
