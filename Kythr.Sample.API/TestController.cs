using Microsoft.AspNetCore.Mvc;

namespace Kythr.Sample.API
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