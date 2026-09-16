using Serilog;
using AnalyticsPlatform.Api.Endpoints;
using AnalyticsPlatform.Api.HealthChecks;
using AnalyticsPlatform.Api.Middleware;
using AnalyticsPlatform.Application;
using AnalyticsPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddCheck<PowerBiHealthCheck>("powerbi")
    .AddCheck<SqlHealthCheck>("sql");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins != null && origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapAnalyticsEndpoints();
app.MapDashboardEndpoints();
app.MapDataSourceEndpoints();
app.MapDataQualityEndpoints();
app.MapPowerBiEndpoints();
app.MapReportEndpoints();
app.MapLlmEndpoints();
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program { }
