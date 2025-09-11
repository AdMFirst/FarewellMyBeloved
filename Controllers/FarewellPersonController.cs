using Microsoft.AspNetCore.Mvc;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using FarewellMyBeloved.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

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
        const long maxFileSize = 5 * 1024 * 1024; // 5MB

        if (ModelState.IsValid)
        {
            // Server-side file size validation
            if (!viewModel.UsePortraitUrl && viewModel.PortraitFile != null && viewModel.PortraitFile.Length > maxFileSize)
            {
                ModelState.AddModelError("PortraitFile", "Portrait file size must be less than 5MB.");
            }

            if (!viewModel.UseBackgroundUrl && viewModel.BackgroundFile != null && viewModel.BackgroundFile.Length > maxFileSize)
            {
                ModelState.AddModelError("BackgroundFile", "Background file size must be less than 5MB.");
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

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
                // Compress and upload portrait
                using var compressedPortraitStream = new MemoryStream();
                using var image = Image.Load(viewModel.PortraitFile.OpenReadStream());
                await image.SaveAsJpegAsync(compressedPortraitStream, new JpegEncoder
                {
                    Quality = 85
                });
                compressedPortraitStream.Position = 0;
                var compressedPortraitFile = new FormFile(compressedPortraitStream, 0, compressedPortraitStream.Length, "PortraitFile", viewModel.PortraitFile.FileName)
                {
                    Headers = viewModel.PortraitFile.Headers,
                    ContentType = viewModel.PortraitFile.ContentType
                };
                farewellPerson.PortraitUrl = await _s3Service.UploadFileAsync(compressedPortraitFile, S3UploadType.Portrait);
            }

            if (viewModel.UseBackgroundUrl)
            {
                farewellPerson.BackgroundUrl = viewModel.BackgroundUrl;
            }
            else if (viewModel.BackgroundFile != null)
            {
                // Compress and upload background
                using var compressedBackgroundStream = new MemoryStream();
                using var image = Image.Load(viewModel.BackgroundFile.OpenReadStream());
                await image.SaveAsJpegAsync(compressedBackgroundStream, new JpegEncoder
                {
                    Quality = 85
                });
                compressedBackgroundStream.Position = 0;
                var compressedBackgroundFile = new FormFile(compressedBackgroundStream, 0, compressedBackgroundStream.Length, "BackgroundFile", viewModel.BackgroundFile.FileName)
                {
                    Headers = viewModel.BackgroundFile.Headers,
                    ContentType = viewModel.BackgroundFile.ContentType
                };
                farewellPerson.BackgroundUrl = await _s3Service.UploadFileAsync(compressedBackgroundFile, S3UploadType.Background);
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