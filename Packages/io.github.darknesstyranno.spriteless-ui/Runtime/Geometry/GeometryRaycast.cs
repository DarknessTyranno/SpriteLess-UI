using UnityEngine;

namespace SpriteLessUI.Geometry
{
    internal static class GeometryRaycast
    {
        public static bool ContainsEllipse(
            Vector2 point,
            Vector2 center,
            float radiusX,
            float radiusY)
        {
            if (radiusX <= Mathf.Epsilon || radiusY <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 offset = point - center;
            float normalizedX = offset.x / radiusX;
            float normalizedY = offset.y / radiusY;
            return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
        }

        public static bool ContainsRoundedRectangle(
            Vector2 point,
            Rect rect,
            CornerRadii requestedRadii)
        {
            if (!rect.Contains(point))
            {
                return false;
            }

            CornerRadii radii = requestedRadii.Normalized(rect.width, rect.height);
            if (point.x < rect.xMin + radii.BottomLeft
                && point.y < rect.yMin + radii.BottomLeft)
            {
                return ContainsCircle(
                    point,
                    new Vector2(
                        rect.xMin + radii.BottomLeft,
                        rect.yMin + radii.BottomLeft),
                    radii.BottomLeft);
            }

            if (point.x > rect.xMax - radii.BottomRight
                && point.y < rect.yMin + radii.BottomRight)
            {
                return ContainsCircle(
                    point,
                    new Vector2(
                        rect.xMax - radii.BottomRight,
                        rect.yMin + radii.BottomRight),
                    radii.BottomRight);
            }

            if (point.x > rect.xMax - radii.TopRight
                && point.y > rect.yMax - radii.TopRight)
            {
                return ContainsCircle(
                    point,
                    new Vector2(
                        rect.xMax - radii.TopRight,
                        rect.yMax - radii.TopRight),
                    radii.TopRight);
            }

            if (point.x < rect.xMin + radii.TopLeft
                && point.y > rect.yMax - radii.TopLeft)
            {
                return ContainsCircle(
                    point,
                    new Vector2(
                        rect.xMin + radii.TopLeft,
                        rect.yMax - radii.TopLeft),
                    radii.TopLeft);
            }

            return true;
        }

        public static bool ContainsPolygon(
            Vector2 point,
            Vector2[] points,
            int pointCount)
        {
            bool inside = false;
            int previous = pointCount - 1;
            for (int current = 0; current < pointCount; current++)
            {
                Vector2 first = points[previous];
                Vector2 second = points[current];
                if (IsOnSegment(point, first, second))
                {
                    return true;
                }

                bool crosses = (second.y > point.y) != (first.y > point.y)
                    && point.x < (first.x - second.x)
                        * (point.y - second.y)
                        / (first.y - second.y)
                        + second.x;
                if (crosses)
                {
                    inside = !inside;
                }

                previous = current;
            }

            return inside;
        }

        public static bool ContainsQuad(
            Vector2 point,
            Vector2 first,
            Vector2 second,
            Vector2 third,
            Vector2 fourth)
        {
            return ContainsTriangle(point, first, second, third)
                || ContainsTriangle(point, first, third, fourth);
        }

        public static bool ContainsCircle(Vector2 point, Vector2 center, float radius)
        {
            return radius > Mathf.Epsilon
                && (point - center).sqrMagnitude <= radius * radius;
        }

        private static bool ContainsTriangle(
            Vector2 point,
            Vector2 first,
            Vector2 second,
            Vector2 third)
        {
            float firstCross = Cross(second - first, point - first);
            float secondCross = Cross(third - second, point - second);
            float thirdCross = Cross(first - third, point - third);
            bool hasNegative = firstCross < -Mathf.Epsilon
                || secondCross < -Mathf.Epsilon
                || thirdCross < -Mathf.Epsilon;
            bool hasPositive = firstCross > Mathf.Epsilon
                || secondCross > Mathf.Epsilon
                || thirdCross > Mathf.Epsilon;
            return !(hasNegative && hasPositive);
        }

        private static bool IsOnSegment(Vector2 point, Vector2 first, Vector2 second)
        {
            Vector2 segment = second - first;
            Vector2 offset = point - first;
            float cross = Mathf.Abs(Cross(segment, offset));
            float tolerance = Mathf.Max(1f, segment.magnitude) * 0.00001f;
            if (cross > tolerance)
            {
                return false;
            }

            float dot = Vector2.Dot(offset, segment);
            return dot >= 0f && dot <= segment.sqrMagnitude;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }
    }
}
