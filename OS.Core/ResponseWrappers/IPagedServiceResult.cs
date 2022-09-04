namespace OS.Core.ResponseWrappers;

public interface IPagedServiceResult<T> : IServiceResult<T>
{
    public Pagination.Pagination? Pagination { get; }
}