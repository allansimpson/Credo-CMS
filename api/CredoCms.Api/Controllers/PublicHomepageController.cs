using CredoCms.Application.Homepage;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/homepage")]
public sealed class PublicHomepageController : ControllerBase
{
    private readonly IHomepageService _svc;
    public PublicHomepageController(IHomepageService svc) => _svc = svc;

    [HttpGet]
    public Task<HomepageDto> GetAsync(CancellationToken ct)
        => _svc.GetAsync(IsAuthenticatedMember(), ct);

    private bool IsAuthenticatedMember()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;
        return User.IsInRole(SystemConstants.Roles.Member)
            || User.IsInRole(SystemConstants.Roles.Editor)
            || User.IsInRole(SystemConstants.Roles.Administrator);
    }
}
