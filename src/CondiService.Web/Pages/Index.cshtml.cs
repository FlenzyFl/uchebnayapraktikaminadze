using CondiService.Web.Data;
using CondiService.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public int Total { get; set; }
    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int WaitingCount { get; set; }
    public int CompletedCount { get; set; }

    public async Task OnGetAsync()
    {
        Total = await _db.RepairRequests.CountAsync();
        NewCount = await _db.RepairRequests.CountAsync(r => r.Status == RequestStatus.New);
        InProgressCount = await _db.RepairRequests.CountAsync(r => r.Status == RequestStatus.InProgress);
        WaitingCount = await _db.RepairRequests.CountAsync(r => r.Status == RequestStatus.WaitingParts);
        CompletedCount = await _db.RepairRequests.CountAsync(r => r.Status == RequestStatus.Completed);
    }
}
