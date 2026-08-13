using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class ConvexPolygonGeometry
    {
        private const float MiterLimit = 4f;

        public static void Build(
            VertexHelper vertexHelper,
            Vector2[] shapePoints,
            int pointCount,
            float maximumInset,
            Color32 fillColor,
            bool borderEnabled,
            float borderWidth,
            Color32 borderColor,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            CopyPoints(shapePoints, buffer.Second, pointCount);

            float clampedBorderWidth = borderEnabled
                ? Mathf.Clamp(borderWidth, 0f, maximumInset)
                : 0f;
            bool hasBorder = clampedBorderWidth > Mathf.Epsilon;
            Color32 outerShapeColor = hasBorder ? borderColor : fillColor;

            float outerFeatherWidth = antiAliasingEnabled
                ? Mathf.Min(antiAliasingWidth, hasBorder ? clampedBorderWidth : antiAliasingWidth)
                : 0f;
            if (outerFeatherWidth > Mathf.Epsilon
                && TryBuildInset(shapePoints, pointCount, outerFeatherWidth, buffer.Second))
            {
                GeometryWriter.AddRing(
                    vertexHelper,
                    shapePoints,
                    Transparent(outerShapeColor),
                    buffer.Second,
                    outerShapeColor,
                    pointCount);
            }

            if (!hasBorder)
            {
                GeometryWriter.AddFilledPolygon(
                    vertexHelper,
                    CalculateCenter(buffer.Second, pointCount),
                    buffer.Second,
                    pointCount,
                    fillColor);
                return;
            }

            if (!TryBuildInset(shapePoints, pointCount, clampedBorderWidth, buffer.Third))
            {
                GeometryWriter.AddFilledPolygon(
                    vertexHelper,
                    CalculateCenter(buffer.Second, pointCount),
                    buffer.Second,
                    pointCount,
                    borderColor);
                return;
            }

            GeometryWriter.AddRing(
                vertexHelper,
                buffer.Second,
                borderColor,
                buffer.Third,
                borderColor,
                pointCount);

            if (antiAliasingEnabled
                && antiAliasingWidth > Mathf.Epsilon
                && TryBuildInset(
                    shapePoints,
                    pointCount,
                    clampedBorderWidth + antiAliasingWidth,
                    buffer.Fourth))
            {
                GeometryWriter.AddRing(
                    vertexHelper,
                    buffer.Third,
                    borderColor,
                    buffer.Fourth,
                    fillColor,
                    pointCount);
                GeometryWriter.AddFilledPolygon(
                    vertexHelper,
                    CalculateCenter(buffer.Fourth, pointCount),
                    buffer.Fourth,
                    pointCount,
                    fillColor);
                return;
            }

            GeometryWriter.AddFilledPolygon(
                vertexHelper,
                CalculateCenter(buffer.Third, pointCount),
                buffer.Third,
                pointCount,
                fillColor);
        }

        public static bool TryBuildInset(
            Vector2[] source,
            int pointCount,
            float distance,
            Vector2[] destination)
        {
            for (int i = 0; i < pointCount; i++)
            {
                Vector2 previous = source[(i + pointCount - 1) % pointCount];
                Vector2 current = source[i];
                Vector2 next = source[(i + 1) % pointCount];
                Vector2 previousDirection = (current - previous).normalized;
                Vector2 nextDirection = (next - current).normalized;
                Vector2 previousNormal = new Vector2(-previousDirection.y, previousDirection.x);
                Vector2 nextNormal = new Vector2(-nextDirection.y, nextDirection.x);
                Vector2 previousLinePoint = current + previousNormal * distance;
                Vector2 nextLinePoint = current + nextNormal * distance;

                float denominator = Cross(previousDirection, nextDirection);
                if (Mathf.Abs(denominator) <= Mathf.Epsilon)
                {
                    return false;
                }

                float lineDistance = Cross(nextLinePoint - previousLinePoint, nextDirection)
                    / denominator;
                Vector2 intersection = previousLinePoint + previousDirection * lineDistance;
                destination[i] = LimitMiter(current, intersection, distance);
            }

            return SignedArea(destination, pointCount) > Mathf.Epsilon;
        }

        private static void CopyPoints(Vector2[] source, Vector2[] destination, int pointCount)
        {
            for (int i = 0; i < pointCount; i++)
            {
                destination[i] = source[i];
            }
        }

        private static Vector2 LimitMiter(Vector2 origin, Vector2 point, float distance)
        {
            Vector2 offset = point - origin;
            float maximumLength = Mathf.Abs(distance) * MiterLimit;
            if (maximumLength <= Mathf.Epsilon || offset.sqrMagnitude <= maximumLength * maximumLength)
            {
                return point;
            }

            return origin + offset.normalized * maximumLength;
        }

        private static Vector2 CalculateCenter(Vector2[] points, int pointCount)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < pointCount; i++)
            {
                center += points[i];
            }

            return center / pointCount;
        }

        private static float SignedArea(Vector2[] points, int pointCount)
        {
            float twiceArea = 0f;
            for (int i = 0; i < pointCount; i++)
            {
                Vector2 current = points[i];
                Vector2 next = points[(i + 1) % pointCount];
                twiceArea += current.x * next.y - next.x * current.y;
            }

            return twiceArea * 0.5f;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static Color32 Transparent(Color32 color)
        {
            color.a = 0;
            return color;
        }
    }
}
