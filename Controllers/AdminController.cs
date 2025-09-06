using AspNet.Security.OAuth.GitHub;
using FarewellMyBeloved.Models;
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

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
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