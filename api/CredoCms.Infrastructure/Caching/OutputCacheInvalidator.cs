using CredoCms.Application.Caching;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Infrastructure.Caching;

public sealed class OutputCacheInvalidator : IOutputCacheInvalidator
{
    private readonly IOutputCacheStore _store;

    public OutputCacheInvalidator(IOutputCacheStore store) => _store = store;

    public Task InvalidateAsync(string tag, CancellationToken ct = default)
        => _store.EvictByTagAsync(tag, ct).AsTask();

    public async Task InvalidateAsync(IEnumerable<string> tags, CancellationToken ct = default)
    {
        foreach (var t in tags)
        {
            await _store.EvictByTagAsync(t, ct).ConfigureAwait(false);
        }
    }
}
