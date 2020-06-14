using System;
using System.Threading.Tasks;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTracing;

namespace JP.Demo.Chassis.TransactionApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private static readonly Random rand = new Random();
        private readonly ILogger<TransactionController> logger;
        private readonly RequestReplyImplementation impl;
        private readonly ITracer tracer;

        public TransactionController(ILogger<TransactionController> logger, RequestReplyImplementation impl, ITracer tracer)
        {
            this.logger = logger;
            this.impl = impl;
            this.tracer = tracer;
        }

        [HttpGet]
        [Route("create")]
        public async Task<ActionResult<TransactionReply>> Create()
        {
            logger.LogInformation("Generating and sending new transaction...");
            var trans = GenerateNewRandomTransaction();

            var spanBuilder = tracer.BuildSpan("Send transaction request to Kafka");
            using var rSpan = spanBuilder.StartActive(true);
            var reply = await impl.RequestTransaction(trans);

            if (reply == null)
            {
                return StatusCode(500);
            }

            return Ok(reply);
        }

        private TransactionRequest GenerateNewRandomTransaction()
        {
            return new TransactionRequest
            {
                AmountCents = rand.Next(1, 100000),
                FromAccount = "BANK-A-" + rand.Next(1, 10).ToString().PadLeft(8, '0'),
                ToAccount = "BANK-B-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0')
            };
        }
    }
}
