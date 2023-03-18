using System.Collections;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OS.Core.Pagination;
using OS.Core.ResponseWrappers.Models;

namespace OS.Core.ResponseWrappers;

/// <summary>
/// Response wrapper for business service methods.
/// <code>
/// public async Task<![CDATA[<PagedServiceResult<ICollection<Foo>> Bar()]]>
/// <para />
/// {
///   ... //do something
///   return <see cref="PagedServiceResult{T}.Ok"/>
/// <para />
/// }
/// </code>
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class PagedServiceResult<T> : IPagedServiceResult<T> where T : ICollection
{
    private PagedServiceResult() { }

    //dummy
    [BindNever, Newtonsoft.Json.JsonIgnore, JsonIgnore]
    Result? IServiceResult.Result { get; }
    public bool IsSuccess { get; private init; }
    public Result<T>? Result { get; private init; }
    public Error? Error { get; private init; }
    public Pagination.Pagination? Pagination { get; private init; }

    public static IPagedServiceResult<T> Ok(T data, IPaginationFilter filter, long totalCount)
    {
        return new PagedServiceResult<T>
        {
            IsSuccess = true,
            Result = Result<T>.Ok(data),
            Pagination = new Pagination.Pagination(filter, totalCount)
        };
    }

    public static IPagedServiceResult<T> BusinessRuleError(string? errorMessage = null)
    {
        return new PagedServiceResult<T>
        {
            IsSuccess = false,
            Error = Error.BusinessRule(errorMessage)
        };
    }

    public static IPagedServiceResult<T> ForbiddenError(string? errorMessage = null)
    {
        return new PagedServiceResult<T>
        {
            IsSuccess = false,
            Error = Error.Forbidden(errorMessage)
        };
    }

    public static IPagedServiceResult<T> NotFoundError(string? errorMessage = null)
    {
        return new PagedServiceResult<T>
        {
            IsSuccess = false,
            Error = Error.NotFound(errorMessage)
        };
    }
}