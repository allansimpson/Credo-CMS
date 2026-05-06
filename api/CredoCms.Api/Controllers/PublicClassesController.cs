using CredoCms.Application.Caching;
using CredoCms.Application.Classes;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Public classes endpoint. Two response shapes are emitted depending on the
/// caller's role:
///   • Anonymous → public-safe DTO (member-only fields absent from the JSON)
///   • Member+ → member-augmented DTO (DefaultRoom, TeacherLeaderId,
///     TeacherFreeText, DetailedScheduleJson, MaterialsNeeded populated)
/// SiteSettings drives recent-past visibility (<c>ShowRecentPastOnPublicClasses</c>
/// + <c>RecentPastClassesLookbackDays</c>). The settings are read straight from
/// the repository here because <c>SiteSettingsDto</c> doesn't expose Phase 4
/// fields yet — that surface lands in Q16.
/// </summary>
[ApiController]
[Route("api/public/classes")]
public sealed class PublicClassesController : ControllerBase
{
    private readonly IClassService _classes;
    private readonly ISiteSettingsRepository _settings;

    public PublicClassesController(IClassService classes, ISiteSettingsRepository settings)
    {
        _classes = classes;
        _settings = settings;
    }

    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 60,
        Tags = new[] { OutputCacheTags.Classes })]
    public async Task<ActionResult> ListAsync(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        if (IsMemberPlus())
        {
            var dto = await _classes.ListMemberAsync(
                settings.ShowRecentPastOnPublicClasses,
                settings.RecentPastClassesLookbackDays,
                ct);
            return Ok(dto);
        }
        var publicDto = await _classes.ListPublicAsync(
            settings.ShowRecentPastOnPublicClasses,
            settings.RecentPastClassesLookbackDays,
            ct);
        return Ok(publicDto);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 60,
        Tags = new[] { OutputCacheTags.Classes })]
    public async Task<ActionResult> GetAsync(string slug, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        if (IsMemberPlus())
        {
            var detail = await _classes.GetMemberBySlugAsync(
                slug, settings.ShowRecentPastOnPublicClasses,
                settings.RecentPastClassesLookbackDays, ct);
            return detail is null ? NotFound() : Ok(detail);
        }
        var publicDetail = await _classes.GetPublicBySlugAsync(
            slug, settings.ShowRecentPastOnPublicClasses,
            settings.RecentPastClassesLookbackDays, ct);
        return publicDetail is null ? NotFound() : Ok(publicDetail);
    }

    private bool IsMemberPlus()
    {
        var user = User;
        if (user?.Identity?.IsAuthenticated != true) return false;
        return user.IsInRole(SystemConstants.Roles.Member)
            || user.IsInRole(SystemConstants.Roles.Editor)
            || user.IsInRole(SystemConstants.Roles.Administrator);
    }
}
