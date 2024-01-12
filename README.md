# SimpleAlgorandStream
*A component that monitors an Algorand node for transactions and state changes and sends them as messages to multiple push targets.*

## Alpha

This is an initial release with minimal testing and comes with no guarantees. It is intended at this point in time to garner feature request feedback and identify bugs.

## What is it?

This is a .NET Core executable that can run alongside your Algorand node to monitor for changes you are interested in. It pushes those changes, blocks and state deltas, as well formed JSON messages to different types of push target client.

It supports front end clients and server-to-server communication, and allows for reliable messaging using message queues.

## What is it for?

It supports a push model, where instead of using Indexer or a database, your dApp or app can receive messages about only those changes you are interested in. For example, 
if your game or dApp connects to a node and allows a player to make in app purchases by payments to an address, you can filter for transactions
to that specific address. Your game or dApp can then make updates in real time, without having to query a data store or maintain an Indexer component.

With the message queue target, you can also use this component to reliably push messages to a queue, and then have a worker process consume those messages and update a database 
for post-hoc and ad-hoc queries later on. Your datastore would be much smaller as it would only contain the data you are interested in.

## How does it work?

** To see examples of how to use this component, see the SimpleAlgorandStream.ClientTest folder. **

The component connects to an Algorand node and long polls the Algod API for ``blocks`` and ``deltas``, round by round.
As the stream of information is received, it pushes them to two types of configured push targets:
-RabbitMQ 
-SignalR 

#### SignalR

This technology provides a ``hub``, to which clients can connect and receive messages using a client library. The client library is available for many platforms, including JavaScript, .NET, Java, Python, etc.
It is primarily aimed at front end clients, such as web apps, mobile apps. 

Behind the scenes it attempts to use various protocols to maintain a low-latency connection with the hub. SignalR clients first attempt WebSockets, 
falling back to Server Sent Events, and finally to long polling.

In theory it can handle thousands of concurrent connections per CPU core.

For example, your dApp single page app could be connected to a node, implementing a game where people can make in app purchases. Should a purchase or swap happen, then all
connected dApp pages will be updated in real time with the new state. Clients can discard messages they are not interested in. This means dApps can operate without the need
for an Indexer or large database that has to be queried.

#### RabbitMQ

This is AMQP 0.9.1, a messaging protocol. 

Here an 'exchange' is implemented. Block and State Delta messages are pushed to the exchange. Clients can connect to the exchange and bind a 'queue'.
This means that message delivery is 'reliable' and 'durable'. If a client is not operational, the message will be queued until the client connects and consumes the message.

With this model, the client is expected to be a server, and responsible for filtering messages it does not need.



