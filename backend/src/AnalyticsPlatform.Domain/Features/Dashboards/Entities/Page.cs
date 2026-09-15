using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Dashboards.Entities;

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
