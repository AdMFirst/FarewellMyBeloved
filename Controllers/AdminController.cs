using AspNet.Security.OAuth.GitHub;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.Services;
using FarewellMyBeloved.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace FarewellMyBeloved.Controllers;

[Route("Admin")]
public class AdminController : Controller
{

    private readonly ApplicationDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly IConfiguration _configuration;


    public AdminController(ApplicationDbContext context, IS3Service s3Service, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _s3Service = s3Service;
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var viewModel = new AdminIndexViewModel();

        // Generate Farewell People chart data
        await GenerateChartDataAsync<FarewellPerson>(viewModel.FarewellPeopleChartData,
            fp => fp.CreatedAt);

        // Generate Farewell Messages chart data
        await GenerateChartDataAsync<FarewellMessage>(viewModel.FarewellMessagesChartData,
            fm => fm.CreatedAt);

        // Generate Content Reports chart data
        await GenerateChartDataAsync<ContentReport>(viewModel.ContentReportsChartData,
            cr => cr.CreatedAt);

        // Get last 50 moderator logs
        viewModel.ModeratorLogs = await _context.ModeratorLogs
            .OrderByDescending(ml => ml.CreatedAt)
            .Take(50)
            .Select(ml => new ModeratorLogViewModel
            {
                Id = ml.Id,
                ModeratorName = ml.ModeratorName,
                TargetType = ml.TargetType,
                TargetId = ml.TargetId,
                Action = ml.Action,
                Reason = ml.Reason,
                Details = ml.Details,
                CreatedAt = ml.CreatedAt
            })
            .ToListAsync();

        // Get last 50 content reports
        viewModel.ContentReports = await _context.ContentReports
            .Include(cr => cr.ModeratorLogs)
            .OrderByDescending(cr => cr.CreatedAt)
            .Take(50)
            .Select(cr => new ContentReportViewModel
            {
                Id = cr.Id,
                Email = cr.Email,
                FarewellPersonId = cr.FarewellPersonId,
                FarewellMessageId = cr.FarewellMessageId,
                Reason = cr.Reason,
                Explanation = cr.Explanation,
                CreatedAt = cr.CreatedAt,
                ResolvedAt = cr.ResolvedAt
            })
            .ToListAsync();

        return View(viewModel);
    }

    private async Task GenerateChartDataAsync<T>(ChartDataViewModel chartData,
        Func<T, DateTime> dateSelector) where T : class
    {
        var now = DateTime.Now;
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        var nowJakarta = TimeZoneInfo.ConvertTimeFromUtc(now.ToUniversalTime(), timeZoneInfo);

        // Get all data and filter client-side
        var allData = await _context.Set<T>().ToListAsync();

        // Last 7 days data
        for (int i = 6; i >= 0; i--)
        {
            var date = nowJakarta.AddDays(-i);
            var startOfDay = TimeZoneInfo.ConvertTimeToUtc(date.Date, timeZoneInfo);
            var endOfDay = startOfDay.AddDays(1);

            var count = allData.Count(e => dateSelector(e) >= startOfDay && dateSelector(e) < endOfDay);

            chartData.Last7DaysLabels.Add(date.ToString("dd/MM"));
            chartData.Last7DaysData.Add(count);
        }

        // Last 4 weeks data
        for (int i = 3; i >= 0; i--)
        {
            var weekStart = nowJakarta.AddDays(-(i * 7 + 6));
            var weekEnd = weekStart.AddDays(7);

            var startOfWeek = TimeZoneInfo.ConvertTimeToUtc(weekStart.Date, timeZoneInfo);
            var endOfWeek = TimeZoneInfo.ConvertTimeToUtc(weekEnd.Date, timeZoneInfo);

            var count = allData.Count(e => dateSelector(e) >= startOfWeek && dateSelector(e) < endOfWeek);

            var weekNumber = GetWeekNumber(weekStart);
            chartData.Last4WeeksLabels.Add($"Week {weekNumber}");
            chartData.Last4WeeksData.Add(count);
        }
    }

    private int GetWeekNumber(DateTime date)
    {
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }

    /// <summary>
    /// Extracts the object key from a full S3 URL.
    /// </summary>
    /// <param name="url">
    /// The full S3 URL. Can be <c>null</c> or empty.  
    /// Expected format: {endpoint}/{bucket}/{key}
    /// </param>
    /// <returns>
    /// - If <paramref name="url"/> is <c>null</c> or empty → <c>string.Empty</c>  
    /// - If <paramref name="url"/> starts with the configured endpoint and bucket → the extracted key  
    /// - Otherwise, returns the original <paramref name="url"/> (meaning it's not an S3 URL).
    /// </returns>
    private string ExtractS3KeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        // Remove the endpoint and bucket name to get the key
        // URL format: {endpoint}/{bucket}/{key}
        var endpoint = _configuration.GetSection("S3")["Endpoint"];
        var bucketName = _configuration.GetSection("S3")["Bucket"];

