using OS.Core.Pagination;

namespace OS.MongoDb
{
    public sealed class PaginationResult<T> : IPaginationResult<T>
    {
        public PaginationResult(T data)
        {
            Data = data;
        }

        public PaginationResult(T data, IPaginationFilter filter, long totalCount)
        {
            Data = data;
            Pagination = new Pagination(filter, totalCount);
        }


        public T Data { get; }
        public Pagination Pagination { get; }
    }
}
