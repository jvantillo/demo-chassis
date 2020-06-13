using System;
using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Util;
using Prometheus;

namespace JP.Demo.Chassis.TransactionApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddJaegerTracing(options => {
                options.JaegerAgentHost = Configuration["JAEGER_AGENT_HOST"];
                options.ServiceName = Configuration["GLOBAL_NAME"];
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();      // Disabled because using self-signed certificate and I don't want to deal with this right now

            app.UseRouting();

            app.UseHttpMetrics(); // Enable Prometheus HTTP metrics

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics(); // Enable Prometheus metrics
            });
        }
    }

    // Jaeger for .NET Core sources:
    // https://github.com/jaegertracing/jaeger-client-csharp
    // https://medium.com/imaginelearning/jaeger-tracing-on-kubernetes-with-net-core-8b5feddb6f2f
    // https://itnext.io/jaeger-tracing-on-kubernetes-with-asp-net-core-and-traefik-86b1d9fd5489

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

    public static class JaegerTracingServiceCollectionExtensions
    {
        public static IServiceCollection AddJaegerTracing(this IServiceCollection services, Action<JaegerTracingOptions> setupAction = null)
        {
            // Run setup action
            if (setupAction != null)
            {
                services.ConfigureJaegerTracing(setupAction);
            }

            // Configure Open Tracing with default behavior for .NET
            services.AddOpenTracing(builder =>
            {
                builder.ConfigureAspNetCore(options =>
                {
                    // Exclude noise
                    options.Hosting.IgnorePatterns.Add(x => x.Request.Path == "/health");
                    options.Hosting.IgnorePatterns.Add(x => x.Request.Path == "/metrics");
                });
            });

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
}
