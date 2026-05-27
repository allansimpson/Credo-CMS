namespace CredoCms.Domain.Email;

/// <summary>Outbound email category. Drives suppression-list checks (transactional
/// bypasses suppression; everything else respects it) and recipient-preference
/// filtering (e.g., <c>News</c> respects <c>ApplicationUser.ReceiveNewsEmails</c>).</summary>
public enum EmailCategory
{
    Transactional = 0,
    News = 1,
    Blog = 2,
    Broadcast = 3,
    GroupCommunication = 4,
}

/// <summary>How an email address ended up on the suppression list. Drives the
/// admin UI "remove from suppression" warning copy.</summary>
public enum SuppressionType
{
    HardBounce = 0,
    SpamComplaint = 1,
    Unsubscribe = 2,
    ManualSuppression = 3,
}

/// <summary>Where the suppression record came from.</summary>
public enum SuppressionSource
{
    SendGridWebhook = 0,
    MemberAction = 1,
    Admin = 2,
}

/// <summary>Provider selection for outbound email. <c>None</c> forces
/// <c>EmailEnabled=false</c> regardless of UI state.</summary>
public enum EmailProvider
{
    None = 0,
    SendGrid = 1,
    Smtp = 2,
}

/// <summary>SMS provider stub. Twilio is structurally present in v1 but the
/// implementation is deferred to v1.5 — selecting it has no effect.</summary>
public enum SmsProvider
{
    None = 0,
    Twilio = 1,
}

/// <summary>Audience selector for a broadcast. <c>SpecificGroups</c> uses
/// <c>EmailBroadcast.TargetGroupIdsJson</c>.</summary>
public enum BroadcastTargetMode
{
    AllMembers = 0,
    SpecificGroups = 1,
}

/// <summary>Send-now or scheduled-for-later. Scheduled broadcasts are picked
/// up by the broadcast worker when <c>ScheduledSendAt &lt;= now</c>.</summary>
public enum BroadcastSendMode
{
    SendNow = 0,
    Scheduled = 1,
}

/// <summary>Lifecycle state of a broadcast.</summary>
public enum BroadcastStatus
{
    Draft = 0,
    Scheduled = 1,
    Sending = 2,
    Sent = 3,
    Canceled = 4,
    Failed = 5,
}

/// <summary>Per-recipient delivery state. Updated by the SendGrid webhook
/// handler as events arrive.</summary>
public enum RecipientStatus
{
    Pending = 0,
    Delivered = 1,
    Bounced = 2,
    ComplainedSpam = 3,
    Suppressed = 4,
    Failed = 5,
}

/// <summary>Categories the admin-notification digest service tracks
/// independently. Each category has its own <c>AdminNotificationLastSent</c>
/// row per user.</summary>
public enum AdminNotificationCategory
{
    ConnectCardSubmissions = 0,
    GroupJoinRequests = 1,
}

/// <summary>How often digest emails fire. <c>Off</c> disables digests entirely
/// for the user.</summary>
public enum AdminNotificationFrequency
{
    Off = 0,
    Every30Minutes = 1,
    Hourly = 2,
    Daily = 3,
}
