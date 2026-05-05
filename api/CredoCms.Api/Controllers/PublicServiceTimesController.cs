using CredoCms.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/service-times")]
public sealed class PublicServiceTimesController : ControllerBase
{
    private readonly IServiceTimeService _svc;
    public PublicServiceTimesController(IServiceTimeService svc) => _svc = svc;

    [HttpGet]
    public Task<List<PublicServiceTimeDto>> ListAsync(CancellationToken ct) => _svc.ListPublicAsync(ct);
}
