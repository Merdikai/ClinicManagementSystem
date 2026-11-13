using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_EmptyCredentials_ShouldReturn400BadRequest_WithProblemDetails()
    {
        var loginDto = new LoginDto("", "");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Username is required");
    }

    [Fact]
    public async Task Login_ValidAdmin_ShouldReturn200Ok_AndSetHttpOnlyCookie()
    {
        var loginDto = new LoginDto("admin", "Admin@123456!");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResult.Should().NotBeNull();
        authResult!.Token.Should().NotBeNullOrWhiteSpace();
        authResult.Username.Should().Be("admin");
        authResult.Roles.Should().Contain("Admin");

        // Verify Set-Cookie header contains clinic_refresh_token
        response.Headers.Contains("Set-Cookie").Should().BeTrue();
        var cookies = string.Join(";", response.Headers.GetValues("Set-Cookie"));
        cookies.Should().Contain("clinic_refresh_token");
        cookies.ToLower().Should().Contain("httponly");
    }

    [Fact]
    public async Task RefreshToken_WithoutCsrfHeader_ShouldReturn403Forbidden()
    {
        // First login to get a cookie
        var loginDto = new LoginDto("admin", "Admin@123456!");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        // We do NOT add X-XSRF-TOKEN
        var refreshResponse = await _client.SendAsync(request);

        // Should be rejected by AntiForgery / CSRF protection
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

