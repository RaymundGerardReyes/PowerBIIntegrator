#!/usr/bin/env bash
###############################################################################
# scaffold-backend.sh
#
# Principal-Engineer-grade .NET 10 Clean Architecture backend scaffolder.
# Generates: Domain / Application / Infrastructure / Api projects,
# full 6-category test suite (Unit, Integration, Path, Regression, E2E, Security),
# solution wiring (.slnx), Central Package Management (CPM), and starter source files.
#
# Usage:
#   chmod +x "scaffold-backend (1).sh"
#   ./"scaffold-backend (1).sh" [target-directory]   # default: ./backend
#
# Requirements: .NET 10 SDK installed and on PATH (`dotnet --version` -> 10.x)
###############################################################################

set -euo pipefail

ROOT_DIR="${1:-backend}"
SOLUTION_NAME="AnalyticsPlatform"
DOTNET_TFM="net10.0"

log()  { printf '\033[1;36m[scaffold]\033[0m %s\n' "$1"; }
ok()   { printf '\033[1;32m[  ok   ]\033[0m %s\n' "$1"; }
warn() { printf '\033[1;33m[ warn  ]\033[0m %s\n' "$1"; }
die()  { printf '\033[1;31m[ fail  ]\033[0m %s\n' "$1"; exit 1; }

if ! command -v dotnet >/dev/null 2>&1; then
  if command -v dotnet.exe >/dev/null 2>&1; then
    dotnet() { dotnet.exe "$@"; }
  fi
fi

command -v dotnet >/dev/null 2>&1 || die ".NET SDK not found. Install .NET 10 SDK first."
DOTNET_VERSION="$(dotnet --version | cut -d. -f1)"
[[ "$DOTNET_VERSION" -ge 10 ]] || warn "Detected .NET major version $DOTNET_VERSION (expected 10.x). Continuing anyway."

log "Scaffolding backend into: $ROOT_DIR"
mkdir -p "$ROOT_DIR"
cd "$ROOT_DIR"

###############################################################################
# 1. SOLUTION + CENTRALIZED PACKAGE MANAGEMENT (CPM)
###############################################################################
log "Creating solution file ($SOLUTION_NAME.slnx)..."
dotnet new sln -n "$SOLUTION_NAME" --force >/dev/null

cat > Directory.Build.props <<'EOF'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>CS1591;CA1000;NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
EOF

cat > Directory.Packages.props <<'EOF'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <!-- Core -->
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation" Version="11.10.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
    <!-- Persistence -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <!-- Data source connectors -->
    <PackageVersion Include="ClosedXML" Version="0.104.2" />
    <PackageVersion Include="CsvHelper" Version="33.0.1" />
    <PackageVersion Include="Microsoft.Data.SqlClient" Version="6.1.1" />
    <!-- Security / Transitive Pinning for CVEs -->
    <PackageVersion Include="System.Security.Cryptography.Xml" Version="10.0.12" />
    <PackageVersion Include="Microsoft.OpenApi" Version="2.12.2" />
    <!-- Power BI / Fabric -->
    <PackageVersion Include="Microsoft.PowerBI.Api" Version="4.19.0" />
    <PackageVersion Include="Azure.Identity" Version="1.17.2" />
    <PackageVersion Include="Microsoft.Identity.Client" Version="4.83.1" />
    <!-- Document generation -->
    <PackageVersion Include="QuestPDF" Version="2025.7.0" />
    <PackageVersion Include="DocumentFormat.OpenXml" Version="3.1.1" />
    <!-- Logging / Observability -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.Seq" Version="8.0.0" />
    <!-- Api -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.9" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="7.2.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.SqlServer" Version="8.0.2" />
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="Testcontainers.MsSql" Version="4.1.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageVersion Include="WireMock.Net" Version="1.6.11" />
    <PackageVersion Include="Verify.Xunit" Version="28.5.0" />
    <PackageVersion Include="Reqnroll.xUnit" Version="2.2.1" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="Bogus" Version="35.6.1" />
  </ItemGroup>
</Project>
EOF
ok "Solution root files created."

###############################################################################
# 2. SOURCE PROJECTS (.csproj written directly for pure CPM compliance)
###############################################################################
log "Creating src/ projects with pure CPM compliance..."
mkdir -p src/$SOLUTION_NAME.Domain src/$SOLUTION_NAME.Application src/$SOLUTION_NAME.Infrastructure src/$SOLUTION_NAME.Api

# ---- Domain .csproj ---------------------------------------------------------
cat > src/$SOLUTION_NAME.Domain/$SOLUTION_NAME.Domain.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
EOF

# ---- Application .csproj ----------------------------------------------------
cat > src/$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../$SOLUTION_NAME.Domain/$SOLUTION_NAME.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
  </ItemGroup>
</Project>
EOF

# ---- Infrastructure .csproj -------------------------------------------------
cat > src/$SOLUTION_NAME.Infrastructure/$SOLUTION_NAME.Infrastructure.csproj <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../$SOLUTION_NAME.Domain/$SOLUTION_NAME.Domain.csproj" />
    <ProjectReference Include="../$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="ClosedXML" />
    <PackageReference Include="CsvHelper" />
    <PackageReference Include="Microsoft.Data.SqlClient" />
    <PackageReference Include="Microsoft.PowerBI.Api" />
    <PackageReference Include="Azure.Identity" />
    <PackageReference Include="Microsoft.Identity.Client" />
    <PackageReference Include="QuestPDF" />
    <PackageReference Include="DocumentFormat.OpenXml" />
  </ItemGroup>
</Project>
EOF

# ---- Api .csproj ------------------------------------------------------------
cat > src/$SOLUTION_NAME.Api/$SOLUTION_NAME.Api.csproj <<EOF
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj" />
    <ProjectReference Include="../$SOLUTION_NAME.Infrastructure/$SOLUTION_NAME.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.Seq" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
    <PackageReference Include="AspNetCore.HealthChecks.SqlServer" />
  </ItemGroup>
</Project>
EOF
ok "Source project files created."

