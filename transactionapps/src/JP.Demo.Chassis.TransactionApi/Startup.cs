using JP.Demo.Chassis.SharedCode.Kafka;
using JP.Demo.Chassis.SharedCode.Schemas;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            services.Configure<KafkaConfig>(Configuration.GetSection("KafkaConfig"));

            services.AddJaegerTracing(options => {
                options.JaegerAgentHost = Configuration["JAEGER_AGENT_HOST"];
                options.ServiceName = Configuration["GLOBAL_NAME"];
            });

            services.AddHostedService<KafkaReplyWorker>();

            services.AddSingleton<KafkaSender<TransactionRequest>>();
            services.AddSingleton<KafkaRequestReplyGroup>();
            services.AddSingleton<RequestReplyImplementation>();
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
}
