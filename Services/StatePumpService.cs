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

namespace SimpleAlgorandStream.Services
{
    internal class StatePumpService : BackgroundService
    {
        private readonly IOptionsMonitor<AlgodSource> _algodSourceMonitor;
        private readonly IOptionsMonitor<PushTargets> _pushTargetsMonitor;
        private HttpClient _client;
        private AlgorandApi _algorand;
        private IHttpClientFactory _clientFactory;
        private readonly ILogger<StatePumpService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private IConnection _rabbitMQConnection;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public StatePumpService(IOptionsMonitor<AlgodSource> algodSource,
                                IOptionsMonitor<PushTargets> pushTargets,
                                IHttpClientFactory clientFactory,
                                ILogger<StatePumpService> logger,
                                IHostApplicationLifetime appLifetime)
        {
            try
            {
                _appLifetime = appLifetime;
                _algodSourceMonitor = algodSource;
                _pushTargetsMonitor = pushTargets;

                _algodSourceMonitor.OnChange(async _ =>
                {
                    await setupClient();
                });
                _logger = logger;
                _clientFactory = clientFactory;


                setupClient().Wait();
                setupPushTargets().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot start pump service. Exiting.");
                _appLifetime.StopApplication();
            }

        }


        private async Task setupPushTargets()
        {
            //RabbitMQ (AMQP)
            var factory = new ConnectionFactory() { HostName = _pushTargetsMonitor.CurrentValue.RabbitMQ.HostName };
            _rabbitMQConnection = factory.CreateConnection();

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

            while (!stoppingToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync();
                try
                {
                    var block = await _algorand.GetBlockAsync(currentRound);
                    var delta = await _algorand.GetLedgerStateDeltaAsync(currentRound, Format.Json);

                    await pumpToTargets(block, delta);

                    currentRound++;
                }
                finally
                {
                    _semaphore.Release();
                }
                Thread.Sleep(1000);
            }
        }

        private async Task pumpToTargets(CertifiedBlock block, Model.LedgerStateDelta delta)
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
            string json = JsonConvert.SerializeObject(message, settings);
            var body = Encoding.UTF8.GetBytes(json);

            // RabbitMQ (AMQP)
            if (_pushTargetsMonitor.CurrentValue.RabbitMQ.Enabled)
            {
                using (var channel = _rabbitMQConnection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: _pushTargetsMonitor.CurrentValue.RabbitMQ.ExchangeName, type: ExchangeType.Fanout);


                    channel.BasicPublish(exchange: _pushTargetsMonitor.CurrentValue.RabbitMQ.ExchangeName,
                                         routingKey: "",
                                         basicProperties: null,
                                         body: body);
                }
            }

            // SignalR



        }
    }
}
