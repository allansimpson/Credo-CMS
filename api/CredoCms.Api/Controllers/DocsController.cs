using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Phase 6 — serves the Astro-built operator docs from
/// <c>wwwroot/docs/</c> behind the AdminShell auth policy. Anonymous /
/// Member requests get the church-themed 404 (covert routing — the
/// existence of the docs subtree is not disclosed). Path-traversal
/// guarded; only whitelisted MIME types served.
/// </summary>
[ApiController]
public sealed class DocsController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".htm", ".css", ".js", ".mjs", ".map", ".json",
        ".svg", ".png", ".webp", ".jpg", ".jpeg", ".gif", ".ico",
        ".woff", ".woff2", ".ttf", ".eot",
        ".txt", ".pf_meta", ".pf_index", ".pagefind", // Pagefind index files
    };

    private readonly IWebHostEnvironment _env;
    private readonly FileExtensionContentTypeProvider _mime = new();

    public DocsController(IWebHostEnvironment env) => _env = env;

    [HttpGet("/docs")]
    [AllowAnonymous]
    public IActionResult Index() => ServeOrCovert("index.html");

    /// <summary>Catch-all for docs file requests under <c>/docs/...</c>.
    /// AllowAnonymous is intentional — the action does the auth check
    /// itself and returns 404 for unauthorized callers, so the subtree's
    /// existence is never disclosed (covert routing). [Authorize] would
    /// trigger a cookie-auth challenge that leaks via 302/401.</summary>
    [HttpGet("/docs/{**path}")]
    [AllowAnonymous]
    public IActionResult Asset(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return ServeOrCovert("index.html");
        return ServeOrCovert(path);
    }

    private IActionResult ServeOrCovert(string path)
    {
        // Covert auth check — anonymous AND non-Admin/Editor get 404.
        if (User?.Identity?.IsAuthenticated != true) return NotFound();
        if (!User.IsInRole("Administrator") && !User.IsInRole("Editor"))
            return NotFound();
        return ServeFile(path);
    }

    private IActionResult ServeFile(string requestedPath)
    {
        // Reject path traversal. Disallow ".." segments + absolute paths.
        if (requestedPath.Contains("..", StringComparison.Ordinal)) return NotFound();
        if (Path.IsPathRooted(requestedPath)) return NotFound();

        var docsRoot = Path.Combine(_env.WebRootPath ?? string.Empty, "docs");
        var fullPath = Path.GetFullPath(Path.Combine(docsRoot, requestedPath));

        // Final containment check — the resolved path must still live under
        // wwwroot/docs after Path.Combine + GetFullPath normalize.
        if (!fullPath.StartsWith(docsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !string.Equals(fullPath, docsRoot, StringComparison.Ordinal))
        {
            return NotFound();
        }

        // If the path resolves to a directory, look for an index.html inside it.
        if (Directory.Exists(fullPath))
        {
            var indexPath = Path.Combine(fullPath, "index.html");
            if (System.IO.File.Exists(indexPath)) fullPath = indexPath;
            else return NotFound();
        }

        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var ext = Path.GetExtension(fullPath);
        if (!AllowedExtensions.Contains(ext)) return NotFound();

        if (!_mime.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(fullPath, contentType);
    }
}
