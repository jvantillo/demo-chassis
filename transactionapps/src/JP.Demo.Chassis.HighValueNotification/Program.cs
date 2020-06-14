using JP.Demo.Chassis.JaegerShared;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JP.Demo.Chassis.HighValueNotification
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

                    // Set up our tracing configuration. We expect this to be in env vars
                    services.AddJaegerTracingForService(options =>
                    {
                        options.JaegerAgentHost = config["JAEGER_AGENT_HOST"];
                        options.ServiceName = config["GLOBAL_NAME"];
                    });

                    // Set up our Kafka config
                    services.Configure<KafkaConfig>(hostContext.Configuration.GetSection("KafkaConfig"));

                    // Register services
                    services.AddSingleton<KafkaConsumer<TransactionCreated>>();

                    // Add our background worker
                    services.AddHostedService<Worker>();
                });
    }
}
