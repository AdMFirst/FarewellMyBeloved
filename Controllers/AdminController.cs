using AspNet.Security.OAuth.GitHub;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace FarewellMyBeloved.Controllers;

[Route("admin")]
public class AdminController : Controller
{

    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("FarewellPeople")]
    public async Task<IActionResult> FarewellPeople(int page = 1)
    {
        const int pageSize = 10;
        
        var totalCount = await _context.FarewellPeople.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var farewellPeople = await _context.FarewellPeople
            .OrderBy(fp => fp.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var viewModel = new FarewellPeopleIndexViewModel
        {
            FarewellPeople = farewellPeople,
            PageNumber = page,
            TotalPages = totalPages,
            TotalItems = totalCount,
            PageSize = pageSize
        };
        
        return View(viewModel);
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("FarewellMessages")]
    public async Task<IActionResult> FarewellMessages(int page = 1)
    {
        const int pageSize = 10;

        var totalCount = await _context.FarewellMessages.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var farewellMessages = await _context.FarewellMessages
            .Include(fm => fm.FarewellPerson)
            .OrderBy(fm => fm.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new FarewellMessagesIndexViewModel
        {
            FarewellMessages = farewellMessages,
            PageNumber = page,
            TotalPages = totalPages,
            TotalItems = totalCount,
            PageSize = pageSize
        };
        
        return View(viewModel);
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