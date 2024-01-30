
using DevLab.JmesPath;
using Microsoft.Extensions.Options;
using SimpleAlgorandStream.Config;
using SimpleAlgorandStream.Model;
using System.Text;

namespace SimpleAlgorandStream.Services
{
    internal class PrefilterExchangeFinderService : BackgroundService
    {
        IOptionsMonitor<PushTargets> _pushTargets;
        IHttpClientFactory _clientFactory;
        ILogger<PrefilterExchangeFinderService> _logger;
        private HttpClient _client;
        private bool clientReady = false;

        public PrefilterExchangeFinderService(IOptionsMonitor<PushTargets> pushTargets,
                                IHttpClientFactory clientFactory,
                                ILogger<PrefilterExchangeFinderService> logger)
        {
            _pushTargets = pushTargets;
            _clientFactory = clientFactory;
            _logger = logger;
            _pushTargets.OnChange((change) =>
            {
                setupClient();
            });
            setupClient();

        }

        private void setupClient()
        {
            
            if (_pushTargets.CurrentValue.RabbitMQ.Enabled)
            {
                _logger.LogInformation("Setting up RMQ management API client.");
                _client = _clientFactory.CreateClient("PrefilterExchangeFinderService");
                _client.Timeout = Timeout.InfiniteTimeSpan;
                UriBuilder uri = new UriBuilder("http", _pushTargets.CurrentValue.RabbitMQ.HostName, _pushTargets.CurrentValue.RabbitMQ.RMQAPIPort);
                _client.BaseAddress = uri.Uri;
                var byteArray = Encoding.ASCII.GetBytes($"{_pushTargets.CurrentValue.RabbitMQ.RMQAPIUserName}:{_pushTargets.CurrentValue.RabbitMQ.RMQAPIPassword}"); // replace with your username and password
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                
                clientReady = true;
                _logger.LogInformation("RMQ management API client ready.");
            }
            else
            {
                clientReady= false;
            }

            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                _logger.LogInformation("Looking for RMQ prefiltered exchanges.");
                if (_pushTargets.CurrentValue.RabbitMQ.Enabled && clientReady)
                {
                    _logger.LogInformation("RMQ feature enabled and client ready.");
                    try
                    {
                        _logger.LogInformation("Getting exchanges from RMQ management API.");
                        List<Exchange>? exchanges = await _client.GetFromJsonAsync<List<Exchange>>("api/exchanges");
                        
                        if (exchanges != null)
                        {
                            exchanges = exchanges.Where(x => x.Arguments.ContainsKey("prefilter")).ToList();
                            _logger.LogInformation($"Processing {exchanges.Count} exchanges.");
                            foreach (var exchange in exchanges)
                            {
                                try
                                {
                                    var jmes = new JmesPath();
                                    var jmesExpression = jmes.Parse(exchange.Arguments["prefilter"]);
                                    if (jmesExpression == null)
                                    {
                                        _logger.LogWarning($"Exchange {exchange.Name} had a bad JMESPath filter expression.");
                                    }
                                    else
                                    {
                                        StatePumpService.KnownPrefilterExchanges.AddOrUpdate(exchange.Name, jmesExpression, (key, oldValue) => jmesExpression);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Exchange {exchange.Name} had a bad JMESPath filter expression.");
                                }
                            }
                            _logger.LogInformation($"Checking for expired exchanges.");
                            
                            StatePumpService.KnownPrefilterExchanges.Keys
                                .Where(x => !exchanges.Any(y => y.Name == x))
                                .ToList()
                                .ForEach(x => StatePumpService.KnownPrefilterExchanges.TryRemove(x, out _));


                            _logger.LogInformation($"Done processing exchanges.");
                        }
                        else
                        {
                            _logger.LogInformation("No exchanges to process.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed getting exchanges from RabbitMQ management API. Please check the configuration.");
                    }

                    _logger.LogInformation($"Waiting to check again for {_pushTargets.CurrentValue.RabbitMQ.PrefilterExchangeDiscoveryFrequency}.");
                    var delay = _pushTargets.CurrentValue.RabbitMQ.PrefilterExchangeDiscoveryFrequency;
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    _logger.LogInformation("RMQ feature not enabled or client not yet ready. Checking again in 1 second.");
                    await Task.Delay(1000);
                }

            } while (!stoppingToken.IsCancellationRequested);
        }
    }
}
