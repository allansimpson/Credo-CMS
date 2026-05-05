namespace CredoCms.Domain.Common;

/// <summary>
/// Fixed identifiers shared across the application — seeded into the database and
/// used throughout the codebase as well-known references.
/// </summary>
public static class SystemConstants
{
    /// <summary>The single Site Settings row's primary key.</summary>
    public static readonly Guid SiteSettingsId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>The System User's primary key (used for background-job writes and
    /// any system-generated audit-log entries).</summary>
    public static readonly Guid SystemUserId = new("22222222-2222-2222-2222-222222222222");

    /// <summary>The single AnnouncementBanner row's primary key.</summary>
    public static readonly Guid AnnouncementBannerId = new("33333333-3333-3333-3333-333333333333");

    public const string SystemUserEmail = "system@credocms.local";
    public const string SystemUserDisplayName = "System";

    /// <summary>Role names. Match what's seeded into AspNetRoles.</summary>
    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Editor = "Editor";
        public const string Member = "Member";

        public static readonly IReadOnlyList<string> All = [Administrator, Editor, Member];

        /// <summary>Roles whose members can access the admin shell.</summary>
        public static readonly IReadOnlyList<string> AdminShellRoles = [Administrator, Editor];
    }
}
