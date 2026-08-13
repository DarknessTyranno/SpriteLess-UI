using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class RoundedRectangleGeometry
    {
        private const float ArcTargetLength = 4f;
        private const int MinimumCornerSegments = 2;
        private const int MaximumCornerSegments = 12;

        public static void Build(
            VertexHelper vertexHelper,
            Rect rect,
            CornerRadii requestedRadii,
            Color32 fillColor,
            bool borderEnabled,
            float borderWidth,
            Color32 borderColor,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            CornerRadii outerRadii = requestedRadii.Normalized(rect.width, rect.height);
            int cornerSegments = GetCornerSegments(outerRadii.Max);
            int pointCount = outerRadii.Max <= Mathf.Epsilon ? 4 : 4 * (cornerSegments + 1);
            buffer.EnsureCapacity(pointCount);

            BuildPerimeter(rect, outerRadii, cornerSegments, pointCount, buffer.Second);

            float clampedBorderWidth = borderEnabled
                ? Mathf.Clamp(borderWidth, 0f, Mathf.Min(rect.width, rect.height) * 0.5f)
                : 0f;
            bool hasBorder = clampedBorderWidth > Mathf.Epsilon;
            Color32 outerShapeColor = hasBorder ? borderColor : fillColor;

            float outerFeatherWidth = antiAliasingEnabled
                ? Mathf.Min(
                    antiAliasingWidth,
                    hasBorder
                        ? clampedBorderWidth
                        : Mathf.Min(rect.width, rect.height) * 0.5f)
                : 0f;
            if (outerFeatherWidth > Mathf.Epsilon)
            {
                Rect solidOuterRect = Inset(rect, outerFeatherWidth);
                CornerRadii solidOuterRadii = outerRadii
                    .Inset(outerFeatherWidth)
                    .Normalized(solidOuterRect.width, solidOuterRect.height);
                BuildPerimeter(rect, outerRadii, cornerSegments, pointCount, buffer.First);
                BuildPerimeter(
                    solidOuterRect,
                    solidOuterRadii,
                    cornerSegments,
                    pointCount,
                    buffer.Second);
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

            Rect innerRect = Inset(rect, clampedBorderWidth);
            if (innerRect.width <= Mathf.Epsilon || innerRect.height <= Mathf.Epsilon)
            {
                GeometryWriter.AddFilledPolygon(vertexHelper, rect.center, buffer.Second, pointCount, borderColor);
                return;
            }

            CornerRadii innerRadii = outerRadii
                .Inset(clampedBorderWidth)
                .Normalized(innerRect.width, innerRect.height);
            BuildPerimeter(innerRect, innerRadii, cornerSegments, pointCount, buffer.Third);

            GeometryWriter.AddRing(
                vertexHelper,
                buffer.Second,
                borderColor,
                buffer.Third,
                borderColor,
                pointCount);

            Rect featherRect = Inset(innerRect, antiAliasingWidth);
            if (antiAliasingEnabled
                && antiAliasingWidth > Mathf.Epsilon
                && featherRect.width > Mathf.Epsilon
                && featherRect.height > Mathf.Epsilon)
            {
                CornerRadii featherRadii = innerRadii
                    .Inset(antiAliasingWidth)
                    .Normalized(featherRect.width, featherRect.height);
                BuildPerimeter(featherRect, featherRadii, cornerSegments, pointCount, buffer.Fourth);
                GeometryWriter.AddRing(
                    vertexHelper,
                    buffer.Third,
                    borderColor,
                    buffer.Fourth,
                    fillColor,
                    pointCount);
                GeometryWriter.AddFilledPolygon(
                    vertexHelper,
                    featherRect.center,
                    buffer.Fourth,
                    pointCount,
                    fillColor);
                return;
            }

            GeometryWriter.AddFilledPolygon(
                vertexHelper,
                innerRect.center,
                buffer.Third,
                pointCount,
                fillColor);
        }

        internal static int GetCornerSegments(float radius)
        {
            if (radius <= Mathf.Epsilon)
            {
                return 1;
            }

            float arcLength = radius * Mathf.PI * 0.5f;
            return Mathf.Clamp(
                Mathf.CeilToInt(arcLength / ArcTargetLength),
                MinimumCornerSegments,
                MaximumCornerSegments);
        }

        internal static void BuildPerimeter(
            Rect rect,
            CornerRadii radii,
            int cornerSegments,
            int pointCount,
            Vector2[] points)
        {
            if (pointCount == 4)
            {
                points[0] = new Vector2(rect.xMin, rect.yMin);
                points[1] = new Vector2(rect.xMax, rect.yMin);
                points[2] = new Vector2(rect.xMax, rect.yMax);
                points[3] = new Vector2(rect.xMin, rect.yMax);
                return;
            }

            int index = 0;
            AddCorner(points, ref index, new Vector2(rect.xMin + radii.BottomLeft, rect.yMin + radii.BottomLeft), radii.BottomLeft, 180f, 270f, cornerSegments);
            AddCorner(points, ref index, new Vector2(rect.xMax - radii.BottomRight, rect.yMin + radii.BottomRight), radii.BottomRight, 270f, 360f, cornerSegments);
            AddCorner(points, ref index, new Vector2(rect.xMax - radii.TopRight, rect.yMax - radii.TopRight), radii.TopRight, 0f, 90f, cornerSegments);
            AddCorner(points, ref index, new Vector2(rect.xMin + radii.TopLeft, rect.yMax - radii.TopLeft), radii.TopLeft, 90f, 180f, cornerSegments);
        }

        private static void AddCorner(
            Vector2[] points,
            ref int index,
            Vector2 center,
            float radius,
            float startAngle,
            float endAngle,
            int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(startAngle, endAngle, i / (float)segments) * Mathf.Deg2Rad;
                points[index++] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
        }

        internal static Rect Inset(Rect rect, float amount)
        {
            return new Rect(
                rect.xMin + amount,
                rect.yMin + amount,
                Mathf.Max(0f, rect.width - amount * 2f),
                Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static Color32 Transparent(Color32 color)
        {
            color.a = 0;
            return color;
        }
    }
}
