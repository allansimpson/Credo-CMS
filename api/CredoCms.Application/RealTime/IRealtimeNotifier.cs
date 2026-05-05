namespace CredoCms.Application.RealTime;

public sealed record ContentChangedMessage(string EntityType, Guid EntityId, string Action);

public interface IRealtimeNotifier
{
    Task NotifyContentChangedAsync(ContentChangedMessage message, CancellationToken ct = default);
}
