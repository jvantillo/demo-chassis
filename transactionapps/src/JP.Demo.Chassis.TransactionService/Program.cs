using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JP.Demo.Chassis.TransactionService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<KafkaConfig>(hostContext.Configuration.GetSection("KafkaConfig"));
                    services.AddSingleton<KafkaSender<TransactionCreated>>();
                    services.AddSingleton<KafkaSender<TransactionReply>>();
                    services.AddHostedService<ConsumerWorker>();
                });
    }
}
