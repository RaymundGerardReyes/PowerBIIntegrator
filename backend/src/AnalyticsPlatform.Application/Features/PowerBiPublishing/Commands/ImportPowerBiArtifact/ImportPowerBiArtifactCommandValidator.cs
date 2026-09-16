using FluentValidation;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.ImportPowerBiArtifact;

public class ImportPowerBiArtifactCommandValidator : AbstractValidator<ImportPowerBiArtifactCommand>
{
    public ImportPowerBiArtifactCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("WorkspaceId is required.");

        RuleFor(x => x.DatasetDisplayName)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("DatasetDisplayName is required and cannot exceed 255 characters.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(f => f.EndsWith(".pbix", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .WithMessage("File must have a supported extension (.pbix, .xlsx, .rdl, .json).");
    }
}
