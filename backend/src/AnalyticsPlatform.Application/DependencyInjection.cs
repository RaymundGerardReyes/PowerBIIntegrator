using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using AnalyticsPlatform.Application.Common.Behaviors;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Behaviors;

namespace AnalyticsPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Features.DataQuality.Behaviors.PipelineRunAuditBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PromptGuardrailBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResponseGuardrailBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LlmTelemetryBehavior<,>));

        // Data Quality & Pipeline Orchestration Stages
        services.AddScoped<Features.DataQuality.Orchestration.Stages.ProfilingStage>();
        services.AddScoped<Features.DataQuality.Orchestration.Stages.DeduplicationStage>();
        services.AddScoped<Features.DataQuality.Orchestration.Stages.CleaningStage>();
        services.AddScoped<Features.DataQuality.Orchestration.Stages.TransformationStage>();

        services.AddScoped<Features.DataQuality.Abstractions.IDataQualityStage>(sp => sp.GetRequiredService<Features.DataQuality.Orchestration.Stages.ProfilingStage>());
        services.AddScoped<Features.DataQuality.Abstractions.IDataQualityStage>(sp => sp.GetRequiredService<Features.DataQuality.Orchestration.Stages.DeduplicationStage>());
        services.AddScoped<Features.DataQuality.Abstractions.IDataQualityStage>(sp => sp.GetRequiredService<Features.DataQuality.Orchestration.Stages.CleaningStage>());
        services.AddScoped<Features.DataQuality.Abstractions.IDataQualityStage>(sp => sp.GetRequiredService<Features.DataQuality.Orchestration.Stages.TransformationStage>());

        services.AddScoped<Features.DataQuality.Orchestration.PipelineOrchestrator>();

        return services;
    }
}
