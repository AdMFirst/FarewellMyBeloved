using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    // GET: FarewellMessage
    public async Task<IActionResult> Index()
    {
        var farewellMessages = await _context.FarewellMessages
            .Include(m => m.FarewellPerson)
            .Where(m => m.IsPublic)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        return View(farewellMessages);
    }

    // GET: FarewellMessage/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var farewellMessage = await _context.FarewellMessages
            .Include(m => m.FarewellPerson)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (farewellMessage == null)
        {
            return NotFound();
        }

        return View(farewellMessage);
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
            
            // Redirect to the person's details page
            return RedirectToAction("Details", "FarewellPerson", new { id = viewModel.FarewellPersonId });
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
        
        return View(viewModel);
    }

    // GET: FarewellMessage/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var farewellMessage = await _context.FarewellMessages.FindAsync(id);
        if (farewellMessage == null)
        {
            return NotFound();
        }

        var viewModel = new CreateFarewellMessageViewModel
        {
            Message = farewellMessage.Message,
            AuthorName = farewellMessage.AuthorName,
            AuthorEmail = farewellMessage.AuthorEmail,
            FarewellPersonId = farewellMessage.FarewellPersonId
        };

        var farewellPerson = await _context.FarewellPeople.FindAsync(farewellMessage.FarewellPersonId);
        if (farewellPerson != null)
        {
            viewModel.FarewellPersonName = farewellPerson.Name;
        }

        return View(viewModel);
    }

    // POST: FarewellMessage/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Message,AuthorName,AuthorEmail,FarewellPersonId")] CreateFarewellMessageViewModel viewModel)
    {
        if (id == 0)
        {
            return NotFound();
        }

        var farewellMessage = await _context.FarewellMessages.FindAsync(id);
        if (farewellMessage == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                farewellMessage.Message = viewModel.Message;
                farewellMessage.AuthorName = viewModel.AuthorName;
                farewellMessage.AuthorEmail = viewModel.AuthorEmail;
                farewellMessage.FarewellPersonId = viewModel.FarewellPersonId ?? 0;

                _context.Update(farewellMessage);
                await _context.SaveChangesAsync();
                
                // Redirect to the person's details page
                return RedirectToAction("Details", "FarewellPerson", new { id = viewModel.FarewellPersonId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FarewellMessageExists(farewellMessage.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
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
        
        return View(viewModel);
    }

    // GET: FarewellMessage/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var farewellMessage = await _context.FarewellMessages
            .Include(m => m.FarewellPerson)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (farewellMessage == null)
        {
            return NotFound();
        }

        return View(farewellMessage);
    }

    // POST: FarewellMessage/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var farewellMessage = await _context.FarewellMessages.FindAsync(id);
        if (farewellMessage != null)
        {
            var farewellPersonId = farewellMessage.FarewellPersonId;
            _context.FarewellMessages.Remove(farewellMessage);
            await _context.SaveChangesAsync();
            
            // Redirect to the person's details page
            return RedirectToAction("Details", "FarewellPerson", new { id = farewellPersonId });
        }

        return RedirectToAction(nameof(Index));
    }

    private bool FarewellMessageExists(int id)
    {
        return _context.FarewellMessages.Any(e => e.Id == id);
    }
}