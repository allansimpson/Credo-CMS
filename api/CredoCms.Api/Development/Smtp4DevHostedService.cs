using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CredoCms.Api.Development;

/// <summary>Auto-launches <c>smtp4dev</c> alongside the API in Development so
/// outbound mail has somewhere to land without a separate terminal. Idempotent
/// — if the SMTP port is already in use (a catcher started elsewhere, or a
/// previous run that didn't shut down cleanly), the spawn is skipped.
/// Production is unaffected; this service is only registered when
/// <see cref="Microsoft.Extensions.Hosting.IHostEnvironment.IsDevelopment"/>.
/// </summary>
internal sealed class Smtp4DevHostedService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<Smtp4DevHostedService> _log;
    private readonly Smtp4DevOptions _opts;
    private Process? _proc;

    public Smtp4DevHostedService(
        ILogger<Smtp4DevHostedService> log,
        Microsoft.Extensions.Options.IOptions<Smtp4DevOptions> opts)
    {
        _log = log;
        _opts = opts.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_opts.Enabled)
        {
            _log.LogDebug("smtp4dev auto-start disabled via configuration.");
            return;
        }

        if (await IsPortInUseAsync(_opts.SmtpPort, cancellationToken).ConfigureAwait(false))
        {
            _log.LogInformation(
                "smtp4dev: SMTP port {Port} already in use — assuming a catcher is already running, skipping spawn.",
                _opts.SmtpPort);
            return;
        }

        var probed = new List<string>();
        // `dotnet tool install -g` on Windows ships a `.cmd` shim, not a
        // standalone `.exe`. We check both so the same probe works on every
        // dev box. The bare-name fallback lets the Windows loader resolve
        // via PATH at exec time if file probing misses.
        var fallbackName = OperatingSystem.IsWindows() ? "smtp4dev.cmd" : "smtp4dev";
        var resolvedExe = LocateExecutable(probed) ?? fallbackName;

        // `.cmd` / `.bat` files can't be invoked directly when
        // UseShellExecute = false. Wrap them with `cmd.exe /c` so we still
        // capture stdout/stderr while letting the shell expand the shim.
        var isShim = OperatingSystem.IsWindows()
            && (resolvedExe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
                || resolvedExe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));

        var psi = new ProcessStartInfo
        {
            FileName = isShim ? "cmd.exe" : resolvedExe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (isShim)
        {
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(resolvedExe);
        }
        psi.ArgumentList.Add($"--urls=http://localhost:{_opts.WebUiPort}");
        psi.ArgumentList.Add($"--smtpport={_opts.SmtpPort}");
        // Empty db path = in-memory store; messages reset on restart,
        // which is the right dev behavior.
        psi.ArgumentList.Add("--db=");

        try
        {
            _proc = Process.Start(psi);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            // ERROR_FILE_NOT_FOUND — couldn't resolve even via PATH.
            _log.LogWarning(
                "smtp4dev: executable not found. Tried: {Paths}. " +
                "If the tool is installed elsewhere, set the DOTNET_TOOLS_PATH env var, " +
                "add the install directory to PATH, or run `dotnet tool install -g Rnwood.Smtp4dev`. " +
                "Otherwise, configure Email → SMTP manually or set Provider to None and toggle Email Enabled to log messages to this console.",
                string.Join("; ", probed.Append("PATH lookup")));
            return;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "smtp4dev: failed to start child process.");
            return;
        }

        if (_proc is null)
        {
            _log.LogWarning("smtp4dev: Process.Start returned null.");
            return;
        }

        _proc.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) _log.LogDebug("[smtp4dev] {Line}", e.Data);
        };
        _proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) _log.LogWarning("[smtp4dev] {Line}", e.Data);
        };
        _proc.BeginOutputReadLine();
        _proc.BeginErrorReadLine();

        _log.LogInformation(
            "smtp4dev started: SMTP on localhost:{Smtp}, web UI at http://localhost:{Web}",
            _opts.SmtpPort,
            _opts.WebUiPort);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_proc is null || _proc.HasExited) return Task.CompletedTask;
        try
        {
            _proc.Kill(entireProcessTree: true);
            _proc.WaitForExit(2000);
            _log.LogInformation("smtp4dev stopped.");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "smtp4dev: failed to stop child process cleanly.");
        }
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _proc?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string? LocateExecutable(List<string> probed)
    {
        // `dotnet tool install -g` may emit either `smtp4dev.exe` or
        // `smtp4dev.cmd` on Windows depending on the package's tool
        // metadata. Check both names in each candidate directory.
        var exeNames = OperatingSystem.IsWindows()
            ? new[] { "smtp4dev.exe", "smtp4dev.cmd" }
            : new[] { "smtp4dev" };

        // Directories where global / per-tool installs might land,
        // most-likely first. We surface the full probed list in the
        // warning when nothing resolves so you can see where we looked.
        var dirs = new List<string>();

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(profile))
            dirs.Add(Path.Combine(profile, ".dotnet", "tools"));

        var userProfileEnv = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrEmpty(userProfileEnv))
            dirs.Add(Path.Combine(userProfileEnv, ".dotnet", "tools"));
        var homeEnv = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(homeEnv))
            dirs.Add(Path.Combine(homeEnv, ".dotnet", "tools"));

        var toolsPathEnv = Environment.GetEnvironmentVariable("DOTNET_TOOLS_PATH");
        if (!string.IsNullOrEmpty(toolsPathEnv))
            dirs.Add(toolsPathEnv);

        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(programFiles))
                dirs.Add(Path.Combine(programFiles, "dotnet", "tools"));
        }

        foreach (var dir in dirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var name in exeNames)
            {
                var candidate = Path.Combine(dir, name);
                probed.Add(candidate);
                if (File.Exists(candidate)) return candidate;
            }
        }

        // PATH lookup — useful when the dev installed via Chocolatey,
        // Scoop, winget, etc. instead of `dotnet tool install`.
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            foreach (var name in exeNames)
            {
                try
                {
                    var candidate = Path.Combine(dir, name);
                    if (File.Exists(candidate)) return candidate;
                }
                catch { /* malformed PATH entry, skip */ }
            }
        }

        return null;
    }

    private static async Task<bool> IsPortInUseAsync(int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(1));
            await client.ConnectAsync(IPAddress.Loopback, port, timeoutCts.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>Configuration knobs for <see cref="Smtp4DevHostedService"/>. Bound
/// from the <c>Smtp4Dev</c> section of <c>appsettings.Development.json</c>.</summary>
public sealed class Smtp4DevOptions
{
    /// <summary>Port the SMTP listener binds. Match this in Site Settings →
    /// Email → SMTP → Port.</summary>
    public int SmtpPort { get; set; } = 2525;

    /// <summary>Port the smtp4dev web inbox binds. Browse to
    /// <c>http://localhost:{WebUiPort}</c> to read captured mail.</summary>
    public int WebUiPort { get; set; } = 5050;

    /// <summary>Set <c>false</c> to opt out of auto-spawn — useful if you're
    /// running smtp4dev manually or as a Docker container instead.</summary>
    public bool Enabled { get; set; } = true;
}
