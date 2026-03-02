using CondiService.Web.Data;
using CondiService.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages.Requests;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<RepairRequest> Items { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? type { get; set; }

    public string Query => q?.Trim() ?? string.Empty;
    public string StatusFilter => status?.Trim() ?? string.Empty;
    public string TypeFilter => type?.Trim() ?? string.Empty;

    public List<SelectListItem> StatusOptions { get; } = new()
    {
        new("Новая", ((int)RequestStatus.New).ToString()),
        new("В работе", ((int)RequestStatus.InProgress).ToString()),
        new("Ожидание комплектующих", ((int)RequestStatus.WaitingParts).ToString()),
        new("Завершена", ((int)RequestStatus.Completed).ToString()),
    };

    public async Task OnGetAsync()
    {
        var query = _db.RepairRequests
            .Include(r => r.AssignedSpecialist)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Query))
        {
            if (int.TryParse(Query, out var id))
                query = query.Where(r => r.Id == id);
            else
                query = query.Where(r => r.DeviceModel.Contains(Query) || r.ProblemDescription.Contains(Query) || r.EquipmentType.Contains(Query));
        }

        if (!string.IsNullOrWhiteSpace(TypeFilter))
            query = query.Where(r => r.EquipmentType.Contains(TypeFilter));

        if (!string.IsNullOrWhiteSpace(StatusFilter) && int.TryParse(StatusFilter, out var st))
            query = query.Where(r => (int)r.Status == st);

        Items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public string StatusBadge(RequestStatus status)
    {
		return status switch
		{
			RequestStatus.New => "<span class=\"badge badge--new\">Новая</span>",
			RequestStatus.InProgress => "<span class=\"badge badge--progress\">В работе</span>",
			RequestStatus.WaitingParts => "<span class=\"badge badge--waiting\">Ожидание комплектующих</span>",
			RequestStatus.Completed => "<span class=\"badge badge--done\">Завершена</span>",
			_ => $"<span class=\"badge\">{status}</span>"
		};
    }
}
