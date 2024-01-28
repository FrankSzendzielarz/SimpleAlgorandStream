# SimpleAlgorandStream
*A component that monitors an Algorand node for transactions and state changes and sends them as messages to multiple push targets.*

## Quick guide
- See the release binaries here and install somewhere with connectivity to your node  [Binaries.](https://github.com/FrankSzendzielarz/SimpleAlgorandStream/releases)
- Disable RabbitMQ in the appsettings.json OR use the RabbitMQ [quickstart here] (https://rabbitmq.com/download.html)
- Check the configuration (see Configuration and Logging below)
- Run the executable alongside your node and connect using either a RabbitMQ or JS client - see the ClientTest example for an example of an HTML and RabbitMQ client.

## Alpha

This is an initial release with minimal testing and comes with no guarantees. It is intended at this point in time to garner feature request feedback and identify bugs.

If you think this could be useful, or evolve into something useful, please add your feature requests and ideas to the github repo.

So far this has been tested on a limited set of data. Logger will output any block or state delta JSON that failed to serialise/deserialise. Please log an issue if you find an incompatible model.

## What is it?

This is a .NET Core executable that can run alongside your Algorand node to monitor for changes you are interested in. It pushes those changes, blocks and state deltas, as well formed JSON messages to different types of push target client.

It supports front end clients and server-to-server communication, and allows for reliable messaging using message queues.

Mobile and web front-ends can easily subscribe to filtered Algorand events using WebSockets, Server Sent Events, or Long Polling.
Servers can subscribe to Algorand events using AMQP 0.9.1 and/or RabbitMQ.

If you want State Deltas, your node should be configured to maintain them. Otherwise only blocks and transactions are produced. 
The State Deltas are produced with the following additional config settings in the node config.json:

```json
    "EnableDeveloperAPI": true, 
    "EnableTxnEvalTracer": true,
```

## What is it for?

It supports a push model, where instead of *necessarily* using Indexer or a database, your dApp or app can receive messages about only those changes you are interested in. For example, 
if your game or dApp connects to a node and allows a player to make in app purchases by payments to an address, you can filter for transactions
to that specific address. Your game or dApp can then make updates in real time, without having to query a data store or maintain an Indexer component.

With the message queue target, you can also use this component to reliably push messages to a queue, and then have a worker process consume those messages and update a database 
for post-hoc and ad-hoc queries later on. Your datastore would be compact as it would only contain the data you are interested in.

## How does it work?

**To see examples of how to use this component, see the SimpleAlgorandStream.ClientTest folder.**

The component connects to an Algorand node and long polls the Algod API for ``blocks`` and ``deltas``, round by round.
As the stream of information is received, it pushes them to two types of configured push targets:
- RabbitMQ 
- SignalR 

#### RabbitMQ

This is AMQP 0.9.1, a messaging protocol. 

Here an 'exchange' is implemented. Block and State Delta messages are pushed to the exchange. Clients can connect to the exchange and bind a 'queue'.
This means that message delivery is **reliable** and **durable**. If a client is not operational, the message will be queued until the client connects and consumes the message.

With this model, the client is expected to be a server, and responsible for filtering messages it does not need.

This can be a useful addition to various solution architectures. For example, you could have a local reactive stream that pre-filters messages, discarding the majority
of MQ block transactions you don't need, or transform the messages into a different format, before, say, pushing the data to your own web or mobile clients or
to cloud stream analytics services or databases.


#### SignalR

This technology provides a ``hub``, to which clients can connect and receive messages using a client library. The client library is available for many platforms, including JavaScript, .NET, Java, Python, etc.
It is primarily aimed at front end clients, such as web apps, mobile apps. 

Behind the scenes it attempts to use various protocols to maintain a low-latency connection with the hub. SignalR clients first attempt WebSockets, 
falling back to Server Sent Events, and finally to long polling.

In theory it can handle thousands of concurrent connections per CPU core.

For example, your dApp single page app could be connected to a node, implementing a game where people can make in app purchases. Should a purchase or swap happen, then all
connected dApp pages will be updated in real time with the new state. Clients can discard messages they are not interested in. This means dApps can operate without the need
for an Indexer or large database that has to be queried.

#### Filtering

Unfiltered the SignalR hub would stream all state changes on the network to potentially thousands of clients. This would cause high costs for data egress.
For this reason we introduce a facility for clients to set their own message filters using **JMESPath** JSON query language.

For example, this code from the sample client in Javascript shows how to filter for blocks with transactions coming from a specific sender:

```javascript
var filter = "Block.block.txns[*].txn.snd | contains(@, 'aJaVAfWZoKsh6bc7KO1nfiamjthhF844i3txYJTkmVw=')";
connection.invoke("SetFilter", filter).then(function (result) {
    if (result) {
        console.log("Filter applied.");
    } else {
        console.log("Filter application failed.");
    }
}).catch(function (err) {
    return console.error(err.toString());
});
```

Server apps using queues are expected to filter messages themselves.


## Deployment

For now, the component is built as self-contained platform specific binaries. If you have .NET 7 runtime installed, you can build this solution to a ``portable`` binary, or submit a feature request to have these produced automatically too.

Binaries are for Windows, OSX and Linux - architectures ARM64 and x64.

Binaries are available here [Binaries.](https://github.com/FrankSzendzielarz/SimpleAlgorandStream/releases)

## Configuration and Logging

NB: The AlgodSource section can be updated in realtime.

```json
{
  //Algorand node source configuration section.

  "AlgodSource": {
    "ApiUri": "http://localhost:4001/",
    "ApiToken": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
    "ExponentialBackoff": false,  // If true, will retry with increasing delay if the node is not available.
    "RetryFrequency": "00:00:05", // Retry frequency for lost connection when backoff is disabled.
    "StartupDelay": "00:00:05"    // A startup delay after component start up before messages are acquired and pumped to allow for development clients to launch without missing initial messages.
  },

  // Confiuration section for currently supported push targets

  "PushTargets": {
    //Do not use the algorand internal field names and instead use friendly names, eg: GenesisHash instead of gh.
     "UseFriendlyNames": false,

    //SignalR hub configuration. See the HTML client demonstration for usage.
    "SignalR": {
      "HubName": "AlgorandFeedHub",
      "Port": 5000,
      "Enabled": true  // Set to false to disable this target.
    },

    //RabbitMQ server exchange configuration
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "ExchangeName": "AlgorandFeed",
      "Enabled":  true // Set to false to disable this target.
    }
  },

  // Microsoft.Extension.Logging configuration section. See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-7.0
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Console": {
      "LogLevel": {
        "Default": "Information"
      }
    },
    "Debug": {
      "LogLevel": {
        "Default": "None"
      }
    },
    "EventSource": {
      "LogLevel": {
        "Default": "None"
      }
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "None"
      }
    }

  }

}
```



