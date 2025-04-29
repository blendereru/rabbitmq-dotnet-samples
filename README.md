# RabbitMQ
`RabbitMQ` is a message broker that enables applications to communicate by sending and receiving messages through queues.
This project encapsulates my exploration and understanding of RabbitMQ, a robust open-source message
broker that facilitates asynchronous communication between distributed systems. Through this summary,
I aim to distill the core concepts, components, and practical applications of `RabbitMQ` as presented in its
[official](https://www.rabbitmq.com/docs) documentation.

[![rabbitmq](https://img.shields.io/badge/rabbitmq-4.0-orange?style=flat&logo=rabbitmq&link=https://www.rabbitmq.com/docs/4.0)](https://www.rabbitmq.com/docs/4.0)
## Table of Contents
* [Direct reply-to](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Direct%20reply-to)
* [Local random exchange](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Local%20random%20exchange)
* [Alternate exchanges](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Alternative%20Exchanges)
* [Quorum queues](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Quorum%20Queues)
* [Dead lettering](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Dead%20Lettering)
* [Streams](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Streams)
    * [Single Active Consumer](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Streams/Single%20Active%20Consumer)
    * [Super Streams](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Streams/Super%20Streams)
        * [Single Active Consumer](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Streams/Super%20Streams/Single%20Active%20Consumer)
* [Consumer Acknowledgements and Publisher Confirms](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Consumer%20Acks%20and%20Publisher%20Confirms)
* [Plugins](https://www.rabbitmq.com/docs/plugins)
    * [Federation plugin](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Plugins/Federation)
    * [Shovel plugin](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Plugins/Shovel)
    * [STOMP plugin](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Plugins/STOMP)
## References
Each project in the repository is split into the `Publisher`(somewhere `Producer`) and `Consumer` parts.
`Publisher` is the client that publishes the messages to the broker, and `Consumer` handles(consumes) them.
## License
This project is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)

The `Apache License 2.0` is a permissive open-source license that allows you to freely use, modify, and distribute the software, including for commercial purposes. It also provides an express grant of patent rights from contributors to users.