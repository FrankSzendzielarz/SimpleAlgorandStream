using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleAlgorandStream.Config
{
    internal class PushTargets
    {
        public SignalR SignalR { get; set; }
        public RabbitMQ RabbitMQ { get; set; }
    }

    internal class SignalR
    {
        public string HubName { get; set; }
        public int Port { get; set; }
        public bool Enabled { get; set; }
    }

    internal class RabbitMQ
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string ExchangeName { get; set; }
        public bool Enabled { get; set; }
    }

}
