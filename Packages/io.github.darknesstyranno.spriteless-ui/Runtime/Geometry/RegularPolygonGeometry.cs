using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class RegularPolygonGeometry
    {
        public const int MinimumSides = 3;
        public const int MaximumSides = 64;

        // Uniform fitting keeps every edge equal even in a non-square RectTransform.
        public static float BuildPerimeter(Rect rect, int sides, float rotation, Vector2[] points)
        {
            Vector2 min = Vector2.one * float.MaxValue;
            Vector2 max = Vector2.one * float.MinValue;
            for (int i = 0; i < sides; i++)
            {
                float angle = (90f - rotation + 360f * i / sides) * Mathf.Deg2Rad;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            float radius = Mathf.Min(rect.width / (max.x - min.x), rect.height / (max.y - min.y));
            Vector2 offset = rect.center - (min + max) * (radius * 0.5f);
            for (int i = 0; i < sides; i++)
                points[i] = offset + points[i] * radius;
            return radius * Mathf.Cos(Mathf.PI / sides);
        }

        public static int BuildRoundedPerimeter(Rect rect, int sides, float rotation, float radius,
            float inset, int segments, Vector2[] points, out float inradius)
        {
            inradius = BuildPerimeter(rect, sides, rotation, points);
            Vector2 center = Vector2.zero;
            for (int i = 0; i < sides; i++) center += points[i];
            center /= sides;
            float scale = Mathf.Max(0f, (inradius - inset) / inradius);
            for (int i = 0; i < sides; i++) points[i] = center + (points[i] - center) * scale;
            if (segments == 0) return sides;
            float roundedRadius = Mathf.Max(0f, Mathf.Min(radius, inradius * 0.998f) - inset);
            Vector2 first = points[0], last = points[sides - 1];
            // Expand backwards so the still-unread sharp vertices are preserved.
            for (int i = sides - 1; i >= 0; i--)
                PolygonRounding.WriteCorner(i == 0 ? last : points[i - 1], points[i], i == sides - 1 ? first : points[i + 1],
                    roundedRadius, segments, points, i * (segments + 1));
            return sides * (segments + 1);
        }

        public static int BuildRoundedPerimeter(Rect rect, int sides, float rotation, float radius, ShapePointBuffer buffer)
        {
            int segments = PolygonRounding.Segments(radius, TessellationQuality.Multiplier);
            buffer.EnsureCapacity(sides * (segments + 1));
            return BuildRoundedPerimeter(rect, sides, rotation, radius, 0f, segments, buffer.First, out _);
        }

        public static void Build(VertexHelper vh, Rect rect, int sides, float rotation,
            Color32 fill, bool borderEnabled, float borderWidth, Color32 border,
            bool aa, float aaWidth, ShapePointBuffer buffer, float radius = 0f)
        {
            int segments = PolygonRounding.Segments(radius, TessellationQuality.Multiplier);
            buffer.EnsureCapacity(sides * (segments + 1));
            int count = BuildRoundedPerimeter(rect, sides, rotation, radius, 0f, segments, buffer.First, out float inradius);
            float width = borderEnabled ? Mathf.Clamp(borderWidth, 0f, inradius) : 0f;
            bool hasBorder = width > 0f;
            bool solidBorder = hasBorder && width >= inradius - 0.00001f;
            Color32 outerColor = hasBorder ? border : fill;
            float outerAa = aa ? Mathf.Min(aaWidth, hasBorder ? width : inradius * 0.99f) : 0f;
            BuildRoundedPerimeter(rect, sides, rotation, radius, outerAa, segments, buffer.Second, out _);
            if (outerAa > 0f)
            {
                Color32 transparent = outerColor; transparent.a = 0;
                GeometryWriter.AddRing(vh, buffer.First, transparent, buffer.Second, outerColor, count);
            }
            if (!hasBorder || solidBorder)
            {
                GeometryWriter.AddFilledPolygon(vh, Center(buffer.Second, count), buffer.Second, count, outerColor);
                return;
            }
            BuildRoundedPerimeter(rect, sides, rotation, radius, width, segments, buffer.Third, out _);
            GeometryWriter.AddRing(vh, buffer.Second, border, buffer.Third, border, count);
            float innerAa = aa ? Mathf.Min(aaWidth, (inradius - width) * 0.99f) : 0f;
            BuildRoundedPerimeter(rect, sides, rotation, radius, width + innerAa, segments, buffer.Fourth, out _);
            if (innerAa > 0f) GeometryWriter.AddRing(vh, buffer.Third, border, buffer.Fourth, fill, count);
            GeometryWriter.AddFilledPolygon(vh, Center(buffer.Fourth, count), buffer.Fourth, count, fill);
        }

        private static Vector2 Center(Vector2[] points, int count)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < count; i++) center += points[i];
            return center / count;
        }
    }
}
