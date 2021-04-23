using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EventBusRabbitMQ
{
    public class RabbitMQConnection : IRabbitMQConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private bool _disposed;
        public bool IsConnected 
        { 
            get 
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            } 
        }


        public RabbitMQConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            if (!IsConnected)
            {
                TryConnect();
            }
        }
        public bool TryConnect()
        {
            try
            {
                _connection = _connectionFactory.CreateConnection();
            }
            catch (BrokerUnreachableException)
            {
                Thread.Sleep(2000);
                _connection = _connectionFactory.CreateConnection();
                throw;
            }

            if (IsConnected)
            {
                return true;
            }

            return false;
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No rabbit Connection");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _disposed = true;
                _connection.Dispose();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void Enqueue<T>(string queueName, T message)
        {

            using (var channel = CreateModel())
            {

                // durable: the queue will survive a broker restart
                // exclusive: used by only one connection and the queue will be deleted when that connection closes
                // autoDelete: queue that has had at least one consumer is deleted when last consumer unsubscribes
                // arguments: optional; used by plugins and broker-specific features such as message TTL, queue length limit, etc
                channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false,  arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false); // don't dispatch a new message to a worker until it has processed and acknowledged the previous one

                var payload = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(payload);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true; // keep message durable if rabbit crash. More friendly than 1 or 2
                properties.DeliveryMode = 2; // same as persistent

                channel.ConfirmSelect();


                var outstandingConfirms = new ConcurrentDictionary<ulong, byte[]>();

                void cleanOutstandingConfirms(ulong sequenceNumber, bool multiple)
                {
                    if (multiple)
                    {
                        var confirmed = outstandingConfirms.Where(k => k.Key <= sequenceNumber);
                        foreach (var entry in confirmed)
                        {
                            outstandingConfirms.TryRemove(entry.Key, out _);
                        }
                    }
                    else
                    {
                        outstandingConfirms.TryRemove(sequenceNumber, out _);
                    }
                }
                
                channel.BasicAcks += (sender, ea) => cleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
                channel.BasicNacks += (sender, ea) =>
                {
                    outstandingConfirms.TryGetValue(ea.DeliveryTag, out byte[] body);
                    Console.WriteLine($"Message with body {body} has been nack-ed. Sequence number: {ea.DeliveryTag}, multiple: {ea.Multiple}");
                    cleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
                };


                outstandingConfirms.TryAdd(channel.NextPublishSeqNo, body);
                channel.BasicPublish(exchange: "", routingKey: queueName, mandatory: true, basicProperties: properties, body: body);
                channel.WaitForConfirmsOrDie();



            }
        }
    }
}