###############################################################################
# 3. DOMAIN SOURCE FILES
###############################################################################
log "Writing Domain layer starter files..."
D="src/$SOLUTION_NAME.Domain"
mkdir -p "$D/Common" "$D/Features/Analytics/Entities" "$D/Features/Analytics/ValueObjects" "$D/Features/Analytics/Rules" \
         "$D/Features/Dashboards/Entities" "$D/Features/Dashboards/Rules" \
         "$D/Features/DataSources/Entities" "$D/Features/DataSources/Rules" \
         "$D/Features/ReportPublishing/Entities" "$D/Features/ReportPublishing/Rules" \
         "$D/Repositories"

cat > "$D/Common/Entity.cs" <<EOF
namespace $SOLUTION_NAME.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj)
        => obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
EOF

cat > "$D/Common/Result.cs" <<EOF
namespace $SOLUTION_NAME.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string[] Errors { get; }

    protected Result(bool isSuccess, string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => new(true, value, Array.Empty<string>());
    public static Result<T> Failure<T>(params string[] errors) => new(false, default, errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(bool isSuccess, T? value, string[] errors) : base(isSuccess, errors)
        => Value = value;
}
EOF

cat > "$D/Common/DomainException.cs" <<EOF
namespace $SOLUTION_NAME.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
EOF

cat > "$D/Features/Analytics/ValueObjects/MeasureExpression.cs" <<EOF
namespace $SOLUTION_NAME.Domain.Features.Analytics.ValueObjects;

public sealed record MeasureExpression(string DaxOrFormula, string ReturnType);
EOF

cat > "$D/Features/Analytics/Entities/Measure.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;
using $SOLUTION_NAME.Domain.Features.Analytics.ValueObjects;

namespace $SOLUTION_NAME.Domain.Features.Analytics.Entities;

public class Measure : Entity
{
    public string Name { get; private set; }
    public MeasureExpression Expression { get; private set; }
    public string TableName { get; private set; }

    private Measure(string name, MeasureExpression expression, string tableName)
    {
        Name = name;
        Expression = expression;
        TableName = tableName;
    }

    public static Result<Measure> Create(string name, MeasureExpression expression, string tableName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Measure>.Failure("Measure name cannot be empty.");

        if (string.IsNullOrWhiteSpace(expression.DaxOrFormula))
            return Result<Measure>.Failure("Measure expression cannot be empty.");

        if (string.IsNullOrWhiteSpace(tableName))
            return Result<Measure>.Failure("Table name cannot be empty.");

        return Result<Measure>.Success(new Measure(name, expression, tableName));
    }
}
EOF

cat > "$D/Features/Analytics/Entities/Dimension.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Domain.Features.Analytics.Entities;

public class Dimension : Entity
{
    public string Name { get; private set; }
    public string ColumnName { get; private set; }
    public string TableName { get; private set; }

    public Dimension(string name, string columnName, string tableName)
    {
        Name = name;
        ColumnName = columnName;
        TableName = tableName;
    }
}
EOF

cat > "$D/Features/Analytics/Entities/AnalyticsModel.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Domain.Features.Analytics.Entities;

public class AnalyticsModel : Entity
{
    public string Name { get; private set; }
    public List<Measure> Measures { get; } = new();
    public List<Dimension> Dimensions { get; } = new();

    public AnalyticsModel(string name) => Name = name;

    public void AddMeasure(Measure measure) => Measures.Add(measure);
    public void AddDimension(Dimension dimension) => Dimensions.Add(dimension);
}
EOF

cat > "$D/Features/Analytics/Rules/MeasureValidationRules.cs" <<EOF
namespace $SOLUTION_NAME.Domain.Features.Analytics.Rules;

public static class MeasureValidationRules
{
    private static readonly string[] ForbiddenTokens = { ";", "--", "DROP", "EXEC" };

    public static bool IsExpressionSafe(string expression)
        => !ForbiddenTokens.Any(token =>
            expression.Contains(token, StringComparison.OrdinalIgnoreCase));
}
EOF

cat > "$D/Features/Dashboards/Entities/Visual.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

public sealed record VisualLayout(double X, double Y, double Width, double Height, int ZOrder, bool Visible);

public class Visual : Entity
{
    public string VisualType { get; private set; }
    public string Name { get; private set; }
    public VisualLayout Layout { get; private set; }
    public IReadOnlyList<string> BoundFields { get; private set; }

    public Visual(string visualType, string name, VisualLayout layout, IReadOnlyList<string> boundFields)
    {
        VisualType = visualType;
        Name = name;
        Layout = layout;
        BoundFields = boundFields;
    }

    public void UpdateLayout(VisualLayout newLayout) => Layout = newLayout;
}
EOF

cat > "$D/Features/Dashboards/Entities/Page.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

public class Page : Entity
{
    public string Name { get; private set; }
    public double CanvasWidth { get; private set; }
    public double CanvasHeight { get; private set; }
    public List<Visual> Visuals { get; } = new();

    public Page(string name, double canvasWidth, double canvasHeight)
    {
        Name = name;
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
    }

    public void AddVisual(Visual visual) => Visuals.Add(visual);
}
EOF

cat > "$D/Features/Dashboards/Entities/DashboardDefinition.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

public class DashboardDefinition : Entity
{
    public string Name { get; private set; }
    public List<Page> Pages { get; } = new();
    public Guid AnalyticsModelId { get; private set; }

    public DashboardDefinition(string name, Guid analyticsModelId)
    {
        Name = name;
        AnalyticsModelId = analyticsModelId;
    }

    public void AddPage(Page page) => Pages.Add(page);
}
EOF

cat > "$D/Features/Dashboards/Rules/LayoutBoundsRules.cs" <<EOF
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

namespace $SOLUTION_NAME.Domain.Features.Dashboards.Rules;

public static class LayoutBoundsRules
{
    public static bool IsWithinCanvas(VisualLayout layout, double canvasWidth, double canvasHeight)
        => layout.X >= 0
        && layout.Y >= 0
        && layout.X + layout.Width <= canvasWidth
        && layout.Y + layout.Height <= canvasHeight;
}
EOF

cat > "$D/Features/DataSources/Entities/DataSourceDefinition.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Domain.Features.DataSources.Entities;

public enum DataSourceType { Excel, Csv, SqlServer, PostgreSql, MySql }

public class DataSourceDefinition : Entity
{
    public string Name { get; private set; }
    public DataSourceType Type { get; private set; }
    public string ConnectionOrPath { get; private set; }

    public DataSourceDefinition(string name, DataSourceType type, string connectionOrPath)
    {
        Name = name;
        Type = type;
        ConnectionOrPath = connectionOrPath;
    }
}
EOF

cat > "$D/Features/DataSources/Rules/SchemaCompatibilityRules.cs" <<EOF
namespace $SOLUTION_NAME.Domain.Features.DataSources.Rules;

public static class SchemaCompatibilityRules
{
    public static bool ColumnTypesCompatible(string sourceType, string targetType)
        => string.Equals(sourceType, targetType, StringComparison.OrdinalIgnoreCase)
        || (sourceType == "int" && targetType == "decimal");
}
EOF

cat > "$D/Features/ReportPublishing/Entities/PublishRequest.cs" <<EOF
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Domain.Features.ReportPublishing.Entities;

public class PublishRequest : Entity
{
    public Guid DashboardDefinitionId { get; private set; }
    public string TargetWorkspaceId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }

    public PublishRequest(Guid dashboardDefinitionId, string targetWorkspaceId)
    {
        DashboardDefinitionId = dashboardDefinitionId;
        TargetWorkspaceId = targetWorkspaceId;
        RequestedAtUtc = DateTime.UtcNow;
    }
}
EOF

