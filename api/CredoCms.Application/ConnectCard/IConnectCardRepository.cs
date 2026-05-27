using CredoCms.Domain.ConnectCard;

namespace CredoCms.Application.ConnectCard;

public interface IConnectCardRepository
{
    Task<ConnectCardSubmission?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<ConnectCardSubmission>> ListAsync(AdminConnectCardListQuery query, CancellationToken ct = default);
    Task AddAsync(ConnectCardSubmission entity, CancellationToken ct = default);
    Task UpdateAsync(ConnectCardSubmission entity, CancellationToken ct = default);
}
