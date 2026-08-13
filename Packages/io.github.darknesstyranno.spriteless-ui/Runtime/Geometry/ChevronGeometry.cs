using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class ChevronGeometry
    {
        internal const float MinimumSpread = 0.05f;
        private const float EquilateralForwardRatio = 0.8660254f;

        public static void Build(
            VertexHelper vertexHelper,
            Rect rect,
            ChevronType type,
            ShapeDirection direction,
            float requestedThickness,
            float requestedSpread,
            ArcCap cap,
            StrokeJoin join,
            Color32 color,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            if (!TryGetPath(
                    rect,
                    type,
                    direction,
                    requestedThickness,
                    requestedSpread,
                    join,
                    out Vector2 start,
                    out Vector2 middle,
                    out Vector2 end,
                    out float thickness))
            {
                return;
            }

            StrokeGeometry.Build(
                vertexHelper,
                start,
                middle,
                end,
                thickness,
                cap,
                join,
                color,
                antiAliasingEnabled,
                antiAliasingWidth,
                buffer);
        }

        internal static bool ContainsPoint(
            Vector2 point,
            Rect rect,
            ChevronType type,
            ShapeDirection direction,
            float requestedThickness,
            float requestedSpread,
            ArcCap cap,
            StrokeJoin join,
            ShapePointBuffer buffer)
        {
            return TryGetPath(
                    rect,
                    type,
                    direction,
                    requestedThickness,
                    requestedSpread,
                    join,
                    out Vector2 start,
                    out Vector2 middle,
                    out Vector2 end,
                    out float thickness)
                && StrokeGeometry.ContainsPoint(
                    point,
                    start,
                    middle,
                    end,
                    thickness,
                    cap,
                    join,
                    buffer);
        }

        internal static bool TryGetPath(
            Rect rect,
            ChevronType type,
            ShapeDirection direction,
            float requestedThickness,
            float requestedSpread,
            StrokeJoin join,
            out Vector2 start,
            out Vector2 middle,
            out Vector2 end,
            out float thickness)
        {
            float marginMultiplier = join == StrokeJoin.Miter
                ? StrokeGeometry.MiterLimit
                : 1f;
            float minimumSize = Mathf.Min(rect.width, rect.height);
            float maximumThickness = minimumSize * 0.95f / marginMultiplier;
            thickness = Mathf.Clamp(requestedThickness, 0f, maximumThickness);
            if (thickness <= Mathf.Epsilon)
            {
                start = default;
                middle = default;
                end = default;
                return false;
            }

            float margin = thickness * 0.5f * marginMultiplier;
            ShapeDirectionUtility.GetHalfExtents(
                rect,
                direction,
                out float forwardExtent,
                out float sideExtent);
            forwardExtent -= margin;
            sideExtent -= margin;
            if (forwardExtent <= Mathf.Epsilon || sideExtent <= Mathf.Epsilon)
            {
                start = default;
                middle = default;
                end = default;
                return false;
            }

            Vector2 forward = ShapeDirectionUtility.GetForward(direction);
            Vector2 left = ShapeDirectionUtility.GetLeft(forward);
            float spread = Mathf.Clamp(requestedSpread, MinimumSpread, 1f);
            switch (type)
            {
                case ChevronType.Stretch:
                    sideExtent *= spread;
                    break;

                case ChevronType.Equilateral:
                    sideExtent = Mathf.Min(
                        sideExtent,
                        forwardExtent / EquilateralForwardRatio) * spread;
                    forwardExtent = sideExtent * EquilateralForwardRatio;
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(type), type, null);
            }

            Vector2 backCenter = rect.center - forward * forwardExtent;
            start = backCenter + left * sideExtent;
            middle = rect.center + forward * forwardExtent;
            end = backCenter - left * sideExtent;
            return true;
        }
    }
}
