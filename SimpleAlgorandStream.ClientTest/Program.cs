using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace SimpleAlgorandStream.ClientTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {

                // The server offers a predefined exchange with unfiltered messages
                var unfilteredQueueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: unfilteredQueueName,
                                  exchange: "AlgorandFeed",
                                  routingKey: "");


                // Or we can declare an exchange with a filter in the following way
                channel.ExchangeDelete("AlgorandTest1"); // (only for demo purposes)
                Dictionary<string, object> preFilter = new Dictionary<string, object>()
                {
                    { "prefilter","Block.block.txns[*].txn.snd | [?@] | contains(@, 'Gs6HXQ0r1GuOPxGDjLTu9PhxwLmrDToCmXhzHQisUOU=')" }
                };
                channel.ExchangeDeclare(exchange: "AlgorandTest1", type: ExchangeType.Fanout, durable: true, autoDelete: false, preFilter);
                var filteredQueueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: filteredQueueName,
                                  exchange: "AlgorandTest1",
                                  routingKey: "");
                
                
                Console.WriteLine("Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = System.Text.Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Exchange {ea.Exchange} delivered message: {message}");
                };
                channel.BasicConsume(queue: unfilteredQueueName,
                                     autoAck: true,
                                     consumer: consumer);
                channel.BasicConsume(queue: filteredQueueName,
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Send transactions to node to see updates here. If using Sandbox the next block may take a minute to appear.");
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}