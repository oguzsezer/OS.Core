namespace OS.MongoDb
{
    public interface IEntity<out TKey> where TKey : IEquatable<TKey>
    {
        public TKey Id { get; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}