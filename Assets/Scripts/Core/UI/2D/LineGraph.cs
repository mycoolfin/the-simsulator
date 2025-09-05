using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.TwoD
{
    public class LineGraphSeries
    {
        public List<float> Points { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }

        public LineGraphSeries(string name, Color color)
        {
            Name = name;
            Color = color;
            Points = new List<float>();
        }
    }

    public class LineGraph
    {
        private readonly VisualElement graph;
        private readonly Dictionary<string, LineGraphSeries> series;
        private bool isCleared = false;

        public LineGraph(VisualElement parent)
        {
            graph = new VisualElement();
            graph.style.flexGrow = 1;
            graph.generateVisualContent += Plot;
            parent.Add(graph);
            series = new Dictionary<string, LineGraphSeries>();
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
            if (series.Count == 0 || !series.Values.Any(s => s.Points.Count > 0))
                return;

            // Calculate the maximum x and y values across all series.
            float xMax = series.Values.Where(s => s.Points.Count > 0).Max(s => s.Points.Count);
            float yMax = series.Values.Where(s => s.Points.Count > 0).Max(s => s.Points.Max());

            if (yMax == 0) yMax = 1; // Avoid division by zero.

            // Draw current point lines for each series with data.
            foreach (LineGraphSeries seriesData in series.Values)
            {
                if (seriesData.Points.Count == 0)
                    continue;

                float currentValue = seriesData.Points[^1];
                Vector2 start = GetGraphPosition(0, currentValue, xMax, yMax);
                Vector2 end = GetGraphPosition(xMax, currentValue, xMax, yMax);
                
                // Create a very muted version of the series color (reduce alpha to 0.2)
                Color mutedColor = new(seriesData.Color.r, seriesData.Color.g, seriesData.Color.b, 0.2f);
                mgc.painter2D.strokeColor = mutedColor;
                mgc.painter2D.lineJoin = LineJoin.Round;
                mgc.painter2D.lineCap = LineCap.Round;
                mgc.painter2D.lineWidth = 1f;
                mgc.painter2D.BeginPath();
                mgc.painter2D.MoveTo(start);
                mgc.painter2D.LineTo(end);
                mgc.painter2D.Stroke();
            }

            // Draw line plots for each series.
            foreach (LineGraphSeries seriesData in series.Values)
            {
                if (seriesData.Points.Count == 0)
                    continue;

                mgc.painter2D.strokeColor = seriesData.Color;
                mgc.painter2D.lineJoin = LineJoin.Round;
                mgc.painter2D.lineCap = LineCap.Round;
                mgc.painter2D.lineWidth = 2f;
                mgc.painter2D.BeginPath();
                mgc.painter2D.MoveTo(GetGraphPosition(0, 0, xMax, yMax));
                for (int i = 0; i < seriesData.Points.Count; i++)
                    mgc.painter2D.LineTo(GetGraphPosition(i + 1, seriesData.Points[i], xMax, yMax));
                mgc.painter2D.Stroke();
            }
        }

        public void AddSeries(string name, Color color)
        {
            if (!series.ContainsKey(name))
            {
                series[name] = new LineGraphSeries(name, color);
            }
        }

        public void RemoveSeries(string name)
        {
            if (series.ContainsKey(name))
            {
                series.Remove(name);
                graph.MarkDirtyRepaint();
            }
        }

        public void SetSeriesPoints(string seriesName, List<float> points)
        {
            if (series.ContainsKey(seriesName))
            {
                series[seriesName].Points = new List<float>(points);
                graph.MarkDirtyRepaint();
                isCleared = false;
            }
        }

        public void AddPointToSeries(string seriesName, float point)
        {
            if (series.ContainsKey(seriesName))
            {
                series[seriesName].Points.Add(point);
                graph.MarkDirtyRepaint();
                isCleared = false;
            }
        }

        public void ClearAllSeriesPoints()
        {
            if (!isCleared)
            {
                foreach (var seriesData in series.Values)
                {
                    seriesData.Points.Clear();
                }
                graph.MarkDirtyRepaint();
                isCleared = true;
            }
        }

        public void ClearSeriesPoints(string seriesName)
        {
            if (series.ContainsKey(seriesName))
            {
                series[seriesName].Points.Clear();
                graph.MarkDirtyRepaint();
            }
        }

        public IEnumerable<string> GetSeriesNames()
        {
            return series.Keys;
        }

        public void SetSeriesColor(string seriesName, Color color)
        {
            if (series.ContainsKey(seriesName))
            {
                series[seriesName].Color = color;
                graph.MarkDirtyRepaint();
            }
        }
    }
}
