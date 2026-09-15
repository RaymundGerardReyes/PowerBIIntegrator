using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Domain.Features.Dashboards.Rules;

public static class LayoutBoundsRules
{
    public static bool IsWithinCanvas(VisualLayout layout, double canvasWidth, double canvasHeight)
        => layout.X >= 0
        && layout.Y >= 0
        && layout.X + layout.Width <= canvasWidth
        && layout.Y + layout.Height <= canvasHeight;
}
