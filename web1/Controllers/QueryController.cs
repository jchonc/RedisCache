using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using web1.Models;
using web1.Services;

namespace web1.Controllers
{
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly SearchCacheService _searchCacheService;

        public QueryController(SearchCacheService searchCacheService)
        {
            _searchCacheService = searchCacheService;
        }

        [HttpPost("api/query")]
        public object DoSearch([FromBody] ApiRequest request)
        {
            const int maxTimeOut = 10000;
            var waitHandles = request.sourceIds.Select(sourceId =>
                    _searchCacheService.EnsureSearchResult(request.clientId, sourceId, request.pageSize))
                .Where(handle => handle != null).ToArray();

            if (waitHandles.Any() && !WaitHandle.WaitAll(waitHandles, maxTimeOut)) return "fail to fetch it";

            var results = request.sourceIds.Select(sourceId =>
                _searchCacheService.FetchSearchResult(request.clientId, sourceId, request.pageSize)).ToList();

            return string.Join(", ", results);
        }
    }
}