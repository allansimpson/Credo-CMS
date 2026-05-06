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

    // --- Phase 4: profile fields ---
    // Note: PhoneNumber is inherited from IdentityUser<Guid>. Max-length is enforced
    // via FluentValidation on the profile API input model rather than [MaxLength] here
    // (which would conflict with Identity's column definition).

    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? StateOrRegion { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(2000)]
    public string? PhotoBlobUrl { get; set; }

    [MaxLength(2000)]
    public string? PhotoWebpBlobUrl { get; set; }

    [MaxLength(500)]
    public string? PhotoAltText { get; set; }

    /// <summary>Optional public author bio (ProseMirror JSON). Surfaced on the
    /// blog author archive when this user has at least one published post.</summary>
    public string? PublicAuthorBio { get; set; }

    // --- Phase 4: directory opt-in (default OFF) ---
    public bool IsListedInDirectory { get; set; }
    public bool ShowEmailInDirectory { get; set; }
    public bool ShowPhoneInDirectory { get; set; }
    public bool ShowAddressInDirectory { get; set; }
    public bool ShowPhotoInDirectory { get; set; }

    // --- Phase 4: notification preferences ---
    public bool ReceiveNewsEmails { get; set; } = true;
    public bool ReceiveBlogEmails { get; set; }
    public bool ReceiveBroadcastEmails { get; set; } = true;
    public bool ReceiveGroupEmailsGlobal { get; set; } = true;
}
