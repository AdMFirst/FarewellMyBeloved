using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FarewellMyBeloved.Controllers
{
    [Route("AboutUs")]
    public class AboutUsController : Controller
    {
        private readonly IStringLocalizer<AboutUsController> _localizer;

        public AboutUsController(IStringLocalizer<AboutUsController> localizer)
        {
            _localizer = localizer;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            ViewData["AboutUs"] = _localizer["AboutUs"];
            return View();
        }
    }
}