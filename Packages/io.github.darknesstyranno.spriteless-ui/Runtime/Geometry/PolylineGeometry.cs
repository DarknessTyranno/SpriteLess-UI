using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    // Connected strips share their join boundaries. Unlike stacked segment quads,
    // ordinary corners do not double-blend translucent colors or introduce AA seams.
    internal sealed class PolylineGeometry
    {
        private const float MinimumSegment = 0.00001f;
        private const float MiterLimit = 4f;
        private readonly List<Vector2> m_Points = new List<Vector2>();
        private VertexHelper m_Vertices;
        private Rect m_Rect;
        private Color32 m_Color;
        private Color32 m_Transparent;
        private float m_HalfWidth;
        private float m_Feather;
        private float m_Detail;
        private int m_Previous;

        public void Build(VertexHelper vertices, IReadOnlyList<Vector2> points, Rect rect, float thickness,
            ArcCap cap, StrokeJoin join, Color32 color, float feather, float detail)
        {
            if (thickness <= 0f) return;
            m_Points.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = rect.min + Vector2.Scale(points[i], rect.size);
                if (m_Points.Count == 0 || (point - m_Points[m_Points.Count - 1]).sqrMagnitude > MinimumSegment * MinimumSegment)
                    m_Points.Add(point);
            }
            if (m_Points.Count < 2) return;
            m_Vertices = vertices;
            m_Rect = rect;
            m_Color = color;
            m_Transparent = color;
            m_Transparent.a = 0;
            m_HalfWidth = thickness * 0.5f;
            m_Feather = Mathf.Min(feather, m_HalfWidth * 0.95f);
            m_Detail = detail;

            int start = 0;
            for (int i = 1; i < m_Points.Count - 1; i++)
            {
                Vector2 incoming = (m_Points[i] - m_Points[i - 1]).normalized;
                Vector2 outgoing = (m_Points[i + 1] - m_Points[i]).normalized;
                // A reversal has no finite offset-line intersection. End the current
                // strip here; retracing/crossing a path intentionally overlaps it.
                if (Vector2.Dot(incoming, outgoing) < -0.9999f)
                {
                    BuildStrip(start, i, cap, join);
                    start = i;
                }
            }
            BuildStrip(start, m_Points.Count - 1, cap, join);
            m_Vertices = null;
        }

        private void BuildStrip(int start, int end, ArcCap cap, StrokeJoin join)
        {
            m_Previous = -1;
            Vector2 firstDirection = (m_Points[start + 1] - m_Points[start]).normalized;
            Vector2 lastDirection = (m_Points[end] - m_Points[end - 1]).normalized;
            float startInset = cap == ArcCap.Flat ? Mathf.Min(m_Feather, Vector2.Distance(m_Points[start], m_Points[start + 1]) * 0.25f) : 0f;
            float endInset = cap == ArcCap.Flat ? Mathf.Min(m_Feather, Vector2.Distance(m_Points[end], m_Points[end - 1]) * 0.25f) : 0f;
            AddSection(m_Points[start], Left(firstDirection), -Left(firstDirection),
                m_Points[start] + firstDirection * startInset);
            int first = m_Previous;
            for (int i = start + 1; i < end; i++) AddJoin(i, join);
            AddSection(m_Points[end], Left(lastDirection), -Left(lastDirection),
                m_Points[end] - lastDirection * endInset);
            if (cap == ArcCap.Round)
            {
                AddRoundCap(m_Points[start], -Left(firstDirection));
                AddRoundCap(m_Points[end], Left(lastDirection));
            }
            else if (m_Feather > 0f)
            {
                AddQuad(first + 2, first + 3, first + 1, first);
                AddQuad(m_Previous + 2, m_Previous, m_Previous + 1, m_Previous + 3);
            }
        }

        private void AddJoin(int index, StrokeJoin join)
        {
            Vector2 center = m_Points[index];
            Vector2 before = center - m_Points[index - 1];
            Vector2 after = m_Points[index + 1] - center;
            float available = Mathf.Min(before.magnitude, after.magnitude) * 0.45f;
            Vector2 incoming = before.normalized;
            Vector2 outgoing = after.normalized;
            float dot = Mathf.Clamp(Vector2.Dot(incoming, outgoing), -1f, 1f);
            Vector2 firstNormal = Left(incoming);
            Vector2 lastNormal = Left(outgoing);
            Vector2 miter = (firstNormal + lastNormal) / Mathf.Max(0.00001f, 1f + dot);
            float turn = Cross(incoming, outgoing);
            float reach = Mathf.Abs(Vector2.Dot(miter, incoming)) * m_HalfWidth;
            if (dot > 0.99999f || (join == StrokeJoin.Miter && miter.sqrMagnitude <= MiterLimit * MiterLimit && reach <= available))
            {
                AddSection(center, miter, -miter, center);
                return;
            }

            // Limit the inside intersection to the adjacent segments. Large widths
            // and short edges then stay finite instead of creating inverted spikes.
            Vector2 inner = miter * (reach > available ? available / reach : 1f);
            float sweep = Mathf.Atan2(turn, dot);
            int segments = join == StrokeJoin.Round
                ? Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(sweep) * Mathf.Rad2Deg / 15f * m_Detail), 1, 48)
                : 1; // Miter limit falls back to a bevel.
            for (int i = 0; i <= segments; i++)
            {
                float angle = sweep * i / segments;
                Vector2 normal = Rotate(firstNormal, angle);
                AddSection(center, turn > 0f ? inner : normal, turn > 0f ? -normal : -inner, center);
            }
        }

        // Directions may be longer than one at a miter. Both contours use identical
        // topology; the feather is entirely inside the requested stroke width.
        private void AddSection(Vector2 center, Vector2 left, Vector2 right, Vector2 innerCenter)
        {
            int current = m_Vertices.currentVertCount;
            float solid = m_HalfWidth - m_Feather;
            AddVertex(innerCenter + left * solid, m_Color);
            AddVertex(innerCenter + right * solid, m_Color);
            if (m_Feather > 0f)
            {
                AddVertex(center + left * m_HalfWidth, m_Transparent);
                AddVertex(center + right * m_HalfWidth, m_Transparent);
            }
            if (m_Previous >= 0)
            {
                AddQuad(m_Previous, current, current + 1, m_Previous + 1);
                if (m_Feather > 0f)
                {
                    AddQuad(m_Previous + 2, current + 2, current, m_Previous);
                    AddQuad(m_Previous + 1, current + 1, current + 3, m_Previous + 3);
                }
            }
            m_Previous = current;
        }

        private void AddRoundCap(Vector2 center, Vector2 normal)
        {
            // Even 256 reversing points at maximum capture detail stay below uGUI's
            // 65k vertex limit (two caps per segment in the worst case).
            int segments = Mathf.Clamp(Mathf.CeilToInt(12 * m_Detail), 4, 48);
            int centerIndex = m_Vertices.currentVertCount;
            AddVertex(center, m_Color);
            int previous = -1;
            for (int i = 0; i <= segments; i++)
            {
                Vector2 direction = Rotate(normal, -Mathf.PI * i / segments);
                int current = m_Vertices.currentVertCount;
                AddVertex(center + direction * (m_HalfWidth - m_Feather), m_Color);
                if (m_Feather > 0f) AddVertex(center + direction * m_HalfWidth, m_Transparent);
                if (previous >= 0)
                {
                    AddTriangle(centerIndex, previous, current);
                    if (m_Feather > 0f) AddQuad(previous, previous + 1, current + 1, current);
                }
                previous = current;
            }
        }

        private void AddVertex(Vector2 position, Color32 color)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = new Vector2((position.x - m_Rect.xMin) / m_Rect.width, (position.y - m_Rect.yMin) / m_Rect.height);
            m_Vertices.AddVert(vertex);
        }

        private void AddQuad(int a, int b, int c, int d)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        private void AddTriangle(int a, int b, int c)
        {
            // Shared inner join vertices produce zero-area triangles in the strip.
            var va = new UIVertex();
            var vb = new UIVertex();
            var vc = new UIVertex();
            m_Vertices.PopulateUIVertex(ref va, a);
            m_Vertices.PopulateUIVertex(ref vb, b);
            m_Vertices.PopulateUIVertex(ref vc, c);
            float area = Cross(vb.position - va.position, vc.position - va.position);
            if (area == 0f) return;
            if (area > 0f) m_Vertices.AddTriangle(a, c, b);
            else m_Vertices.AddTriangle(a, b, c);
        }

        private static Vector2 Left(Vector2 direction) => new Vector2(-direction.y, direction.x);
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static Vector2 Rotate(Vector2 v, float angle)
        {
            float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
