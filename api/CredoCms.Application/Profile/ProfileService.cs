using CredoCms.Application.Common;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CredoCms.Application.Profile;

/// <summary>
/// Profile service. The four mutators all share a "load → guard → patch →
/// audit" shape; rather than abstracting that into a helper, the symmetry is
/// kept literal so each section reads top-to-bottom without indirection.
/// </summary>
public sealed class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogger _audit;

    public ProfileService(UserManager<ApplicationUser> userManager, IAuditLogger audit)
    {
        _userManager = userManager;
        _audit = audit;
    }

    public async Task<ProfileDto?> GetProfileAsync(Guid callerUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(callerUserId.ToString());
        return user is null ? null : ToDto(user);
    }

    public async Task<ProfileMutationResult> UpdatePersonalInfoAsync(
        Guid callerUserId,
        UpdatePersonalInfoRequest request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(callerUserId.ToString());
        if (user is null) return ProfileMutationResult.Failure("Profile not found.");

        // Photo alt text is required when a photo is set so the directory
        // listing has accessible text. Empty alt with a photo present is a
        // service-level invariant (validators handle the symmetric case).
        if (!string.IsNullOrWhiteSpace(request.PhotoBlobUrl)
            && string.IsNullOrWhiteSpace(request.PhotoAltText))
        {
            return ProfileMutationResult.Failure("Alt text is required when a photo is set.");
        }

        user.PhoneNumber = NullIfBlank(request.PhoneNumber);
        user.AddressLine1 = NullIfBlank(request.AddressLine1);
        user.AddressLine2 = NullIfBlank(request.AddressLine2);
        user.City = NullIfBlank(request.City);
        user.StateOrRegion = NullIfBlank(request.StateOrRegion);
        user.PostalCode = NullIfBlank(request.PostalCode);
        user.Country = NullIfBlank(request.Country);
        user.PhotoBlobUrl = NullIfBlank(request.PhotoBlobUrl);
        user.PhotoWebpBlobUrl = NullIfBlank(request.PhotoWebpBlobUrl);
        user.PhotoAltText = NullIfBlank(request.PhotoAltText);
        user.PublicAuthorBio = NullIfBlank(request.PublicAuthorBio);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ProfileMutationResult.Failure([.. updateResult.Errors.Select(e => e.Description)]);
        }

        await _audit.WriteAsync(
            "Profile.PersonalInfoUpdated",
            nameof(ApplicationUser),
            user.Id.ToString(),
            details: new
            {
                user.PhoneNumber,
                user.City,
                user.StateOrRegion,
                user.Country,
                hasPhoto = !string.IsNullOrEmpty(user.PhotoBlobUrl),
                hasBio = !string.IsNullOrEmpty(user.PublicAuthorBio),
            },
            cancellationToken: ct);

        return ProfileMutationResult.Success(ToDto(user));
    }

    public async Task<ProfileMutationResult> UpdateDirectoryAsync(
        Guid callerUserId,
        UpdateDirectoryRequest request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(callerUserId.ToString());
        if (user is null) return ProfileMutationResult.Failure("Profile not found.");

        // Master toggle gates per-field toggles. Forcing them off when the
        // master is off means /api/members can rely on a single AND check.
        var listed = request.IsListedInDirectory;
        user.IsListedInDirectory = listed;
        user.ShowEmailInDirectory = listed && request.ShowEmailInDirectory;
        user.ShowPhoneInDirectory = listed && request.ShowPhoneInDirectory;
        user.ShowAddressInDirectory = listed && request.ShowAddressInDirectory;
        user.ShowPhotoInDirectory = listed && request.ShowPhotoInDirectory;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ProfileMutationResult.Failure([.. updateResult.Errors.Select(e => e.Description)]);
        }

        await _audit.WriteAsync(
            "Profile.DirectoryUpdated",
            nameof(ApplicationUser),
            user.Id.ToString(),
            details: new
            {
                user.IsListedInDirectory,
                user.ShowEmailInDirectory,
                user.ShowPhoneInDirectory,
                user.ShowAddressInDirectory,
                user.ShowPhotoInDirectory,
            },
            cancellationToken: ct);

        return ProfileMutationResult.Success(ToDto(user));
    }

    public async Task<ProfileMutationResult> UpdateNotificationsAsync(
        Guid callerUserId,
        UpdateNotificationsRequest request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(callerUserId.ToString());
        if (user is null) return ProfileMutationResult.Failure("Profile not found.");

        user.ReceiveNewsEmails = request.ReceiveNewsEmails;
        user.ReceiveBlogEmails = request.ReceiveBlogEmails;
        user.ReceiveBroadcastEmails = request.ReceiveBroadcastEmails;
        user.ReceiveGroupEmailsGlobal = request.ReceiveGroupEmailsGlobal;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ProfileMutationResult.Failure([.. updateResult.Errors.Select(e => e.Description)]);
        }

        await _audit.WriteAsync(
            "Profile.NotificationsUpdated",
            nameof(ApplicationUser),
            user.Id.ToString(),
            details: new
            {
                user.ReceiveNewsEmails,
                user.ReceiveBlogEmails,
                user.ReceiveBroadcastEmails,
                user.ReceiveGroupEmailsGlobal,
            },
            cancellationToken: ct);

        return ProfileMutationResult.Success(ToDto(user));
    }

    internal static ProfileDto ToDto(ApplicationUser u) => new(
        u.Id,
        u.Email ?? string.Empty,
        u.FirstName,
        u.LastName,
        u.DisplayName,
        u.PhoneNumber,
        u.AddressLine1,
        u.AddressLine2,
        u.City,
        u.StateOrRegion,
        u.PostalCode,
        u.Country,
        u.PhotoBlobUrl,
        u.PhotoWebpBlobUrl,
        u.PhotoAltText,
        u.PublicAuthorBio,
        u.IsListedInDirectory,
        u.ShowEmailInDirectory,
        u.ShowPhoneInDirectory,
        u.ShowAddressInDirectory,
        u.ShowPhotoInDirectory,
        u.ReceiveNewsEmails,
        u.ReceiveBlogEmails,
        u.ReceiveBroadcastEmails,
        u.ReceiveGroupEmailsGlobal);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
