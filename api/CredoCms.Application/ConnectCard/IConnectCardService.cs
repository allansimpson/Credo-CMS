namespace CredoCms.Application.ConnectCard;

/// <summary>
/// Connect card service. Permission rules:
///   submit                         anonymous OK; defended by Turnstile +
///                                  honeypot + 5s time-to-submit + per-IP
///                                  rate limit (5/hr, enforced at the
///                                  middleware layer above this service)
///   list / detail / status / notes Editor + Administrator
///   resend ack / hard delete       Administrator only
/// All admin mutations write an audit entry.
/// </summary>
public interface IConnectCardService
{
    Task<SubmitConnectCardResult> SubmitAsync(
        SubmitConnectCardRequest request,
        string? remoteIp,
        CancellationToken ct = default);

    Task<List<AdminConnectCardListItemDto>> ListAdminAsync(AdminConnectCardListQuery query, CancellationToken ct = default);
    Task<AdminConnectCardDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default);

    Task<AdminConnectCardDetailDto?> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default);
    Task<AdminConnectCardDetailDto?> UpdateNotesAsync(Guid id, UpdateNotesRequest request, CancellationToken ct = default);

    Task<bool> ResendAcknowledgmentAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
