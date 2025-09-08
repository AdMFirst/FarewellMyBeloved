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

    public async Task<IActionResult> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return RedirectToAction("Index");
        }

        var searchResults = await _context.FarewellPeople
            .Where(p => p.IsPublic &&
                       (p.Name.Contains(searchTerm) ||
                        p.Description.Contains(searchTerm)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Sign the image before displaying
        foreach (var person in searchResults)
        {
            if (!string.IsNullOrEmpty(person.PortraitUrl))
            {
                person.PortraitUrl = await SignImage(person.PortraitUrl);
            }
            if (!string.IsNullOrEmpty(person.BackgroundUrl))
            {
                person.BackgroundUrl = await SignImage(person.BackgroundUrl);
            }
        }

        ViewBag.SearchTerm = searchTerm;
        return View("Search", searchResults);
    }

    [HttpGet("/{slug:minlength(1)}")]
    public async Task<IActionResult> Slug(string slug)
    {
        if (slug == "index")
        {
            return RedirectToAction("Index");
        }
        else if (slug == "Admin")
        {
            return RedirectToAction("Index", "Admin");
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
        if (!string.IsNullOrEmpty(farewellPerson.PortraitUrl))
        {
            farewellPerson.PortraitUrl = await SignImage(farewellPerson.PortraitUrl);
        }
        if (!string.IsNullOrEmpty(farewellPerson.BackgroundUrl))
        {
            farewellPerson.BackgroundUrl = await SignImage(farewellPerson.BackgroundUrl);
        }

        return View("Slug", farewellPerson);
    }

    private async Task<string> SignImage(string imagePath)
    {
        // check if image is a valid image url
        if (!IsValidImagePath(imagePath))
        {
            if (imagePath.Equals("DELETED BY ADMIN"))
            {
                return _configuration.GetSection("Image")["DeletedUrl"] ?? "https://s6.imgcdn.dev/YQkM7y.jpg";
            }
            return _configuration.GetSection("Image")["DefaultUrl"] ?? "https://s6.imgcdn.dev/YQO8MN.webp";
        }

        // Extract key from the image path
        var endpoint = _configuration.GetSection("S3")["Endpoint"];
        var bucketName = _configuration.GetSection("S3")["Bucket"];

        if (imagePath.StartsWith($"{endpoint}/{bucketName}/"))
        {
            var key = imagePath.Substring($"{endpoint}/{bucketName}/".Length);
            return await _s3Service.GetSignedUrlAsync(key);
        }

        return imagePath;
    }
    
    private bool IsValidImagePath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return false;

        var lower = imagePath.ToLowerInvariant().Trim();

        // common image extensions
        return Uri.IsWellFormedUriString(imagePath, UriKind.Absolute)
            || lower.EndsWith(".jpg")
            || lower.EndsWith(".jpeg")
            || lower.EndsWith(".png")
            || lower.EndsWith(".gif")
            || lower.EndsWith(".bmp")
            || lower.EndsWith(".tiff")
            || lower.EndsWith(".tif")
            || lower.EndsWith(".webp")
            || lower.EndsWith(".svg")
            || lower.EndsWith(".ico")
            || lower.EndsWith(".heic")
            || lower.EndsWith(".heif");
    }

}
