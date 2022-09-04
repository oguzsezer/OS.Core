namespace OS.Core.Pagination
{
    public interface IPagination
    {
        public int CurrentPage { get; }
        public int TotalPages { get; }
        public long TotalCount { get; }
        public bool HasPreviousPage { get; }
        public int PreviousPage { get; }
        public bool HasNextPage { get; }
        public int NextPage { get; }
    }
}
