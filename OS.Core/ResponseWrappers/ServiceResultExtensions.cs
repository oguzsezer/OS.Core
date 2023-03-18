using System.Collections;
using OS.Core.ResponseWrappers.Models;

namespace OS.Core.ResponseWrappers
{
    /// <summary>
    /// IServiceResult extensions
    /// </summary>
    public static class ServiceResultExtensions
    {
        /// <summary>
        /// <inheritdoc cref="ResponseWrappers.ApiResult"/>
        /// </summary>
        /// <param name="serviceResult"></param>
        /// <returns><see cref="ResponseWrappers.ApiResult"/></returns>
        public static ApiResult ApiResult(this IServiceResult serviceResult)
        {
            return serviceResult.IsSuccess
                ? new ApiResult((int)serviceResult.Result.Status)
                : new ApiResult((int)serviceResult.Error.Reason, serviceResult.Error.ErrorMessage);
        }

        /// <summary>
        /// <inheritdoc cref="ResponseWrappers.ApiResult"/>
        /// </summary>
        /// <param name="serviceResult"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="ResponseWrappers.ApiResult"/></returns>
        public static ApiResult ApiResult<T>(this IServiceResult<T> serviceResult)
        {
            return serviceResult.IsSuccess
                ? new ApiResult((int)serviceResult.Result.Status, serviceResult.Result.Data)
                : new ApiResult((int)serviceResult.Error.Reason, serviceResult.Error.ErrorMessage);
        }

        /// <summary>
        /// <inheritdoc cref="ResponseWrappers.ApiResult"/>
        /// </summary>
        /// <typeparam name="T">ICollection</typeparam>
        /// <param name="pagedServiceResult">pagination response</param>
        /// <returns><see cref="ResponseWrappers.ApiResult"/></returns>
        public static ApiResult ApiResult<T>(this IPagedServiceResult<T> pagedServiceResult) where T : ICollection
        {
            return pagedServiceResult.IsSuccess 
                ? new ApiResult((int)pagedServiceResult.Result.Status, new PaginationResponse<T>(pagedServiceResult))
                : new ApiResult((int)pagedServiceResult.Error.Reason, pagedServiceResult.Error.ErrorMessage);
        }
    }
}
