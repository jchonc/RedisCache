using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using StackExchange.Redis;
using web2.Models;

namespace web2.Services
{
    public class FakeDownstreamService : BackgroundService
    {
        private readonly ConnectionMultiplexer _connection;

        private readonly Dictionary<string, DataSource> _knownSources = new()
        {
            {"src1", new DataSource {TotalAvailable = 230, BatchSize = 100}},
            {"src2", new DataSource {TotalAvailable = 130, BatchSize = 70}}
        };

        private readonly Random _rand;

        public FakeDownstreamService(ConnectionMultiplexer connection)
        {
            _connection = connection;
            _rand = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var subscriber = _connection.GetSubscriber();
            var publisher = _connection.GetSubscriber();
            var db = _connection.GetDatabase();
            subscriber.Subscribe("_search_requested_", (channel, value) =>
            {
                var request = JsonConvert.DeserializeObject<SearchRequest>(value);
                // Wait randomly between 1 - 5 seconds
                Task.Delay(_rand.Next(1, 5) * 1000, stoppingToken).Wait(stoppingToken);

                if (_knownSources.TryGetValue(request.sourceId, out var source))
                {
                    if (!int.TryParse(request.nextId, out var pageNumber)) pageNumber = 0;

                    var response = new SearchResponse
                    {
                        queryId = request.queryId
                    };

                    if (source.TotalAvailable > (pageNumber + 1) * source.BatchSize)
                    {
                        // not the last batch
                        response.count = source.BatchSize;
                        response.nextId = (pageNumber + 1).ToString();
                    }
                    else
                    {
                        // last batch it is
                        response.count = source.TotalAvailable - pageNumber * source.BatchSize;
                        response.nextId = string.Empty;
                    }

                    publisher.Publish("_search_data_arrival_", JsonConvert.SerializeObject(response));
                }
            });
            return Task.Delay(-1, stoppingToken);
        }
    }
}