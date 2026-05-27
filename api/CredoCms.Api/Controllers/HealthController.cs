using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Liveness endpoint for Azure App Service health probes.
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", utc = DateTimeOffset.UtcNow });
}
