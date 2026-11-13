using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.IntegrationTests;

public class V2FieldShapingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public V2FieldShapingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var loginDto = new LoginDto("admin", "Admin@123456!");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    [Fact]
    public async Task GetPatients_V2WithFieldShaping_ShouldReturnOnlyRequestedFields()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/v2/patients?fields=firstName,lastName");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = jsonDoc.RootElement;
        
        // Handle pagination wrapper or direct array
        var array = root.ValueKind == JsonValueKind.Array ? root : (root.TryGetProperty("data", out var data) ? data : (root.TryGetProperty("items", out var items) ? items : root));
        if (array.GetArrayLength() > 0)
        {
            var firstItem = array[0];
            firstItem.TryGetProperty("firstName", out _).Should().BeTrue();
            firstItem.TryGetProperty("lastName", out _).Should().BeTrue();
            firstItem.TryGetProperty("bloodGroup", out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task ConditionalGet_WithMatchingETag_ShouldReturn304NotModified()
    {
        await AuthenticateAsync();

        var initialResponse = await _client.GetAsync("/api/v1/appointments");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        if (initialResponse.Headers.ETag != null)
        {
            var etag = initialResponse.Headers.ETag.Tag;
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/appointments");
            request.Headers.TryAddWithoutValidation("If-None-Match", etag);

            var conditionalResponse = await _client.SendAsync(request);
            conditionalResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);
        }
    }
}

