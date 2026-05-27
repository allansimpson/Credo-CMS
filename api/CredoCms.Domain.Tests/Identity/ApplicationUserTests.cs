using CredoCms.Domain.Identity;

namespace CredoCms.Domain.Tests.Identity;

public sealed class ApplicationUserTests
{
    [Fact]
    public void DisplayName_concatenates_first_and_last_with_a_single_space()
    {
        var user = new ApplicationUser { FirstName = "Ada", LastName = "Lovelace" };

        user.DisplayName.Should().Be("Ada Lovelace");
    }

    [Theory]
    [InlineData("", "Lovelace", "Lovelace")]
    [InlineData("Ada", "", "Ada")]
    [InlineData("  ", "  ", "")]
    public void DisplayName_trims_whitespace(string first, string last, string expected)
    {
        var user = new ApplicationUser { FirstName = first, LastName = last };

        user.DisplayName.Should().Be(expected);
    }

    [Fact]
    public void Defaults_match_the_seed_expectations()
    {
        var user = new ApplicationUser();

        user.IsActive.Should().BeTrue("new users are active by default");
        user.RequirePasswordChangeOnFirstLogin.Should().BeFalse(
            "only seeded admins / invited users should set this flag");
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
