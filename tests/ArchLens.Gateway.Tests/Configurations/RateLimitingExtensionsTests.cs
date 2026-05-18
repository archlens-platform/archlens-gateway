using System.Threading.RateLimiting;
using ArchLens.Gateway.Configurations;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArchLens.Gateway.Tests.Configurations;

public sealed class RateLimitingExtensionsTests
{
    [Fact]
    public void AddRateLimiting_ShouldReturnSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddRateLimiting();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddRateLimiting_ShouldRegisterRateLimiterOptions()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddRateLimiting();

        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<RateLimiterOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    [Fact]
    public void AddRateLimiting_ShouldConfigureGlobalLimiter()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddRateLimiting();

        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>();
        options.Value.GlobalLimiter.Should().NotBeNull();
    }

    [Fact]
    public void AddRateLimiting_ShouldConfigureOnRejectedHandler()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddRateLimiting();

        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>();
        options.Value.OnRejected.Should().NotBeNull();
    }

    [Fact]
    public async Task OnRejected_ShouldReturn429StatusCode()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddRateLimiting();
        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>();

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var lease = new FakeRateLimitLease(false);
        var onRejectedContext = new OnRejectedContext
        {
            HttpContext = httpContext,
            Lease = lease
        };

        await options.Value.OnRejected!(onRejectedContext, CancellationToken.None);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task OnRejected_ShouldSetRetryAfterHeader_WhenMetadataNotAvailable()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddRateLimiting();
        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>();

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var lease = new FakeRateLimitLease(false);
        var onRejectedContext = new OnRejectedContext
        {
            HttpContext = httpContext,
            Lease = lease
        };

        await options.Value.OnRejected!(onRejectedContext, CancellationToken.None);

        httpContext.Response.Headers.RetryAfter.ToString().Should().Be("60");
    }

    [Fact]
    public async Task OnRejected_ShouldWriteJsonErrorResponse()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddRateLimiting();
        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>();

        var httpContext = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var lease = new FakeRateLimitLease(false);
        var onRejectedContext = new OnRejectedContext
        {
            HttpContext = httpContext,
            Lease = lease
        };

        await options.Value.OnRejected!(onRejectedContext, CancellationToken.None);

        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("RATE_LIMIT_EXCEEDED");
        body.Should().Contain("Too many requests");
    }

    private sealed class FakeRateLimitLease(bool isAcquired) : RateLimitLease
    {
        public override bool IsAcquired => isAcquired;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
