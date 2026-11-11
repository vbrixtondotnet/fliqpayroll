using Microsoft.AspNetCore.Identity;

namespace FliqPayroll.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>True when the user still has to change their password (defaults to true for every new and existing user).</summary>
    public bool PasswordChange { get; set; } = true;
}
