using System.ComponentModel.DataAnnotations;

namespace CondiService.Web.Models;

public class RequestComment
{
    public int Id { get; set; }

    [Required]
    public int RepairRequestId { get; set; }
    public RepairRequest? RepairRequest { get; set; }

    [Required]
    public string AuthorUserId { get; set; } = string.Empty;
    public ApplicationUser? AuthorUser { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "Комментарий")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "Дата")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