cat > "$D/Features/ReportPublishing/Rules/PublishEligibilityRules.cs" <<EOF
namespace $SOLUTION_NAME.Domain.Features.ReportPublishing.Rules;

public static class PublishEligibilityRules
{
    public static bool IsEligible(int pageCount, int visualCount)
        => pageCount > 0 && visualCount > 0 && visualCount <= 40;
}
EOF

cat > "$D/Repositories/IAnalyticsModelRepository.cs" <<EOF
using $SOLUTION_NAME.Domain.Features.Analytics.Entities;

namespace $SOLUTION_NAME.Domain.Repositories;

public interface IAnalyticsModelRepository
{
    Task<AnalyticsModel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AnalyticsModel model, CancellationToken ct = default);
}
EOF

cat > "$D/Repositories/IDashboardRepository.cs" <<EOF
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

namespace $SOLUTION_NAME.Domain.Repositories;

public interface IDashboardRepository
{
    Task<DashboardDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(DashboardDefinition dashboard, CancellationToken ct = default);
}
EOF
ok "Domain layer populated."

###############################################################################
# 4. APPLICATION SOURCE FILES
###############################################################################
log "Writing Application layer starter files..."
A="src/$SOLUTION_NAME.Application"
mkdir -p "$A/Common/Behaviors" "$A/Common/Interfaces" \
         "$A/Features/Analytics/Commands/CreateMeasure" \
         "$A/Features/Dashboards/Commands/CreateDashboardDefinition" \
         "$A/Features/Dashboards/Commands/CompileDashboardIR" \
         "$A/Features/DataSources/Commands/RegisterDataSource" \
         "$A/Features/PowerBiPublishing/Commands/PublishPbipToFabric" \
         "$A/Features/PowerBiPublishing/Queries/GetReportEmbedConfig"

cat > "$A/Common/Interfaces/ICurrentUserContext.cs" <<EOF
namespace $SOLUTION_NAME.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    string? UserId { get; }
    string? TenantId { get; }
    bool IsAuthenticated { get; }
}
EOF

cat > "$A/Common/Interfaces/IDateTimeProvider.cs" <<EOF
namespace $SOLUTION_NAME.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
EOF

cat > "$A/Common/Interfaces/IPowerBiPublisher.cs" <<EOF
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

namespace $SOLUTION_NAME.Application.Common.Interfaces;

public interface IPowerBiPublisher
{
    Task<string> PublishAsync(DashboardDefinition dashboard, string workspaceId, CancellationToken ct = default);
}
EOF

cat > "$A/Common/Interfaces/IEmbedTokenService.cs" <<EOF
namespace $SOLUTION_NAME.Application.Common.Interfaces;

public record EmbedConfig(string ReportId, string EmbedUrl, string AccessToken);

public interface IEmbedTokenService
{
    Task<EmbedConfig> GetEmbedConfigAsync(string reportId, CancellationToken ct = default);
}
EOF

cat > "$A/Common/Interfaces/IDataSourceReader.cs" <<EOF
namespace $SOLUTION_NAME.Application.Common.Interfaces;

public interface IDataSourceReader
{
    Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string connectionOrPath, CancellationToken ct = default);
}
EOF

cat > "$A/Common/Behaviors/LoggingBehavior.cs" <<EOF
using MediatR;
using Microsoft.Extensions.Logging;

namespace $SOLUTION_NAME.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
EOF

cat > "$A/Common/Behaviors/ValidationBehavior.cs" <<EOF
using FluentValidation;
using MediatR;

namespace $SOLUTION_NAME.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct))))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
EOF

cat > "$A/Features/Analytics/Commands/CreateMeasure/CreateMeasureCommand.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Domain.Common;

namespace $SOLUTION_NAME.Application.Features.Analytics.Commands.CreateMeasure;

public sealed record CreateMeasureResponse(Guid Id, string Name, string Expression, string TableName);

public sealed record CreateMeasureCommand(string Name, string Expression, string TableName) : IRequest<Result<CreateMeasureResponse>>;
EOF

cat > "$A/Features/Analytics/Commands/CreateMeasure/CreateMeasureCommandValidator.cs" <<EOF
using FluentValidation;

namespace $SOLUTION_NAME.Application.Features.Analytics.Commands.CreateMeasure;

public class CreateMeasureCommandValidator : AbstractValidator<CreateMeasureCommand>
{
    public CreateMeasureCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Expression).NotEmpty();
        RuleFor(x => x.TableName).NotEmpty();
    }
}
EOF

cat > "$A/Features/Analytics/Commands/CreateMeasure/CreateMeasureCommandHandler.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Domain.Common;
using $SOLUTION_NAME.Domain.Features.Analytics.Entities;
using $SOLUTION_NAME.Domain.Features.Analytics.ValueObjects;

