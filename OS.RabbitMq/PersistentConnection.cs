using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace OS.RabbitMq
{
    internal class PersistentConnection : IPersistentConnection
    {
        private readonly ILogger<PersistentConnection> _logger;
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private IConnection _connection;
        private bool _disposed;
        private readonly object _syncRoot = new();

        public PersistentConnection(ILogger<PersistentConnection> logger, IOptions<Settings> eventBusConfigOptions)
        {
            _logger = logger;
            var eventBusConfig = eventBusConfigOptions.Value;
            _connectionFactory = new ConnectionFactory
            {
                Endpoint = new AmqpTcpEndpoint(new Uri(eventBusConfig.Endpoint)),
                HostName = eventBusConfig.HostName,
                UserName = eventBusConfig.UserName,
                Password = eventBusConfig.Password,
                DispatchConsumersAsync = true
            };

            _retryCount = eventBusConfig.Retry.MaxRetry;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex, ex.Message);
            }

        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ Connections");
            }

            return _connection.CreateModel();
        }

        public bool TryConnect()
        {
            _logger.LogInformation("Trying to connect to RabbitMQ");

            lock (_syncRoot)
            {
                var policy = Policy.Handle<SocketException>().Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (ex, time) =>
                        {
                            _logger.LogWarning(ex,
                                "Could not connect to RabbitMQ after {TimeOut}s ({ExceptionMessage})",
                                $"{time.TotalSeconds:n1}", ex.Message);
                        });

                policy.Execute(() => { _connection = _connectionFactory.CreateConnection(); });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.ConnectionBlocked += OnConnectionBlocked;
                    _connection.CallbackException += OnCallbackException;

                    _logger.LogInformation(
                        "RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events",
                        _connection.Endpoint.HostName);
                    return true;
                }

                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened.");
                return false;
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs args)
        {
            if (_disposed) return;
            _logger.LogWarning("RabbitMQ Connection is down. Trying to Reconnect.");
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs args)
        {
            if (_disposed) return;
            _logger.LogWarning("RabbitMQ Connection is down. Trying to Reconnect.");
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs args)
        {
            if (_disposed) return;
            _logger.LogWarning("RabbitMQ Connection threw exception. Trying to Reconnect.");
        }
    }
}
