using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OS.Core.Pagination
{
    public interface IPaginationFilter
    {
        public int Page { get; set; }
        public int PageSize { get; set; }

        [BindNever, Newtonsoft.Json.JsonIgnore, JsonIgnore]
        public int Skip => Page < 2 ? 0 : (Page - 1) * PageSize;
    }
}
