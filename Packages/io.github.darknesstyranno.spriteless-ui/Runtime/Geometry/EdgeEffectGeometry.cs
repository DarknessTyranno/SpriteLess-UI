using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class EdgeEffectGeometry
    {
        private const float MinimumInnerSize = 0.001f;

        public static void BuildRoundedRectangle(
            VertexHelper vertexHelper,
            Rect rect,
            CornerRadii requestedRadii,
            bool borderEnabled,
            float borderWidth,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            float effectWidth,
            Vector2 direction,
            Color32 effectColor,
            ShapePointBuffer buffer)
        {
            if (!CanBuild(effectWidth, direction, effectColor))
            {
                return;
            }

            CornerRadii shapeRadii = requestedRadii.Normalized(rect.width, rect.height);
            float fillInset = GetRectFillInset(
                rect,
                borderEnabled,
                borderWidth,
                antiAliasingEnabled,
                antiAliasingWidth);
            Rect fillRect = RoundedRectangleGeometry.Inset(rect, fillInset);
            if (!HasArea(fillRect))
            {
                return;
            }

            float clampedWidth = Mathf.Min(
                effectWidth,
                Mathf.Min(fillRect.width, fillRect.height) * 0.5f - MinimumInnerSize);
            if (clampedWidth <= Mathf.Epsilon)
            {
                return;
            }

            Rect innerRect = RoundedRectangleGeometry.Inset(fillRect, clampedWidth);
            CornerRadii fillRadii = shapeRadii
                .Inset(fillInset)
                .Normalized(fillRect.width, fillRect.height);
            CornerRadii innerRadii = fillRadii
                .Inset(clampedWidth)
                .Normalized(innerRect.width, innerRect.height);
            int cornerSegments = RoundedRectangleGeometry.GetCornerSegments(shapeRadii.Max);
            int pointCount = shapeRadii.Max <= Mathf.Epsilon
                ? 4
                : 4 * (cornerSegments + 1);
            buffer.EnsureCapacity(pointCount);
            RoundedRectangleGeometry.BuildPerimeter(
                fillRect,
                fillRadii,
                cornerSegments,
                pointCount,
                buffer.First);
            RoundedRectangleGeometry.BuildPerimeter(
                innerRect,
                innerRadii,
                cornerSegments,
                pointCount,
                buffer.Second);
            AddDirectionalStrip(
                vertexHelper,
                buffer.First,
                buffer.Second,
                pointCount,
                direction,
                effectColor);
        }

        public static void BuildCircle(
            VertexHelper vertexHelper,
            Rect rect,
            bool borderEnabled,
            float borderWidth,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            float effectWidth,
            Vector2 direction,
            Color32 effectColor,
            ShapePointBuffer buffer)
        {
            if (!CanBuild(effectWidth, direction, effectColor))
            {
                return;
            }

            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;
            float fillInset = GetEllipseFillInset(
                radiusX,
                radiusY,
                borderEnabled,
                borderWidth,
                antiAliasingEnabled,
                antiAliasingWidth);
            float fillRadiusX = radiusX - fillInset;
            float fillRadiusY = radiusY - fillInset;
            float clampedWidth = Mathf.Min(
                effectWidth,
                Mathf.Min(fillRadiusX, fillRadiusY) - MinimumInnerSize);
            if (clampedWidth <= Mathf.Epsilon)
            {
                return;
            }

            int pointCount = CircleGeometry.GetSegmentCount(Mathf.Max(radiusX, radiusY));
            buffer.EnsureCapacity(pointCount);
            CircleGeometry.BuildEllipse(
                rect.center,
                fillRadiusX,
                fillRadiusY,
                buffer.First,
                pointCount);
            CircleGeometry.BuildEllipse(
                rect.center,
                fillRadiusX - clampedWidth,
                fillRadiusY - clampedWidth,
                buffer.Second,
                pointCount);
            AddDirectionalStrip(
                vertexHelper,
                buffer.First,
                buffer.Second,
                pointCount,
                direction,
                effectColor);
        }

        public static void BuildQuadrilateral(
            VertexHelper vertexHelper,
            Rect rect,
            CornerOffsets cornerOffsets,
            bool borderEnabled,
            float borderWidth,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            float effectWidth,
            Vector2 direction,
            Color32 effectColor,
            ShapePointBuffer buffer)
        {
            if (!CanBuild(effectWidth, direction, effectColor))
            {
                return;
            }

            const int pointCount = 4;
            buffer.EnsureCapacity(pointCount);
            QuadrilateralGeometry.BuildPerimeter(rect, cornerOffsets, buffer.First);

            if (!TryGetQuadrilateralFillInset(
                    buffer.First,
                    rect,
                    borderEnabled,
                    borderWidth,
                    antiAliasingEnabled,
                    antiAliasingWidth,
                    buffer.Second,
                    out float fillInset))
            {
                return;
            }

            Vector2[] fillPoints = buffer.First;
            if (fillInset > Mathf.Epsilon)
            {
                if (!QuadrilateralGeometry.TryBuildOffset(
                        buffer.First,
                        fillInset,
                        buffer.Second))
                {
                    return;
                }

                fillPoints = buffer.Second;
            }

            if (!QuadrilateralGeometry.TryBuildOffset(
                    buffer.First,
                    fillInset + effectWidth,
                    buffer.Third))
            {
                return;
            }

            AddDirectionalStrip(
                vertexHelper,
                fillPoints,
                buffer.Third,
                pointCount,
                direction,
                effectColor);
        }

        public static void BuildTriangle(
            VertexHelper vertexHelper,
            Rect rect,
            TriangleType triangleType,
            ShapeDirection shapeDirection,
            TriangleRightAngle rightAngle,
            bool borderEnabled,
            float borderWidth,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            float effectWidth,
            Vector2 direction,
            Color32 effectColor,
            ShapePointBuffer buffer)
        {
            if (!CanBuild(effectWidth, direction, effectColor))
            {
                return;
            }

            int pointCount = TriangleGeometry.PointCount;
            buffer.EnsureCapacity(pointCount);
            TriangleGeometry.BuildPerimeter(
                rect,
                triangleType,
                shapeDirection,
                rightAngle,
                buffer.First);

            float maximumInset = Mathf.Min(rect.width, rect.height) * 0.5f;
            float clampedBorderWidth = borderEnabled
                ? Mathf.Clamp(borderWidth, 0f, maximumInset)
                : 0f;
            bool hasBorder = clampedBorderWidth > Mathf.Epsilon;
            float fillInset = clampedBorderWidth;
            if (antiAliasingEnabled && antiAliasingWidth > Mathf.Epsilon)
            {
                float featherInset = hasBorder
                    ? clampedBorderWidth + antiAliasingWidth
                    : antiAliasingWidth;
                if (ConvexPolygonGeometry.TryBuildInset(
                        buffer.First,
                        pointCount,
                        featherInset,
                        buffer.Second))
                {
                    fillInset = featherInset;
                }
            }

            Vector2[] fillPoints = buffer.First;
            if (fillInset > Mathf.Epsilon)
            {
                if (!ConvexPolygonGeometry.TryBuildInset(
                        buffer.First,
                        pointCount,
                        fillInset,
                        buffer.Second))
                {
                    return;
                }

                fillPoints = buffer.Second;
            }

            if (!ConvexPolygonGeometry.TryBuildInset(
                    buffer.First,
                    pointCount,
                    fillInset + effectWidth,
                    buffer.Third))
            {
                return;
            }

            AddDirectionalStrip(
                vertexHelper,
                fillPoints,
                buffer.Third,
                pointCount,
                direction,
                effectColor);
        }

        private static float GetRectFillInset(
            Rect rect,
            bool borderEnabled,
            float borderWidth,
            bool antiAliasingEnabled,
            float antiAliasingWidth)
        {
            float maximumInset = Mathf.Min(rect.width, rect.height) * 0.5f;
            float clampedBorderWidth = borderEnabled
                ? Mathf.Clamp(borderWidth, 0f, maximumInset)
                : 0f;
            bool hasBorder = clampedBorderWidth > Mathf.Epsilon;
            float fillInset = clampedBorderWidth;

            if (!antiAliasingEnabled || antiAliasingWidth <= Mathf.Epsilon)
            {
                return fillInset;
            }

            if (!hasBorder)
            {
                return Mathf.Min(antiAliasingWidth, maximumInset);
            }

            Rect featherRect = RoundedRectangleGeometry.Inset(
                rect,
                fillInset + antiAliasingWidth);
            return HasArea(featherRect) ? fillInset + antiAliasingWidth : fillInset;
        }

        private static float GetEllipseFillInset(
            float radiusX,
            float radiusY,
            bool borderEnabled,
            float borderWidth,
            bool antiAliasingEnabled,
            float antiAliasingWidth)
        {
            float maximumInset = Mathf.Min(radiusX, radiusY);
            float clampedBorderWidth = borderEnabled
                ? Mathf.Clamp(borderWidth, 0f, maximumInset)
                : 0f;
            bool hasBorder = clampedBorderWidth > Mathf.Epsilon;
            float fillInset = clampedBorderWidth;

            if (!antiAliasingEnabled || antiAliasingWidth <= Mathf.Epsilon)
            {
                return fillInset;
            }

            if (!hasBorder)
            {
                return Mathf.Min(antiAliasingWidth, maximumInset);
            }

            return radiusX - fillInset - antiAliasingWidth > Mathf.Epsilon
                && radiusY - fillInset - antiAliasingWidth > Mathf.Epsilon
                    ? fillInset + antiAliasingWidth
                    : fillInset;
        }

        private static bool TryGetQuadrilateralFillInset(
            Vector2[] shapePoints,
            Rect rect,
            bool borderEnabled,
            float borderWidth,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            Vector2[] scratchPoints,
            out float fillInset)
        {
            float maximumInset = Mathf.Min(rect.width, rect.height) * 0.5f;
            float clampedBorderWidth = borderEnabled
                ? Mathf.Clamp(borderWidth, 0f, maximumInset)
                : 0f;
            bool hasBorder = clampedBorderWidth > Mathf.Epsilon;

            if (hasBorder
                && !QuadrilateralGeometry.TryBuildOffset(
                    shapePoints,
                    clampedBorderWidth,
                    scratchPoints))
            {
                fillInset = 0f;
                return false;
            }

            if (!antiAliasingEnabled || antiAliasingWidth <= Mathf.Epsilon)
            {
                fillInset = clampedBorderWidth;
                return true;
            }

            float featherInset = hasBorder
                ? clampedBorderWidth + antiAliasingWidth
                : antiAliasingWidth;
            fillInset = QuadrilateralGeometry.TryBuildOffset(
                shapePoints,
                featherInset,
                scratchPoints)
                    ? featherInset
                    : clampedBorderWidth;
            return true;
        }

        private static void AddDirectionalStrip(
            VertexHelper vertexHelper,
            Vector2[] outerPoints,
            Vector2[] innerPoints,
            int pointCount,
            Vector2 direction,
            Color32 effectColor)
        {
            Vector2 normalizedDirection = direction.normalized;
            int outerStart = vertexHelper.currentVertCount;
            for (int i = 0; i < pointCount; i++)
            {
                Color32 vertexColor = effectColor;
                vertexColor.a = (byte)Mathf.RoundToInt(
                    effectColor.a * GetDirectionalWeight(
                        outerPoints,
                        pointCount,
                        i,
                        normalizedDirection));
                GeometryWriter.AddVertex(vertexHelper, outerPoints[i], vertexColor);
            }

            int innerStart = vertexHelper.currentVertCount;
            effectColor.a = 0;
            for (int i = 0; i < pointCount; i++)
            {
                GeometryWriter.AddVertex(vertexHelper, innerPoints[i], effectColor);
            }

            for (int i = 0; i < pointCount; i++)
            {
                int next = (i + 1) % pointCount;
                vertexHelper.AddTriangle(outerStart + i, outerStart + next, innerStart + next);
                vertexHelper.AddTriangle(outerStart + i, innerStart + next, innerStart + i);
            }
        }

        private static float GetDirectionalWeight(
            Vector2[] points,
            int pointCount,
            int index,
            Vector2 direction)
        {
            int previousIndex = FindDistinctPoint(points, pointCount, index, -1);
            int nextIndex = FindDistinctPoint(points, pointCount, index, 1);
            Vector2 previousDirection = (points[index] - points[previousIndex]).normalized;
            Vector2 nextDirection = (points[nextIndex] - points[index]).normalized;
            Vector2 previousNormal = new Vector2(previousDirection.y, -previousDirection.x);
            Vector2 nextNormal = new Vector2(nextDirection.y, -nextDirection.x);
            return Mathf.Clamp01(Mathf.Max(
                Vector2.Dot(previousNormal, direction),
                Vector2.Dot(nextNormal, direction)));
        }

        private static int FindDistinctPoint(
            Vector2[] points,
            int pointCount,
            int index,
            int step)
        {
            int candidate = index;
            for (int i = 0; i < pointCount - 1; i++)
            {
                candidate = (candidate + step + pointCount) % pointCount;
                if ((points[candidate] - points[index]).sqrMagnitude > Mathf.Epsilon)
                {
                    return candidate;
                }
            }

            return index;
        }

        private static bool CanBuild(float width, Vector2 direction, Color32 color)
        {
            return width > Mathf.Epsilon
                && direction.sqrMagnitude > Mathf.Epsilon
                && color.a > 0;
        }

        private static bool HasArea(Rect rect)
        {
            return rect.width > Mathf.Epsilon && rect.height > Mathf.Epsilon;
        }
    }
}
