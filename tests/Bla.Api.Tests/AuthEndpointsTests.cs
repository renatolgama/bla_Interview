using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bla.Application.Contracts.Auth;
using FluentAssertions;

namespace Bla.Api.Tests;

public class AuthEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    private static RegisterRequest Register(string email) =>
        new(email, "Test User", "Passw0rd1");

    [Fact]
    public async Task Health_WithoutToken_Returns200()
    {
        var response = await _client.GetAsync("/api/auth/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithValidData_Returns201WithTokenAndNormalizedEmail()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", Register("New.User@Example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be("new.user@example.com");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Register("dup@example.com"));

        var response = await _client.PostAsJsonAsync("/api/auth/register", Register("dup@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("weak@example.com", "Weak", "short"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Register("login@example.com"));

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("login@example.com", "Passw0rd1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Register("wrongpass@example.com"));

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("wrongpass@example.com", "Wrong0000"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsCurrentUserProfile()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", Register("me@example.com"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        me!.Email.Should().Be("me@example.com");
        me.Id.Should().Be(auth.User.Id);
    }
}
