using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class TriangleGeometry
    {
        internal const int PointCount = 3;
        private const float EquilateralHeightRatio = 0.8660254f;

        public static void Build(
            VertexHelper vertexHelper,
            Rect rect,
            TriangleType type,
            ShapeDirection direction,
            TriangleRightAngle rightAngle,
            Color32 fillColor,
            bool borderEnabled,
            float borderWidth,
            Color32 borderColor,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            buffer.EnsureCapacity(PointCount);
            BuildPerimeter(rect, type, direction, rightAngle, buffer.First);
            bool hasBorder = borderEnabled && borderWidth > Mathf.Epsilon;
            bool hasAntiAliasing = antiAliasingEnabled && antiAliasingWidth > Mathf.Epsilon;
            if (!hasBorder && !hasAntiAliasing)
            {
                GeometryWriter.AddFilledPolygonTriangulated(
                    vertexHelper,
                    buffer.First,
                    PointCount,
                    fillColor,
                    buffer.Indices);
                return;
            }

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
            TriangleType type,
            ShapeDirection direction,
            TriangleRightAngle rightAngle,
            Vector2[] points)
        {
            switch (type)
            {
                case TriangleType.Isosceles:
                    BuildDirectional(rect, direction, false, points);
                    break;

                case TriangleType.Equilateral:
                    BuildDirectional(rect, direction, true, points);
                    break;

                case TriangleType.Right:
                    BuildRight(rect, rightAngle, points);
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static void BuildDirectional(
            Rect rect,
            ShapeDirection direction,
            bool equilateral,
            Vector2[] points)
        {
            Vector2 forward = ShapeDirectionUtility.GetForward(direction);
            Vector2 left = ShapeDirectionUtility.GetLeft(forward);
            ShapeDirectionUtility.GetHalfExtents(
                rect,
                direction,
                out float forwardExtent,
                out float sideExtent);

            if (equilateral)
            {
                float sideLength = Mathf.Min(
                    sideExtent * 2f,
                    forwardExtent * 2f / EquilateralHeightRatio);
                sideExtent = sideLength * 0.5f;
                forwardExtent = sideLength * EquilateralHeightRatio * 0.5f;
            }

            Vector2 baseCenter = rect.center - forward * forwardExtent;
            points[0] = baseCenter + left * sideExtent;
            points[1] = baseCenter - left * sideExtent;
            points[2] = rect.center + forward * forwardExtent;
        }

        private static void BuildRight(
            Rect rect,
            TriangleRightAngle rightAngle,
            Vector2[] points)
        {
            Vector2 bottomLeft = new Vector2(rect.xMin, rect.yMin);
            Vector2 bottomRight = new Vector2(rect.xMax, rect.yMin);
            Vector2 topRight = new Vector2(rect.xMax, rect.yMax);
            Vector2 topLeft = new Vector2(rect.xMin, rect.yMax);

            switch (rightAngle)
            {
                case TriangleRightAngle.BottomLeft:
                    points[0] = bottomLeft;
                    points[1] = bottomRight;
                    points[2] = topLeft;
                    break;

                case TriangleRightAngle.BottomRight:
                    points[0] = bottomRight;
                    points[1] = topRight;
                    points[2] = bottomLeft;
                    break;

                case TriangleRightAngle.TopRight:
                    points[0] = topRight;
                    points[1] = topLeft;
                    points[2] = bottomRight;
                    break;

                case TriangleRightAngle.TopLeft:
                    points[0] = topLeft;
                    points[1] = bottomLeft;
                    points[2] = topRight;
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(rightAngle),
                        rightAngle,
                        null);
            }
        }
    }
}