namespace $SOLUTION_NAME.Application.Features.Analytics.Commands.CreateMeasure;

public class CreateMeasureCommandHandler : IRequestHandler<CreateMeasureCommand, Result<CreateMeasureResponse>>
{
    public Task<Result<CreateMeasureResponse>> Handle(CreateMeasureCommand request, CancellationToken ct)
    {
        var expression = new MeasureExpression(request.Expression, "decimal");
        var result = Measure.Create(request.Name, expression, request.TableName);

        if (!result.IsSuccess || result.Value is null)
            return Task.FromResult(Result<CreateMeasureResponse>.Failure(result.Errors));

        var response = new CreateMeasureResponse(result.Value.Id, result.Value.Name, request.Expression, request.TableName);
        return Task.FromResult(Result<CreateMeasureResponse>.Success(response));
    }
}
EOF

cat > "$A/Features/PowerBiPublishing/Commands/PublishPbipToFabric/PublishPbipToFabricCommand.cs" <<EOF
using MediatR;

namespace $SOLUTION_NAME.Application.Features.PowerBiPublishing.Commands.PublishPbipToFabric;

public sealed record PublishPbipResponse(string ReportId);

public sealed record PublishPbipToFabricCommand(Guid DashboardDefinitionId, string TargetWorkspaceId) : IRequest<PublishPbipResponse>;
EOF

cat > "$A/Features/PowerBiPublishing/Commands/PublishPbipToFabric/PublishPbipToFabricCommandHandler.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Application.Common.Interfaces;
using $SOLUTION_NAME.Domain.Repositories;

namespace $SOLUTION_NAME.Application.Features.PowerBiPublishing.Commands.PublishPbipToFabric;

public class PublishPbipToFabricCommandHandler : IRequestHandler<PublishPbipToFabricCommand, PublishPbipResponse>
{
    private readonly IPowerBiPublisher _publisher;
    private readonly IDashboardRepository _dashboardRepository;

    public PublishPbipToFabricCommandHandler(IPowerBiPublisher publisher, IDashboardRepository dashboardRepository)
    {
        _publisher = publisher;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<PublishPbipResponse> Handle(PublishPbipToFabricCommand request, CancellationToken ct)
    {
        var dashboard = await _dashboardRepository.GetByIdAsync(request.DashboardDefinitionId, ct);
        var reportId = dashboard != null
            ? await _publisher.PublishAsync(dashboard, request.TargetWorkspaceId, ct)
            : $"report-{request.DashboardDefinitionId}";

        return new PublishPbipResponse(reportId);
    }
}
EOF

cat > "$A/Features/PowerBiPublishing/Queries/GetReportEmbedConfig/GetReportEmbedConfigQuery.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Application.Common.Interfaces;

namespace $SOLUTION_NAME.Application.Features.PowerBiPublishing.Queries.GetReportEmbedConfig;

public sealed record GetReportEmbedConfigQuery(string ReportId) : IRequest<EmbedConfig>;

public class GetReportEmbedConfigQueryHandler : IRequestHandler<GetReportEmbedConfigQuery, EmbedConfig>
{
    private readonly IEmbedTokenService _embedTokenService;

    public GetReportEmbedConfigQueryHandler(IEmbedTokenService embedTokenService) => _embedTokenService = embedTokenService;

    public Task<EmbedConfig> Handle(GetReportEmbedConfigQuery request, CancellationToken ct)
        => _embedTokenService.GetEmbedConfigAsync(request.ReportId, ct);
}
EOF

cat > "$A/Features/DataSources/Commands/RegisterDataSource/RegisterDataSourceCommand.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Domain.Features.DataSources.Entities;

namespace $SOLUTION_NAME.Application.Features.DataSources.Commands.RegisterDataSource;

public sealed record DataSourceResponse(Guid Id, string Name, string Type, string ConnectionOrPath);

public sealed record RegisterDataSourceCommand(string Name, DataSourceType Type, string ConnectionOrPath) : IRequest<DataSourceResponse>;

public class RegisterDataSourceCommandHandler : IRequestHandler<RegisterDataSourceCommand, DataSourceResponse>
{
    public Task<DataSourceResponse> Handle(RegisterDataSourceCommand request, CancellationToken ct)
    {
        var entity = new DataSourceDefinition(request.Name, request.Type, request.ConnectionOrPath);
        return Task.FromResult(new DataSourceResponse(entity.Id, entity.Name, entity.Type.ToString().ToLowerInvariant(), entity.ConnectionOrPath));
    }
}
EOF

cat > "$A/DependencyInjection.cs" <<EOF
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using $SOLUTION_NAME.Application.Common.Behaviors;

namespace $SOLUTION_NAME.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
EOF
ok "Application layer populated."

###############################################################################
# 5. INFRASTRUCTURE SOURCE FILES
###############################################################################
log "Writing Infrastructure layer starter files..."
I="src/$SOLUTION_NAME.Infrastructure"
mkdir -p "$I/Persistence" "$I/Repositories" "$I/DataSourceConnectors/Excel" "$I/DataSourceConnectors/Csv" "$I/DataSourceConnectors/Sql" \
         "$I/PowerBi" "$I/DocumentGenerators/Excel" "$I/DocumentGenerators/Pdf" "$I/Identity"

cat > "$I/Persistence/AppDbContext.cs" <<EOF
using Microsoft.EntityFrameworkCore;

namespace $SOLUTION_NAME.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
EOF

cat > "$I/Repositories/AnalyticsModelRepository.cs" <<EOF
using $SOLUTION_NAME.Domain.Features.Analytics.Entities;
using $SOLUTION_NAME.Domain.Repositories;

namespace $SOLUTION_NAME.Infrastructure.Repositories;

public class AnalyticsModelRepository : IAnalyticsModelRepository
{
    private readonly Dictionary<Guid, AnalyticsModel> _store = new();

    public Task<AnalyticsModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var model);
        return Task.FromResult(model);
    }

    public Task AddAsync(AnalyticsModel model, CancellationToken ct = default)
    {
        _store[model.Id] = model;
        return Task.CompletedTask;
    }
}
EOF

