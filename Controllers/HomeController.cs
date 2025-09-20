using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using FarewellMyBeloved.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FarewellMyBeloved.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IS3Service s3Service, IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _s3Service = s3Service;
        _configuration = configuration;
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
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Slug(string slug)
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

        // Sign all the image first before displaying
        farewellPerson.BackgroundUrl = await _s3Service.DetectAndGetSignedUrlAsync(farewellPerson.BackgroundUrl);
        farewellPerson.PortraitUrl = await _s3Service.DetectAndGetSignedUrlAsync(farewellPerson.PortraitUrl);


        return View("Slug", farewellPerson);
    }

}
