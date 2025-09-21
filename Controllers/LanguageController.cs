using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace FarewellMyBeloved.Controllers;

public class LanguageController : Controller
{
    [HttpGet]
    public IActionResult SetCulture(string culture, string returnUrl = "/")
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            culture = "en-US";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
        );

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return LocalRedirect(returnUrl);
    }
}

