namespace CredoCms.Application.Auditing;

public sealed record AuditLogEntryDto(
    Guid Id,
    DateTimeOffset Timestamp,
    Guid? UserId,
    string UserDisplayNameSnapshot,
    string Action,
    string EntityType,
    string? EntityId,
    string? DetailsJson,
    string? IpAddress);
