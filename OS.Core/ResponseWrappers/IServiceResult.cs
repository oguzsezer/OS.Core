using OS.Core.ResponseWrappers.Models;

namespace OS.Core.ResponseWrappers
{
    public interface IServiceResult
    {
        public bool IsSuccess { get; }
        public Result? Result { get; }
        public Error? Error { get; }
    }

    public interface IServiceResult<T> : IServiceResult
    {
        public new Result<T>? Result { get; }
    }
}
