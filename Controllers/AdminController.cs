using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarewellMyBeloved.Controllers;

[Route("admin")]
public class AdminController : Controller
{
    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }


    [HttpGet("login")]
    [AllowAnonymous] // must allow anyone
    public IActionResult Login()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/admin/index" // go here after successful login
        }, "GitHub");
    }


    [HttpGet("logout")]
    [Authorize] // any logged-in user
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}