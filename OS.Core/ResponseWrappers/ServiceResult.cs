using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OS.Core.ResponseWrappers.Models;

namespace OS.Core.ResponseWrappers
{
    /// <summary>
    /// Response wrapper for business service methods.
    /// <code>
    /// public async Task<![CDATA[<ServiceResult> Foo()]]>
    /// <para />
    /// {
    ///   ... //do something
    ///   return <see cref="ServiceResult.Accepted"/>
    /// <para />
    /// }
    /// </code>
    /// </summary>
    public sealed class ServiceResult : IServiceResult
    {
        private ServiceResult() { }

        public bool IsSuccess { get; private init; }
        public Result? Result { get; private init; }
        public Error? Error { get; private init; }

        internal static IServiceResult Processing()
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Result = Result.Processing()
            };
        }

        public static IServiceResult Accepted()
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Result = Result.Accepted()
            };
        }

        public static IServiceResult NoContent()
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Result = Result.NoContent()
            };
        }

        public static IServiceResult BusinessRuleError(string? errorMessage = null)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Error = Error.BusinessRule(errorMessage)
            };
        }

        public static IServiceResult ForbiddenError(string? errorMessage = null)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Error = Error.Forbidden(errorMessage)
            };
        }

        public static IServiceResult NotFoundError(string? errorMessage = null)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Error = Error.NotFound(errorMessage)
            };
        }

        public static IServiceResult ConflictError(string? errorMessage = null)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Error = Error.Conflict(errorMessage)
            };
        }
    }

    /// <summary>
    /// Response wrapper for business service methods.
    /// <code>
    /// public async Task<![CDATA[<]]><see cref="ServiceResult{T}"/><![CDATA[>]]>
    /// <para />
    /// {
    ///   ... //do something
    ///   return <see cref="ServiceResult{Foo}.Created"/>
    /// <para />
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">return type</typeparam>
    public sealed class ServiceResult<T> : IServiceResult<T>
    {
        private ServiceResult() { }

        //dummy
        [BindNever, Newtonsoft.Json.JsonIgnore, JsonIgnore]
        Result? IServiceResult.Result { get; }
        public bool IsSuccess { get; private init; }
        public Result<T>? Result { get; private init; }
        public Error? Error { get; private init; }
        
        public static IServiceResult<T> Ok(T data)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Result = Result<T>.Ok(data)
            };
        }

        public static IServiceResult<T> Created(T data)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Result = Result<T>.Created(data)
            };
        }

        public static IServiceResult<T> BusinessRuleError(string? errorMessage = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Error = Error.BusinessRule(errorMessage)
            };
        }

        public static IServiceResult<T> ForbiddenError(string? errorMessage = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Error = Error.Forbidden(errorMessage)
            };
        }

        public static IServiceResult<T> NotFoundError(string? errorMessage = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Error = Error.NotFound(errorMessage)
            };
        }

        public static IServiceResult ConflictError(string? errorMessage = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Error = Error.Conflict(errorMessage)
            };
        }
    }
}
