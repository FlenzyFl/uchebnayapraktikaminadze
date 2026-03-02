using CondiService.Web.Data;
using CondiService.Web.Models;
using CondiService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages.Quality;

[Authorize(Roles = SeedData.RoleQualityManager)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<RepairRequest> Overdue { get; set; } = new();
    public List<(RepairRequest req, RequestComment comment)> HelpRequests { get; set; } = new();

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;

        Overdue = await _db.RepairRequests
            .AsNoTracking()
            .Include(r => r.AssignedSpecialist)
            .Where(r => r.Status != RequestStatus.Completed && r.DueAt < now)
            .OrderBy(r => r.DueAt)
            .Take(50)
            .ToListAsync();

        var help = await _db.RequestComments
            .AsNoTracking()
            .Include(c => c.RepairRequest!)
                .ThenInclude(r => r.AssignedSpecialist)
            .Include(c => c.AuthorUser)
            .Where(c => c.Type == CommentType.HelpRequest)
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .ToListAsync();

        HelpRequests = help
            .Where(c => c.RepairRequest != null)
            .Select(c => (c.RepairRequest!, c))
            .ToList();
    }
}
