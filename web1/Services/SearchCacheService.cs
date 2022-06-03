using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using StackExchange.Redis;
using web1.Models;

namespace web1.Services
{
    public class SearchCacheService : BackgroundService
    {
        private readonly ConnectionMultiplexer _connection;
        private readonly ConcurrentDictionary<string, ManualResetEvent> _locks;
        private readonly ISubscriber _publisher;
        private ISubscriber _subscriber;

        public SearchCacheService(ConnectionMultiplexer connection)
        {
            _connection = connection;
            _locks = new ConcurrentDictionary<string, ManualResetEvent>();
            _publisher = _connection.GetSubscriber();
        }

        public string FetchSearchResult(string clientId, string sourceId, int pageSize)
        {
            var queryId = $"{clientId}-{sourceId}";
            var db = _connection.GetDatabase();
            var keys = db.HashGetAll(queryId);
            if (keys.Any())
            {
                var total = (int) keys.Single(key => key.Name == "total").Value;
                var cursor = (int) keys.Single(key => key.Name == "cursor").Value;
                var available = total - cursor;
                if (available >= pageSize)
                {
                    available -= pageSize;
                    db.HashIncrement(queryId, "cursor", pageSize);
                    return $"{pageSize} data returned, {available} left";
                }

                db.HashSet(queryId, "cursor", total);
                return $"{available} data returned, nothing left";
            }

            return "something went wrong, unknown search";
        }

        public WaitHandle EnsureSearchResult(string clientId, string sourceId, int pageSize)
        {
            var queryId = $"{clientId}-{sourceId}";
            var db = _connection.GetDatabase();
            var keys = db.HashGetAll(queryId);
            var nextId = string.Empty;
            var total = 0L;
            if (keys.Any())
            {
                total = (int) keys.Single(key => key.Name == "total").Value;
                var cursor = (int) keys.Single(key => key.Name == "cursor").Value;
                nextId = keys.Single(key => key.Name == "nextId").Value;
                var size = total - cursor;
                if (size > pageSize) return null;
                // How to mark the end of data? 
            }
            else
            {
                db.HashSet(queryId, new[]
                {
                    new HashEntry("total", total),
                    new HashEntry("cursor", 0L),
                    new HashEntry("nextId", nextId)
                });
            }

            _publisher.Publish("_search_requested_", JsonConvert.SerializeObject(new SearchRequest
                {
                    queryId = queryId,
                    sourceId = sourceId,
                    nextId = db.HashGet(queryId, "nextId").ToString()
                })
            );
            return _locks.GetOrAdd(queryId, _ => new ManualResetEvent(false));
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _subscriber = _connection.GetSubscriber();
            _subscriber.Subscribe("_search_data_arrival_", (channel, value) =>
            {
                var db = _connection.GetDatabase();
                var response = JsonConvert.DeserializeObject<SearchResponse>(value);
                if (_locks.TryGetValue(response.queryId, out var handle))
                {
                    db.HashSet(response.queryId, "nextId", response.nextId);
                    db.HashIncrement(response.queryId, "total", response.count);
                    handle.Set();
                    _locks.TryRemove(value, out _);
                }
            });
            return Task.Delay(-1, stoppingToken);
        }
    }
}