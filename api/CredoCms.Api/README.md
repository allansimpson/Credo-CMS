# CredoCms.Api

ASP.NET Core 10 host for the Credo CMS backend. Composes the Application and Infrastructure layers, exposes the HTTP API consumed by the SPA, and runs the background services (scheduled publishing, broadcast worker, admin notification digest, etc.).

## Running locally

1. Restore + build the solution: `dotnet build` from `api/`.
2. Make sure SQL Server is reachable via the `DefaultConnection` string in your local `appsettings.Development.json` (gitignored). Azurite must be running for blob uploads.
3. Start the API: `dotnet run --project CredoCms.Api` or hit F5 in Visual Studio / Rider. Migrations apply on startup; seed data loads on the first run.
4. The SPA expects the API at the URL configured in `app/.env` (defaults work out of the box with the standard launchSettings profile).

## Email

The invite-member, broadcast, and admin-digest flows all dispatch through `IEmailService`. Routing is decided at runtime from Site Settings → Email:

| `EmailProvider`          | Behavior                                                                            |
| ------------------------ | ----------------------------------------------------------------------------------- |
| `None`                   | `LoggingEmailService` — logs the full message to Serilog at Information level.      |
| `SendGrid`               | `SendGridEmailService` — requires `SendGridApiKey`.                                  |
| `Smtp`                   | `SmtpEmailService` — requires `SmtpHost` (port/username/password/SSL as needed).    |

The master `EmailEnabled` flag short-circuits everything when off, regardless of provider — a safe-by-default posture so fresh deploys can't accidentally send mail. The seed sets it to `false`.

### Auto-starting smtp4dev in Development

`Smtp4DevHostedService` is registered only when `IHostEnvironment.IsDevelopment()`. On startup it probes `localhost:2525`; if nothing is listening it spawns `smtp4dev` as a child process, forwards its stdout/stderr to Serilog, and tears it down cleanly on API shutdown. The catcher captures every message and renders an inbox at `http://localhost:5050`.

**One-time setup** on each dev machine:

```bash
dotnet tool install -g Rnwood.Smtp4dev
```

That's it. The next time you run the API you should see:

```
smtp4dev started: SMTP on localhost:2525, web UI at http://localhost:5050
```

If the tool isn't installed, the API logs a one-line nudge with the install command and continues without it — nothing breaks.

**To actually route mail through smtp4dev**, configure Site Settings → Email & Notifications:

- Provider: `SMTP`
- Host: `localhost`
- Port: `2525`
- Use SSL/TLS: **off**
- Email enabled: **on**
- From / Reply-to: anything sensible (the catcher doesn't validate sender domains)

Then hit **Send test** in the admin UI. The message should appear in the smtp4dev inbox within a second or two. Invitations sent from the Users page land in the same inbox.

### Knobs

Override defaults via `appsettings.Development.json` (gitignored):

```json
{
  "Smtp4Dev": {
    "Enabled": true,
    "SmtpPort": 2525,
    "WebUiPort": 5050
  }
}
```

Set `Enabled: false` to opt out — useful if you're running smtp4dev manually in a separate terminal, as a Docker container, or via a Windows service.

### Alternatives

- **Papercut SMTP** — Windows-native tray catcher, no command line; listens on port 25 by default. Set the hosted service to `Enabled: false` if you use this.
- **Mailtrap** — cloud-hosted fake inbox (free tier); paste their SMTP credentials into the same Site Settings tab. Useful when you want to share previews with teammates.
- **SendGrid free tier** — 100 emails/day with real delivery. Switch Provider to `SendGrid` and paste your API key.
- **`EmailProvider = None` + `EmailEnabled = true`** — the API writes the full HTML and plain-text bodies to its own console via `LoggingEmailService`. No tools required; handy for quick previews when you don't want to install anything.

## Background services

All hosted services are registered in `CredoCms.Infrastructure/Composition`. The ones that touch outbound mail:

- `ScheduledPublishingService` — publishes due news/blog/sermons, then calls `IEmailOnPublishService` to enqueue subscriber broadcasts.
- `BroadcastSendWorker` — drains the broadcast queue, batching sends through `IEmailService`.
- `AdminNotificationDigestService` — periodic digest to admins (connect cards, prayer requests, etc.); cadence is controlled by Site Settings → Email → `AdminNotificationFrequency`.

When `EmailEnabled` is `false`, these services still run and still record sent-state in the database, but the actual SMTP/SendGrid call short-circuits inside `LoggingEmailService`.
