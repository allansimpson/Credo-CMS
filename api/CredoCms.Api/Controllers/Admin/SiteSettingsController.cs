using CredoCms.Application.SiteSettingsManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/site-settings")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public sealed class SiteSettingsAdminController : ControllerBase
{
    private readonly ISiteSettingsService _service;

    public SiteSettingsAdminController(ISiteSettingsService service) => _service = service;

    [HttpGet]
    public Task<SiteSettingsDto> GetAsync(CancellationToken ct) => _service.GetAsync(ct);

    [HttpPut]
    public async Task<ActionResult<SiteSettingsDto>> UpdateAsync(
        [FromBody] UpdateSiteSettingsRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await _service.UpdateAsync(request, ct);
            return Ok(updated);
        }
        catch (OptimisticConcurrencyException ex)
        {
            return Conflict(new { errors = new[] { ex.Message } });
        }
    }
}
