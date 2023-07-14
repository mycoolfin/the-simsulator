using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LineGraph
{
    private VisualElement graph;
    private Color lineColor;
    private List<float> points;

    public LineGraph(VisualElement parent, Color lineColor)
    {
        graph = new VisualElement();
        graph.style.flexGrow = 1;
        graph.generateVisualContent += Plot;
        parent.Add(graph);
        this.lineColor = lineColor;
        points = new();
    }

    private Vector2 GetGraphPosition(float x, float y, float xMax, float yMax)
    {
        return new Vector2(
            (x / xMax) * graph.resolvedStyle.width,
            (1f - (y / yMax)) * graph.resolvedStyle.height
        );
    }

    private void Plot(MeshGenerationContext mgc)
    {
        if (points.Count == 0)
            return;

        float xMax = points.Count;
        float yMax = points.Max();

        // Draw current point line.
        Vector2 start = GetGraphPosition(0, points[points.Count - 1], xMax, yMax);
        Vector2 end = GetGraphPosition(xMax, points[points.Count - 1], xMax, yMax);
        mgc.painter2D.strokeColor = Color.gray;
        mgc.painter2D.lineJoin = LineJoin.Round;
        mgc.painter2D.lineCap = LineCap.Round;
        mgc.painter2D.lineWidth = 1f;
        mgc.painter2D.BeginPath();
        mgc.painter2D.MoveTo(start);
        mgc.painter2D.LineTo(end);
        mgc.painter2D.Stroke();

        // Draw line plot.
        mgc.painter2D.strokeColor = lineColor;
        mgc.painter2D.lineJoin = LineJoin.Round;
        mgc.painter2D.lineCap = LineCap.Round;
        mgc.painter2D.lineWidth = 2f;
        mgc.painter2D.BeginPath();
        mgc.painter2D.MoveTo(GetGraphPosition(0, 0, xMax, yMax));
        for (int i = 0; i < points.Count; i++)
            mgc.painter2D.LineTo(GetGraphPosition(i + 1, points[i], xMax, yMax));
        mgc.painter2D.Stroke();
    }

    public void SetPoints(List<float> points)
    {
        this.points = points;
        graph.MarkDirtyRepaint();
    }

    public void ClearPoints()
    {
        points = new();
        graph.MarkDirtyRepaint();
    }
}
