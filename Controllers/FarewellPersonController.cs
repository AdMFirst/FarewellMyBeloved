using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarewellMyBeloved.Models;
using FarewellMyBeloved.ViewModels;
using System.Threading.Tasks;

namespace FarewellMyBeloved.Controllers;

public class FarewellPersonController : Controller
{
    private readonly ApplicationDbContext _context;

    public FarewellPersonController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: FarewellPerson
    public async Task<IActionResult> Index()
    {
        var farewellPeople = await _context.FarewellPeople
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(farewellPeople);
    }

    // GET: FarewellPerson/Search
    public async Task<IActionResult> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return RedirectToAction("Index");
        }

        var searchResults = await _context.FarewellPeople
            .Where(p => p.IsPublic &&
                       (p.Name.Contains(searchTerm) ||
                        p.Description.Contains(searchTerm)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            
        ViewBag.SearchTerm = searchTerm;
        return View("Search", searchResults);
    }

    // GET: FarewellPerson/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var farewellPerson = await _context.FarewellPeople
            .Include(p => p.Messages)
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (farewellPerson == null)
        {
            return NotFound();
        }

        return View(farewellPerson);
    }

    // GET: FarewellPerson/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: FarewellPerson/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,PortraitUrl,BackgroundUrl")] CreateFarewellPersonViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var farewellPerson = new FarewellPerson
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                PortraitUrl = viewModel.PortraitUrl,
                BackgroundUrl = viewModel.BackgroundUrl,
                Slug = GenerateSlug(viewModel.Name),
                IsPublic = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(farewellPerson);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(viewModel);
    }

    // GET: FarewellPerson/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var farewellPerson = await _context.FarewellPeople.FindAsync(id);
        if (farewellPerson == null)
        {
            return NotFound();
        }
        
        var viewModel = new CreateFarewellPersonViewModel
        {
            Name = farewellPerson.Name,
            Description = farewellPerson.Description,
            PortraitUrl = farewellPerson.PortraitUrl,
            BackgroundUrl = farewellPerson.BackgroundUrl
        };
        
        return View(viewModel);
    }

    // POST: FarewellPerson/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Description,PortraitUrl,BackgroundUrl")] CreateFarewellPersonViewModel viewModel)
    {
        if (id == 0)
        {
            return NotFound();
        }

        var farewellPerson = await _context.FarewellPeople.FindAsync(id);
        if (farewellPerson == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                farewellPerson.Name = viewModel.Name;
                farewellPerson.Description = viewModel.Description;
                farewellPerson.PortraitUrl = viewModel.PortraitUrl;
                farewellPerson.BackgroundUrl = viewModel.BackgroundUrl;
                farewellPerson.Slug = GenerateSlug(viewModel.Name);
                farewellPerson.UpdatedAt = DateTime.UtcNow;

                _context.Update(farewellPerson);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FarewellPersonExists(farewellPerson.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(viewModel);
    }

    // GET: FarewellPerson/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var farewellPerson = await _context.FarewellPeople
            .FirstOrDefaultAsync(m => m.Id == id);
        if (farewellPerson == null)
        {
            return NotFound();
        }

        return View(farewellPerson);
    }

    // POST: FarewellPerson/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var farewellPerson = await _context.FarewellPeople.FindAsync(id);
        if (farewellPerson != null)
        {
            _context.FarewellPeople.Remove(farewellPerson);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool FarewellPersonExists(int id)
    {
        return _context.FarewellPeople.Any(e => e.Id == id);
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