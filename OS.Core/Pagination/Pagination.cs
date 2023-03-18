namespace OS.Core.Pagination
{
    public class Pagination : IPagination
    {
        private readonly int _pageSize = 10;

        public Pagination(IPagination pagination)
        {
            CurrentPage = pagination.CurrentPage;
            TotalCount = pagination.TotalCount;
            TotalPages = (int)Math.Ceiling(TotalCount / (float)_pageSize);
        }

        public Pagination(IPaginationFilter filter, long totalCount)
        {
            _pageSize = filter.PageSize;
            TotalCount = totalCount;
            CurrentPage = filter.Page;
            TotalPages = (int)Math.Ceiling(TotalCount / (float)_pageSize);
        }

        public int CurrentPage { get; }
        public int TotalPages { get; }
        public long TotalCount { get; }
        public bool HasPreviousPage => CurrentPage > 1;
        public int PreviousPage => HasPreviousPage ? CurrentPage == 1 ? CurrentPage : CurrentPage - 1 : 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int NextPage => HasNextPage ? CurrentPage == TotalPages ? TotalPages : CurrentPage + 1 : CurrentPage;
    }
}
