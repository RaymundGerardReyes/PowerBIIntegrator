using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AnalyticsPlatform.McpServer.Security;
using Xunit;

namespace AnalyticsPlatform.UnitTests.McpServer;

public sealed class ToolPermissionMiddlewareTests
{
    private readonly ToolPermissionMiddleware _middleware = new(NullLogger<ToolPermissionMiddleware>.Instance);

    [Fact]
    public void AuthorizeToolInvocation_WhenStandardTool_AlwaysAllows()
    {
        var authorized = _middleware.AuthorizeToolInvocation("get_analytics_model", "corr-1", isPrivilegedCaller: false, allowSensitive: false);
        authorized.Should().BeTrue();
    }

    [Fact]
    public void AuthorizeToolInvocation_WhenPrivilegedToolAndNotPrivilegedCaller_Denies()
    {
        var authorized = _middleware.AuthorizeToolInvocation("publish_pbip_to_fabric", "corr-2", isPrivilegedCaller: false);
        authorized.Should().BeFalse();
    }

    [Fact]
    public void AuthorizeToolInvocation_WhenPrivilegedToolAndPrivilegedCaller_Allows()
    {
        var authorized = _middleware.AuthorizeToolInvocation("publish_pbip_to_fabric", "corr-3", isPrivilegedCaller: true);
        authorized.Should().BeTrue();
    }

    [Fact]
    public void AuthorizeToolInvocation_WhenSensitiveToolAndSensitiveForbidden_Denies()
    {
        var authorized = _middleware.AuthorizeToolInvocation("query_event_summary", "corr-4", allowSensitive: false);
        authorized.Should().BeFalse();
    }

    [Fact]
    public void AuthorizeToolInvocation_WhenSensitiveToolAndSensitiveAllowed_Allows()
    {
        var authorized = _middleware.AuthorizeToolInvocation("query_event_summary", "corr-5", allowSensitive: true);
        authorized.Should().BeTrue();
    }
}

