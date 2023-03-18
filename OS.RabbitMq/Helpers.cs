using Newtonsoft.Json;

namespace OS.RabbitMq
{
    internal class Helpers
    {
        internal static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto, 
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }
}
