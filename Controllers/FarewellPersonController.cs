using Microsoft.AspNetCore.Mvc;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using FarewellMyBeloved.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using ICU4N.Text;

namespace FarewellMyBeloved.Controllers;

public class FarewellPersonController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly IStringLocalizer<FarewellPersonController> _localizer;

    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/webp",
        "image/svg+xml",
        "image/tiff"
    };

    public FarewellPersonController(ApplicationDbContext context, IS3Service s3Service, IStringLocalizer<FarewellPersonController> localizer)
    {
        _context = context;
        _s3Service = s3Service;
        _localizer = localizer;
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

        // Server-side file validations (size + MIME)
        if (!viewModel.UsePortraitUrl && viewModel.PortraitFile != null)
        {
            if (viewModel.PortraitFile.Length > maxFileSize)
            {
                ModelState.AddModelError("PortraitFile", "Portrait file size must be less than 5MB.");
            }
            if (!IsValidMimeType(viewModel.PortraitFile))
            {
                ModelState.AddModelError("PortraitFile", "Portrait file must be an image");
            }
        }

        if (!viewModel.UseBackgroundUrl && viewModel.BackgroundFile != null)
        {
            if (viewModel.BackgroundFile.Length > maxFileSize)
            {
                ModelState.AddModelError("BackgroundFile", "Background file size must be less than 5MB.");
            }
            if (!IsValidMimeType(viewModel.BackgroundFile))
            {
                ModelState.AddModelError("BackgroundFile", "Background file must be an image");
            }
        }

        // Generate slug and ensure uniqueness before any uploads
        var slug = GenerateSlug(viewModel.Name);
        if (await _context.FarewellPeople.AnyAsync(p => p.Slug == slug))
        {
            ModelState.AddModelError(nameof(viewModel.Name), _localizer["SlugAlreadyExists"]);
        }

        // If any validation failed, return early without uploading to S3
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        // Build entity after validation passes
        var farewellPerson = new FarewellPerson
        {
            Name = viewModel.Name,
            Description = viewModel.Description,
            Slug = slug,
            Email = viewModel.Email,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        string? uploadedPortraitUrl = null;
        string? uploadedBackgroundUrl = null;

        if (viewModel.UsePortraitUrl)
        {
            farewellPerson.PortraitUrl = viewModel.PortraitUrl;
        }
        else if (viewModel.PortraitFile != null)
        {
            // Compress and upload portrait AFTER validation passes
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
            uploadedPortraitUrl = await _s3Service.UploadFileAsync(compressedPortraitFile, S3UploadType.Portrait);
            farewellPerson.PortraitUrl = uploadedPortraitUrl;
        }

        if (viewModel.UseBackgroundUrl)
        {
            farewellPerson.BackgroundUrl = viewModel.BackgroundUrl;
        }
        else if (viewModel.BackgroundFile != null)
        {
            // Compress and upload background AFTER validation passes
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
            uploadedBackgroundUrl = await _s3Service.UploadFileAsync(compressedBackgroundFile, S3UploadType.Background);
            farewellPerson.BackgroundUrl = uploadedBackgroundUrl;
        }

        _context.Add(farewellPerson);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Clean up uploaded files if DB save fails (e.g., slug race condition)
            if (!string.IsNullOrEmpty(uploadedPortraitUrl))
            {
                await _s3Service.DetectAndDeleteFileAsync(uploadedPortraitUrl);
            }
            if (!string.IsNullOrEmpty(uploadedBackgroundUrl))
            {
                await _s3Service.DetectAndDeleteFileAsync(uploadedBackgroundUrl);
            }

            ModelState.AddModelError(nameof(viewModel.Name), _localizer["SlugAlreadyExists"]);
            return View(viewModel);
        }

        return Redirect($"/{farewellPerson.Slug}");
    }

    public async Task<IActionResult> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            ViewData["Search"] = _localizer["Search"];
            return View();
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
            person.PortraitUrl = await _s3Service.DetectAndGetSignedUrlAsync(person.PortraitUrl);
            person.BackgroundUrl = await _s3Service.DetectAndGetSignedUrlAsync(person.BackgroundUrl);
        }

        ViewBag.SearchTerm = searchTerm;
        return View("Search", searchResults);
    }


    


    private string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Guid.NewGuid().ToString("n");

        // ICU4N transliteration for robust, multilingual slugs
        var transliterator = Transliterator.GetInstance("Any-Latin; Latin-ASCII; NFD; [:Nonspacing Mark:] Remove; NFC");
        var latin = transliterator.Transliterate(name);
        var sbSlug = new StringBuilder(latin.Length);
        foreach (var ch in latin.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sbSlug.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') sbSlug.Append('-');
            else if (ch < 128) sbSlug.Append('-');
        }
        var slug = Regex.Replace(sbSlug.ToString(), "-{2,}", "-").Trim('-');
        if (string.IsNullOrEmpty(slug)) slug = Guid.NewGuid().ToString("n");
        return slug;

        // Normalize and remove diacritics
        var normalized = name.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark && cat != UnicodeCategory.SpacingCombiningMark)
                sb.Append(c);
        }
        var cleaned = sb.ToString();

        // Lowercase and replace some common dash/space variants
        cleaned = cleaned.ToLowerInvariant()
            .Replace('\u2013', '-') // en dash
            .Replace('\u2014', '-') // em dash
            .Replace('\u2015', '-') // horizontal bar
            .Replace('\u2212', '-') // minus sign
            .Replace('\u00A0', ' ') // no-break space
            .Replace('\u2009', ' ') // thin space
            .Replace('\u200B', ' '); // zero-width space

        // Keep ASCII letters/numbers, convert others to hyphen
        var sb2 = new StringBuilder();
        foreach (var ch in cleaned)
        {
            if (ch >= 'a' && ch <= 'z') sb2.Append(ch);
            else if (ch >= '0' && ch <= '9') sb2.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') sb2.Append('-');
            else sb2.Append('-');
        }

        var interim = sb2.ToString();

        // Collapse multiple hyphens and trim, and remove any remaining unwanted chars listed originally
        var collapsed = Regex.Replace(interim, "-{2,}", "-").Trim('-');

        // Final pass to remove characters you explicitly stripped in the original (rare after above)
        collapsed = collapsed
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
            .Replace("…", "")
            .Replace("—", "-")
            .Replace("–", "-")
            .Replace("―", "-")
            .Replace("  ", " ")
            .Trim()
            .Replace(" ", "-");

        return collapsed;
    }


    private bool IsValidMimeType(IFormFile file)
    {
        if (file == null)
            return false;

        return AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant());
    }

}
