using Algorand.KMD;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using SimpleAlgorandStream.Config;
using SimpleAlgorandStream.Services;

namespace SimpleAlgorandStream
{
    internal class Program
    {
        private static IConfiguration configuration;
        static async Task Main(string[] args)
        {
            var host=configure(args);

            await host.RunConsoleAsync();
        }

        private static IHostBuilder configure(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AlgodSource>(hostContext.Configuration.GetSection("AlgodSource"));
                    services.AddHostedService<StatePumpService>();
                    services.AddHttpClient<StatePumpService>()
                            .AddPolicyHandler((x) =>
                            {

                                //using a dynamic policy to allow for configuration changes
                                var algodSourceConfig = hostContext.Configuration.GetSection("AlgodSource").Get<AlgodSource>();
                                if (algodSourceConfig == null) throw new Exception("Cannot start service without AlgodSource configuration");
                                return configureSourcePolicy(algodSourceConfig);
                            });

                    
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddEventSourceLogger();
                    logging.AddDebug();
                    logging.AddApplicationInsights();
                });

        private static IAsyncPolicy<HttpResponseMessage> configureSourcePolicy(AlgodSource? algodSourceConfig)
        {

            IAsyncPolicy<HttpResponseMessage> sourcePolicy = null;
            var retryPolicyBuilder = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound);


            if (algodSourceConfig!.ExponentialBackoff)
            {
                sourcePolicy = retryPolicyBuilder.WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            }
            else
            {
                sourcePolicy = retryPolicyBuilder.WaitAndRetryForeverAsync(retryAttempt => algodSourceConfig.RetryFrequency);
            }
            return sourcePolicy;


        }
    }
}