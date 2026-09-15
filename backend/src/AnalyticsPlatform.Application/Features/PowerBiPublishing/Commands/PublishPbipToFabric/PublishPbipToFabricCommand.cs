using MediatR;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.PublishPbipToFabric;

public sealed record PublishPbipResponse(string ReportId);

public sealed record PublishPbipToFabricCommand(Guid DashboardDefinitionId, string TargetWorkspaceId) : IRequest<PublishPbipResponse>;
