using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JP.Demo.Chassis.AccountService
{
    class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    var env = ctx.HostingEnvironment;
                    builder.SetBasePath(Directory.GetCurrentDirectory());
                    builder.AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: true,
                            reloadOnChange: true);
                    builder.AddEnvironmentVariables(prefix: "APPSETTING_ASPNETCORE_");
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<VideosWatcher>();
                });

        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();


            return;
            var ended = new ManualResetEventSlim();
            var starting = new ManualResetEventSlim();

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                System.Console.WriteLine("Unloding fired");
                starting.Set();
                System.Console.WriteLine("Waiting for completion");
                ended.Wait();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Console.WriteLine("ProcessExit fired");
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Console.WriteLine("UnhandledException fired");
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("CancelKeyPress fired");
            };

            System.Console.WriteLine("Waiting for signals");

            var t = new Thread(WillCrashAfterABit);
            t.Start();

            starting.Wait();

            System.Console.WriteLine("Received signal gracefully shutting down");
            Thread.Sleep(5000);
            ended.Set();
        }

        private static void WillCrashAfterABit()
        {
            Thread.Sleep(2000);
            throw new ApplicationException("Demo exception");
        }
    }

    public class VideosWatcher : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Starting watcher service");
            await Task.Delay(5 * 1000, stoppingToken);
            Console.WriteLine("Stopping watcher service");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Graceful stop, host shutting down");
            return base.StopAsync(cancellationToken);
        }
    }
}
