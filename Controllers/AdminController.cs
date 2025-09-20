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


    private readonly IStateParameterService _stateParameterService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, IS3Service s3Service, IConfiguration configuration, IStateParameterService stateParameterService, ILogger<AdminController> logger)
    {
        _context = context;
        _configuration = configuration;
        _s3Service = s3Service;
        _stateParameterService = stateParameterService;
        _logger = logger;
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
    public async Task<IActionResult> Login()
    {
        _logger.LogInformation("GitHub OAuth login initiated");
        
        // Generate and store state parameter for CSRF protection
        var state = _stateParameterService.GenerateStateParameter();
        await _stateParameterService.StoreStateParameterAsync(state);

        _logger.LogInformation("Redirecting to GitHub OAuth with state parameter");
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = $"/Admin/callback?state={state}" // go here after successful login with state parameter
        }, "GitHub");
    }


    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string? state, string? error, string? error_description)
    {
        _logger.LogInformation("GitHub OAuth callback received");
        
        // Handle error response from GitHub
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError("GitHub OAuth error: {Error} - {ErrorDescription}", error, error_description ?? "No description");
            
            return RedirectToAction("OAuthError", new {
                error = "oauth_error",
                error_description = error_description ?? "Authentication failed"
            });
        }

        // Validate state parameter to prevent CSRF attacks
        if (string.IsNullOrEmpty(state))
        {
            _logger.LogWarning("OAuth callback received without state parameter - potential CSRF attack");
            return RedirectToAction("OAuthError", new {
                error = "missing_state",
                error_description = "Invalid authentication request"
            });
        }

        _logger.LogDebug("Validating OAuth state parameter");
        var isValidState = await _stateParameterService.ValidateStateParameterAsync(state);
        if (!isValidState)
        {
            _logger.LogWarning("OAuth callback received with invalid state parameter - potential CSRF attack");
            return RedirectToAction("OAuthError", new {
                error = "invalid_state",
                error_description = "Invalid authentication request"
            });
        }

        // State is valid, remove it from storage
        await _stateParameterService.RemoveStateParameterAsync(state);
        _logger.LogInformation("OAuth state parameter validated successfully, proceeding with authentication");

        // Proceed with the authentication challenge
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/Admin" // go here after successful login
        }, "GitHub");
    }

    [HttpGet("error")]
    [AllowAnonymous]
    public IActionResult OAuthError(string? error, string? error_description)
    {
        _logger.LogWarning("OAuth error page accessed with error: {Error}", error ?? "unknown");
        
        if (error == "oauth_error")
        {
            ViewBag.ErrorMessage = "Authentication failed";
            ViewBag.DetailedMessage = error_description ?? "An error occurred during GitHub authentication.";
        }
        else if (error == "missing_state" || error == "invalid_state")
        {
            _logger.LogWarning("Potential CSRF attack detected: {Error}", error);
            ViewBag.ErrorMessage = "Invalid authentication request";
            ViewBag.DetailedMessage = "The authentication request appears to be invalid or tampered with. Please try again.";
        }
        else
        {
            ViewBag.ErrorMessage = "Authentication error";
            ViewBag.DetailedMessage = "An unknown error occurred during authentication.";
        }
        
        ViewBag.RequestId = HttpContext.TraceIdentifier;
        return View("OAuthError");
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
        viewModel.PortraitImageUrl = await _s3Service.DetectAndGetSignedUrlAsync(farewellPerson.PortraitUrl);
        viewModel.BackgroundImageUrl = await _s3Service.DetectAndGetSignedUrlAsync(farewellPerson.BackgroundUrl);

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
                    await _s3Service.DetectAndDeleteFileAsync(farewellPerson.PortraitUrl);
                }
                catch (Exception ex)
                {
                    // Log error but continue with the operation
                    _logger.LogError(ex, "Failed to delete old portrait image for FarewellPerson ID {FarewellPersonId}", farewellPerson.Id);
                }
            }

            if (farewellPerson.BackgroundUrl != viewModel.FarewellPerson.BackgroundUrl && !string.IsNullOrEmpty(farewellPerson.BackgroundUrl))
            {
                try
                {
                    await _s3Service.DetectAndDeleteFileAsync(farewellPerson.BackgroundUrl);
                }
                catch (Exception ex)
                {
                    // Log error but continue with the operation
                    _logger.LogError(ex, "Failed to delete old background image for FarewellPerson ID {FarewellPersonId}", farewellPerson.Id);
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

            // Set the related report to be resolved if one was selected
            if (viewModel.FarewellPerson.SelectedContentReportId.HasValue)
            {
                var contentReport = await _context.ContentReports
                    .FirstOrDefaultAsync(cr => cr.Id == viewModel.FarewellPerson.SelectedContentReportId.Value);
                
                if (contentReport != null && !contentReport.ResolvedAt.HasValue)
                {
                    contentReport.ResolvedAt = DateTime.UtcNow;
                }
            }

            _context.ModeratorLogs.Add(moderatorLog);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("FarewellPeople");
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("FarewellMessages/Edit/{id}")]
    public async Task<IActionResult> EditFarewellMessage(int id)
    {
        var farewellMessage = await _context.FarewellMessages
            .Include(fm => fm.FarewellPerson)
            .FirstOrDefaultAsync(fm => fm.Id == id);

        if (farewellMessage == null)
        {
            return NotFound();
        }

        // Get all content reports that might be related to this message
        var relatedReports = await _context.ContentReports
            .Where(cr => cr.FarewellMessageId == id)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync();

        var viewModel = new EditFarewellMessageViewModel
        {
            Id = farewellMessage.Id,
            Message = farewellMessage.Message,
            AuthorName = farewellMessage.AuthorName,
            AuthorEmail = farewellMessage.AuthorEmail,
            FarewellPersonId = farewellMessage.FarewellPersonId,
            FarewellPersonName = farewellMessage.FarewellPerson?.Name ?? string.Empty,
            CreatedAt = farewellMessage.CreatedAt,
            IsPublic = farewellMessage.IsPublic,
            RelatedContentReports = relatedReports
        };

        return View(viewModel);
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpPost("FarewellMessages/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFarewellMessage(int id, EditFarewellMessageViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        var farewellMessage = await _context.FarewellMessages
            .Include(fm => fm.FarewellPerson)
            .FirstOrDefaultAsync(fm => fm.Id == id);

        if (farewellMessage == null)
        {
            return NotFound();
        }

        // Validate required action log fields
        if (string.IsNullOrWhiteSpace(viewModel.ActionReason))
        {
            ModelState.AddModelError("ActionReason", "Action reason is required");
        }

        if (string.IsNullOrWhiteSpace(viewModel.ActionDetails))
        {
            ModelState.AddModelError("ActionDetails", "Action details are required");
        }

        // If validation fails, return to the view with errors
        if (!ModelState.IsValid)
        {
            // Get all content reports that might be related to this message
            var relatedReports = await _context.ContentReports
                .Where(cr => cr.FarewellMessageId == id)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToListAsync();

            viewModel.RelatedContentReports = relatedReports;
            viewModel.FarewellPersonName = farewellMessage.FarewellPerson?.Name ?? string.Empty;

            return View(viewModel);
        }

        // Edit functionality with logging
        var moderatorName = User.Identity?.Name ?? "Unknown Admin";

        // Check if anything actually changed
        var hasChanges = farewellMessage.Message != viewModel.Message ||
                         farewellMessage.AuthorName != viewModel.AuthorName ||
                         farewellMessage.AuthorEmail != viewModel.AuthorEmail ||
                         farewellMessage.IsPublic != viewModel.IsPublic;

        if (hasChanges)
        {
            farewellMessage.Message = viewModel.Message;
            farewellMessage.AuthorName = viewModel.AuthorName;
            farewellMessage.AuthorEmail = viewModel.AuthorEmail;
            farewellMessage.IsPublic = viewModel.IsPublic;

            // Create moderator log for changes
            var moderatorLog = new ModeratorLog
            {
                ModeratorName = moderatorName,
                TargetType = "FarewellMessage",
                TargetId = farewellMessage.Id,
                Action = "edit",
                Reason = viewModel.ActionReason ?? "admin_edit",
                Details = viewModel.ActionDetails ?? $"Farewell message '{farewellMessage.Message}' updated by admin",
                ContentReportId = viewModel.SelectedContentReportId
            };

            // Set the related report to be resolved if one was selected
            if (viewModel.SelectedContentReportId.HasValue)
            {
                var contentReport = await _context.ContentReports
                    .FirstOrDefaultAsync(cr => cr.Id == viewModel.SelectedContentReportId.Value);
                
                if (contentReport != null && !contentReport.ResolvedAt.HasValue)
                {
                    contentReport.ResolvedAt = DateTime.UtcNow;
                }
            }
            //Console.WriteLine(viewModel.SelectedContentReportId != null ? viewModel.SelectedContentReportId: "Content report not sighterd");
            _context.ModeratorLogs.Add(moderatorLog);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("FarewellMessages");
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpGet("FarewellMessages/Delete/{id}")]
    public async Task<IActionResult> DeleteFarewellMessage(int id)
    {
        var farewellMessage = await _context.FarewellMessages
            .Include(fm => fm.FarewellPerson)
            .FirstOrDefaultAsync(fm => fm.Id == id);

        if (farewellMessage == null)
        {
            return NotFound();
        }

        // Get related content reports
        var relatedReports = await _context.ContentReports
            .Where(cr => cr.FarewellMessageId == id)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync();

        var viewModel = new DeleteFarewellMessageViewModel
        {
            Id = farewellMessage.Id,
            Message = farewellMessage.Message,
            AuthorName = farewellMessage.AuthorName,
            AuthorEmail = farewellMessage.AuthorEmail,
            FarewellPersonId = farewellMessage.FarewellPersonId,
            FarewellPersonName = farewellMessage.FarewellPerson?.Name ?? string.Empty,
            CreatedAt = farewellMessage.CreatedAt,
            RelatedContentReports = relatedReports
        };

        return View(viewModel);
    }

    [Authorize(Policy = "AdminsOnly")]
    [HttpPost("FarewellMessages/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFarewellMessage(int id, DeleteFarewellMessageViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var farewellMessage = await _context.FarewellMessages
            .FirstOrDefaultAsync(fm => fm.Id == id);

        if (farewellMessage == null)
        {
            return NotFound();
        }

        var moderatorName = User.Identity?.Name ?? "Unknown Admin";

        try
        {
            // Remove the farewell message
            _context.FarewellMessages.Remove(farewellMessage);

            // Create moderator log for deletion
            var moderatorLog = new ModeratorLog
            {
                ModeratorName = moderatorName,
                TargetType = "FarewellMessage",
                TargetId = farewellMessage.Id,
                Action = "delete",
                Reason = viewModel.ActionReason ?? "admin_delete",
                Details = viewModel.ActionDetails ?? $"Farewell message '{farewellMessage.Message}' deleted by admin",
                ContentReportId = viewModel.SelectedContentReportId
            };

            // Set the related report to be resolved if one was selected
            if (viewModel.SelectedContentReportId.HasValue)
            {
                var contentReport = await _context.ContentReports
                    .FirstOrDefaultAsync(cr => cr.Id == viewModel.SelectedContentReportId.Value);
                
                if (contentReport != null && !contentReport.ResolvedAt.HasValue)
                {
                    contentReport.ResolvedAt = DateTime.UtcNow;
                }
            }
            Console.WriteLine(viewModel.SelectedContentReportId);
            _context.ModeratorLogs.Add(moderatorLog);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("FarewellMessages");
        }
        catch (Exception ex)
        {
            // Log the error
            _logger.LogError(ex, "Error deleting farewell message ID {FarewellMessageId}", id);
            
            // Re-populate the view model for error display
            var relatedReports = await _context.ContentReports
                .Where(cr => cr.FarewellMessageId == id)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToListAsync();

            viewModel.RelatedContentReports = relatedReports;
            viewModel.FarewellPersonName = farewellMessage.FarewellPerson?.Name ?? string.Empty;

            ModelState.AddModelError("", "An error occurred while deleting the farewell message. Please try again.");
            return View(viewModel);
        }
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
            PortraitUrl = await _s3Service.DetectAndGetSignedUrlAsync(farewellPerson.PortraitUrl),
            BackgroundUrl = await _s3Service.DetectAndGetSignedUrlAsync(farewellPerson.BackgroundUrl),
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
            await _s3Service.DetectAndDeleteFileAsync(farewellPerson.PortraitUrl);
            await _s3Service.DetectAndDeleteFileAsync(farewellPerson.BackgroundUrl);

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

            // Set the related report to be resolved if one was selected
            if (viewModel.SelectedContentReportId.HasValue)
            {
                var contentReport = await _context.ContentReports
                    .FirstOrDefaultAsync(cr => cr.Id == viewModel.SelectedContentReportId.Value);
                
                if (contentReport != null && !contentReport.ResolvedAt.HasValue)
                {
                    contentReport.ResolvedAt = DateTime.UtcNow;
                }
            }


            _context.ModeratorLogs.Add(moderatorLog);
            await _context.SaveChangesAsync();
            

            return RedirectToAction("FarewellPeople");
        }
        catch (Exception ex)
        {
            // Log the error
            _logger.LogError(ex, "Error deleting farewell person ID {FarewellPersonId}", id);
            
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