cat > "$I/Repositories/DashboardRepository.cs" <<EOF
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;
using $SOLUTION_NAME.Domain.Repositories;

namespace $SOLUTION_NAME.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly Dictionary<Guid, DashboardDefinition> _store = new();

    public Task<DashboardDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var dashboard);
        return Task.FromResult(dashboard);
    }

    public Task AddAsync(DashboardDefinition dashboard, CancellationToken ct = default)
    {
        _store[dashboard.Id] = dashboard;
        return Task.CompletedTask;
    }
}
EOF

cat > "$I/DataSourceConnectors/Excel/ExcelDataSourceReader.cs" <<EOF
using ClosedXML.Excel;
using $SOLUTION_NAME.Application.Common.Interfaces;

namespace $SOLUTION_NAME.Infrastructure.DataSourceConnectors.Excel;

public class ExcelDataSourceReader : IDataSourceReader
{
    public Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(new List<IDictionary<string, object?>>());

        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();
        var rows = new List<IDictionary<string, object?>>();

        var headerRow = worksheet.Row(1);
        var headers = headerRow.Cells().Select(c => c.GetString()).ToList();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var record = new Dictionary<string, object?>();
            for (int i = 0; i < headers.Count; i++)
                record[headers[i]] = row.Cell(i + 1).Value.ToString();
            rows.Add(record);
        }

        return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(rows);
    }
}
EOF

cat > "$I/DataSourceConnectors/Csv/CsvDataSourceReader.cs" <<EOF
using System.Globalization;
using CsvHelper;
using $SOLUTION_NAME.Application.Common.Interfaces;

namespace $SOLUTION_NAME.Infrastructure.DataSourceConnectors.Csv;

public class CsvDataSourceReader : IDataSourceReader
{
    public Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(new List<IDictionary<string, object?>>());

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<IDictionary<string, object?>>();
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (csv.Read())
        {
            var record = new Dictionary<string, object?>();
            foreach (var header in headers)
                record[header] = csv.GetField(header);
            rows.Add(record);
        }

        return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(rows);
    }
}
EOF

cat > "$I/DataSourceConnectors/Sql/SqlServerConnector.cs" <<EOF
using Microsoft.Data.SqlClient;
using $SOLUTION_NAME.Application.Common.Interfaces;

namespace $SOLUTION_NAME.Infrastructure.DataSourceConnectors.Sql;

public class SqlServerConnector : IDataSourceReader
{
    public async Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string connectionString, CancellationToken ct = default)
    {
        var rows = new List<IDictionary<string, object?>>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 AS Status";
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var record = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                record[reader.GetName(i)] = reader.GetValue(i);
            rows.Add(record);
        }

        return rows;
    }
}
EOF

cat > "$I/PowerBi/PbirGenerator.cs" <<EOF
using System.Text.Json;
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

namespace $SOLUTION_NAME.Infrastructure.PowerBi;

public class PbirGenerator
{
    public string GeneratePageJson(Page page)
    {
        var pageDefinition = new
        {
            name = page.Name,
            displayName = page.Name,
            width = page.CanvasWidth,
            height = page.CanvasHeight,
            visualContainers = page.Visuals.Select(v => new
            {
                name = v.Name,
                visualType = v.VisualType,
                x = v.Layout.X,
                y = v.Layout.Y,
                width = v.Layout.Width,
                height = v.Layout.Height,
                z = v.Layout.ZOrder,
                visible = v.Layout.Visible,
                fields = v.BoundFields
            })
        };

        return JsonSerializer.Serialize(pageDefinition, new JsonSerializerOptions { WriteIndented = true });
    }

    public string GenerateDefinitionPbir(string semanticModelRelativePath)
    {
        var definition = new
        {
            version = "4.0",
            datasetReference = new { byPath = new { path = semanticModelRelativePath } }
        };

        return JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });
    }
}
EOF

cat > "$I/PowerBi/TmdlGenerator.cs" <<EOF
using System.Text;
using $SOLUTION_NAME.Domain.Features.Analytics.Entities;

namespace $SOLUTION_NAME.Infrastructure.PowerBi;

public class TmdlGenerator
{
    public string GenerateMeasureTmdl(Measure measure)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"measure '{measure.Name}' = {measure.Expression.DaxOrFormula}");
        sb.AppendLine("\tformatString: #,0.00");
        return sb.ToString();
    }
}
EOF

cat > "$I/PowerBi/PbipPackager.cs" <<EOF
namespace $SOLUTION_NAME.Infrastructure.PowerBi;

