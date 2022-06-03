using System.Collections.Generic;

namespace web1.Models
{
    public class ApiRequest
    {
        public string clientId { get; set; }
        public List<string> sourceIds { get; set; }
        public int pageSize { get; set; }
    }
}