using ArchLens.Gateway.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ArchLens.Gateway.Tests.Middlewares;

public sealed class SecurityHeadersMiddlewareTests
{
    private readonly SecurityHeadersMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;
    private bool _nextDelegateCalled;

    public SecurityHeadersMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _nextDelegateCalled = false;
        _middleware = new SecurityHeadersMiddleware(Next);
    }

    private Task Next(HttpContext context)
    {
        _nextDelegateCalled = true;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetXContentTypeOptions()
    {
        await _middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Headers["X-Content-Type-Options"].ToString()
            .Should().Be("nosniff");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetXFrameOptions()
    {
        await _middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Headers["X-Frame-Options"].ToString()
            .Should().Be("DENY");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetXXssProtection()
    {
        await _middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Headers["X-XSS-Protection"].ToString()
            .Should().Be("0");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetReferrerPolicy()
    {
        await _middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Headers["Referrer-Policy"].ToString()
            .Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetPermissionsPolicy()
    {
        await _middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Headers["Permissions-Policy"].ToString()
            .Should().Be("camera=(), microphone=(), geolocation=()");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCacheControl()
    {
        await _middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Headers["Cache-Control"].ToString()
            .Should().Be("no-store");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextDelegate()
    {
        await _middleware.InvokeAsync(_httpContext);

        _nextDelegateCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetAllSixSecurityHeaders()
    {
        await _middleware.InvokeAsync(_httpContext);

        var headers = _httpContext.Response.Headers;
        headers.Should().ContainKey("X-Content-Type-Options");
        headers.Should().ContainKey("X-Frame-Options");
        headers.Should().ContainKey("X-XSS-Protection");
        headers.Should().ContainKey("Referrer-Policy");
        headers.Should().ContainKey("Permissions-Policy");
        headers.Should().ContainKey("Cache-Control");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetHeadersBeforeCallingNext()
    {
        var headersSetBeforeNext = false;
        var middleware = new SecurityHeadersMiddleware(context =>
        {
            headersSetBeforeNext = context.Response.Headers.ContainsKey("X-Content-Type-Options")
                && context.Response.Headers.ContainsKey("X-Frame-Options");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(_httpContext);

        headersSetBeforeNext.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotModifyExistingResponseBody()
    {
        _httpContext.Response.Body = new MemoryStream();

        await _middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Body.Length.Should().Be(0);
    }
}
