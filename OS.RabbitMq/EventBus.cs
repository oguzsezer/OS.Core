using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace OS.RabbitMq
{
    /// <summary>
    /// RabbitMq eventbus implementation with auto-retry mechanism
    /// </summary>
    internal sealed class EventBus : IEventBus
    {
        private readonly ILogger<EventBus> _logger;
        private readonly IPersistentConnection _persistentConnection;
        private readonly IMediator _mediator;
        private IModel _consumerChannel;
        private readonly string _queueName;
        private readonly string _exchangeName;
        private readonly string _deadLetterExchangeName;
        private readonly string _retryExchangeName;
        private readonly string _retryQueueName;
        private readonly Settings _eventBusConfig;
        private const string RETRY_POSTFIX = ".retry";
        private const string RETRY_COUNT_HEADER = "x-retry-count";
        private static volatile bool _isConsumerStarted;
        private static IEnumerable<Type>? _assemblyTypes;

        public EventBus(ILogger<EventBus> logger, IOptions<Settings> eventBusConfigOptions,
            IPersistentConnection rabbitMqPersistentConnection, IMediator mediator)
        {
            _logger = logger;
            _persistentConnection = rabbitMqPersistentConnection;
            _mediator = mediator;
            _eventBusConfig = eventBusConfigOptions.Value;
            _queueName = _eventBusConfig.QueueName;
            _exchangeName = _eventBusConfig.ExchangeName;
            _deadLetterExchangeName = $"{_exchangeName}.dlx";
            _retryExchangeName = $"{_exchangeName}{RETRY_POSTFIX}";
            _retryQueueName = $"{_queueName}{RETRY_POSTFIX}";
        }

        public void SubscribeAndStartConsuming()
        {
            if (_isConsumerStarted) return;
            _isConsumerStarted = true;
            var consumableType = typeof(IConsumable);
            var consumableEventTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                consumableEventTypes.AddRange(assembly.DefinedTypes
                    .Where(type => consumableType.IsAssignableFrom(type) && consumableType != type).Cast<Type>()
                    .ToList());
            }
            _consumerChannel = CreateConsumerChannel();
            consumableEventTypes.ForEach(x => BindQueue(x.Name));
            StartBasicConsume();

            void BindQueue(string eventName)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using var channel = _persistentConnection.CreateModel();
                channel.QueueBind(_queueName, _exchangeName, eventName);
                channel.QueueBind(_retryQueueName, _retryExchangeName, $"{eventName}{RETRY_POSTFIX}");
                channel.QueueBind(_queueName, _deadLetterExchangeName, $"{eventName}{RETRY_POSTFIX}");
            }
        }

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _logger.LogInformation("Creating RabbitMQ Channel.");

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(_deadLetterExchangeName, ExchangeType.Direct);
            channel.ExchangeDeclare(_retryExchangeName, ExchangeType.Direct);
            channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct,
                arguments: _eventBusConfig.ExchangeTTLSeconds == default
                    ? null
                    : new Dictionary<string, object>
                        { { "x-message-ttl", _eventBusConfig.ExchangeTTLSeconds * 1000 } });

            channel.QueueDeclare(_queueName, true, false, false,
                _eventBusConfig.QueueTTLSeconds == default
                    ? null
                    : new Dictionary<string, object> { { "x-message-ttl", _eventBusConfig.QueueTTLSeconds * 1000 } });
            channel.QueueDeclare(_retryQueueName, true, false, false,
                new Dictionary<string, object> { { "x-dead-letter-exchange", _deadLetterExchangeName } });
            channel.CallbackException += (sender, args) =>
            {
                _logger.LogWarning("Recreating Channel: {0}", args.Exception.Message);
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                _isConsumerStarted = false;
                SubscribeAndStartConsuming();
            };

            return channel;
        }

        private void StartBasicConsume()
        {
            _logger.LogInformation("Start consuming");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer, consumerTag: $"{Environment.MachineName}.{Guid.NewGuid():N}");
            }
            else
            {
                _logger.LogError("Basic Consume Start error.");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs args)
        {
            var eventName = args.RoutingKey;
            var message = Encoding.UTF8.GetString(args.Body.ToArray());

            var shouldAcknowledge = false;
            try
            {
                eventName = eventName.Replace(RETRY_POSTFIX, "");
                await ProcessEvent(eventName, message);
                shouldAcknowledge = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message {message}");
                if (_eventBusConfig.Retry.Enabled)
                {
                    try
                    {
                        var parsedObject = JObject.Parse(message);
                        parsedObject.TryGetValue(nameof(EventBase.Id), StringComparison.OrdinalIgnoreCase, out var idJToken);
                        var eventId = idJToken.Value<string>();

                        PublishToRetryExchange(args.RoutingKey, args.BasicProperties.Headers, args.Body, eventId);
                        shouldAcknowledge = true;
                    }
                    catch (Exception e)
                    {
                        shouldAcknowledge = false;
                        _logger.LogError(e, "Error publishing message to the retry exchange, will NACK the message now.");
                    }
                }
            }
            finally
            {
                if (shouldAcknowledge)
                {
                    _consumerChannel.BasicAck(args.DeliveryTag, false);
                }
                else
                {
                    _consumerChannel.BasicNack(args.DeliveryTag, false, true);
                }
            }
        }

        private void PublishToRetryExchange(string routingKey, IDictionary<string, object> headers,
            ReadOnlyMemory<byte> body, string eventId)
        {
            routingKey = routingKey.Replace(RETRY_POSTFIX, "");
            var retryCount = 0;
            if (headers != null && headers.Count > 0 && headers.ContainsKey(RETRY_COUNT_HEADER))
            {
                var retryCountObject = headers[RETRY_COUNT_HEADER];
                retryCount = int.Parse(retryCountObject.ToString() ?? "0");
            }

            if (retryCount >= _eventBusConfig.Retry.MaxRetry)
            {
                _logger.LogDebug("Max retry count is reached, message will be removed from queue.");
                return;
            }

            ++retryCount;
            var retryDelay = _eventBusConfig.Retry.ExponentialDelayEnabled
                ? _eventBusConfig.Retry.DelayMilliseconds * retryCount
                : _eventBusConfig.Retry.DelayMilliseconds;

            _logger.LogDebug($"Publishing to retry exchange with {retryDelay / 1000} seconds delay");

            var policy = Policy.Handle<BrokerUnreachableException>().Or<SocketException>().WaitAndRetry(
                _eventBusConfig.Retry.Enabled ? _eventBusConfig.Retry.MaxRetry : 0,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time, retryAttempt, _) =>
                {
                    _logger.LogWarning($"Could not publish event: {eventId} to retry exchange after {time.TotalMilliseconds}ms. RetryAttempt:{retryAttempt} ExceptionMessage:{ex.Message}");
                });
            policy.Execute(PublishToExchange);


            void PublishToExchange()
            {
                using var channel = _persistentConnection.CreateModel();
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                properties.Headers ??= new ConcurrentDictionary<string, object>();
                properties.Headers.Add(RETRY_COUNT_HEADER, retryCount);
                properties.Expiration = retryDelay.ToString();

                channel.BasicPublish(_retryExchangeName, $"{routingKey}{RETRY_POSTFIX}", true, properties, body);
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (_assemblyTypes == default)
            {
                _assemblyTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(p => typeof(IConsumable).IsAssignableFrom(p));
            }
      
            _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);
            var type = _assemblyTypes.First(x => x.Name == eventName);

            var eventType = Type.GetType(type.AssemblyQualifiedName);
            var implementedEvent = JsonConvert.DeserializeObject(message, eventType, Helpers.JsonSerializerSettings);
            await _mediator.Send(implementedEvent);
        }

        public void Publish(EventBase @event)
        {
            try
            {
                var policy = Policy.Handle<BrokerUnreachableException>().Or<SocketException>().WaitAndRetry(
                    _eventBusConfig.Retry.Enabled ? _eventBusConfig.Retry.MaxRetry : 0,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time, retryAttempt, _) =>
                    {
                        _logger.LogWarning($"Could not publish event: {@event.Id} after {time.TotalMilliseconds}ms. RetryAttempt:{retryAttempt} ExceptionMessage:{ex.Message}");
                    });
                policy.Execute(PublishToExchange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

            void PublishToExchange()
            {
                var eventName = @event.GetType().Name;

                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

                using var channel = _persistentConnection.CreateModel();
                _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

                channel.ExchangeDeclare(exchange: _exchangeName, ExchangeType.Direct);

                var message = JsonConvert.SerializeObject(@event,Helpers.JsonSerializerSettings);
                var body = Encoding.UTF8.GetBytes(message);
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);
                channel.BasicPublish(exchange: _exchangeName, routingKey: eventName, mandatory: true, basicProperties: properties, body: body);
            }
        }

        /// <summary>
        /// Returns list of nack-ed events.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task<ICollection<EventBase>> PublishBatch(ICollection<EventBase> events)
        {
            if (events == null || !events.Any())
            {
                return events;
            }

            var nackedEvents = new ConcurrentBag<EventBase>();
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.ConfirmSelect();
                channel.ExchangeDeclare(exchange: _exchangeName, ExchangeType.Direct);
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent


                var outstandingConfirms = new ConcurrentDictionary<ulong, EventBase>();

                void CleanOutstandingConfirms(ulong sequenceNumber, bool multiple)
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

                channel.BasicAcks += (sender, ea) => CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
                channel.BasicNacks += (sender, ea) =>
                {
                    outstandingConfirms.TryGetValue(ea.DeliveryTag, out var @event);
                    nackedEvents.Add(@event);
                    _logger.LogWarning($"Message with id {@event.Id} has been nack-ed. Sequence number: {ea.DeliveryTag}, multiple: {ea.Multiple}");
                    CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
                };

                foreach (var @event in events)
                {
                    var eventName = @event.GetType().Name;
                    var message = JsonConvert.SerializeObject(@event, Helpers.JsonSerializerSettings);
                    var body = Encoding.UTF8.GetBytes(message);
                    outstandingConfirms.TryAdd(channel.NextPublishSeqNo, @event);
                    channel.BasicPublish(exchange: _exchangeName, routingKey: eventName, mandatory: true, basicProperties: properties, body: body);
                }

                const int delayMilliseconds = 10;
                const int maxWaitMilliseconds = 5000;
                int elapsedMilliseconds = 0;
                while (!outstandingConfirms.IsEmpty)
                {
                    await Task.Delay(delayMilliseconds);

                    elapsedMilliseconds += delayMilliseconds;
                    if (elapsedMilliseconds >= maxWaitMilliseconds)
                    {
                        _logger.LogWarning("Max wait time (5 seconds) elapsed for waiting the publish Acks.");
                        break;
                    }
                }

                if (!outstandingConfirms.IsEmpty)
                {
                    foreach (var @event in outstandingConfirms.Values)
                    {
                        nackedEvents.Add(@event);
                    }
                }

                try
                {
                    channel.Close();
                }
                catch
                {
                    // ignored
                }

                return nackedEvents.ToList();
            }
        }

        public void Dispose()
        {
            _consumerChannel?.Dispose();
        }
    }
}
