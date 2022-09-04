using OS.Core.Pagination;

namespace OS.MongoDb
{
    public interface IPaginationResult<out T>
    {
        public T Data { get; }
        public Pagination Pagination { get; }
    }
}
