using CredoCms.Application.Common;
using CredoCms.Domain.Common;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CredoCms.Application.UserManagement;

/// <summary>
/// Admin user-management service. Wraps <see cref="UserManager{TUser}"/> with the
/// invitation flow, role updates, force-logout (security-stamp rotation), and audit
/// logging. Exposed only to Administrator-role API endpoints.
/// </summary>
public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAdminQueries _queries;
    private readonly IInvitationEmailComposer _emailComposer;
    private readonly IEmailService _email;
    private readonly IAuditLogger _audit;
    private readonly Email.IEmailBroadcastRecipientRepository _broadcastRecipients;

    public UserAdminService(
        UserManager<ApplicationUser> userManager,
        IUserAdminQueries queries,
        IInvitationEmailComposer emailComposer,
        IEmailService email,
        IAuditLogger audit,
        Email.IEmailBroadcastRecipientRepository broadcastRecipients)
    {
        _userManager = userManager;
        _queries = queries;
        _emailComposer = emailComposer;
        _email = email;
        _audit = audit;
        _broadcastRecipients = broadcastRecipients;
    }

    public Task<PagedResult<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken ct = default)
    {
        var safe = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
        };
        return _queries.ListAsync(safe, ct);
    }

    public Task<UserDetailDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        _queries.GetAsync(id, ct);

    public async Task<UserMutationResult> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            return UserMutationResult.Failure("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false,
            IsActive = true,
            RequirePasswordChangeOnFirstLogin = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return UserMutationResult.Failure([.. createResult.Errors.Select(e => e.Description)]);
        }

        if (request.Roles.Count > 0)
        {
            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
            {
                return UserMutationResult.Failure([.. roleResult.Errors.Select(e => e.Description)]);
            }
        }

        if (request.SendInvitation)
        {
            var invitationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var message = await _emailComposer.ComposeInvitationAsync(user, invitationToken, ct);
            await _email.SendTransactionalAsync(message, ct);
        }

        await _audit.WriteAsync(
            "User.Created",
            nameof(ApplicationUser),
            user.Id.ToString(),
            details: new { user.Email, user.FirstName, user.LastName, request.Roles, request.SendInvitation },
            cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("User created but query failed to find it.");

        return UserMutationResult.Success(detail);
    }

    public async Task<UserMutationResult> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return UserMutationResult.Failure("User not found.");
        }

        var emailChanged = !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            user.Email = request.Email;
            user.UserName = request.Email;
            user.NormalizedEmail = _userManager.NormalizeEmail(request.Email);
            user.NormalizedUserName = _userManager.NormalizeName(request.Email);
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return UserMutationResult.Failure([.. updateResult.Errors.Select(e => e.Description)]);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = request.Roles.Except(currentRoles, StringComparer.Ordinal).ToList();
        var rolesToRemove = currentRoles.Except(request.Roles, StringComparer.Ordinal).ToList();

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return UserMutationResult.Failure([.. addResult.Errors.Select(e => e.Description)]);
            }
        }

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return UserMutationResult.Failure([.. removeResult.Errors.Select(e => e.Description)]);
            }
        }

        // If the user was deactivated, rotate their security stamp so any open
        // sessions are invalidated on the next validation tick.
        if (!user.IsActive)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        await _audit.WriteAsync(
            "User.Updated",
            nameof(ApplicationUser),
            user.Id.ToString(),
            details: new { user.Email, user.FirstName, user.LastName, request.IsActive, rolesToAdd, rolesToRemove },
            cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("User updated but query failed to find it.");

        return UserMutationResult.Success(detail);
    }

    public async Task<UserMutationResult> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return UserMutationResult.Failure("User not found.");
        }

        user.IsActive = false;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return UserMutationResult.Failure([.. update.Errors.Select(e => e.Description)]);
        }
        await _userManager.UpdateSecurityStampAsync(user);

        await _audit.WriteAsync("User.Deactivated", nameof(ApplicationUser), user.Id.ToString(), cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("User deactivated but query failed.");
        return UserMutationResult.Success(detail);
    }

    public async Task<UserMutationResult> ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return UserMutationResult.Failure("User not found.");
        }

        user.IsActive = true;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return UserMutationResult.Failure([.. update.Errors.Select(e => e.Description)]);
        }

        await _audit.WriteAsync("User.Reactivated", nameof(ApplicationUser), user.Id.ToString(), cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("User reactivated but query failed.");
        return UserMutationResult.Success(detail);
    }

    public async Task<UserMutationResult> ForceLogoutAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return UserMutationResult.Failure("User not found.");
        }

        await _userManager.UpdateSecurityStampAsync(user);

        await _audit.WriteAsync("User.ForceLoggedOut", nameof(ApplicationUser), user.Id.ToString(), cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("Force-logout succeeded but query failed.");
        return UserMutationResult.Success(detail);
    }

    public async Task<UserMutationResult> SendPasswordResetEmailAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return UserMutationResult.Failure("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var message = await _emailComposer.ComposePasswordResetAsync(user, token, ct);
        await _email.SendTransactionalAsync(message, ct);

        await _audit.WriteAsync(
            "User.PasswordResetEmailSent",
            nameof(ApplicationUser),
            user.Id.ToString(),
            cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("Reset-email succeeded but query failed.");
        return UserMutationResult.Success(detail);
    }

    public async Task<UserMutationResult> UpdateProfileFieldsAsync(Guid id, UpdateUserProfileFieldsRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return UserMutationResult.Failure("User not found.");

        // Admin-edit mirrors the self-profile contract: photo without alt is
        // an invariant violation, master directory toggle gates per-field.
        if (!string.IsNullOrWhiteSpace(request.PhotoBlobUrl)
            && string.IsNullOrWhiteSpace(request.PhotoAltText))
        {
            return UserMutationResult.Failure("Alt text is required when a photo is set.");
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

        var listed = request.IsListedInDirectory;
        user.IsListedInDirectory = listed;
        user.ShowEmailInDirectory = listed && request.ShowEmailInDirectory;
        user.ShowPhoneInDirectory = listed && request.ShowPhoneInDirectory;
        user.ShowAddressInDirectory = listed && request.ShowAddressInDirectory;
        user.ShowPhotoInDirectory = listed && request.ShowPhotoInDirectory;

        user.ReceiveNewsEmails = request.ReceiveNewsEmails;
        user.ReceiveBlogEmails = request.ReceiveBlogEmails;
        user.ReceiveBroadcastEmails = request.ReceiveBroadcastEmails;
        user.ReceiveGroupEmailsGlobal = request.ReceiveGroupEmailsGlobal;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return UserMutationResult.Failure([.. update.Errors.Select(e => e.Description)]);
        }

        await _audit.WriteAsync(
            "User.ProfileFieldsUpdatedByAdmin",
            nameof(ApplicationUser),
            user.Id.ToString(),
            details: new
            {
                user.IsListedInDirectory,
                user.ReceiveNewsEmails,
                user.ReceiveBlogEmails,
                user.ReceiveBroadcastEmails,
                user.ReceiveGroupEmailsGlobal,
                hasPhoto = !string.IsNullOrEmpty(user.PhotoBlobUrl),
            },
            cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("Profile updated but query failed.");
        return UserMutationResult.Success(detail);
    }

    public async Task<UserMutationResult> ResetNotificationsAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return UserMutationResult.Failure("User not found.");

        // Defaults match ApplicationUserConfiguration: News + Broadcast + Group
        // global on, Blog off.
        user.ReceiveNewsEmails = true;
        user.ReceiveBlogEmails = false;
        user.ReceiveBroadcastEmails = true;
        user.ReceiveGroupEmailsGlobal = true;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return UserMutationResult.Failure([.. update.Errors.Select(e => e.Description)]);
        }

        await _audit.WriteAsync(
            "User.NotificationsResetByAdmin",
            nameof(ApplicationUser),
            user.Id.ToString(),
            cancellationToken: ct);

        var detail = await _queries.GetAsync(user.Id, ct)
            ?? throw new InvalidOperationException("Reset succeeded but query failed.");
        return UserMutationResult.Success(detail);
    }

    public Task<AdminUserNotesDto?> GetAdminNotesAsync(Guid id, CancellationToken ct = default) =>
        _queries.GetAdminNotesAsync(id, ct);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<UserMutationResult> HardDeleteAsync(Guid id, HardDeleteUserRequest request, CancellationToken ct = default)
    {
        if (id == SystemConstants.SystemUserId)
        {
            return UserMutationResult.Failure("The System User cannot be deleted.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return UserMutationResult.Failure("User not found.");
        }

        if (!string.Equals(request.ConfirmDisplayName?.Trim(), user.DisplayName, StringComparison.Ordinal))
        {
            return UserMutationResult.Failure(
                "Confirmation text does not match the user's display name. Hard delete aborted.");
        }

        var snapshot = new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            DisplayName = user.DisplayName,
            user.CreatedAt,
        };

        // Null UserId on broadcast-recipient rows BEFORE deleting the user.
        // Snapshot fields preserve the audit row's meaning; cascading
        // foreign-key delete would discard the historical record.
        await _broadcastRecipients.NullUserReferencesAsync(id, ct).ConfigureAwait(false);

        var del = await _userManager.DeleteAsync(user);
        if (!del.Succeeded)
        {
            return UserMutationResult.Failure([.. del.Errors.Select(e => e.Description)]);
        }

        await _audit.WriteAsync(
            "User.HardDeleted",
            nameof(ApplicationUser),
            id.ToString(),
            details: snapshot,
            cancellationToken: ct);

        // Returning a synthetic deleted-detail keeps the response shape predictable.
        return UserMutationResult.Success(new UserDetailDto(
            id, snapshot.Email ?? string.Empty, snapshot.FirstName, snapshot.LastName,
            snapshot.DisplayName, IsActive: false, EmailConfirmed: false, LockoutEnabled: false,
            LockoutEndUtc: null, snapshot.CreatedAt, LastLoginAt: null,
            Roles: Array.Empty<string>()));
    }
}

/// <summary>
/// Read-side queries that need DB-context-aware joins (user → roles → counts).
/// Implementation lives in Infrastructure and uses EF Core under the hood.
/// </summary>
public interface IUserAdminQueries
{
    Task<PagedResult<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken ct = default);
    Task<UserDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AdminUserNotesDto?> GetAdminNotesAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Composes invitation and password-reset emails using the public-facing site URL.
/// Implementation lives in Infrastructure (it knows the absolute URL of the SPA).
/// </summary>
public interface IInvitationEmailComposer
{
    Task<EmailMessage> ComposeInvitationAsync(ApplicationUser user, string invitationToken, CancellationToken ct = default);
    Task<EmailMessage> ComposePasswordResetAsync(ApplicationUser user, string resetToken, CancellationToken ct = default);
}