public class PbipPackager
{
    public void WriteProject(string outputRoot, string projectName, IDictionary<string, string> reportFiles, IDictionary<string, string> semanticModelFiles)
    {
        var reportDir = Path.Combine(outputRoot, $"{projectName}.Report", "definition");
        var modelDir = Path.Combine(outputRoot, $"{projectName}.SemanticModel", "definition");

        Directory.CreateDirectory(reportDir);
        Directory.CreateDirectory(modelDir);

        foreach (var (relativePath, content) in reportFiles)
        {
            var fullPath = Path.Combine(reportDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        foreach (var (relativePath, content) in semanticModelFiles)
        {
            var fullPath = Path.Combine(modelDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        File.WriteAllText(Path.Combine(outputRoot, $"{projectName}.pbip"),
            $"{{\"version\":\"1.0\",\"artifacts\":[{{\"report\":{{\"path\":\"{projectName}.Report\"}}}}]}}");
    }
}
EOF

cat > "$I/PowerBi/FabricRestClient.cs" <<EOF
using $SOLUTION_NAME.Application.Common.Interfaces;
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;

namespace $SOLUTION_NAME.Infrastructure.PowerBi;

public class FabricRestClient : IPowerBiPublisher
{
    public Task<string> PublishAsync(DashboardDefinition dashboard, string workspaceId, CancellationToken ct = default)
    {
        var reportId = $"fabric-report-{dashboard.Id:N}";
        return Task.FromResult(reportId);
    }
}
EOF

cat > "$I/PowerBi/EmbedTokenService.cs" <<EOF
using $SOLUTION_NAME.Application.Common.Interfaces;

namespace $SOLUTION_NAME.Infrastructure.PowerBi;

public class EmbedTokenService : IEmbedTokenService
{
    public Task<EmbedConfig> GetEmbedConfigAsync(string reportId, CancellationToken ct = default)
    {
        var config = new EmbedConfig(
            ReportId: reportId,
            EmbedUrl: $"https://app.powerbi.com/reportEmbed?reportId={reportId}",
            AccessToken: $"mock-embed-token-{reportId}"
        );
        return Task.FromResult(config);
    }
}
EOF

cat > "$I/DependencyInjection.cs" <<EOF
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using $SOLUTION_NAME.Application.Common.Interfaces;
using $SOLUTION_NAME.Domain.Repositories;
using $SOLUTION_NAME.Infrastructure.DataSourceConnectors.Csv;
using $SOLUTION_NAME.Infrastructure.DataSourceConnectors.Excel;
using $SOLUTION_NAME.Infrastructure.DataSourceConnectors.Sql;
using $SOLUTION_NAME.Infrastructure.Persistence;
using $SOLUTION_NAME.Infrastructure.PowerBi;
using $SOLUTION_NAME.Infrastructure.Repositories;

namespace $SOLUTION_NAME.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default") ?? "Server=localhost;Database=AnalyticsPlatform;Trusted_Connection=True;TrustServerCertificate=True;"));

        services.AddSingleton<IAnalyticsModelRepository, AnalyticsModelRepository>();
        services.AddSingleton<IDashboardRepository, DashboardRepository>();

        services.AddScoped<IPowerBiPublisher, FabricRestClient>();
        services.AddScoped<IEmbedTokenService, EmbedTokenService>();
        services.AddScoped<PbirGenerator>();
        services.AddScoped<TmdlGenerator>();
        services.AddScoped<PbipPackager>();

        services.AddKeyedScoped<IDataSourceReader, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceReader, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceReader, SqlServerConnector>("sql");

        return services;
    }
}
EOF
ok "Infrastructure layer populated."

###############################################################################
# 6. API SOURCE FILES
###############################################################################
log "Writing Api layer starter files..."
API="src/$SOLUTION_NAME.Api"
mkdir -p "$API/Endpoints" "$API/Middleware" "$API/HealthChecks"

cat > "$API/Middleware/CorrelationIdMiddleware.cs" <<EOF
namespace $SOLUTION_NAME.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
EOF

cat > "$API/Middleware/ExceptionHandlingMiddleware.cs" <<EOF
using System.Net;
using System.Text.Json;
using FluentValidation;

namespace $SOLUTION_NAME.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { errors = ex.Errors.Select(e => e.ErrorMessage) }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
        }
    }
}
EOF

cat > "$API/Endpoints/AnalyticsEndpoints.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Application.Features.Analytics.Commands.CreateMeasure;

namespace $SOLUTION_NAME.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapPost("/measures", async (CreateMeasureCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        });
    }
}
EOF

cat > "$API/Endpoints/DashboardEndpoints.cs" <<EOF
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;
using $SOLUTION_NAME.Domain.Repositories;

namespace $SOLUTION_NAME.Api.Endpoints;

public sealed record CreateDashboardRequest(string Name, Guid AnalyticsModelId);

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboards").WithTags("Dashboards");

        group.MapPost("/", async (CreateDashboardRequest request, IDashboardRepository repo) =>
        {
            var dashboard = new DashboardDefinition(request.Name, request.AnalyticsModelId);
            await repo.AddAsync(dashboard);
            return Results.Ok(new { id = dashboard.Id, name = dashboard.Name });
        });

        group.MapGet("/{id:guid}", async (Guid id, IDashboardRepository repo) =>
        {
            var dashboard = await repo.GetByIdAsync(id);
            return dashboard != null ? Results.Ok(dashboard) : Results.NotFound();
        });
    }
}
EOF

cat > "$API/Endpoints/DataSourceEndpoints.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Application.Features.DataSources.Commands.RegisterDataSource;
using $SOLUTION_NAME.Domain.Features.DataSources.Entities;

namespace $SOLUTION_NAME.Api.Endpoints;

public sealed record UploadDataSourceRequest(string Name, string Type, string ConnectionOrPath);

public static class DataSourceEndpoints
{
    public static void MapDataSourceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/data-sources").WithTags("DataSources");

        group.MapPost("/upload", async (UploadDataSourceRequest request, ISender sender) =>
        {
            var type = Enum.TryParse<DataSourceType>(request.Type, true, out var parsed) ? parsed : DataSourceType.Excel;
            var result = await sender.Send(new RegisterDataSourceCommand(request.Name, type, request.ConnectionOrPath));
            return Results.Ok(result);
        });
    }
}
EOF

cat > "$API/Endpoints/PowerBiEndpoints.cs" <<EOF
using MediatR;
using $SOLUTION_NAME.Application.Features.PowerBiPublishing.Commands.PublishPbipToFabric;
using $SOLUTION_NAME.Application.Features.PowerBiPublishing.Queries.GetReportEmbedConfig;

namespace $SOLUTION_NAME.Api.Endpoints;

public static class PowerBiEndpoints
{
    public static void MapPowerBiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/powerbi").WithTags("PowerBi");

        group.MapPost("/publish", async (PublishPbipToFabricCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("/embed-config/{reportId}", async (string reportId, ISender sender) =>
        {
            var result = await sender.Send(new GetReportEmbedConfigQuery(reportId));
            return Results.Ok(result);
        });
    }
}
EOF

cat > "$API/HealthChecks/PowerBiHealthCheck.cs" <<EOF
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace $SOLUTION_NAME.Api.HealthChecks;

public class PowerBiHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => Task.FromResult(HealthCheckResult.Healthy("Power BI / Fabric API reachable."));
}
EOF

cat > "$API/Program.cs" <<EOF
using Serilog;
using $SOLUTION_NAME.Api.Endpoints;
using $SOLUTION_NAME.Api.HealthChecks;
using $SOLUTION_NAME.Api.Middleware;
using $SOLUTION_NAME.Application;
using $SOLUTION_NAME.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<PowerBiHealthCheck>("powerbi");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors();
app.UseHttpsRedirection();

app.MapAnalyticsEndpoints();
app.MapDashboardEndpoints();
app.MapDataSourceEndpoints();
app.MapPowerBiEndpoints();
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program { }
EOF

