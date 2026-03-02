using System.ComponentModel.DataAnnotations;
using CondiService.Web.Data;
using CondiService.Web.Models;
using CondiService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CondiService.Web.Pages.Requests;

[Authorize(Roles = SeedData.RoleOperator + "," + SeedData.RoleManagerName)]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(60)]
        public string EquipmentType { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string DeviceModel { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string ProblemDescription { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string CustomerFullName { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var nextId = (_db.RepairRequests.Any() ? _db.RepairRequests.Max(r => r.Id) : 0) + 1;

        _db.RepairRequests.Add(new RepairRequest
        {
            Id = nextId,
            CreatedAt = DateTime.UtcNow,
            EquipmentType = Input.EquipmentType.Trim(),
            DeviceModel = Input.DeviceModel.Trim(),
            ProblemDescription = Input.ProblemDescription.Trim(),
            CustomerFullName = Input.CustomerFullName.Trim(),
            CustomerPhone = Input.CustomerPhone.Trim(),
            Status = RequestStatus.New,
            DueAt = DateTime.UtcNow.AddDays(3)
        });

        await _db.SaveChangesAsync();
        TempData[NotificationService.TempDataKey] = NotificationService.Success($"Заявка №{nextId} создана.");
        return RedirectToPage("/Requests/Details", new { id = nextId });
    }
}