        if (url.StartsWith($"{endpoint}/{bucketName}/"))
        {
            var key = url.Substring($"{endpoint}/{bucketName}/".Length);
            return key;
        }

        return url;
    }

    /// <summary>
    /// Validates if the URL points to the configured S3 bucket.  
    /// If yes, returns the object key; otherwise returns <c>null</c>.
    /// </summary>
    /// <param name="url">The input URL (nullable).</param>
    /// <returns>
    /// - Extracted key if URL belongs to S3 bucket  
    /// - <c>null</c> if the URL is null, empty, non-http, or not an S3 URL
    /// </returns>
    private string? ExtractS3KeyIfValid(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        if (!url.StartsWith("http"))
            return null;

        var key = ExtractS3KeyFromUrl(url);

        // if nothing changed, it's not an S3 URL
        return key == url ? null : key;
    }

    /// <summary>
    /// Generates a signed preview URL if the input is a valid S3 URL.  
    /// Returns the original URL for external links, or empty string for invalid/non-URL input.
    /// </summary>
    /// <param name="url">The input URL (nullable).</param>
    /// <returns>
    /// - Signed S3 URL if input points to S3  
    /// - Original URL if input is external but valid (http)  
    /// - <c>string.Empty</c> if input is null, empty, or non-URL string
    /// </returns>
    private async Task<string> MakePreviewUrlAsync(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        var key = ExtractS3KeyIfValid(url);
        if (key == null)
        {
            // Not S3: if it's an external http URL, return as-is
            // otherwise (non-URL string) return empty
            return url.StartsWith("http") ? url : string.Empty;
        }

        return await _s3Service.GetSignedUrlAsync(key);
    }

    /// <summary>
    /// Detects if the input URL is an S3 object. If yes, deletes the corresponding file.  
    /// Otherwise does nothing.
    /// </summary>
    /// <param name="url">The input URL (nullable).</param>
    private async Task DetectAndDeleteS3ImageAsync(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        var key = ExtractS3KeyIfValid(url);
        if (key == null)
            return;

        await _s3Service.DeleteFileAsync(key);
    }


    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("FarewellPeople")]
    public async Task<IActionResult> FarewellPeople(int page = 1)
    {
        const int pageSize = 10;

        var totalCount = await _context.FarewellPeople.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var farewellPeople = await _context.FarewellPeople
            .OrderByDescending(fp => fp.CreatedAt)
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
            .OrderByDescending(fm => fm.CreatedAt)
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

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("ContentReports")]
    public async Task<IActionResult> ContentReports(int page = 1)
    {
        const int pageSize = 10;

        var totalCount = await _context.ContentReports.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var contentReports = await _context.ContentReports
            .Include(cr => cr.ModeratorLogs)
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load related data for better display
        var farewellPersonIds = contentReports
            .Where(cr => cr.FarewellPersonId.HasValue)
            .Select(cr => cr.FarewellPersonId!.Value)
            .Distinct()
            .ToList();

        var farewellMessageIds = contentReports
            .Where(cr => cr.FarewellMessageId.HasValue)
            .Select(cr => cr.FarewellMessageId!.Value)
            .Distinct()
            .ToList();

        var farewellPersons = await _context.FarewellPeople
            .Where(fp => farewellPersonIds.Contains(fp.Id))
            .ToListAsync();

        var farewellMessages = await _context.FarewellMessages
            .Include(fm => fm.FarewellPerson)
            .Where(fm => farewellMessageIds.Contains(fm.Id))
            .ToListAsync();

        var viewModel = new ContentReportsIndexViewModel
        {
            ContentReports = contentReports,
            PageNumber = page,
            TotalPages = totalPages,
            TotalItems = totalCount,
            PageSize = pageSize,
            PersonLookup = farewellPersons.ToDictionary(p => p.Id),
            MessageLookup = farewellMessages.ToDictionary(m => m.Id)
        };

        return View(viewModel);
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("AdminLogs")]
    public async Task<IActionResult> AdminLogs(int page = 1)
    {
        const int pageSize = 250;

        var totalCount = await _context.ModeratorLogs.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await _context.ModeratorLogs
            .OrderByDescending(ml => ml.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new AdminLogsIndexViewModel
        {
            Logs = logs,
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

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("FarewellPeople/Edit/{id}")]
    public async Task<IActionResult> EditFarewellPerson(int id)
    {
        var farewellPerson = await _context.FarewellPeople
            .Include(fp => fp.Messages)
            .FirstOrDefaultAsync(fp => fp.Id == id);

        if (farewellPerson == null)
        {
            return NotFound();
        }

        // Get all content reports that might be related to this person
        var relatedReports = await _context.ContentReports
            .Where(cr => cr.FarewellPersonId == id)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync();

        var viewModel = new AdminEditFarewellPersonViewModel
        {
            FarewellPerson = new EditFarewellPersonViewModel
            {
                Id = farewellPerson.Id,
                Name = farewellPerson.Name,
                Slug = farewellPerson.Slug,
                Description = farewellPerson.Description,
                PortraitUrl = farewellPerson.PortraitUrl,
                BackgroundUrl = farewellPerson.BackgroundUrl,
                Email = farewellPerson.Email,
                CreatedAt = farewellPerson.CreatedAt,
                UpdatedAt = farewellPerson.UpdatedAt,
                IsPublic = farewellPerson.IsPublic,
                RelatedContentReports = relatedReports
            },
            AllContentReports = relatedReports,
            RelatedMessages = farewellPerson.Messages.ToList()
        };

        // Generate Preview image URLs
        viewModel.PortraitImageUrl = await MakePreviewUrlAsync(farewellPerson.PortraitUrl);
        viewModel.BackgroundImageUrl = await MakePreviewUrlAsync(farewellPerson.BackgroundUrl);

        return View(viewModel);
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpPost("FarewellPeople/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFarewellPerson(int id, AdminEditFarewellPersonViewModel viewModel)
    {
        if (id != viewModel.FarewellPerson.Id)
        {
            return BadRequest();
        }

        var farewellPerson = await _context.FarewellPeople
            .Include(fp => fp.Messages)
            .FirstOrDefaultAsync(fp => fp.Id == id);

        if (farewellPerson == null)
        {
            return NotFound();
        }

        // Validate required action log fields
        if (string.IsNullOrWhiteSpace(viewModel.FarewellPerson.ActionReason))
        {
            ModelState.AddModelError("FarewellPerson.ActionReason", "Action reason is required");
        }

        if (string.IsNullOrWhiteSpace(viewModel.FarewellPerson.ActionDetails))
        {
            ModelState.AddModelError("FarewellPerson.ActionDetails", "Action details are required");
        }

        // If validation fails, return to the view with errors
        if (!ModelState.IsValid)
        {
            // Get all content reports that might be related to this person
            var relatedReports = await _context.ContentReports
                .Where(cr => cr.FarewellPersonId == id)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToListAsync();

            viewModel.AllContentReports = relatedReports;
            viewModel.RelatedMessages = farewellPerson.Messages.ToList();

            return View(viewModel);
        }

        // Edit functionality with logging
        var moderatorName = User.Identity?.Name ?? "Unknown Admin";

        // Check if anything actually changed
        var hasChanges = farewellPerson.Name != viewModel.FarewellPerson.Name ||
                           farewellPerson.Slug != viewModel.FarewellPerson.Slug ||
                           farewellPerson.Description != viewModel.FarewellPerson.Description ||
                           farewellPerson.PortraitUrl != viewModel.FarewellPerson.PortraitUrl ||
                           farewellPerson.BackgroundUrl != viewModel.FarewellPerson.BackgroundUrl ||
                           farewellPerson.Email != viewModel.FarewellPerson.Email ||
                           farewellPerson.IsPublic != viewModel.FarewellPerson.IsPublic;

        if (hasChanges)
        {
            // Delete old images from S3 if they changed
            if (farewellPerson.PortraitUrl != viewModel.FarewellPerson.PortraitUrl && !string.IsNullOrEmpty(farewellPerson.PortraitUrl))
            {
                try
                {
                    var oldPortraitKey = ExtractS3KeyFromUrl(farewellPerson.PortraitUrl);
                    await _s3Service.DeleteFileAsync(oldPortraitKey);
                }
                catch (Exception ex)
                {
                    // Log error but continue with the operation
                    Console.WriteLine($"Failed to delete old portrait image: {ex.Message}");
                }
            }

            if (farewellPerson.BackgroundUrl != viewModel.FarewellPerson.BackgroundUrl && !string.IsNullOrEmpty(farewellPerson.BackgroundUrl))
            {
                try
                {
                    var oldBackgroundKey = ExtractS3KeyFromUrl(farewellPerson.BackgroundUrl);
                    await _s3Service.DeleteFileAsync(oldBackgroundKey);
                }
                catch (Exception ex)
                {
                    // Log error but continue with the operation
                    Console.WriteLine($"Failed to delete old background image: {ex.Message}");
                }
            }

            farewellPerson.Name = viewModel.FarewellPerson.Name;
            farewellPerson.Slug = viewModel.FarewellPerson.Slug;
            farewellPerson.Description = viewModel.FarewellPerson.Description;
            farewellPerson.PortraitUrl = viewModel.FarewellPerson.PortraitUrl;
            farewellPerson.BackgroundUrl = viewModel.FarewellPerson.BackgroundUrl;
            farewellPerson.Email = viewModel.FarewellPerson.Email;
            farewellPerson.IsPublic = viewModel.FarewellPerson.IsPublic;
            farewellPerson.UpdatedAt = DateTime.UtcNow;

            // Create moderator log for changes
            var moderatorLog = new ModeratorLog
            {
                ModeratorName = moderatorName,
                TargetType = "FarewellPerson",
                TargetId = farewellPerson.Id,
                Action = "edit",
                Reason = viewModel.FarewellPerson.ActionReason ?? "admin_edit",
                Details = viewModel.FarewellPerson.ActionDetails ?? $"Farewell person '{farewellPerson.Name}' updated by admin",
                ContentReportId = viewModel.FarewellPerson.SelectedContentReportId
            };

            _context.ModeratorLogs.Add(moderatorLog);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("FarewellPeople");
    }


    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("FarewellPeople/Delete/{id}")]
    public async Task<IActionResult> DeleteFarewellPerson(int id)
    {
        var farewellPerson = await _context.FarewellPeople
            .Include(p => p.Messages)
            .FirstOrDefaultAsync(fp => fp.Id == id);

        if (farewellPerson == null)
        {
            return NotFound();
        }

        // Get related content reports
        var relatedReports = await _context.ContentReports
            .Where(cr => cr.FarewellPersonId == id)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync();

        var viewModel = new DeleteFarewellPersonViewModel
        {
            Name = farewellPerson.Name,
            Slug = farewellPerson.Slug,
            Description = farewellPerson.Description,
            PortraitUrl = await MakePreviewUrlAsync(farewellPerson.PortraitUrl),
            BackgroundUrl = await MakePreviewUrlAsync(farewellPerson.BackgroundUrl),
            FarewellMessages = farewellPerson.Messages.ToList(),
            RelatedContentReports = relatedReports
        };

        return View(viewModel);
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpPost("FarewellPeople/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFarewellPerson(int id, DeleteFarewellPersonViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var farewellPerson = await _context.FarewellPeople
            .FirstOrDefaultAsync(fp => fp.Id == id);

        if (farewellPerson == null)
        {
            return NotFound();
        }

        var moderatorName = User.Identity?.Name ?? "Unknown Admin";

        try
        {
            // Delete old images from S3 if they exist
            await DetectAndDeleteS3ImageAsync(farewellPerson.PortraitUrl);
            await DetectAndDeleteS3ImageAsync(farewellPerson.BackgroundUrl);

            // Remove the farewell person
            _context.FarewellPeople.Remove(farewellPerson);

            // Create moderator log for deletion
            var moderatorLog = new ModeratorLog
            {
                ModeratorName = moderatorName,
                TargetType = "FarewellPerson",
                TargetId = farewellPerson.Id,
                Action = "delete",
                Reason = viewModel.ActionReason ?? "admin_delete",
                Details = viewModel.ActionDetails ?? $"Farewell person '{farewellPerson.Name}' deleted by admin",
                ContentReportId = viewModel.SelectedContentReportId
            };

            _context.ModeratorLogs.Add(moderatorLog);
            await _context.SaveChangesAsync();

            return RedirectToAction("FarewellPeople");
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error deleting farewell person: {ex.Message}");
            
            // Re-populate the view model for error display
            var relatedReports = await _context.ContentReports
                .Where(cr => cr.FarewellPersonId == id)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToListAsync();

            viewModel.RelatedContentReports = relatedReports;
            viewModel.FarewellMessages = farewellPerson.Messages.ToList();

            ModelState.AddModelError("", "An error occurred while deleting the farewell person. Please try again.");
            return View(viewModel);
        }
    }
}