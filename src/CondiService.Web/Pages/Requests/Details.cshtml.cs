using CondiService.Web.Data;
using CondiService.Web.Models;
using CondiService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages.Requests;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public RepairRequest Item { get; set; } = null!;

    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanComment { get; set; }
    public bool CanHelpRequest { get; set; }
    public const string SurveyUrl = "https://docs.google.com/forms/d/e/1FAIpQLSdhZcExx6LSIXxk0ub55mSu-WIh23WYdGG9HY5EZhLDo7P8eA/viewform?usp=sf_link";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _db.RepairRequests
            .Include(r => r.AssignedSpecialist)
            .Include(r => r.Comments)
                .ThenInclude(c => c.AuthorUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (item == null)
            return NotFound();

        Item = item;

        CanEdit = User.IsInRole(SeedData.RoleOperator) || User.IsInRole(SeedData.RoleManagerName) || User.IsInRole(SeedData.RoleSpecialist) || User.IsInRole(SeedData.RoleQualityManager);
        CanDelete = User.IsInRole(SeedData.RoleManagerName);
        CanComment = User.IsInRole(SeedData.RoleSpecialist) || User.IsInRole(SeedData.RoleManagerName) || User.IsInRole(SeedData.RoleQualityManager);

        var me = await _userManager.GetUserAsync(User);
        CanHelpRequest = User.IsInRole(SeedData.RoleSpecialist) && me != null && Item.AssignedSpecialistId == me.Id && Item.Status != RequestStatus.Completed;

        return Page();
    }

    public async Task<IActionResult> OnPostAddCommentAsync(int id, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData[NotificationService.TempDataKey] = NotificationService.Warning("Комментарий пустой – ничего не добавлено.");
            return RedirectToPage(new { id });
        }

        if (!User.IsInRole(SeedData.RoleSpecialist) && !User.IsInRole(SeedData.RoleManagerName))
            return Forbid();

        var req = await _db.RepairRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (req == null)
            return NotFound();

        var me = await _userManager.GetUserAsync(User);
        if (me == null)
            return Forbid();

        _db.RequestComments.Add(new RequestComment
        {
            RepairRequestId = id,
            AuthorUserId = me.Id,
            Message = message.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData[NotificationService.TempDataKey] = NotificationService.Success("Комментарий добавлен.");
        return RedirectToPage(new { id });
    }

    public string StatusBadge(RequestStatus status)
    {
        var (css, text) = status switch
        {
            RequestStatus.New => ("badge badge--new", "Новая"),
            RequestStatus.InProgress => ("badge badge--progress", "В работе"),
            RequestStatus.WaitingParts => ("badge badge--waiting", "Ожидание комплектующих"),
            RequestStatus.Completed => ("badge badge--done", "Завершена"),
            _ => ("badge", status.ToString())
        };
        return $"<span class=\"{css}\">{text}</span>";
    }


    public async Task<IActionResult> OnPostHelpAsync(int id, string message)
    {
        var item = await _db.RepairRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (item == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Forbid();


        if (!User.IsInRole(SeedData.RoleSpecialist) || item.AssignedSpecialistId != user.Id)
            return Forbid();

        var text = string.IsNullOrWhiteSpace(message) ? "Нужна помощь: не получается выполнить ремонт в срок/по технической причине." : message.Trim();

        var comment = new RequestComment
        {
            RepairRequestId = id,
            AuthorUserId = user.Id,
            Message = text,
            Type = CommentType.HelpRequest,
            CreatedAt = DateTime.UtcNow
        };

        _db.RequestComments.Add(comment);
        await _db.SaveChangesAsync();

        TempData[NotificationService.TempDataKey] = NotificationService.Warning("Запрос помощи отправлен менеджеру по качеству.");
        return RedirectToPage(new { id });
    }

    public IActionResult OnGetSurveyQr(int id)
    {
        var payload = SurveyUrl;
        var bytes = QrCodeService.GeneratePng(payload, 8);
        return File(bytes, "image/png");
    }

}