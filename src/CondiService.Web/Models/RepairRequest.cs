using System.ComponentModel.DataAnnotations;

namespace CondiService.Web.Models;

public class RepairRequest
{
    [Key]
    public int Id { get; set; } // "Номер заявки"

    [Required]
    [Display(Name = "Дата добавления")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(60)]
    [Display(Name = "Тип оборудования")]
    public string EquipmentType { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Модель устройства")]
    public string DeviceModel { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Описание проблемы")]
    public string ProblemDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "ФИО заказчика")]
    public string CustomerFullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Телефон")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Display(Name = "Статус")]
    public RequestStatus Status { get; set; } = RequestStatus.New;

    [Display(Name = "Дата завершения")]
    public DateTime? CompletedAt { get; set; }

    [Display(Name = "Комплектующие")]
    public string? RepairParts { get; set; }

    // Specialist assignment
    public string? AssignedSpecialistId { get; set; }
    public ApplicationUser? AssignedSpecialist { get; set; }

    // Customer link (optional)
    public string? CustomerUserId { get; set; }
    public ApplicationUser? CustomerUser { get; set; }

    public List<RequestComment> Comments { get; set; } = new();
}
