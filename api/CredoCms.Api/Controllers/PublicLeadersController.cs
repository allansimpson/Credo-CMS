using CredoCms.Application.Leaders;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/leaders")]
public sealed class PublicLeadersController : ControllerBase
{
    private readonly ILeaderService _svc;
    public PublicLeadersController(ILeaderService svc) => _svc = svc;

    [HttpGet]
    public Task<List<PublicLeaderDto>> ListAsync(CancellationToken ct) => _svc.ListPublicAsync(ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicLeaderDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var item = await _svc.GetPublicAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }
}
