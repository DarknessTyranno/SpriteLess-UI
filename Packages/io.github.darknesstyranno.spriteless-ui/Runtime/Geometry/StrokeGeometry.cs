using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class StrokeGeometry
    {
        internal const float MiterLimit = 4f;

        private const int MaximumContourPoints = 64;
        private const int RoundCapSegments = 8;
        private const int MaximumJoinSegments = 12;
        private const float JoinSegmentAngle = 15f;

        public static void Build(
            VertexHelper vertexHelper,
            Vector2 start,
            Vector2 middle,
            Vector2 end,
            float thickness,
            ArcCap cap,
            StrokeJoin join,
            Color32 color,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            float halfWidth = thickness * 0.5f;
            if (halfWidth <= Mathf.Epsilon)
            {
                return;
            }

            buffer.EnsureCapacity(MaximumContourPoints);
            int outerPointCount = BuildContour(
                start,
                middle,
                end,
                halfWidth,
                cap,
                join,
                buffer.First);
            if (outerPointCount < 3)
            {
                return;
            }

            float featherWidth = antiAliasingEnabled
                ? Mathf.Min(antiAliasingWidth, halfWidth * 0.95f)
                : 0f;
            if (featherWidth <= Mathf.Epsilon)
            {
                GeometryWriter.AddFilledPolygonTriangulated(
                    vertexHelper,
                    buffer.First,
                    outerPointCount,
                    color,
                    buffer.Indices,
                    buffer.Triangles);
                return;
            }

            int innerPointCount = BuildContour(
                start,
                middle,
                end,
                halfWidth - featherWidth,
                cap,
                join,
                buffer.Second);
            if (innerPointCount != outerPointCount)
            {
                throw new System.InvalidOperationException("Stroke feather contours must have matching point counts.");
            }

            Color32 transparent = color;
            transparent.a = 0;
            bool fillAdded = GeometryWriter.AddFilledPolygonTriangulated(
                vertexHelper,
                buffer.Second,
                innerPointCount,
                color,
                buffer.Indices,
                buffer.Triangles);
            if (!fillAdded)
            {
                return;
            }

            GeometryWriter.AddRing(
                vertexHelper,
                buffer.First,
                transparent,
                buffer.Second,
                color,
                outerPointCount);
        }

        internal static bool ContainsPoint(
            Vector2 point,
            Vector2 start,
            Vector2 middle,
            Vector2 end,
            float thickness,
            ArcCap cap,
            StrokeJoin join,
            ShapePointBuffer buffer)
        {
            float halfWidth = thickness * 0.5f;
            if (halfWidth <= Mathf.Epsilon)
            {
                return false;
            }

            buffer.EnsureCapacity(MaximumContourPoints);
            int pointCount = BuildContour(
                start,
                middle,
                end,
                halfWidth,
                cap,
                join,
                buffer.First);
            return pointCount >= 3
                && GeometryRaycast.ContainsPolygon(point, buffer.First, pointCount);
        }

        private static int BuildContour(
            Vector2 start,
            Vector2 middle,
            Vector2 end,
            float halfWidth,
            ArcCap cap,
            StrokeJoin join,
            Vector2[] points)
        {
            Vector2 firstDirection = middle - start;
            Vector2 secondDirection = end - middle;
            if (firstDirection.sqrMagnitude <= Mathf.Epsilon
                || secondDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0;
            }

            firstDirection.Normalize();
            secondDirection.Normalize();
            float turn = Cross(firstDirection, secondDirection);
            if (Mathf.Abs(turn) <= Mathf.Epsilon)
            {
                return 0;
            }

            Vector2 firstLeft = ShapeDirectionUtility.GetLeft(firstDirection);
            Vector2 secondLeft = ShapeDirectionUtility.GetLeft(secondDirection);
            int pointCount = 0;

            AddPoint(points, ref pointCount, start + firstLeft * halfWidth);
            AddJoin(
                points,
                ref pointCount,
                middle,
                firstDirection,
                secondDirection,
                firstLeft,
                secondLeft,
                halfWidth,
                join,
                turn < 0f);
            AddPoint(points, ref pointCount, end + secondLeft * halfWidth);
            AddEndCap(
                points,
                ref pointCount,
                end,
                secondLeft,
                halfWidth,
                cap);
            AddJoin(
                points,
                ref pointCount,
                middle,
                secondDirection,
                firstDirection,
                -secondLeft,
                -firstLeft,
                halfWidth,
                join,
                turn > 0f);
            AddPoint(points, ref pointCount, start - firstLeft * halfWidth);
            AddStartCap(
                points,
                ref pointCount,
                start,
                -firstLeft,
                halfWidth,
                cap);

            return pointCount;
        }

        private static void AddJoin(
            Vector2[] points,
            ref int pointCount,
            Vector2 center,
            Vector2 firstDirection,
            Vector2 secondDirection,
            Vector2 firstNormal,
            Vector2 secondNormal,
            float halfWidth,
            StrokeJoin join,
            bool outerSide)
        {
            if (join == StrokeJoin.Round && outerSide)
            {
                float startAngle = Mathf.Atan2(firstNormal.y, firstNormal.x) * Mathf.Rad2Deg;
                float endAngle = Mathf.Atan2(secondNormal.y, secondNormal.x) * Mathf.Rad2Deg;
                float sweep = Mathf.DeltaAngle(startAngle, endAngle);
                int segments = Mathf.Clamp(
                    Mathf.CeilToInt(Mathf.Abs(sweep) / JoinSegmentAngle),
                    2,
                    MaximumJoinSegments);
                AddArc(
                    points,
                    ref pointCount,
                    center,
                    halfWidth,
                    startAngle,
                    sweep,
                    segments,
                    true);
                return;
            }

            Vector2 firstLinePoint = center + firstNormal * halfWidth;
            Vector2 secondLinePoint = center + secondNormal * halfWidth;
            Vector2 intersection = GetLineIntersection(
                firstLinePoint,
                firstDirection,
                secondLinePoint,
                secondDirection);
            Vector2 offset = intersection - center;
            float maximumLength = halfWidth * MiterLimit;
            if (offset.sqrMagnitude > maximumLength * maximumLength)
            {
                intersection = center + offset.normalized * maximumLength;
            }

            AddPoint(points, ref pointCount, intersection);
        }

        private static void AddEndCap(
            Vector2[] points,
            ref int pointCount,
            Vector2 center,
            Vector2 left,
            float halfWidth,
            ArcCap cap)
        {
            if (cap == ArcCap.Flat)
            {
                AddPoint(points, ref pointCount, center - left * halfWidth);
                return;
            }

            float startAngle = Mathf.Atan2(left.y, left.x) * Mathf.Rad2Deg;
            AddArc(
                points,
                ref pointCount,
                center,
                halfWidth,
                startAngle,
                -180f,
                RoundCapSegments,
                false);
        }

        private static void AddStartCap(
            Vector2[] points,
            ref int pointCount,
            Vector2 center,
            Vector2 right,
            float halfWidth,
            ArcCap cap)
        {
            if (cap == ArcCap.Flat)
            {
                return;
            }

            float startAngle = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;
            for (int i = 1; i < RoundCapSegments; i++)
            {
                float angle = (startAngle - 180f * i / RoundCapSegments) * Mathf.Deg2Rad;
                AddPoint(
                    points,
                    ref pointCount,
                    center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * halfWidth);
            }
        }

        private static void AddArc(
            Vector2[] points,
            ref int pointCount,
            Vector2 center,
            float radius,
            float startAngle,
            float sweep,
            int segments,
            bool includeStart)
        {
            int startIndex = includeStart ? 0 : 1;
            for (int i = startIndex; i <= segments; i++)
            {
                float angle = (startAngle + sweep * i / segments) * Mathf.Deg2Rad;
                AddPoint(
                    points,
                    ref pointCount,
                    center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private static Vector2 GetLineIntersection(
            Vector2 firstPoint,
            Vector2 firstDirection,
            Vector2 secondPoint,
            Vector2 secondDirection)
        {
            float denominator = Cross(firstDirection, secondDirection);
            if (Mathf.Abs(denominator) <= Mathf.Epsilon)
            {
                return (firstPoint + secondPoint) * 0.5f;
            }

            float distance = Cross(secondPoint - firstPoint, secondDirection) / denominator;
            return firstPoint + firstDirection * distance;
        }

        private static void AddPoint(Vector2[] points, ref int pointCount, Vector2 point)
        {
            points[pointCount++] = point;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }
    }
}
