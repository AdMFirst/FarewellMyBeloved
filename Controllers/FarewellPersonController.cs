using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using System.Threading.Tasks;

namespace FarewellMyBeloved.Controllers;

public class FarewellPersonController : Controller
{
    private readonly ApplicationDbContext _context;

    public FarewellPersonController(ApplicationDbContext context)
    {
        _context = context;
    }


    // GET: FarewellPerson/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: FarewellPerson/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,PortraitUrl,BackgroundUrl")] CreateFarewellPersonViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var farewellPerson = new FarewellPerson
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                PortraitUrl = viewModel.PortraitUrl,
                BackgroundUrl = viewModel.BackgroundUrl,
                Slug = GenerateSlug(viewModel.Name),
                IsPublic = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(farewellPerson);
            await _context.SaveChangesAsync();
            return Redirect($"/{farewellPerson.Slug}");
        }
        return View(viewModel);
    }

    private bool FarewellPersonExists(int id)
    {
        return _context.FarewellPeople.Any(e => e.Id == id);
    }

    private string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("!", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace("|", "")
            .Replace("@", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("^", "")
            .Replace("&", "")
            .Replace("*", "")
            .Replace("+", "")
            .Replace("=", "")
            .Replace("`", "")
            .Replace("~", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("—", "-")
            .Replace("–", "-")
            .Replace("―", "-")
            .Replace("…", "")
            .Replace("  ", " ")
            .Trim()
            .Replace(" ", "-");
    }
}