using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StackExchange.Redis;

namespace OS.Cache.Redis
{
    internal class RedisCache : IRedisCache
    {
        private readonly IConnectionMultiplexer _redis;

        private static readonly JsonSerializerSettings? JsonSerializerSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            },
            TypeNameHandling = TypeNameHandling.Auto
        };

        public RedisCache(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return _redis.GetDatabase().StringSetAsync(key, value, expiry, when, flags);
        }

        public Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            var valueAsJson = JsonConvert.SerializeObject(value, JsonSerializerSettings);
            return SetStringAsync(key, valueAsJson, expiry, when, flags);
        }

        public Task<bool> KeyExistsAsync(string key, CommandFlags flags = CommandFlags.None)
        {
            return _redis.GetDatabase().KeyExistsAsync(key, flags);
        }

        public async Task<string> GetStringAsync(string key, CommandFlags flags = CommandFlags.None)
        {
            return await _redis.GetDatabase().StringGetAsync(key, flags);
        }

        public async Task<T> GetObjectAsync<T>(string key, CommandFlags flags = CommandFlags.None)
        {
            var jsonValue = await GetStringAsync(key, flags);
            return string.IsNullOrWhiteSpace(jsonValue)
                ? default
                : JsonConvert.DeserializeObject<T>(jsonValue, JsonSerializerSettings);
        }

        public Task<bool> RemoveKeyAsync(string key, CommandFlags flags = CommandFlags.None)
        {
            return _redis.GetDatabase().KeyDeleteAsync(key, flags);
        }

        public async void Subscribe<T>(string channel, Action<T> processAction)
        {
            var subscriber = _redis.GetSubscriber();
            var channelMessageQueue = await subscriber.SubscribeAsync(channel);
            channelMessageQueue.OnMessage(channelMessage =>
            {
                var value = JsonConvert.DeserializeObject<T>(channelMessage.Message, JsonSerializerSettings);
                processAction(value);
            });
        }

        public async Task Publish<T>(string channel, T value)
        {
            var subscriber = _redis.GetSubscriber();
            var messageJson = JsonConvert.SerializeObject(value, JsonSerializerSettings);
            await subscriber.PublishAsync(channel, messageJson, CommandFlags.FireAndForget);
        }
    }
}
