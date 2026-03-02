using CondiService.Web.Data;
using CondiService.Web.Models;
using CondiService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages.Stats;

[Authorize(Roles = SeedData.RoleManagerName)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public string AverageCompletionTime { get; set; } = "–";

    public class ProblemStatRow
    {
        public string Problem { get; set; } = "";
        public int Count { get; set; }
    }

    public List<ProblemStatRow> ProblemStats { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalCount = await _db.RepairRequests.CountAsync();

        var completed = await _db.RepairRequests
            .Where(r => r.Status == RequestStatus.Completed && r.CompletedAt != null)
            .ToListAsync();

        CompletedCount = completed.Count;

        if (CompletedCount > 0)
        {
            var avg = completed
                .Select(r => (r.CompletedAt!.Value - r.CreatedAt).TotalHours)
                .Average();

            var days = avg / 24.0;
            AverageCompletionTime = days >= 1
                ? $"{days:F1} дн."
                : $"{avg:F1} ч.";
        }

        ProblemStats = await _db.RepairRequests
            .Where(r => !string.IsNullOrWhiteSpace(r.ProblemDescription))
            .GroupBy(r => r.ProblemDescription.Trim())
            .Select(g => new { Problem = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Problem)
            .Take(20)
            .Select(x => new ProblemStatRow { Problem = x.Problem, Count = x.Count })
            .ToListAsync();
    }
}
