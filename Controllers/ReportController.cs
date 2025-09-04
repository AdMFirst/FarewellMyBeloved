using Microsoft.AspNetCore.Mvc;
using FarewellMyBeloved.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarewellMyBeloved.Controllers;

[Route("report")]
public class ReportController : Controller
{

    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public ReportController(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpGet("")]
    public IActionResult Index(string id, string what, string? referer = null)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(what) || (what != "FarewellPerson" && what != "FarewellMessage"))
        {
            return NotFound();
        }

        // Pass the query to the view
        ViewBag.Id = id;
        ViewBag.What = what;

        // check if referer is a local URL to prevent open redirect
        ViewBag.Referer = !string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer) ? referer : Url.Action("Index", "Home");

        var reasonStrings = _configuration.GetSection("Admin:ReasonStrings").Get<List<string>>() ?? new List<string>(["Spam", "Abuse", "Inappropriate Content", "Other"]);
        ViewBag.Reasons = reasonStrings.Select(s => new SelectListItem { Value = s.ToLower().Replace(" ", ""), Text = s }).ToList();

        return View();
    }

    [HttpGet("success")]
    public IActionResult Success(string? reportId = null)
    {
        if (string.IsNullOrEmpty(reportId))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReportId = reportId;
        return View();
    }

    [HttpPost("")]
    public async Task<IActionResult> Index(ContentReport report)
    {
        // Set the CreatedAt timestamp if not already set
        if (report.CreatedAt == default)
        {
            report.CreatedAt = DateTime.UtcNow;
        }
        //Console.WriteLine($"New report submitted:[{report.Id}] - {report.FarewellPersonId} - {report.FarewellMessageId} - {report.Reason} - {report.Explanation}");

        // Add the report to the database
        _context.ContentReports.Add(report);
        await _context.SaveChangesAsync();

        

        return RedirectToAction("Success", new { reportId = report.Id.ToString() });
    }
}