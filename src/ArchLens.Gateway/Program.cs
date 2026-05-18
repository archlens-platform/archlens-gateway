using System.Text;
using ArchLens.Gateway.Configurations;
using ArchLens.Gateway.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", "gateway")
            .Enrich.WithProperty("Application", "archlens")
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = context.Configuration["Otlp:Endpoint"]
                    ?? "http://otel-collector:4317";
                options.Protocol = OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = "archlens-gateway"
                };
            }));

    builder.AddOpenTelemetryObservability();
    builder.AddRateLimiting();

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    var jwtSection = builder.Configuration.GetRequiredSection("Jwt");
    var jwtKey = jwtSection["Key"]
        ?? throw new InvalidOperationException("Configuration 'Jwt:Key' is required");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"] ?? "archlens-auth",
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"] ?? "archlens-services",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(5),
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                        context.Response.Headers.Append("Token-Expired", "true");
                    return Task.CompletedTask;
                },
            };
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser())
        .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins(
                    builder.Configuration.GetValue<string>("FrontendUrl") ?? "http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseRateLimiter();
    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapReverseProxy();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
