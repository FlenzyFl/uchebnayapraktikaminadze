using System.ComponentModel.DataAnnotations;
using CondiService.Web.Data;
using CondiService.Web.Models;
using CondiService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Pages.Requests;

[Authorize]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public RepairRequest Item { get; set; } = null!;

    public List<SelectListItem> StatusItems { get; } = new()
    {
        new("Новая", RequestStatus.New.ToString()),
        new("В работе", RequestStatus.InProgress.ToString()),
        new("Ожидание комплектующих", RequestStatus.WaitingParts.ToString()),
        new("Завершена", RequestStatus.Completed.ToString()),
    };

    public List<SelectListItem> SpecialistItems { get; set; } = new();

    public bool CanEditCustomerFields { get; set; }

    [BindProperty]
    public bool DueExtensionApprovedInput { get; set; }

    [BindProperty]
    public string? DueExtensionCommentInput { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!CanEdit())
            return Forbid();

        var item = await _db.RepairRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (item == null)
            return NotFound();

        Item = item;
        CanEditCustomerFields = User.IsInRole(SeedData.RoleOperator) || User.IsInRole(SeedData.RoleManagerName);
        DueExtensionApprovedInput = Item.DueExtensionApproved;
        DueExtensionCommentInput = Item.DueExtensionComment;

        var specialistsRaw = await _db.Users
            .AsNoTracking()
            .Join(_db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur.RoleId })
            .Join(_db.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.u, Role = r.Name })
            .Where(x => x.Role == SeedData.RoleSpecialist)
            .Select(x => new { x.u.Id, x.u.FullName, x.u.UserName })
            .ToListAsync();

        SpecialistItems = specialistsRaw
            .Select(u => new SelectListItem($"{u.FullName} ({u.UserName})", u.Id))
            .OrderBy(x => x.Text)
            .ToList();
return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!CanEdit())
            return Forbid();

        var dbItem = await _db.RepairRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (dbItem == null)
            return NotFound();

        var canEditCustomer = User.IsInRole(SeedData.RoleOperator) || User.IsInRole(SeedData.RoleManagerName);

        // Validation
        if (string.IsNullOrWhiteSpace(Item.ProblemDescription))
            ModelState.AddModelError("Item.ProblemDescription", "Описание проблемы обязательно.");

        if (canEditCustomer)
        {
            if (string.IsNullOrWhiteSpace(Item.EquipmentType))
                ModelState.AddModelError("Item.EquipmentType", "Тип оборудования обязателен.");
            if (string.IsNullOrWhiteSpace(Item.DeviceModel))
                ModelState.AddModelError("Item.DeviceModel", "Модель устройства обязательна.");
            if (string.IsNullOrWhiteSpace(Item.CustomerFullName))
                ModelState.AddModelError("Item.CustomerFullName", "ФИО заказчика обязательно.");
            if (string.IsNullOrWhiteSpace(Item.CustomerPhone))
                ModelState.AddModelError("Item.CustomerPhone", "Телефон обязателен.");
        }

        if (!ModelState.IsValid)
        {
            return await OnGetAsync(id);
        }

        var prevStatus = dbItem.Status;

        var isQualityManager = User.IsInRole(SeedData.RoleQualityManager);
        var prevDueAt = dbItem.DueAt;
        var newDueAt = Item.DueAt;

        var deadlineChanged = newDueAt.Date != prevDueAt.Date;

        if (isQualityManager && deadlineChanged && newDueAt.Date > prevDueAt.Date && !DueExtensionApprovedInput)
            ModelState.AddModelError("DueExtensionApprovedInput", "Для продления срока требуется согласование с заказчиком.");

        dbItem.Status = Item.Status;
        dbItem.AssignedSpecialistId = string.IsNullOrWhiteSpace(Item.AssignedSpecialistId) ? null : Item.AssignedSpecialistId;
        dbItem.ProblemDescription = Item.ProblemDescription.Trim();
        dbItem.RepairParts = string.IsNullOrWhiteSpace(Item.RepairParts) ? null : Item.RepairParts.Trim();

        if (canEditCustomer)
        {
            dbItem.EquipmentType = Item.EquipmentType.Trim();
            dbItem.DeviceModel = Item.DeviceModel.Trim();
            dbItem.CustomerFullName = Item.CustomerFullName.Trim();
            dbItem.CustomerPhone = Item.CustomerPhone.Trim();
        }

        if (dbItem.Status == RequestStatus.Completed && dbItem.CompletedAt == null)
            dbItem.CompletedAt = DateTime.UtcNow;
        if (dbItem.Status != RequestStatus.Completed)
            dbItem.CompletedAt = null;

        await _db.SaveChangesAsync();

        if (prevStatus != dbItem.Status)
            TempData[NotificationService.TempDataKey] = NotificationService.Info($"Статус изменён: {ToRu(prevStatus)} → {ToRu(dbItem.Status)}");
        else
            TempData[NotificationService.TempDataKey] = NotificationService.Success("Изменения сохранены.");

        return RedirectToPage("/Requests/Details", new { id });
    }

    private bool CanEdit() => User.IsInRole(SeedData.RoleOperator) || User.IsInRole(SeedData.RoleManagerName) || User.IsInRole(SeedData.RoleSpecialist) || User.IsInRole(SeedData.RoleQualityManager);

    private static string ToRu(RequestStatus st) => st switch
    {
        RequestStatus.New => "Новая",
        RequestStatus.InProgress => "В работе",
        RequestStatus.WaitingParts => "Ожидание комплектующих",
        RequestStatus.Completed => "Завершена",
        _ => st.ToString()
    };
}