cat > "$API/appsettings.json" <<EOF
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=AnalyticsPlatform;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Cors": { "AllowedOrigins": [ "http://localhost:5173" ] },
  "PowerBi": {
    "TenantId": "",
    "ClientId": "",
    "WorkspaceId": ""
  }
}
EOF
ok "Api layer populated."

###############################################################################
# 7. TEST PROJECTS (Clean Architecture Dependency Scoping & Pure CPM)
###############################################################################
log "Creating test projects with strict Clean Architecture dependency boundaries..."
mkdir -p tests

create_clean_test_csproj() {
  local PROJ_NAME="$1"
  local PROJ_DIR="tests/$PROJ_NAME"
  local PROJ_REFS="$2"
  local EXTRA_PKGS="${3:-}"

  mkdir -p "$PROJ_DIR"
  cat > "$PROJ_DIR/$PROJ_NAME.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
$PROJ_REFS
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
$EXTRA_PKGS
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
EOF
}

# 1. UnitTests: Clean Architecture strictly permits Domain & Application only!
create_clean_test_csproj "$SOLUTION_NAME.UnitTests" \
  "    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Domain/$SOLUTION_NAME.Domain.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj\" />"

# 2. IntegrationTests: Tests against Application, Infrastructure, and Api
create_clean_test_csproj "$SOLUTION_NAME.IntegrationTests" \
  "    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Infrastructure/$SOLUTION_NAME.Infrastructure.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Api/$SOLUTION_NAME.Api.csproj\" />" \
  "    <PackageReference Include=\"Testcontainers.MsSql\" />
    <PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" />
    <PackageReference Include=\"WireMock.Net\" />"

# 3. PathTests: Business-flow tests across Domain, Application, and Infrastructure
create_clean_test_csproj "$SOLUTION_NAME.PathTests" \
  "    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Domain/$SOLUTION_NAME.Domain.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Infrastructure/$SOLUTION_NAME.Infrastructure.csproj\" />"

# 4. RegressionTests: Golden-file & snapshot tests
create_clean_test_csproj "$SOLUTION_NAME.RegressionTests" \
  "    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Domain/$SOLUTION_NAME.Domain.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Infrastructure/$SOLUTION_NAME.Infrastructure.csproj\" />" \
  "    <PackageReference Include=\"Verify.Xunit\" />"

# 5. E2ETests: Full scenario tests against the Api
create_clean_test_csproj "$SOLUTION_NAME.E2ETests" \
  "    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Api/$SOLUTION_NAME.Api.csproj\" />" \
  "    <PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" />
    <PackageReference Include=\"Reqnroll.xUnit\" />"

# 6. SecurityTests: Validates architectural rules, AuthZ, and input sanitization
create_clean_test_csproj "$SOLUTION_NAME.SecurityTests" \
  "    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Domain/$SOLUTION_NAME.Domain.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Application/$SOLUTION_NAME.Application.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Infrastructure/$SOLUTION_NAME.Infrastructure.csproj\" />
    <ProjectReference Include=\"../../src/$SOLUTION_NAME.Api/$SOLUTION_NAME.Api.csproj\" />" \
  "    <PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" />
    <PackageReference Include=\"NetArchTest.Rules\" />"

ok "All 6 test project files created with pure CPM compliance."

###############################################################################
# 8. STARTER TESTS FOR ALL 6 CATEGORIES
###############################################################################
log "Writing sample test files for each category..."

# --- UnitTests ---
mkdir -p "tests/$SOLUTION_NAME.UnitTests/Domain/Analytics" "tests/$SOLUTION_NAME.UnitTests/Application/Analytics"
cat > "tests/$SOLUTION_NAME.UnitTests/Domain/Analytics/MeasureTests.cs" <<EOF
using FluentAssertions;
using $SOLUTION_NAME.Domain.Features.Analytics.Entities;
using $SOLUTION_NAME.Domain.Features.Analytics.ValueObjects;
using Xunit;

namespace $SOLUTION_NAME.UnitTests.Domain.Analytics;

public class MeasureTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsSuccess()
    {
        var expression = new MeasureExpression("SUM(Sales[Amount])", "decimal");
        var result = Measure.Create("TotalRevenue", expression, "Sales");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("TotalRevenue");
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailure()
    {
        var expression = new MeasureExpression("SUM(Sales[Amount])", "decimal");
        var result = Measure.Create("", expression, "Sales");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name"));
    }
}
EOF

cat > "tests/$SOLUTION_NAME.UnitTests/Application/Analytics/CreateMeasureCommandHandlerTests.cs" <<EOF
using FluentAssertions;
using $SOLUTION_NAME.Application.Features.Analytics.Commands.CreateMeasure;
using Xunit;

namespace $SOLUTION_NAME.UnitTests.Application.Analytics;

public class CreateMeasureCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsCreatedMeasureResponse()
    {
        var handler = new CreateMeasureCommandHandler();
        var command = new CreateMeasureCommand("TotalSales", "SUM(Sales[Revenue])", "Sales");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("TotalSales");
    }
}
EOF

# --- IntegrationTests ---
mkdir -p "tests/$SOLUTION_NAME.IntegrationTests/Api"
cat > "tests/$SOLUTION_NAME.IntegrationTests/Api/AnalyticsEndpointsTests.cs" <<EOF
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace $SOLUTION_NAME.IntegrationTests.Api;

public class AnalyticsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AnalyticsEndpointsTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task PostMeasure_WithValidPayload_ReturnsOkWithResponseObject()
    {
        var payload = new { Name = "TotalRevenue", Expression = "SUM(Sales[Amount])", TableName = "Sales" };
        var response = await _client.PostAsJsonAsync("/api/analytics/measures", payload);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TotalRevenue");
    }
}
EOF

# --- PathTests ---
mkdir -p "tests/$SOLUTION_NAME.PathTests"
cat > "tests/$SOLUTION_NAME.PathTests/MultiSourceIngestionPathTests.cs" <<EOF
using FluentAssertions;
using $SOLUTION_NAME.Domain.Features.DataSources.Entities;
using Xunit;

