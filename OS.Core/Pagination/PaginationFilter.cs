using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace OS.Core.Pagination
{
    public class PaginationFilter : IPaginationFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        [BindNever, Newtonsoft.Json.JsonIgnore, JsonIgnore]
        public int Skip => Page < 2 ? 0 : (Page - 1) * PageSize;
    }
}
