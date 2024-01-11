using Algorand.Algod;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Extensions.Http;
using Polly;
using SimpleAlgorandStream.Config;
using Algorand.Algod.Model;
using Algorand;

namespace SimpleAlgorandStream.Services
{
    internal class StatePumpService : BackgroundService
    {
        private readonly IOptionsMonitor<AlgodSource> _optionsMonitor;
        private HttpClient _client;
        private DefaultApi _algorand;
        private IHttpClientFactory _clientFactory;
        private readonly ILogger<StatePumpService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        


        public StatePumpService(IOptionsMonitor<AlgodSource> optionsMonitor, 
                                IHttpClientFactory clientFactory, 
                                ILogger<StatePumpService> logger,
                                IHostApplicationLifetime appLifetime)
        {
            try
            {
                _appLifetime = appLifetime;
                _optionsMonitor = optionsMonitor;
                _optionsMonitor.OnChange(_ =>
                {
                    setupClient();
                });
                _logger = logger;
                _clientFactory = clientFactory;
                

                setupClient();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot start pump service. Exiting.");
                _appLifetime.StopApplication();
            }

        }


      

        private void setupClient()
        {
            
            _client = _clientFactory.CreateClient("StatePumpService");
            _client.BaseAddress = new Uri(_optionsMonitor.CurrentValue.ApiUri);

            if (!_client.BaseAddress.IsAbsoluteUri)
            {
                throw new Exception("Host must be an absolute path.");
            }
            else
            {
                // there has to be a slash at the end of the base address
                if (!_client.BaseAddress.AbsolutePath.EndsWith("/"))
                {
                    UriBuilder uriBuilder = new UriBuilder(_client.BaseAddress);
                    uriBuilder.Path = _client.BaseAddress.AbsolutePath + "/";
                    _client.BaseAddress = uriBuilder.Uri;
                }
            }

            string token = _optionsMonitor.CurrentValue.ApiToken;
            if (!String.IsNullOrEmpty(token))
                _client.DefaultRequestHeaders.Add("X-Algo-API-Token", token);

            _algorand = new DefaultApi(_client);

           
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ulong currentRound = 0;
            try
            {
                var status = await _algorand.GetStatusAsync();
                currentRound = status!.LastRound;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot get node status. Exiting.");
                _appLifetime.StopApplication();

            }

            //test code:
            currentRound -= 10;
            //end test code

            while (!stoppingToken.IsCancellationRequested)
            {

                var block=await _algorand.GetBlockAsync(currentRound);
                try
                {
                    var delta = await _algorand.GetLedgerStateDeltaAsync(currentRound);
                }
                catch (ApiException<ErrorResponse> ex)
                {
                    
                }
                catch (Exception ex2)
                {

                }

                currentRound++;
                Thread.Sleep(1000);
            }
        }
    }
}
