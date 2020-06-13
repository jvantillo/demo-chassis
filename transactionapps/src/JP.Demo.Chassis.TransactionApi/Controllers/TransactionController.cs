using System;
using System.Threading.Tasks;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JP.Demo.Chassis.TransactionApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private static readonly Random rand = new Random();
        private readonly ILogger<TransactionController> logger;
        private readonly RequestReplyImplementation impl;

        public TransactionController(ILogger<TransactionController> logger, RequestReplyImplementation impl)
        {
            this.logger = logger;
            this.impl = impl;
        }

        [HttpGet]
        [Route("create")]
        public async Task<ActionResult<TransactionReply>> Create()
        {
            logger.LogInformation("Generating and sending new transaction...");
            var trans = GenerateNewRandomTransaction();
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
                Amount = rand.Next(1, 100000),
                From = "BANK-A-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0'),
                To = "BANK-B-" + rand.Next(100000, 1000000).ToString().PadLeft(8, '0')
            };
        }
    }
}
