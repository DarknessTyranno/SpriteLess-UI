using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class CircleGeometry
    {
        private const float ArcTargetLength = 4f;
        private const int MinimumSegments = 16;
        private const int MaximumSegments = 128;

        public static void Build(
            VertexHelper vertexHelper,
            Rect rect,
            Color32 fillColor,
            bool borderEnabled,
            float borderWidth,
            Color32 borderColor,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;
            int pointCount = GetSegmentCount(Mathf.Max(radiusX, radiusY));
            buffer.EnsureCapacity(pointCount);

            BuildEllipse(rect.center, radiusX, radiusY, buffer.Second, pointCount);

            float clampedBorderWidth = borderEnabled
                ? Mathf.Clamp(borderWidth, 0f, Mathf.Min(radiusX, radiusY))
                : 0f;
            bool hasBorder = clampedBorderWidth > Mathf.Epsilon;
            Color32 outerShapeColor = hasBorder ? borderColor : fillColor;

            float outerFeatherWidth = antiAliasingEnabled
                ? Mathf.Min(
                    antiAliasingWidth,
                    hasBorder ? clampedBorderWidth : Mathf.Min(radiusX, radiusY))
                : 0f;
            if (outerFeatherWidth > Mathf.Epsilon)
            {
                BuildEllipse(rect.center, radiusX, radiusY, buffer.First, pointCount);
                BuildEllipse(
                    rect.center,
                    radiusX - outerFeatherWidth,
                    radiusY - outerFeatherWidth,
                    buffer.Second,
                    pointCount);
                GeometryWriter.AddRing(
                    vertexHelper,
                    buffer.First,
                    Transparent(outerShapeColor),
                    buffer.Second,
                    outerShapeColor,
                    pointCount);
            }

            if (!hasBorder)
            {
                GeometryWriter.AddFilledPolygon(vertexHelper, rect.center, buffer.Second, pointCount, fillColor);
                return;
            }

            float innerRadiusX = radiusX - clampedBorderWidth;
            float innerRadiusY = radiusY - clampedBorderWidth;
            if (innerRadiusX <= Mathf.Epsilon || innerRadiusY <= Mathf.Epsilon)
            {
                GeometryWriter.AddFilledPolygon(vertexHelper, rect.center, buffer.Second, pointCount, borderColor);
                return;
            }

            BuildEllipse(rect.center, innerRadiusX, innerRadiusY, buffer.Third, pointCount);
            GeometryWriter.AddRing(
                vertexHelper,
                buffer.Second,
                borderColor,
                buffer.Third,
                borderColor,
                pointCount);

            float featherRadiusX = innerRadiusX - antiAliasingWidth;
            float featherRadiusY = innerRadiusY - antiAliasingWidth;
            if (antiAliasingEnabled
                && antiAliasingWidth > Mathf.Epsilon
                && featherRadiusX > Mathf.Epsilon
                && featherRadiusY > Mathf.Epsilon)
            {
                BuildEllipse(rect.center, featherRadiusX, featherRadiusY, buffer.Fourth, pointCount);
                GeometryWriter.AddRing(
                    vertexHelper,
                    buffer.Third,
                    borderColor,
                    buffer.Fourth,
                    fillColor,
                    pointCount);
                GeometryWriter.AddFilledPolygon(
                    vertexHelper,
                    rect.center,
                    buffer.Fourth,
                    pointCount,
                    fillColor);
                return;
            }

            GeometryWriter.AddFilledPolygon(
                vertexHelper,
                rect.center,
                buffer.Third,
                pointCount,
                fillColor);
        }

        internal static int GetSegmentCount(float radius)
        {
            float circumference = 2f * Mathf.PI * radius;
            int segmentCount = Mathf.Clamp(
                Mathf.CeilToInt(circumference / ArcTargetLength),
                MinimumSegments,
                MaximumSegments);

            return (segmentCount + 3) / 4 * 4;
        }

        internal static void BuildEllipse(
            Vector2 center,
            float radiusX,
            float radiusY,
            Vector2[] points,
            int pointCount)
        {
            for (int i = 0; i < pointCount; i++)
            {
                float angle = i * Mathf.PI * 2f / pointCount;
                points[i] = center + new Vector2(
                    Mathf.Cos(angle) * radiusX,
                    Mathf.Sin(angle) * radiusY);
            }
        }

        private static Color32 Transparent(Color32 color)
        {
            color.a = 0;
            return color;
        }
    }
}
