using Microsoft.AspNetCore.Mvc;

namespace FarewellMyBeloved.Controllers
{
    [Route("AboutUs")]
    public class AboutUsController : Controller
    {
        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }
    }
}