using System.Net;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.IntegrationTests;

public class HealthAndDocsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthAndDocsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy_200Ok()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task OpenApiV1_ShouldReturn200OkWithSwaggerJson()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"openapi\":");
        content.Should().Contain("Clinic Management System API v1");
    }

    [Fact]
    public async Task OpenApiV2_ShouldReturn200OkWithSwaggerJson()
    {
        var response = await _client.GetAsync("/openapi/v2.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"openapi\":");
        content.Should().Contain("Clinic Management System API v2");
    }

    [Fact]
    public async Task ScalarApiDocs_ShouldReturn200OkHtml()
    {
        var response = await _client.GetAsync("/scalar/v1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }
}

