using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ArchLens.Gateway.Tests.Integration;

public sealed class GatewayIntegrationTests : IClassFixture<GatewayWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GatewayIntegrationTests(GatewayWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ShouldContainSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("Permissions-Policy");
    }

    [Fact]
    public async Task HealthCheck_SecurityHeaders_ShouldHaveCorrectValues()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("0");
        response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");
        response.Headers.GetValues("Permissions-Policy").Should().Contain("camera=(), microphone=(), geolocation=()");
    }

    [Fact]
    public async Task UnknownRoute_ShouldStillContainSecurityHeaders()
    {
        var response = await _client.GetAsync("/nonexistent-route");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
    }
}

public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Key", "ThisIsASuperSecretKeyForTestingPurposesOnly1234567890!");
        builder.UseSetting("Jwt:Issuer", "archlens-auth");
        builder.UseSetting("Jwt:Audience", "archlens-services");
        builder.UseSetting("FrontendUrl", "http://localhost:3000");
    }
}
