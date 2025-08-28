using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using System.Threading.Tasks;
using FarewellMyBeloved.Services;

namespace FarewellMyBeloved.Controllers;

public class FarewellPersonController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IS3Service _s3Service;

    public FarewellPersonController(ApplicationDbContext context, IS3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }


    // GET: FarewellPerson/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: FarewellPerson/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFarewellPersonViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var farewellPerson = new FarewellPerson
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                Slug = GenerateSlug(viewModel.Name),
                Email = viewModel.Email,
                IsPublic = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (viewModel.UsePortraitUrl)
            {
                farewellPerson.PortraitUrl = viewModel.PortraitUrl;
            }
            else if (viewModel.PortraitFile != null)
            {
                farewellPerson.PortraitUrl = await _s3Service.UploadFileAsync(viewModel.PortraitFile);
            }

            if (viewModel.UseBackgroundUrl)
            {
                farewellPerson.BackgroundUrl = viewModel.BackgroundUrl;
            }
            else if (viewModel.BackgroundFile != null)
            {
                farewellPerson.BackgroundUrl = await _s3Service.UploadFileAsync(viewModel.BackgroundFile);
            }

            _context.Add(farewellPerson);
            await _context.SaveChangesAsync();
            return Redirect($"/{farewellPerson.Slug}");
        }
        return View(viewModel);
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