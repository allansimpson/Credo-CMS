namespace CredoCms.Application.Services;

public sealed record ServiceTimeDto(
    Guid Id,
    string Name,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    string? Location,
    string? Notes,
    int DisplayOrder,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    Guid? ModifiedByUserId);

public sealed record PublicServiceTimeDto(
    string Name,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    string? Location,
    string? Notes,
    int DisplayOrder);

public sealed record CreateServiceTimeRequest(
    string Name,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    string? Location,
    string? Notes,
    int DisplayOrder,
    bool IsActive);

public sealed record UpdateServiceTimeRequest(
    string Name,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    string? Location,
    string? Notes,
    int DisplayOrder,
    bool IsActive);
