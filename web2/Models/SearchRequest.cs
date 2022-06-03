namespace web2.Models
{
    public class SearchRequest
    {
        public string sourceId { get; set; }
        public string queryId { get; set; }
        public string nextId { get; set; }
    }

    public class SearchResponse
    {
        public string queryId { get; set; }
        public long count { get; set; }
        public string nextId { get; set; }
    }

    public class DataSource
    {
        public int TotalAvailable { get; set; }
        public int BatchSize { get; set; }
    }
}