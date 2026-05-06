using CredoCms.Domain.Volunteers;

namespace CredoCms.Application.Volunteers;

public interface IEventVolunteerRoleRepository
{
    Task<EventVolunteerRole?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<EventVolunteerRole>> ListByEventAsync(Guid eventId, CancellationToken ct = default);
    Task AddAsync(EventVolunteerRole role, CancellationToken ct = default);
    Task UpdateAsync(EventVolunteerRole role, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default);
}
