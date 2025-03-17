## Direct reply-to
In request-response pattern in RabbitMQ, the response is published to the queue that is defined as exclusive(that is the queue that dies upon client disconnection). The server upon receiving the message, reads the `ReplyTo`
property's value and publishes the response there. This approach is generally inefficient, as the creation of a queue for each request-response pair can be expensive. For this purpose RabbitMQ defines an internal queue called
`amq.rabbitmq.reply-to`; by doing so the client is no longer needed to define the reply queue all the time. 

## Workflow
![image](https://github.com/user-attachments/assets/ad3716dc-b791-44f4-9688-a3b790c6f58e)

1) Initially, the client publishes the message to some queue, sets the `CorrelationId` and `ReplyTo` properties.
2) The server then consumes the message from the queue and reads the `ReplyTo` and `CorrelationId`(let's call it c_Id1) property's value. Pubishes the new message to the `ReplyTo` queue(amq.rabbitmq.reply-to), setting the
correlation id to the `c_Id1`.
3) The client consumes the message that came to `amq.rabbitmq.reply-to`, and checks the `CorrelationId`. This correlation id is needed to identify the message that came from specific request. As we have one queue to send responses,
this correlation id is essential; if it matches the specific request's correlation id's value we can say that the response was received.

## Links
* The information about `direct reply-to` can be found [here](https://www.rabbitmq.com/docs/direct-reply-to)
* The information about correlation id can be found [here](https://www.rabbitmq.com/tutorials/tutorial-six-dotnet#correlation-id)
