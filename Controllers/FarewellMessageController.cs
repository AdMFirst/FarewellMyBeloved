using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using System.Threading.Tasks;

namespace FarewellMyBeloved.Controllers;

public class FarewellMessageController : Controller
{
    private readonly ApplicationDbContext _context;

    public FarewellMessageController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: FarewellMessage/Create
    public async Task<IActionResult> Create(int? farewellPersonId)
    {
        var viewModel = new CreateFarewellMessageViewModel();
        
        if (farewellPersonId.HasValue)
        {
            viewModel.FarewellPersonId = farewellPersonId.Value;
            var farewellPerson = await _context.FarewellPeople.FindAsync(farewellPersonId.Value);
            if (farewellPerson != null)
            {
                viewModel.FarewellPersonName = farewellPerson.Name;
            }
        }

        // Populate ViewBag.FarewellPeople for the dropdown
        var farewellPeople = await _context.FarewellPeople
                                           .OrderBy(p => p.Name)
                                           .Where(p => p.IsPublic)
                                           .Select(p => new SelectListItem
                                           {
                                               Value = p.Id.ToString(),
                                               Text = p.Name
                                           })
                                           .ToListAsync();
        ViewBag.FarewellPeople = farewellPeople;
        
        return View(viewModel);
    }

    // POST: FarewellMessage/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Message,AuthorName,AuthorEmail,FarewellPersonId")] CreateFarewellMessageViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var farewellMessage = new FarewellMessage
            {
                Message = viewModel.Message,
                AuthorName = viewModel.AuthorName,
                AuthorEmail = viewModel.AuthorEmail,
                FarewellPersonId = viewModel.FarewellPersonId ?? 0,
                IsPublic = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(farewellMessage);
            await _context.SaveChangesAsync();
            await _context.Entry(farewellMessage).Reference(m => m.FarewellPerson).LoadAsync();
        
            // Redirect to the person's details page
            return Redirect($"/{farewellMessage.FarewellPerson?.Slug}");
        }
        
        // If we get here, something went wrong, so repopulate the FarewellPersonName
        if (viewModel.FarewellPersonId.HasValue)
        {
            var farewellPerson = await _context.FarewellPeople.FindAsync(viewModel.FarewellPersonId.Value);
            if (farewellPerson != null)
            {
                viewModel.FarewellPersonName = farewellPerson.Name;
            }
        }

        // Repopulate ViewBag.FarewellPeople if ModelState is invalid
        var farewellPeople = await _context.FarewellPeople
                                           .OrderBy(p => p.Name)
                                           .Select(p => new SelectListItem
                                           {
                                               Value = p.Id.ToString(),
                                               Text = p.Name
                                           })
                                           .ToListAsync();
        ViewBag.FarewellPeople = farewellPeople;
        
        return View(viewModel);
    }

}