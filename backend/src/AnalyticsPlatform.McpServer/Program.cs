using Serilog;
using AnalyticsPlatform.Application;
using AnalyticsPlatform.Infrastructure;
using AnalyticsPlatform.McpServer.Hosting;
using AnalyticsPlatform.McpServer.Security;
using AnalyticsPlatform.McpServer.Tools;
using AnalyticsPlatform.McpServer.Tools.Analytics;
using AnalyticsPlatform.McpServer.Tools.Dashboards;
using AnalyticsPlatform.McpServer.Tools.DataSources;
using AnalyticsPlatform.McpServer.Tools.PowerBi;
using AnalyticsPlatform.McpServer.Tools.SecurityAudit;

namespace AnalyticsPlatform.McpServer;

public static class McpServerProgram
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

        // Core Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // MCP Security & Audit
        builder.Services.AddSingleton<ToolPermissionMiddleware>();
        builder.Services.AddSingleton<McpAuditLogger>();

        // Register all MCP Tools
        builder.Services.AddSingleton<IMcpTool, GetAnalyticsModelTool>();
        builder.Services.AddSingleton<IMcpTool, ValidateAnalyticsModelTool>();
        builder.Services.AddSingleton<IMcpTool, CompilePbirDefinitionTool>();
        builder.Services.AddSingleton<IMcpTool, CompileTmdlSemanticModelTool>();
        builder.Services.AddSingleton<IMcpTool, CompilePbipPackageTool>();
        builder.Services.AddSingleton<IMcpTool, PublishPbipToFabricTool>();
        builder.Services.AddSingleton<IMcpTool, GetDashboardDefinitionTool>();
        builder.Services.AddSingleton<IMcpTool, GetDataSourceSchemaTool>();
        builder.Services.AddSingleton<IMcpTool, QueryEventSummaryTool>();

        builder.Services.AddSingleton<ToolRegistry>();
        builder.Services.AddSingleton<StdioMcpServerHost>();

        // If launched with --stdio argument or in Stdio mode
        if (args.Contains("--stdio", StringComparer.OrdinalIgnoreCase))
        {
            var app = builder.Build();
            var stdioHost = app.Services.GetRequiredService<StdioMcpServerHost>();
            await stdioHost.RunAsync(CancellationToken.None);
            return;
        }

        var webApp = builder.Build();

        // Map SSE & Message endpoints for Web / Distributed Agent orchestration
        webApp.MapMcpSseEndpoints();

        webApp.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", service = "AnalyticsPlatform.McpServer" }));

        await webApp.RunAsync();
    }
}
