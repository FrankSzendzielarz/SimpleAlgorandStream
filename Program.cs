﻿using Algorand.KMD;
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
using System;

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
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddEventSourceLogger();
                    logging.AddDebug();
                    logging.AddApplicationInsights();
                })
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
                    services.Configure<PushTargets>(hostContext.Configuration.GetSection("PushTargets"));
                    services.AddHostedService<StatePumpService>();
                    services.AddHttpClient<StatePumpService>()
                            .AddPolicyHandler((serviceProvider,x) =>
                            {
                                ILogger<StatePumpService> logger = serviceProvider.GetRequiredService<ILogger<StatePumpService>>();
                                //using a dynamic policy to allow for configuration changes
                                var algodSourceConfig = hostContext.Configuration.GetSection("AlgodSource").Get<AlgodSource>();
                                if (algodSourceConfig == null) throw new Exception("Cannot start service without AlgodSource configuration");
                                return configureSourcePolicy(algodSourceConfig,logger);
                            });


                });
                

        private static IAsyncPolicy<HttpResponseMessage> configureSourcePolicy(AlgodSource? algodSourceConfig, ILogger<StatePumpService> logger)
        {
            IAsyncPolicy<HttpResponseMessage> sourcePolicy = null;
            var retryPolicyBuilder = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode);
              
            if (algodSourceConfig!.ExponentialBackoff)
            {
                sourcePolicy = retryPolicyBuilder.WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (outcome, retryAttempt, timespan) =>
                {
                    logger.LogWarning($"Retry {retryAttempt} to {(outcome.Result?.RequestMessage?.RequestUri?.ToString())??"Unknown destination"} due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                    
                });
            }
            else
            {
                sourcePolicy = retryPolicyBuilder.WaitAndRetryForeverAsync(retryAttempt => algodSourceConfig.RetryFrequency, (outcome, retryAttempt, timespan) =>
                {
                    logger.LogWarning($"Retry {retryAttempt} to {(outcome.Result?.RequestMessage?.RequestUri?.ToString()) ?? "Unknown destination"} due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");

                });
            }
            return sourcePolicy;


        }
    }
}