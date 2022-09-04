using System.Collections;

namespace OS.Core.ResponseWrappers.Models
{
    public class PaginationResponse<T> : Pagination.Pagination where T : ICollection
    {
        public PaginationResponse(IPagedServiceResult<T> serviceResult) : base(serviceResult.Pagination)
        {
            Data = serviceResult.Result.Data;
        }

        public T? Data { get; set; }
    }
}
