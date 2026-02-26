using Microsoft.AspNetCore.Identity;

namespace CondiService.Web.Models;

public class ApplicationUser : IdentityUser
{
    // From imported dataset (userID)
    public int? ExternalUserId { get; set; }

    public string FullName { get; set; } = string.Empty;
}
