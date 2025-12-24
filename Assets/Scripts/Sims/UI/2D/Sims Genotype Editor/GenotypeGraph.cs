using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.TwoD
{
    using TheSimsulator.Sims.Genotype;

    public class GenotypeGraph
    {
        private readonly VisualElement graphCanvas;
        private readonly VisualElement connectionOverlay;

        private List<Node> nodes;
        private List<Connection> connections;

        private readonly List<VisualElement> nodeVisuals = new();

        private int? selectedNodeIndex;
        private int? selectedConnectionIndex;
        private int? hoveredConnectionIndex;

        public event Action<int> OnNodeSelected;
        public event Action<int> OnConnectionSelected;
        public event Action OnEmptySpaceClicked;

        private const float NODE_WIDTH = 200f;
        private const float NODE_HEIGHT = 160f;
        private const float NODE_SPACING = 100f;
        private const float GRAPH_PADDING = 50f;

        private const float CONNECTION_LINE_WIDTH = 10f;
        private const float ARROWHEAD_LENGTH_MULTIPLIER = 4f;
        private const float ARROWHEAD_WIDTH_MULTIPLIER = 2.67f;

        public GenotypeGraph(VisualElement graphCanvas, VisualElement connectionOverlay)
        {
            this.graphCanvas = graphCanvas;
            this.connectionOverlay = connectionOverlay;

            connectionOverlay.generateVisualContent += OnGenerateConnectionVisualContent;
            connectionOverlay.pickingMode = PickingMode.Ignore;

            graphCanvas.RegisterCallback<ClickEvent>(OnGraphCanvasClicked);
            graphCanvas.RegisterCallback<MouseMoveEvent>(OnGraphCanvasMouseMove);
        }

        /// <summary>
        /// Load nodes and connections into the graph.
        /// </summary>
        public void LoadGraph(List<Node> nodes, List<Connection> connections)
        {
            this.nodes = nodes;
            this.connections = connections;

            selectedNodeIndex = null;
            selectedConnectionIndex = null;
            hoveredConnectionIndex = null;

            RefreshGraph();
        }

        /// <summary>
        /// Set the selected node index (for external selection updates).
        /// </summary>
        public void SetSelectedNode(int? nodeIndex)
        {
            selectedNodeIndex = nodeIndex;
            selectedConnectionIndex = null;
            RefreshGraph();
        }

        /// <summary>
        /// Set the selected connection index (for external selection updates).
        /// </summary>
        public void SetSelectedConnection(int? connectionIndex)
        {
            selectedConnectionIndex = connectionIndex;
            selectedNodeIndex = null;
            RefreshGraph();
        }

        /// <summary>
        /// Clear all selections.
        /// </summary>
        public void ClearSelection()
        {
            selectedNodeIndex = null;
            selectedConnectionIndex = null;
            RefreshGraph();
        }

        /// <summary>
        /// Refresh the entire graph visualization.
        /// </summary>
        public void RefreshGraph()
        {
            List<VisualElement> nodesToRemove = graphCanvas.Children()
                .Where(child => child != connectionOverlay)
                .ToList();

            foreach (VisualElement node in nodesToRemove)
            {
                graphCanvas.Remove(node);
            }

            nodeVisuals.Clear();

            for (int i = 0; i < nodes.Count; i++)
            {
                VisualElement nodeVisual = CreateNodeVisual(nodes[i], i);
                nodeVisuals.Add(nodeVisual);
            }

            CreateConnectionPoints();

            if (nodes.Count > 0)
            {
                float requiredWidth = (nodes.Count * (NODE_WIDTH + NODE_SPACING)) + (GRAPH_PADDING * 2) - NODE_SPACING;
                float requiredHeight = NODE_HEIGHT + (GRAPH_PADDING * 2);

                graphCanvas.style.minWidth = requiredWidth;
                graphCanvas.style.minHeight = requiredHeight;
            }

            connectionOverlay.MarkDirtyRepaint();
        }

        private VisualElement CreateNodeVisual(Node node, int nodeIndex)
        {
            // Create node visual from scratch (manual cloning of template structure).
            VisualElement nodeVisual = new();
            nodeVisual.AddToClassList("graph-node");

            // Create incoming ports column.
            VisualElement incomingPorts = new();
            incomingPorts.name = "incoming-ports";
            incomingPorts.AddToClassList("port-column");
            incomingPorts.AddToClassList("incoming-ports");
            incomingPorts.style.position = Position.Absolute;
            incomingPorts.style.left = 0;
            incomingPorts.style.top = 0;
            incomingPorts.style.bottom = 0;
            incomingPorts.style.width = 10;
            incomingPorts.style.flexDirection = FlexDirection.Column;
            incomingPorts.style.justifyContent = Justify.SpaceAround;
            incomingPorts.style.alignItems = Align.Center;
            nodeVisual.Add(incomingPorts);

            // Create node content.
            VisualElement nodeContent = new();
            nodeContent.name = "node-content";
            nodeContent.style.flexGrow = 1;
            nodeContent.style.alignItems = Align.Center;
            nodeContent.style.justifyContent = Justify.Center;
            nodeContent.style.flexDirection = FlexDirection.Column;

            Label label = new($"Node {nodeIndex}");
            label.name = "node-label";
            label.AddToClassList("node-label");
            nodeContent.Add(label);

            nodeVisual.Add(nodeContent);

            // Create outgoing ports column.
            VisualElement outgoingPorts = new();
            outgoingPorts.name = "outgoing-ports";
            outgoingPorts.AddToClassList("port-column");
            outgoingPorts.AddToClassList("outgoing-ports");
            outgoingPorts.style.position = Position.Absolute;
            outgoingPorts.style.right = 0;
            outgoingPorts.style.top = 0;
            outgoingPorts.style.bottom = 0;
            outgoingPorts.style.width = 10;
            outgoingPorts.style.flexDirection = FlexDirection.Column;
            outgoingPorts.style.justifyContent = Justify.SpaceAround;
            outgoingPorts.style.alignItems = Align.Center;
            nodeVisual.Add(outgoingPorts);

            nodeVisual.style.width = NODE_WIDTH;
            nodeVisual.style.minHeight = NODE_HEIGHT;
            nodeVisual.style.marginRight = NODE_SPACING;

            // Apply node color as tint.
            Color color = HSVToRGB(node.Color.H, node.Color.S, node.Color.V);
            nodeVisual.style.backgroundColor = new StyleColor(color);

            // Add selection highlight if selected.
            if (selectedNodeIndex == nodeIndex)
            {
                nodeVisual.AddToClassList("selected");
            }

            // Register click handler.
            int capturedIndex = nodeIndex;
            nodeVisual.RegisterCallback<PointerDownEvent>(evt =>
            {
                OnNodeClicked(capturedIndex);
                evt.StopPropagation();
            });

            graphCanvas.Add(nodeVisual);
            return nodeVisual;
        }

        private void CreateConnectionPoints()
        {
            // Clear all existing connection points.
            foreach (VisualElement nodeVisual in nodeVisuals)
            {
                VisualElement incomingPorts = nodeVisual.Q<VisualElement>("incoming-ports");
                VisualElement outgoingPorts = nodeVisual.Q<VisualElement>("outgoing-ports");

                incomingPorts.Clear();
                outgoingPorts.Clear();
            }

            // Create connection points for each connection.
            foreach (Connection connection in connections)
            {
                int parentIndex = nodes.FindIndex(n => n.Gid == connection.ParentNodeGid);
                int childIndex = nodes.FindIndex(n => n.Gid == connection.ChildNodeGid);

                if (parentIndex == -1 || childIndex == -1) continue;

                // Create outgoing connection point on parent node.
                VisualElement parentVisual = nodeVisuals[parentIndex];
                VisualElement outgoingPorts = parentVisual.Q<VisualElement>("outgoing-ports");
                VisualElement outgoingPoint = new();
                outgoingPoint.AddToClassList("connection-point");
                outgoingPoint.name = $"conn-out-{connection.Gid}";
                outgoingPorts.Add(outgoingPoint);

                // Create incoming connection point on child node.
                VisualElement childVisual = nodeVisuals[childIndex];
                VisualElement incomingPorts = childVisual.Q<VisualElement>("incoming-ports");
                VisualElement incomingPoint = new();
                incomingPoint.AddToClassList("connection-point");
                incomingPoint.name = $"conn-in-{connection.Gid}";
                incomingPorts.Add(incomingPoint);
            }
        }

        private void OnGenerateConnectionVisualContent(MeshGenerationContext ctx)
        {
            if (connections == null || nodes == null) return;

            Painter2D painter = ctx.painter2D;
            painter.lineWidth = CONNECTION_LINE_WIDTH;

            foreach (Connection connection in connections)
            {
                var bezierData = GetConnectionBezierData(connection);
                if (bezierData.HasValue)
                {
                    DrawConnectionCurve(painter, connection, bezierData.Value);
                }
            }
        }

        private struct ConnectionBezierData
        {
            public Vector2 StartPos;
            public Vector2 EndPos;
            public Vector2 ControlPoint1;
            public Vector2 ControlPoint2;
        }

        private ConnectionBezierData? GetConnectionBezierData(Connection connection)
        {
            int parentIndex = nodes.FindIndex(n => n.Gid == connection.ParentNodeGid);
            int childIndex = nodes.FindIndex(n => n.Gid == connection.ChildNodeGid);

            if (parentIndex == -1 || childIndex == -1) return null;

            Vector2 startPos = GetConnectionPointPosition(nodeVisuals[parentIndex], $"conn-out-{connection.Gid}");
            Vector2 endPos = GetConnectionPointPosition(nodeVisuals[childIndex], $"conn-in-{connection.Gid}");
            (int sourceIndex, int sourceCount) = GetConnectionPointIndexInColumn(nodeVisuals[parentIndex], $"conn-out-{connection.Gid}", "outgoing-ports");
            Rect parentBounds = WorldToLocal(nodeVisuals[parentIndex].worldBound);
            Rect childBounds = WorldToLocal(nodeVisuals[childIndex].worldBound);
            (Vector2 cp1, Vector2 cp2) = CalculateConnectionControlPoints(startPos, endPos, parentIndex, childIndex, sourceIndex, sourceCount, parentBounds, childBounds);

            return new ConnectionBezierData
            {
                StartPos = startPos,
                EndPos = endPos,
                ControlPoint1 = cp1,
                ControlPoint2 = cp2
            };
        }

        private Vector2 GetConnectionPointPosition(VisualElement nodeVisual, string connectionPointName)
        {
            VisualElement connectionPoint = nodeVisual.Q<VisualElement>(connectionPointName);
            if (connectionPoint == null)
            {
                // Fallback to node center if connection point not found.
                Rect nodeRect = nodeVisual.worldBound;
                Vector2 nodeCenter = nodeRect.center;
                return connectionOverlay.WorldToLocal(nodeCenter);
            }

            // Get the center of the connection point in connection overlay space.
            Rect pointRect = connectionPoint.worldBound;
            Vector2 pointCenter = pointRect.center;
            return connectionOverlay.WorldToLocal(pointCenter);
        }

        private (int, int) GetConnectionPointIndexInColumn(VisualElement nodeVisual, string connectionPointName, string columnName)
        {
            VisualElement column = nodeVisual.Q<VisualElement>(columnName);
            if (column == null) return (0, 1);

            List<VisualElement> points = column.Children().ToList();
            int count = points.Count;
            int index = points.FindIndex(p => p.name == connectionPointName);

            return (index >= 0 ? index : 0, count > 0 ? count : 1);
        }

        private Rect WorldToLocal(Rect worldRect)
        {
            Vector2 topLeft = connectionOverlay.WorldToLocal(new Vector2(worldRect.xMin, worldRect.yMin));
            Vector2 bottomRight = connectionOverlay.WorldToLocal(new Vector2(worldRect.xMax, worldRect.yMax));
            
            // Ensure min/max are in the correct order after transformation.
            float minX = Mathf.Min(topLeft.x, bottomRight.x);
            float maxX = Mathf.Max(topLeft.x, bottomRight.x);
            float minY = Mathf.Min(topLeft.y, bottomRight.y);
            float maxY = Mathf.Max(topLeft.y, bottomRight.y);
            
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private void DrawConnectionCurve(Painter2D painter, Connection connection, ConnectionBezierData bezierData)
        {
            bool isSelected = selectedConnectionIndex.HasValue &&
                             connections[selectedConnectionIndex.Value].Gid == connection.Gid;
            bool isHovered = hoveredConnectionIndex.HasValue &&
                            connections[hoveredConnectionIndex.Value].Gid == connection.Gid;

            Color connectionColor;
            if (isSelected)
            {
                connectionColor = new(0f, 1f, 0.97f, 1f);
            }
            else if (isHovered)
            {
                connectionColor = new(1f, 1f, 1f, 0.9f);
            }
            else
            {
                connectionColor = new(1f, 1f, 1f, 0.6f);
            }

            painter.strokeColor = connectionColor;

            float arrowLength = CONNECTION_LINE_WIDTH * ARROWHEAD_LENGTH_MULTIPLIER;
            Vector2 arrowheadPoint = SampleBezier(bezierData.StartPos, bezierData.ControlPoint1, bezierData.ControlPoint2, bezierData.EndPos, 0.9f);
            Vector2 direction = (bezierData.EndPos - arrowheadPoint).normalized;
            Vector2 curveEndPoint = bezierData.EndPos - 0.8f * arrowLength * direction;

            painter.BeginPath();
            painter.MoveTo(bezierData.StartPos);
            painter.BezierCurveTo(bezierData.ControlPoint1, bezierData.ControlPoint2, curveEndPoint);
            painter.Stroke();

            DrawArrowhead(painter, direction, bezierData.EndPos, connectionColor);
        }

        private void DrawArrowhead(Painter2D painter, Vector2 direction, Vector2 endPoint, Color color)
        {
            Vector2 perpendicular = new(-direction.y, direction.x);

            float arrowLength = CONNECTION_LINE_WIDTH * ARROWHEAD_LENGTH_MULTIPLIER;
            float arrowWidth = CONNECTION_LINE_WIDTH * ARROWHEAD_WIDTH_MULTIPLIER;

            Vector2 arrowBase = endPoint - direction * arrowLength;
            Vector2 arrowLeft = arrowBase + perpendicular * arrowWidth * 0.5f;
            Vector2 arrowRight = arrowBase - perpendicular * arrowWidth * 0.5f;

            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(endPoint);
            painter.LineTo(arrowLeft);
            painter.LineTo(arrowRight);
            painter.ClosePath();
            painter.Fill();
        }

        private (Vector2, Vector2) CalculateConnectionControlPoints(Vector2 startPos, Vector2 endPos, int parentIndex, int childIndex, int sourceIndex, int sourceCount, Rect parentBounds, Rect childBounds)
        {
            // Calculate how many nodes this connection traverses.
            int nodeDistance = childIndex - parentIndex;
            
            // Calculate source point position ratio in its column (0 = top, 1 = bottom).
            float sourceRatio = sourceCount > 1 ? (float)sourceIndex / (sourceCount - 1) : 0.5f;

            // If connecting to directly adjacent node (moving right), use straight/minimal curve.
            if (nodeDistance == 1)
            {
                // Simple forward curve - small horizontal offset.
                float offset = 50f;
                return (
                    startPos + new Vector2(offset, 0),
                    endPos - new Vector2(offset, 0)
                );
            }

            // For all other connections, route through corner ports.
            // Arc direction: top half (< 0.5) uses top corners, bottom half (>= 0.5) uses bottom corners.
            bool useTopCorners = sourceRatio < 0.5f;
            
            // Calculate corner positions with projection away from nodes.
            float cornerProjection = 150f; // Distance to project corners outward.
            
            Vector2 exitCorner, entryCorner;
            
            if (endPos.x < startPos.x)
            {
                // Backward/self connection: exit right, enter left.
                if (useTopCorners)
                {
                    exitCorner = new Vector2(parentBounds.xMax + cornerProjection, parentBounds.yMin - cornerProjection);
                    entryCorner = new Vector2(childBounds.xMin - cornerProjection, childBounds.yMin - cornerProjection);
                }
                else
                {
                    exitCorner = new Vector2(parentBounds.xMax + cornerProjection, parentBounds.yMax + cornerProjection);
                    entryCorner = new Vector2(childBounds.xMin - cornerProjection, childBounds.yMax + cornerProjection);
                }
            }
            else
            {
                // Forward connection: exit right, enter left.
                if (useTopCorners)
                {
                    exitCorner = new Vector2(parentBounds.xMax + cornerProjection, parentBounds.yMin - cornerProjection);
                    entryCorner = new Vector2(childBounds.xMin - cornerProjection, childBounds.yMin - cornerProjection);
                }
                else
                {
                    exitCorner = new Vector2(parentBounds.xMax + cornerProjection, parentBounds.yMax + cornerProjection);
                    entryCorner = new Vector2(childBounds.xMin - cornerProjection, childBounds.yMax + cornerProjection);
                }
            }
            
            // The control points are the corner ports.
            return (exitCorner, entryCorner);
        }

        private void OnGraphCanvasClicked(ClickEvent evt)
        {
            Vector2 worldPos = graphCanvas.LocalToWorld(evt.localPosition);
            Vector2 clickPos = connectionOverlay.WorldToLocal(worldPos);
            float clickThreshold = CONNECTION_LINE_WIDTH * 0.5f;

            for (int i = 0; i < connections.Count; i++)
            {
                if (IsPointNearConnection(clickPos, connections[i], clickThreshold))
                {
                    OnConnectionClicked(i);
                    evt.StopPropagation();
                    return;
                }
            }

            // Clicked on empty space - deselect everything.
            selectedNodeIndex = null;
            selectedConnectionIndex = null;
            OnEmptySpaceClicked?.Invoke(); // Notify external systems.
            RefreshGraph();
        }

        private void OnGraphCanvasMouseMove(MouseMoveEvent evt)
        {
            Vector2 worldPos = graphCanvas.LocalToWorld(evt.localMousePosition);
            Vector2 mousePos = connectionOverlay.WorldToLocal(worldPos);
            float hoverThreshold = CONNECTION_LINE_WIDTH * 0.5f;

            int? previousHoveredIndex = hoveredConnectionIndex;
            hoveredConnectionIndex = null;

            for (int i = 0; i < connections.Count; i++)
            {
                if (IsPointNearConnection(mousePos, connections[i], hoverThreshold))
                {
                    hoveredConnectionIndex = i;
                    break;
                }
            }

            // Refresh if hover state changed.
            if (hoveredConnectionIndex != previousHoveredIndex)
            {
                connectionOverlay.MarkDirtyRepaint();
            }
        }

        private bool IsPointNearConnection(Vector2 point, Connection connection, float threshold)
        {
            var bezierData = GetConnectionBezierData(connection);
            if (!bezierData.HasValue) return false;

            var data = bezierData.Value;
            
            // Calculate where the curve actually ends (before arrowhead).
            float arrowLength = CONNECTION_LINE_WIDTH * ARROWHEAD_LENGTH_MULTIPLIER;
            Vector2 arrowheadPoint = SampleBezier(data.StartPos, data.ControlPoint1, data.ControlPoint2, data.EndPos, 0.9f);
            Vector2 direction = (data.EndPos - arrowheadPoint).normalized;
            Vector2 curveEndPoint = data.EndPos - 0.8f * arrowLength * direction;
            
            // Check distance to curve using line segments between sample points.
            const int segments = 20;
            Vector2 prevPoint = data.StartPos;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector2 currentPoint = SampleBezier(data.StartPos, data.ControlPoint1, data.ControlPoint2, curveEndPoint, t);
                
                float distance = PointToSegmentDistance(point, prevPoint, currentPoint);
                
                if (distance < threshold)
                    return true;
                    
                prevPoint = currentPoint;
            }
            
            // Check arrowhead hitbox (circular area around arrowhead center).
            float arrowheadRadius = CONNECTION_LINE_WIDTH * ARROWHEAD_WIDTH_MULTIPLIER * 0.5f;
            Vector2 arrowheadCenter = data.EndPos - (2.0f / 3.0f) * arrowLength * direction;
            if (Vector2.Distance(point, arrowheadCenter) < arrowheadRadius)
                return true;

            return false;
        }

        private float PointToSegmentDistance(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            // Calculate the closest point on the line segment to the given point.
            Vector2 segment = segmentEnd - segmentStart;
            Vector2 pointVector = point - segmentStart;
            
            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared < 0.0001f)
                return Vector2.Distance(point, segmentStart);
            
            // Project point onto the line segment (clamped to [0, 1]).
            float t = Mathf.Clamp01(Vector2.Dot(pointVector, segment) / segmentLengthSquared);
            Vector2 closestPoint = segmentStart + t * segment;
            
            return Vector2.Distance(point, closestPoint);
        }

        private Vector2 SampleBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 point = uuu * p0;
            point += 3 * uu * t * p1;
            point += 3 * u * tt * p2;
            point += ttt * p3;

            return point;
        }

        private void OnNodeClicked(int nodeIndex)
        {
            selectedNodeIndex = nodeIndex;
            selectedConnectionIndex = null;
            RefreshGraph();
            OnNodeSelected?.Invoke(nodeIndex);
        }

        private void OnConnectionClicked(int connectionIndex)
        {
            selectedConnectionIndex = connectionIndex;
            selectedNodeIndex = null;
            RefreshGraph();
            OnConnectionSelected?.Invoke(connectionIndex);
        }

        private Color HSVToRGB(float h, float s, float v)
        {
            return Color.HSVToRGB(h, s, v);
        }
    }
}
