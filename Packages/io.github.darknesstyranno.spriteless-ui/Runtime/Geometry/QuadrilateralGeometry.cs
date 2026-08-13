using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class QuadrilateralGeometry
    {
        private const int PointCount = 4;
        public static void Build(
            VertexHelper vertexHelper,
            Rect rect,
            CornerOffsets cornerOffsets,
            Color32 fillColor,
            bool borderEnabled,
            float borderWidth,
            Color32 borderColor,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            buffer.EnsureCapacity(PointCount);
            BuildPerimeter(rect, cornerOffsets, buffer.First);
            ConvexPolygonGeometry.Build(
                vertexHelper,
                buffer.First,
                PointCount,
                Mathf.Min(rect.width, rect.height) * 0.5f,
                fillColor,
                borderEnabled,
                borderWidth,
                borderColor,
                antiAliasingEnabled,
                antiAliasingWidth,
                buffer);
        }

        internal static void BuildPerimeter(
            Rect rect,
            CornerOffsets cornerOffsets,
            Vector2[] points)
        {
            BuildPerimeter(rect, cornerOffsets, 1f, points);
            float minimumCross = Mathf.Max(rect.width * rect.height * 0.000001f, Mathf.Epsilon);
            if (IsConvex(points, minimumCross))
            {
                return;
            }

            float validScale = 0f;
            float invalidScale = 1f;
            for (int i = 0; i < 12; i++)
            {
                float scale = (validScale + invalidScale) * 0.5f;
                BuildPerimeter(rect, cornerOffsets, scale, points);
                if (IsConvex(points, minimumCross))
                {
                    validScale = scale;
                }
                else
                {
                    invalidScale = scale;
                }
            }

            BuildPerimeter(rect, cornerOffsets, validScale, points);
        }

        private static void BuildPerimeter(
            Rect rect,
            CornerOffsets cornerOffsets,
            float scale,
            Vector2[] points)
        {
            CornerOffsets scaledOffsets = cornerOffsets.Scaled(scale);
            points[0] = new Vector2(rect.xMin, rect.yMin) + scaledOffsets.BottomLeft;
            points[1] = new Vector2(rect.xMax, rect.yMin) + scaledOffsets.BottomRight;
            points[2] = new Vector2(rect.xMax, rect.yMax) + scaledOffsets.TopRight;
            points[3] = new Vector2(rect.xMin, rect.yMax) + scaledOffsets.TopLeft;
        }

        private static bool IsConvex(Vector2[] points, float minimumCross)
        {
            for (int i = 0; i < PointCount; i++)
            {
                Vector2 current = points[i];
                Vector2 next = points[(i + 1) % PointCount];
                Vector2 afterNext = points[(i + 2) % PointCount];
                if ((next - current).sqrMagnitude <= Mathf.Epsilon
                    || Cross(next - current, afterNext - next) <= minimumCross)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TryBuildOffset(
            Vector2[] source,
            float distance,
            Vector2[] destination)
        {
            return ConvexPolygonGeometry.TryBuildInset(
                source,
                PointCount,
                distance,
                destination);
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }
    }
}
