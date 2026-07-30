namespace Connection360.Api.Models
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = [];
        public Int64 TotalItems { get; set; }
        public Int64 CurrentPage { get; set; }
        public Int64 Limit { get; set; }
        public Int64 TotalPages => Limit > 0 ? (Int64)Math.Ceiling(TotalItems / (Double)Limit) : 0;
    }
}
