using Algorand.Algod;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleAlgorandStream.Config;

namespace SimpleAlgorandStream.Services
{
    internal class StatePumpService : BackgroundService
    {
        private readonly IOptionsMonitor<AlgodSource> _optionsMonitor;
        private HttpClient _client;
        private DefaultApi _algorand;
        private readonly ILogger<StatePumpService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public StatePumpService(IOptionsMonitor<AlgodSource> optionsMonitor, IHttpClientFactory clientFactory, ILogger<StatePumpService> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
            _httpClientFactory = clientFactory;
            setupClient();

        }

        private void setupClient()
        {
            _client = _httpClientFactory.CreateClient();

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
            while (!stoppingToken.IsCancellationRequested)
            {
                var result=await _algorand.GetStatusAsync();
                Thread.Sleep(5000);
                
            }
        }
    }
}
