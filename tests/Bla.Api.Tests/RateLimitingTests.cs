using System.Net;
using System.Net.Http.Json;
using Bla.Application.Contracts.Auth;
using FluentAssertions;

namespace Bla.Api.Tests;

// Own fixture instance: this class must not consume the login budget of the
// other endpoint test classes (and vice versa).
public class RateLimitingTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Login_AfterFiveAttemptsInWindow_Returns429()
    {
        var client = factory.CreateClient();
        var attempt = new LoginRequest("limited@example.com", "Wrong0000");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            statuses.Add((await client.PostAsJsonAsync("/api/auth/login", attempt)).StatusCode);
        }

        // Five attempts are allowed through (and fail auth), the sixth is
        // throttled before it reaches credential verification.
        statuses.Take(5).Should().OnlyContain(s => s == HttpStatusCode.Unauthorized);
        statuses[5].Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Health_IsNotRateLimited()
    {
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 10; i++)
        {
            statuses.Add((await client.GetAsync("/api/auth/health")).StatusCode);
        }

        statuses.Should().OnlyContain(s => s == HttpStatusCode.OK);
    }
}
