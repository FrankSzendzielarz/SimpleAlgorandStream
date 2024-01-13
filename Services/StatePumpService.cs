using Algorand.Algod.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using SimpleAlgorandStream.Algorand;
using SimpleAlgorandStream.Config;
using SimpleAlgorandStream.Model;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using SimpleAlgorandStream.SignalR;
using Algorand;

namespace SimpleAlgorandStream.Services
{


    public class LoggingHandler : DelegatingHandler
    {
        internal static string lastResponse = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            var response = await base.SendAsync(request, cancellationToken);

            // Log the response details so that we can obtain the JSON
            if (response.Content != null)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                lastResponse = responseContent;

                // Replace the original content with a copy
                response.Content = new StringContent(responseContent);
            }

            return response;
        }
    }

    internal class StatePumpService : BackgroundService
    {
        
        private readonly IOptionsMonitor<AlgodSource> _algodSourceMonitor;
        private readonly IOptionsMonitor<PushTargets> _pushTargetsMonitor;
        private HttpClient _client;
        private AlgorandApi _algorand;
        private IHttpClientFactory _clientFactory;
        private readonly ILogger<StatePumpService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        //INFO: push targets cannot be dynamically changed on configuration change
        private readonly IConnection _rabbitMQConnection;
        private readonly IHubContext<AlgorandHub> _signalRHub;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


        public StatePumpService(IOptionsMonitor<AlgodSource> algodSource,
                                IOptionsMonitor<PushTargets> pushTargets,
                                IHttpClientFactory clientFactory,
                                ILogger<StatePumpService> logger,
                                IHostApplicationLifetime appLifetime,
                                IHubContext<AlgorandHub> hubContext)
        {
            try
            {
                _appLifetime = appLifetime;
                _algodSourceMonitor = algodSource;
                _pushTargetsMonitor = pushTargets;
                _signalRHub = hubContext;
                _algodSourceMonitor.OnChange(async _ =>
                {
                    await setupClient();
                });
                _logger = logger;
                _clientFactory = clientFactory;

                var factory = new ConnectionFactory() { HostName = _pushTargetsMonitor.CurrentValue.RabbitMQ.HostName };
                _rabbitMQConnection = factory.CreateConnection();
                setupClient().Wait();



     
     

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot start pump service. Exiting.");
                _appLifetime.StopApplication();
            }

        }


        

        private async Task setupClient()
        {
            await _semaphore.WaitAsync();
            try
            {
                _client = _clientFactory.CreateClient("StatePumpService");
                _client.Timeout = Timeout.InfiniteTimeSpan;
                _client.BaseAddress = new Uri(_algodSourceMonitor.CurrentValue.ApiUri);

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

                string token = _algodSourceMonitor.CurrentValue.ApiToken;
                if (!String.IsNullOrEmpty(token))
                    _client.DefaultRequestHeaders.Add("X-Algo-API-Token", token);

                _algorand = new AlgorandApi(_client);
            }
            finally
            {
                _semaphore.Release();
            }

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Pump starting.");
            _logger.LogInformation($"Waiting for startup delay of {_algodSourceMonitor.CurrentValue.StartupDelay}");
            await Task.Delay(_algodSourceMonitor.CurrentValue.StartupDelay);
            _logger.LogInformation($"Finished waiting.");

            _logger.LogInformation($"Getting current round.");
            ulong currentRound = 0;
            try
            {
                var status = await _algorand.GetStatusAsync();
                currentRound = status!.LastRound;
                _logger.LogInformation($"Got round as {currentRound}.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot get node status. Exiting.");
                _appLifetime.StopApplication();

            }
            

            while (!stoppingToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync();
                try
                {
                    _logger.LogInformation($"Pumping round {currentRound}");
                    CertifiedBlock? block = null;
                    Model.LedgerStateDelta? delta = null;
                    try
                    {
                        block = await _algorand.GetBlockAsync(currentRound);
                    }
                    catch (ApiException<ErrorResponse> ex)
                    {
                        _logger.LogWarning(ex, $"Block not available for round {currentRound}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Block failed for round {currentRound}");
                        _logger.LogError($"Last http response: {LoggingHandler.lastResponse}" );

                    }
                    try
                    {
                        delta = await _algorand.GetLedgerStateDeltaAsync(currentRound, Format.Json);
                    }
                    catch (ApiException<ErrorResponse> ex)
                    {
                        _logger.LogWarning(ex, $"Delta not available for round {currentRound}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Delta failed for round {currentRound}");
                        _logger.LogError($"Last http response: {LoggingHandler.lastResponse}");
                    }

                    await pumpToTargets(block, delta);

                    currentRound++;
                    _logger.LogInformation($"Waiting for round {currentRound}");
                    ulong nodeRound = 0;
                    do
                    {
                        var status = await _algorand.WaitForBlockAsync(currentRound);
                        nodeRound=status.LastRound;
                    }
                    while(nodeRound < currentRound);

                }
                finally
                {
                    _semaphore.Release();
                }
                
            }
            _logger.LogInformation("Pump ending.");
        }

        private async Task pumpToTargets(CertifiedBlock block, Model.LedgerStateDelta delta)
        {
            byte[] body= new byte[] { };
            string json = "";
            try 
            { 
                StatePushMessage message = new StatePushMessage()
                {
                    Block = block,
                    StateDelta = delta
                };
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new IgnoreShouldSerializeContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                };
                json = JsonConvert.SerializeObject(message, settings);
                body = Encoding.UTF8.GetBytes(json);
                
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Cannot serialize message. Message lost .");
                return;
            }

            try
            {
                // RabbitMQ (AMQP)
                if (_pushTargetsMonitor.CurrentValue.RabbitMQ.Enabled)
                {
                    _logger.LogInformation($"Publishing message to RabbitMQ.");
                    using (var channel = _rabbitMQConnection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: _pushTargetsMonitor.CurrentValue.RabbitMQ.ExchangeName, type: ExchangeType.Fanout);


                        channel.BasicPublish(exchange: _pushTargetsMonitor.CurrentValue.RabbitMQ.ExchangeName,
                                             routingKey: "",
                                             basicProperties: null,
                                             body: body);
                    }
                }
            }catch (Exception ex)
            {
                _logger.LogError(ex, $"Cannot publish message to RabbitMQ. Raw message: {json} ");
                return;
            }

            try
            {
                // SignalR
                if (_pushTargetsMonitor.CurrentValue.SignalR.Enabled)
                {
                    _logger.LogInformation($"Publishing message to SignalR hub.");
                    await AlgorandHub.BroadcastMessage(_signalRHub, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Cannot publish message to SignalR. Raw message: {json} ");
                return;
            }



        }
    }
}
