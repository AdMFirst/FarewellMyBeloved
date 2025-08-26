using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FarewellMyBeloved.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("/{slug:minlength(1)}")]
    public async Task<IActionResult> Index(string slug)
    {
        if (slug == "index")
        {
            return RedirectToAction("Index");
        }

        // Find the slug
        var farewellPerson = await _context.FarewellPeople
            .Include(p => p.Messages)
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsPublic);

        if (farewellPerson == null)
        {
            return NotFound();
        }

        return View("Slug", farewellPerson);
    }
}
