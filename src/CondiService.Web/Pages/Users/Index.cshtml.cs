using CondiService.Web.Data;
using CondiService.Web.Models;
using CondiService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages.Users;

[Authorize(Roles = SeedData.RoleManagerName)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Row> Users { get; set; } = new();

    public record Row(int? ExternalUserId, string FullName, string Phone, string Login, string Role);

    public async Task OnGetAsync()
    {
        var users = await _db.Users.ToListAsync();

        var roles = await _db.Roles.ToListAsync();
        var userRoles = await _db.UserRoles.ToListAsync();

        var roleById = roles.ToDictionary(r => r.Id, r => r.Name ?? "");

        Users = users
            .Select(u =>
            {
                var ur = userRoles.FirstOrDefault(x => x.UserId == u.Id);
                var roleName = ur != null && roleById.TryGetValue(ur.RoleId, out var rn) ? rn : "";
                return new Row(u.ExternalUserId, u.FullName, u.PhoneNumber ?? "", u.UserName ?? "", roleName);
            })
            .OrderBy(r => r.ExternalUserId ?? int.MaxValue)
            .ToList();
    }
}
