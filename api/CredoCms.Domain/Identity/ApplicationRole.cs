using Microsoft.AspNetCore.Identity;

namespace CredoCms.Domain.Identity;

/// <summary>
/// Application role. Phase 1 seeds three: Administrator, Editor, Member.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) { }
}
