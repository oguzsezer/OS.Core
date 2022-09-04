using Microsoft.AspNetCore.Http;

namespace OS.Core.ResponseWrappers.Models
{
    public sealed class Result
    {
        private Result() { }
        public SuccessStatus Status { get; private init; }
        public enum SuccessStatus
        {
            Processing = StatusCodes.Status102Processing,
            Accepted = StatusCodes.Status202Accepted,
            NoContent = StatusCodes.Status204NoContent
        }

        internal static Result Processing()
        {
            return new Result
            {
                Status = SuccessStatus.Processing
            };
        }

        internal static Result Accepted()
        {
            return new Result
            {
                Status = SuccessStatus.Accepted
            };
        }

        internal static Result NoContent()
        {
            return new Result
            {
                Status = SuccessStatus.NoContent
            };
        }
    }

    public sealed class Result<T>
    {
        private Result() { }
        public T? Data { get; private init; }
        public SuccessStatus Status { get; private init; }
        public enum SuccessStatus
        {
            OK = StatusCodes.Status200OK,
            Created = StatusCodes.Status201Created
        }

        internal static Result<T> Ok(T data)
        {
            return new Result<T>
            {
                Status = SuccessStatus.OK,
                Data = data
            };
        }

        internal static Result<T> Created(T data)
        {
            return new Result<T>
            {
                Status = SuccessStatus.Created,
                Data = data
            };
        }
    }
}
