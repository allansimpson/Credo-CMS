using CredoCms.Application.ConnectCard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Anonymous connect-card submission. Rate-limited to 5/hour per IP via
/// <see cref="ConnectCardRateLimitPolicy"/>; service additionally enforces
/// honeypot, 5-second time-to-submit, and Cloudflare Turnstile checks.
/// </summary>
[ApiController]
[Route("api/public/connect-card")]
[AllowAnonymous]
public sealed class PublicConnectCardController : ControllerBase
{
    public const string RateLimitPolicy = "ConnectCardSubmit";

    private readonly IConnectCardService _service;
    public PublicConnectCardController(IConnectCardService service) => _service = service;

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy)]
    public async Task<IActionResult> SubmitAsync([FromBody] SubmitConnectCardRequest request, CancellationToken ct)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _service.SubmitAsync(request, remoteIp, ct);
        // Always 200 on the public surface — the service distinguishes between
        // bot rejections (returned with a generic "submission rejected" so
        // we don't help abuse tooling) and validation rejections (which
        // surface specific messages). 200 + an "ok": true|false body is
        // enough for the SPA to branch on.
        return Ok(new
        {
            ok = result.Succeeded,
            errors = result.Succeeded ? null : result.Errors,
        });
    }
}
