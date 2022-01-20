using Microsoft.AspNetCore.Mvc;

namespace Simple.Service.Monitoring.Sample.API
{
    public class TestController : Controller
    {
        // GET
        public IActionResult Index()
        {
            return Ok();
        }
    }
}