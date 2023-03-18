namespace OS.RabbitMq
{
    public interface IEventBus : IDisposable
    {
        void Publish(EventBase @event);
        Task<ICollection<EventBase>> PublishBatch(ICollection<EventBase> events);
        void SubscribeAndStartConsuming();
    }
}
