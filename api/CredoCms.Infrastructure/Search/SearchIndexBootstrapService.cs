using CredoCms.Application.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Search;

/// <summary>
/// Startup-only background service: if the SearchIndex table is empty, runs
/// a full rebuild. Avoids requiring operators to manually trigger a rebuild
/// after deploy.
/// </summary>
public sealed class SearchIndexBootstrapService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SearchIndexBootstrapService> _logger;

    public SearchIndexBootstrapService(
        IServiceScopeFactory scopes,
        ILogger<SearchIndexBootstrapService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var indexer = scope.ServiceProvider.GetRequiredService<ISearchIndexer>();
            var count = await indexer.CountAsync(stoppingToken).ConfigureAwait(false);
            if (count == 0)
            {
                _logger.LogInformation("Search index empty on startup — running rebuild.");
                await indexer.RebuildAllAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search-index bootstrap skipped (probably no DB yet).");
        }
    }
}
