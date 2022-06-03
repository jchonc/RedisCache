using System.Linq;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using web2.Models;

namespace web2.Controllers
{
    [ApiController]
    public class DownstreamController : ControllerBase
    {
        private readonly ConnectionMultiplexer _connection;
        private readonly string _queryId = "client1-source1";

        public DownstreamController(ConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        [HttpPost("api/setAll")]
        public object DoSetAll([FromBody] HashModel request)
        {
            var db = _connection.GetDatabase(0);

            db.HashSet(_queryId, new[]
            {
                new HashEntry("value_int", request.ValueInt),
                new HashEntry("value_string", request.ValueString)
            });
            return Ok();
        }

        [HttpGet("api/getAll")]
        public object DoGetAll()
        {
            var db = _connection.GetDatabase(0);
            var value = db.HashGetAll(_queryId);
            if (value.Length == 0) return NotFound();

            return new HashModel
            {
                ValueInt = (int) value.Single(a => a.Name == "value_int").Value,
                ValueString = value.Single(a => a.Name == "value_string").Value
            };
        }
    }
}