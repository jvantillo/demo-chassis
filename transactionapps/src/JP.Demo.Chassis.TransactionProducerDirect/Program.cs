using JP.Demo.Chassis.SharedCode.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JP.Demo.Chassis.TransactionProducerDirect
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
                    services.AddHostedService<ProducerWorker>();
                });
    }
}
