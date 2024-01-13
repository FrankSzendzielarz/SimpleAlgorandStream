using DevLab.JmesPath;
using DevLab.JmesPath.Expressions;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Xml;

namespace SimpleAlgorandStream.SignalR
{
    internal class AlgorandHub : Hub
    {
        // This dictionary maps connection ids to filters
        internal static ConcurrentDictionary<string, JmesPathExpression> _filters = new ConcurrentDictionary<string, JmesPathExpression>();

        

        public override Task OnConnectedAsync()
        {
            _filters.TryAdd(Context.ConnectionId, null);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            // When a client disconnects, remove their filter from the dictionary
            _filters.TryRemove(Context.ConnectionId, out _);

            return base.OnDisconnectedAsync(exception);
        }

        
        
        /// <summary>
        /// Clients can call this to set a filter for json objects
        /// </summary>
        public async Task<bool> SetFilter(string filter)
        {
            try
            {
                var jmes = new JmesPath();
                var jmesExpression = jmes.Parse(filter);
                _filters[Context.ConnectionId] = jmesExpression;
                return true;
            }
            catch
            {
                return false;     
            }
           

        }

        public static async Task BroadcastMessage(IHubContext<AlgorandHub> hub, string message)
        {
            // When broadcasting a message, only send it to clients whose filters match the message
            foreach (var connection in _filters)
            {
                try
                {
                    var pathExpression = connection.Value;
                    if (pathExpression != null)
                    {

                        var token = JToken.Parse(message);
                        var result = pathExpression.Transform(token);
                        if (result.Token.ToString().ToLower() != "true")
                        {
                            continue;
                        }
                    }
                    await hub.Clients.Client(connection.Key).SendAsync("ReceiveAlgorandState", message);
                }
                catch
                {
                }

            }
        }
    }

}
