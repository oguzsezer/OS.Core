using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OS.Core.ResponseWrappers.Models;
using Refit;

namespace OS.Core.ResponseWrappers
{
    /// <inheritdoc />
    public class ApiResult : ObjectResult
    {
        public ApiResult WithLocation(string action, string controller, object? routeValues)
        {
            Location = new Location(action, controller, routeValues);
            return this;
        }

        /// <summary>
        /// Already generated path
        /// </summary>
        /// <example>/api/someController/someAction/1</example>
        private readonly string? _locationPath;

        /// <summary>
        /// Will be used to generate the location path if <see cref="_locationPath"/> not already provided
        /// </summary>
        private protected Location? Location;

        private protected ApiResult(object? value, HttpResponseHeaders headers, int statusCode, bool isSuccess) :
            base(value)
        {
            if (isSuccess && headers.TryGetValues(HeaderNames.Location, out var locationValues))
            {
                _locationPath = locationValues.FirstOrDefault();
            }

            StatusCode = statusCode;
        }

        /// <summary>
        /// Creates a new <see cref="ApiResult"/> instance with the provided <paramref name="refitResponse"/>.
        /// </summary>
        /// <param name="refitResponse"></param>
        public ApiResult(IApiResponse<string> refitResponse) : this(
            refitResponse.IsSuccessStatusCode ? refitResponse.Content : refitResponse.Error?.Content,
            refitResponse.Headers, (int)refitResponse.StatusCode, refitResponse.IsSuccessStatusCode)
        {

        }

        /// <summary>
        /// Creates a new <see cref="ApiResult"/> instance with the provided <paramref name="refitResponse"/>.
        /// </summary>
        /// <param name="refitResponse"></param>
        public ApiResult(IApiResponse<object> refitResponse) : this(
            refitResponse.IsSuccessStatusCode ? refitResponse.Content : refitResponse.Error?.Content,
            refitResponse.Headers, (int)refitResponse.StatusCode, refitResponse.IsSuccessStatusCode)
        {
            if (refitResponse.IsSuccessStatusCode)
            {
                DeclaredType = refitResponse.Content?.GetType();
            }
        }

        /// <summary>
        /// Creates a new <see cref="ApiResult"/> instance with the provided <paramref name="refitResponse"/>.
        /// </summary>
        /// <param name="refitResponse"></param>
        public ApiResult(IApiResponse refitResponse) : this(
            refitResponse.IsSuccessStatusCode ? null : refitResponse.Error?.Content,
            refitResponse.Headers, (int)refitResponse.StatusCode, refitResponse.IsSuccessStatusCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ApiResult"/> instance with the provided <paramref name="httpStatusCode"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="httpStatusCode"></param>
        /// <param name="value"></param>
        public ApiResult(HttpStatusCode httpStatusCode, object? value = null) : base(value)
        {
            StatusCode = (int)httpStatusCode;
        }

        /// <summary>
        /// Creates a new <see cref="ApiResult"/> instance with the provided <paramref name="statusCode"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="value"></param>
        public ApiResult(int statusCode, object? value = null) : base(value)
        {
            StatusCode = statusCode;
        }

        /// <inheritdoc />
        public override void OnFormatting(ActionContext context)
        {
            if (!string.IsNullOrWhiteSpace(_locationPath))
            {
                context.HttpContext.Response.Headers.Location = _locationPath;
            }
            else if (Location != null)
            {
                var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
                var urlHelper = urlHelperFactory.GetUrlHelper(context);
                var locationPath = urlHelper.Action(Location.Action, Location.Controller, Location.RouteValues) ?? string.Empty;
                context.HttpContext.Response.Headers.Location = locationPath;
            }

            base.OnFormatting(context);
        }
    }
}
