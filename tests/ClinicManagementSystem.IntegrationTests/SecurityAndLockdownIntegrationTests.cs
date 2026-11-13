using System.Net;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.IntegrationTests;

public class SecurityAndLockdownIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityAndLockdownIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/v1/appointments")]
    [InlineData("/api/v1/patients")]
    [InlineData("/api/v1/billing/invoices")]
    public async Task ProtectedEndpoints_WhenAnonymous_ShouldReturn401Unauthorized(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResponseHeaders_ShouldContainSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact]
    public async Task ResponseHeaders_ShouldIncludeCorrelationId()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        var correlationId = response.Headers.GetValues("X-Correlation-Id").First();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }
}

