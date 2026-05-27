using System.Net.Http.Json;
using CredoCms.Application.Classes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Auth-gate verification on the classes endpoints. The shape contract
/// (anonymous → public-safe DTO; member+ → member-augmented DTO) is owned
/// by ClassService and covered in ClassServicePrivacyTests.
/// </summary>
public sealed class ClassesEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public ClassesEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Public_classes_list_is_anonymous_and_returns_200()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/public/classes", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(200);
    }

    [Fact]
    public async Task Public_class_detail_returns_404_for_unknown_slug()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/public/classes/no-such-slot", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task Admin_class_slots_list_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/admin/class-slots", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_class_slots_create_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new CreateClassSlotRequest(
            "adults", "Adults", "Adults", null, null, null, null, null, null, true, 0);
        var response = await client.PostAsJsonAsync(
            new Uri("/api/admin/class-slots", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_class_offerings_list_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/admin/class-offerings", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }
}
