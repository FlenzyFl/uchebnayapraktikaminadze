using CondiService.Web.Data;
using CondiService.Web.Models;
using CondiService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages.Requests;

[Authorize(Roles = SeedData.RoleManagerName)]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db)
    {
        _db = db;
    }

    public RepairRequest Item { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _db.RepairRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (item == null)
            return NotFound();

        Item = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var item = await _db.RepairRequests.Include(r => r.Comments).FirstOrDefaultAsync(r => r.Id == id);
        if (item == null)
            return NotFound();

        _db.RepairRequests.Remove(item);
        await _db.SaveChangesAsync();

        TempData[NotificationService.TempDataKey] = NotificationService.Success($"Заявка №{id} удалена.");
        return RedirectToPage("/Requests/Index");
    }
}
