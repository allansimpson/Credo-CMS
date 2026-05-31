namespace CredoCms.Infrastructure.Identity;

/// <summary>
/// Configuration for the Have-I-Been-Pwned breach-corpus password screen.
/// Bound from <c>PasswordSecurity:Hibp</c> in configuration.
/// </summary>
public sealed class HibpPasswordValidatorOptions
{
    public const string SectionName = "PasswordSecurity:Hibp";

    /// <summary>Master switch. When false, the validator returns Success unconditionally —
    /// useful for tests / offline dev environments.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Max number of breach appearances tolerated before a password is rejected.
    /// 0 (the default) means "reject if seen in ANY breach" — the safe, NIST-aligned stance.</summary>
    public int MaxBreachCount { get; set; }

    /// <summary>If the HIBP API is unreachable, allow the password through (true) or
    /// reject the submission (false). Default true so an external outage doesn't
    /// take down signups / password resets. The other validators still run.</summary>
    public bool AllowOnApiFailure { get; set; } = true;
}
