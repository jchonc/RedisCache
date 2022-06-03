using Microsoft.AspNetCore.Mvc;

namespace web2.Controllers
{
    [ApiController]
    public class DownstreamController : ControllerBase
    {
        [HttpGet("api/home")]
        public object DoSomething()
        {
            return "ok";
        }
    }
}