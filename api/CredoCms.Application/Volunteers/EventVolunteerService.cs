using CredoCms.Application.Common;
using CredoCms.Application.RealTime;
using CredoCms.Domain.Common;
using CredoCms.Domain.Volunteers;

namespace CredoCms.Application.Volunteers;

public interface IEventVolunteerService
{
    // Admin role management.
    Task<EventVolunteerRole> CreateRoleAsync(Guid eventId, CreateRoleRequest input, CancellationToken ct = default);
    Task<EventVolunteerRole> UpdateRoleAsync(Guid roleId, UpdateRoleRequest input, CancellationToken ct = default);
    Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default);
    Task<List<EventVolunteerRole>> ListRolesAsync(Guid eventId, CancellationToken ct = default);

    // Member signup / cancel.
    Task<EventVolunteerSignup> SignUpAsync(Guid roleId, DateOnly occurrenceDate, CancellationToken ct = default);
    Task CancelSignupAsync(Guid signupId, CancellationToken ct = default);

    // Views.
    Task<List<EventVolunteerSignup>> ListMyUpcomingAsync(CancellationToken ct = default);
    Task<List<EventVolunteerSignup>> ListEventSignupsAsync(Guid eventId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<int> CountActiveSignupsAsync(Guid roleId, DateOnly occurrenceDate, CancellationToken ct = default);
}

public sealed record CreateRoleRequest(string RoleName, string? Description, int SlotsNeeded, int DisplayOrder);
public sealed record UpdateRoleRequest(string RoleName, string? Description, int SlotsNeeded, int DisplayOrder);

public sealed class EventVolunteerService : IEventVolunteerService
{
    private readonly IEventVolunteerRoleRepository _roles;
    private readonly IEventVolunteerSignupRepository _signups;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;
    private readonly IAuditLogger _audit;

    public EventVolunteerService(
        IEventVolunteerRoleRepository roles,
        IEventVolunteerSignupRepository signups,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier,
        IAuditLogger audit)
    {
        _roles = roles;
        _signups = signups;
        _currentUser = currentUser;
        _notifier = notifier;
        _audit = audit;
    }

    public async Task<EventVolunteerRole> CreateRoleAsync(Guid eventId, CreateRoleRequest input, CancellationToken ct = default)
    {
        EnsureAdminShell();
        if (input.SlotsNeeded < 1) throw new InvalidOperationException("SlotsNeeded must be at least 1.");
        var role = new EventVolunteerRole
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            RoleName = input.RoleName,
            Description = input.Description,
            SlotsNeeded = input.SlotsNeeded,
            DisplayOrder = input.DisplayOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        await _roles.AddAsync(role, ct).ConfigureAwait(false);
        await _audit.WriteAsync("EventVolunteerRole.Created", nameof(EventVolunteerRole), role.Id.ToString(),
            new { role.EventId, role.RoleName, role.SlotsNeeded }, ct).ConfigureAwait(false);
        return role;
    }

    public async Task<EventVolunteerRole> UpdateRoleAsync(Guid roleId, UpdateRoleRequest input, CancellationToken ct = default)
    {
        EnsureAdminShell();
        if (input.SlotsNeeded < 1) throw new InvalidOperationException("SlotsNeeded must be at least 1.");
        var role = await _roles.GetAsync(roleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Role not found.");
        role.RoleName = input.RoleName;
        role.Description = input.Description;
        role.SlotsNeeded = input.SlotsNeeded;
        role.DisplayOrder = input.DisplayOrder;
        role.ModifiedAt = DateTimeOffset.UtcNow;
        await _roles.UpdateAsync(role, ct).ConfigureAwait(false);
        return role;
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        EnsureAdminShell();
        await _roles.SoftDeleteAsync(roleId, _currentUser.UserId, ct).ConfigureAwait(false);
    }

    public Task<List<EventVolunteerRole>> ListRolesAsync(Guid eventId, CancellationToken ct = default) =>
        _roles.ListByEventAsync(eventId, ct);

    public async Task<EventVolunteerSignup> SignUpAsync(Guid roleId, DateOnly occurrenceDate, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            throw new InvalidOperationException("Sign-in required.");

        var role = await _roles.GetAsync(roleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Role not found.");

        var existing = await _signups.ListActiveForRoleOccurrenceAsync(roleId, occurrenceDate, ct).ConfigureAwait(false);
        if (existing.Any(s => s.UserId == _currentUser.UserId))
            throw new InvalidOperationException("You're already signed up for this slot.");
        if (existing.Count >= role.SlotsNeeded)
            throw new InvalidOperationException("This role is full for the selected occurrence.");

        var signup = new EventVolunteerSignup
        {
            Id = Guid.NewGuid(),
            EventVolunteerRoleId = roleId,
            EventId = role.EventId,
            OccurrenceDate = occurrenceDate,
            UserId = _currentUser.UserId,
            SignedUpAt = DateTimeOffset.UtcNow,
        };
        await _signups.AddAsync(signup, ct).ConfigureAwait(false);

        await _notifier.NotifyVolunteerSlotAsync(new VolunteerSlotMessage(
            "VolunteerSlotFilled",
            role.EventId, role.Id, occurrenceDate, role.RoleName,
            FilledSlots: existing.Count + 1,
            SlotsNeeded: role.SlotsNeeded), ct).ConfigureAwait(false);

        return signup;
    }

    public async Task CancelSignupAsync(Guid signupId, CancellationToken ct = default)
    {
        var signup = await _signups.GetAsync(signupId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Signup not found.");
        if (signup.UserId != _currentUser.UserId && !IsAdminShell)
            throw new UnauthorizedAccessException("You can only cancel your own signup.");
        if (signup.CanceledAt is not null) return;

        signup.CanceledAt = DateTimeOffset.UtcNow;
        await _signups.UpdateAsync(signup, ct).ConfigureAwait(false);

        var role = await _roles.GetAsync(signup.EventVolunteerRoleId, ct).ConfigureAwait(false);
        if (role is not null)
        {
            var remaining = await _signups.ListActiveForRoleOccurrenceAsync(
                role.Id, signup.OccurrenceDate, ct).ConfigureAwait(false);
            await _notifier.NotifyVolunteerSlotAsync(new VolunteerSlotMessage(
                "VolunteerSlotOpened",
                role.EventId, role.Id, signup.OccurrenceDate, role.RoleName,
                FilledSlots: remaining.Count,
                SlotsNeeded: role.SlotsNeeded), ct).ConfigureAwait(false);
        }
    }

    public Task<List<EventVolunteerSignup>> ListMyUpcomingAsync(CancellationToken ct = default) =>
        _signups.ListUpcomingForUserAsync(_currentUser.UserId, DateOnly.FromDateTime(DateTime.UtcNow), ct);

    public Task<List<EventVolunteerSignup>> ListEventSignupsAsync(Guid eventId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        EnsureAdminShell();
        return _signups.ListByEventAsync(eventId, from, to, ct);
    }

    public async Task<int> CountActiveSignupsAsync(Guid roleId, DateOnly occurrenceDate, CancellationToken ct = default)
    {
        var rows = await _signups.ListActiveForRoleOccurrenceAsync(roleId, occurrenceDate, ct).ConfigureAwait(false);
        return rows.Count;
    }

    private bool IsAdminShell =>
        _currentUser.Roles.Contains(SystemConstants.Roles.Administrator)
        || _currentUser.Roles.Contains(SystemConstants.Roles.Editor);

    private void EnsureAdminShell()
    {
        if (!IsAdminShell) throw new UnauthorizedAccessException("Admin or editor role required.");
    }
}
