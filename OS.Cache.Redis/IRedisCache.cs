using StackExchange.Redis;

namespace OS.Cache.Redis
{
    public interface IRedisCache
    {
        Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None);
        Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None);
        Task<bool> KeyExistsAsync(string key, CommandFlags flags = CommandFlags.None);
        Task<string> GetStringAsync(string key, CommandFlags flags = CommandFlags.None);
        Task<T> GetObjectAsync<T>(string key, CommandFlags flags = CommandFlags.None);
        Task<bool> RemoveKeyAsync(string key, CommandFlags flags = CommandFlags.None);
        /// <summary>
        /// Subscribes to a channel
        /// <para>When a message is received, the provided action <paramref name="processAction"/> will be invoked with the type T.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="processAction"></param>
        void Subscribe<T>(string channel, Action<T> processAction);
        /// <summary>
        /// Publishes a message to the channel using FireAndForget
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task Publish<T>(string channel, T value);
    }
}