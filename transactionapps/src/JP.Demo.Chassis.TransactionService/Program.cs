using System;
using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Util;

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
                    services.AddJaegerTracing(options => {
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

    public static class JaegerTracingServiceCollectionExtensions
    {
        // Jaeger for .NET Core sources:
        // https://github.com/jaegertracing/jaeger-client-csharp
        // https://medium.com/imaginelearning/jaeger-tracing-on-kubernetes-with-net-core-8b5feddb6f2f
        // https://itnext.io/jaeger-tracing-on-kubernetes-with-asp-net-core-and-traefik-86b1d9fd5489

        public static IServiceCollection AddJaegerTracing(this IServiceCollection services, Action<JaegerTracingOptions> setupAction = null)
        {
            // Run setup action
            if (setupAction != null)
            {
                services.ConfigureJaegerTracing(setupAction);
            }

            // Configure Open Tracing with non-default behavior, skipping ASP.Net and Entity Framework
            services.AddOpenTracingCoreServices(builder =>
                builder.AddCoreFx()
                    .AddLoggerProvider());

            services.AddSingleton<ITracer>(serviceProvider =>
            {
                // Get the options for the various parts of the tracer
                var options = serviceProvider.GetService<IOptions<JaegerTracingOptions>>().Value;

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                var senderConfig = new Configuration.SenderConfiguration(loggerFactory)
                    .WithAgentHost(options.JaegerAgentHost)
                    .WithAgentPort(options.JaegerAgentPort);

                var sender = senderConfig.GetSender();

                var reporter = new RemoteReporter.Builder()
                    .WithLoggerFactory(loggerFactory)
                    .WithSender(sender)
                    .Build();

                var sampler = new GuaranteedThroughputSampler(options.SamplingRate, options.LowerBound);

                var tracer = new Tracer.Builder(options.ServiceName)
                    .WithLoggerFactory(loggerFactory)
                    .WithReporter(reporter)
                    .WithSampler(sampler)
                    .Build();

                // Allows code that can't use dependency injection to have access to the tracer.
                if (!GlobalTracer.IsRegistered())
                {
                    GlobalTracer.Register(tracer);
                }

                return tracer;
            });

            return services;
        }

        public static void ConfigureJaegerTracing(this IServiceCollection services, Action<JaegerTracingOptions> setupAction)
        {
            services.Configure(setupAction);
        }
    }

    public class JaegerTracingOptions
    {
        public double SamplingRate { get; set; }
        public double LowerBound { get; set; }
        public string JaegerAgentHost { get; set; }
        public int JaegerAgentPort { get; set; }
        public string ServiceName { get; set; }

        public JaegerTracingOptions()
        {
            SamplingRate = 1d;
            LowerBound = 1d;
            JaegerAgentHost = "i-do-not-exist";
            JaegerAgentPort = 6831;
        }
    }
}
