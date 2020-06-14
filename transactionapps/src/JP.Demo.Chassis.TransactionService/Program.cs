using JP.Demo.Chassis.JaegerShared;
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
                    var config = hostContext.Configuration;
                    services.AddJaegerTracingForService(options => {
                        options.JaegerAgentHost = config["JAEGER_AGENT_HOST"];
                        options.ServiceName = config["GLOBAL_NAME"];
                    });

                    services.Configure<KafkaConfig>(hostContext.Configuration.GetSection("KafkaConfig"));

                    services.AddSingleton<KafkaConsumer<TransactionRequest>>();
                    services.AddSingleton<KafkaSender<TransactionCreated>>();
                    services.AddSingleton<KafkaSender<TransactionReply>>();
                    services.AddHostedService<ConsumerWorker>();
                });
    }
}