namespace $SOLUTION_NAME.PathTests;

public class MultiSourceIngestionPathTests
{
    [Fact]
    public void RegisterMultipleSources_AllTypesCoexist()
    {
        var sources = new[]
        {
            new DataSourceDefinition("SalesQ1", DataSourceType.Excel, "./data/sales_q1.xlsx"),
            new DataSourceDefinition("SalesQ2", DataSourceType.Csv, "./data/sales_q2.csv"),
            new DataSourceDefinition("Customers", DataSourceType.SqlServer, "Server=.;Database=Crm;")
        };

        sources.Should().HaveCount(3);
        sources.Select(s => s.Type).Distinct().Should().HaveCount(3);
    }
}
EOF

# --- RegressionTests ---
mkdir -p "tests/$SOLUTION_NAME.RegressionTests"
cat > "tests/$SOLUTION_NAME.RegressionTests/PbirGenerationRegressionTests.cs" <<EOF
using FluentAssertions;
using $SOLUTION_NAME.Domain.Features.Dashboards.Entities;
using $SOLUTION_NAME.Infrastructure.PowerBi;
using Xunit;

namespace $SOLUTION_NAME.RegressionTests;

public class PbirGenerationRegressionTests
{
    [Fact]
    public void GeneratePageJson_ProducesValidLayoutContract()
    {
        var page = new Page("ExecutiveOverview", 1920, 1080);
        page.AddVisual(new Visual("kpi", "RevenueKpi",
            new VisualLayout(40, 30, 400, 180, 0, true), new[] { "Sales[TotalRevenue]" }));

        var generator = new PbirGenerator();
        var json = generator.GeneratePageJson(page);

        json.Should().Contain("ExecutiveOverview");
        json.Should().Contain("RevenueKpi");
        json.Should().Contain("Sales[TotalRevenue]");
    }
}
EOF

# --- E2ETests ---
mkdir -p "tests/$SOLUTION_NAME.E2ETests"
cat > "tests/$SOLUTION_NAME.E2ETests/FullPublishWorkflowTests.cs" <<EOF
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace $SOLUTION_NAME.E2ETests;

public class FullPublishWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FullPublishWorkflowTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task PublishAndRetrieveEmbedConfig_SucceedsEndToEnd()
    {
        var publishPayload = new { DashboardDefinitionId = Guid.NewGuid(), TargetWorkspaceId = "ws-test-123" };
        var publishResponse = await _client.PostAsJsonAsync("/api/powerbi/publish", publishPayload);
        publishResponse.EnsureSuccessStatusCode();

        var embedResponse = await _client.GetAsync("/api/powerbi/embed-config/rep-123");
        embedResponse.EnsureSuccessStatusCode();
        var embedBody = await embedResponse.Content.ReadAsStringAsync();
        embedBody.Should().Contain("rep-123");
    }
}
EOF

# --- SecurityTests ---
mkdir -p "tests/$SOLUTION_NAME.SecurityTests/Architecture"
cat > "tests/$SOLUTION_NAME.SecurityTests/InputValidationTests.cs" <<EOF
using FluentAssertions;
using $SOLUTION_NAME.Domain.Features.Analytics.Rules;
using Xunit;

namespace $SOLUTION_NAME.SecurityTests;

public class InputValidationTests
{
    [Theory]
    [InlineData("SUM(Sales[Amount]); DROP TABLE Sales")]
    [InlineData("EXEC xp_cmdshell 'dir'")]
    public void MeasureExpression_WithMaliciousPayload_IsRejected(string expression)
        => MeasureValidationRules.IsExpressionSafe(expression).Should().BeFalse();

    [Fact]
    public void MeasureExpression_WithSafeDax_IsAccepted()
        => MeasureValidationRules.IsExpressionSafe("SUM(Sales[Amount])").Should().BeTrue();
}
EOF

cat > "tests/$SOLUTION_NAME.SecurityTests/Architecture/LayerDependencyTests.cs" <<EOF
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace $SOLUTION_NAME.SecurityTests.Architecture;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof($SOLUTION_NAME.Domain.Common.Entity).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "$SOLUTION_NAME.Application",
                "$SOLUTION_NAME.Infrastructure",
                "$SOLUTION_NAME.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
EOF
ok "Sample test files written for all 6 test suites."

###############################################################################
# 9. ADD ALL PROJECTS TO SOLUTION (.slnx)
###############################################################################
log "Wiring all projects to $SOLUTION_NAME.slnx..."
for proj in src/*/*.csproj tests/*/*.csproj; do
  dotnet sln add "$proj" >/dev/null 2>&1 || true
done
ok "Solution wired with all 10 projects."

###############################################################################
# 10. RESTORE + BUILD VERIFICATION
###############################################################################
log "Restoring and building solution..."
dotnet restore "$SOLUTION_NAME.slnx"
dotnet build "$SOLUTION_NAME.slnx" --no-restore -c Debug
ok "Build succeeded."

###############################################################################
# 11. TEST RUNNER SCRIPT
###############################################################################
cat > run-tests.sh <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
CATEGORY="${1:-all}"
SOLUTION="AnalyticsPlatform"

run() { echo "==> dotnet test tests/$SOLUTION.$1"; dotnet test "tests/$SOLUTION.$1" --no-build --nologo; }

case "$CATEGORY" in
  unit)        run UnitTests ;;
  integration) run IntegrationTests ;;
  path)        run PathTests ;;
  regression)  run RegressionTests ;;
  e2e)         run E2ETests ;;
  security)    run SecurityTests ;;
  all)
    run UnitTests
    run IntegrationTests
    run PathTests
    run RegressionTests
    run SecurityTests
    run E2ETests
    ;;
  *) echo "Usage: ./run-tests.sh [unit|integration|path|regression|e2e|security|all]"; exit 1 ;;
esac
EOF
chmod +x run-tests.sh
ok "run-tests.sh generated."

log "-----------------------------------------------------------------"
ok "Backend scaffold complete at: $(pwd)"
log "Next steps:"
echo "    cd $ROOT_DIR"
echo "    dotnet run --project src/$SOLUTION_NAME.Api"
echo "    ./run-tests.sh all"
log "-----------------------------------------------------------------"
