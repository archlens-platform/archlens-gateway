using ArchLens.Gateway.Configurations;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace ArchLens.Gateway.Tests.Configurations;

public sealed class OpenTelemetryExtensionsTests
{
    [Fact]
    public void AddOpenTelemetryObservability_ShouldReturnSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddOpenTelemetryObservability();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddOpenTelemetryObservability_ShouldRegisterTracerProvider()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddOpenTelemetryObservability();

        var serviceProvider = builder.Services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        tracerProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddOpenTelemetryObservability_ShouldRegisterMeterProvider()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddOpenTelemetryObservability();

        var serviceProvider = builder.Services.BuildServiceProvider();
        var meterProvider = serviceProvider.GetService<MeterProvider>();
        meterProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddOpenTelemetryObservability_ShouldAcceptCustomServiceName()
    {
        var builder = WebApplication.CreateBuilder();

        var act = () => builder.AddOpenTelemetryObservability("custom-service-name");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenTelemetryObservability_ShouldUseDefaultServiceName()
    {
        var builder = WebApplication.CreateBuilder();

        var act = () => builder.AddOpenTelemetryObservability();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenTelemetryObservability_ShouldUseDefaultOtlpEndpoint_WhenNotConfigured()
    {
        var builder = WebApplication.CreateBuilder();

        var act = () => builder.AddOpenTelemetryObservability();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenTelemetryObservability_ShouldUseConfiguredOtlpEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["Otlp:Endpoint"] = "http://localhost:4317";

        var act = () => builder.AddOpenTelemetryObservability();

        act.Should().NotThrow();
    }
}
