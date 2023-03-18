namespace OS.RabbitMq
{
    public class Settings
    {
        public string Endpoint { get; set; }
        public string HostName { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        /// <summary>
        /// Name of the queue to be consumed.
        /// <para></para>
        /// <remarks>! Publish DOES NOT use this parameter. Published messages are routed via their routing-key (Type name)</remarks>
        /// </summary>
        public string QueueName { get; set; }
        public int QueueTTLSeconds { get; set; }
        public string ExchangeName { get; set; }
        public int ExchangeTTLSeconds { get; set; }
        public RetryPolicy Retry { get; set; }

        public class RetryPolicy
        {
            public bool Enabled { get; set; }
            public int MaxRetry { get; set; } = 5;
            public int DelayMilliseconds { get; set; }
            public bool ExponentialDelayEnabled { get; set; }
            public bool DelayEnabled { get; set; }
        }
    }
}
