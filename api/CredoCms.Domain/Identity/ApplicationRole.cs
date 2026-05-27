using Microsoft.AspNetCore.Identity;

namespace CredoCms.Domain.Identity;

/// <summary>
/// Application role. Seeded roles: Administrator, Editor, Member.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) { }
}
