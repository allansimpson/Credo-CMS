using CredoCms.Application.Announcements;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/announcement")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementBannerService _svc;
    public AnnouncementController(IAnnouncementBannerService svc) => _svc = svc;

    [HttpGet]
    public Task<AnnouncementBannerDto> GetAsync(CancellationToken ct) => _svc.GetAsync(ct);

    [HttpPut]
    public async Task<ActionResult<AnnouncementBannerDto>> UpdateAsync([FromBody] UpdateAnnouncementBannerRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _svc.UpdateAsync(req, ct);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }
}
