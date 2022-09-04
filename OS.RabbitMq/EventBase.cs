using MediatR;
using Newtonsoft.Json;

namespace OS.RabbitMq
{
    public class EventBase : IRequest
    {
        public EventBase()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.UtcNow;
        }

        public EventBase(string id, DateTime creationDate)
        {
            Id = id;
            CreatedAt = creationDate;
        }

        [JsonProperty]
        public string Id { get; private set; }
        [JsonProperty]
        public DateTime CreatedAt { get; private set; }
    }
}
