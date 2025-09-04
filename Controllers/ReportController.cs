using Microsoft.AspNetCore.Mvc;
using FarewellMyBeloved.Models;
using Microsoft.IdentityModel.Tokens;

namespace FarewellMyBeloved.Controllers;

[Route("report")]
public class ReportController : Controller
{

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
    public IActionResult Index(ContentReport report)
    {
        // Log the form data for now (out of scope for now)
        Console.WriteLine($"Report submitted - Email: {report.Email}, Reason: {report.Reason}, Explanation: {report.Explanation}");
        Console.WriteLine($"Report type: {report.CreatedAt}, ID: {report.FarewellPersonId ?? report.FarewellMessageId}");


        return RedirectToAction("Success", new { reportId = Guid.NewGuid().ToString() });
    }
}