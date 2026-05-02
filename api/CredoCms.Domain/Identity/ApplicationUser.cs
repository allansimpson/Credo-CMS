using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CredoCms.Domain.Identity;

/// <summary>
/// Application user. Extends Identity's <see cref="IdentityUser{Guid}"/> with the
/// Credo-specific profile fields. Excluded from the temporal-versioning system by
/// explicit project decision (privacy).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Soft-deactivation flag. Deactivated users cannot sign in; existing rows that
    /// reference them via foreign key remain valid.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When true, the user must change their password on next sign-in before the rest
    /// of the application is accessible. Set on the seeded default Administrator and
    /// on accounts created via the admin invitation flow.
    /// </summary>
    public bool RequirePasswordChangeOnFirstLogin { get; set; }
}
