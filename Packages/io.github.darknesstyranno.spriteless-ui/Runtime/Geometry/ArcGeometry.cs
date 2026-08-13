using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class ArcGeometry
    {
        private const float ArcTargetLength = 4f;
        private const float MinimumInnerRadius = 0.001f;
        private const int MinimumFullSegments = 16;
        private const int MaximumFullSegments = 128;
        private const int MinimumCapSegments = 4;
        private const int MaximumCapSegments = 24;

        public static void Build(
            VertexHelper vertexHelper,
            Rect rect,
            float thickness,
            float startAngle,
            float sweepAngle,
            ArcCap cap,
            Color32 color,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;
            float maximumThickness = Mathf.Max(
                0f,
                Mathf.Min(radiusX, radiusY) - MinimumInnerRadius);
            float clampedThickness = Mathf.Clamp(thickness, 0f, maximumThickness);
            float clampedSweep = Mathf.Clamp(sweepAngle, -360f, 360f);
            float absoluteSweep = Mathf.Abs(clampedSweep);
            if (clampedThickness <= Mathf.Epsilon || absoluteSweep <= Mathf.Epsilon)
            {
                return;
            }

            bool closed = absoluteSweep >= 360f - Mathf.Epsilon;
            if (closed)
            {
                clampedSweep = Mathf.Sign(clampedSweep) * 360f;
                absoluteSweep = 360f;
            }

            float featherWidth = antiAliasingEnabled
                ? Mathf.Min(antiAliasingWidth, clampedThickness * 0.5f)
                : 0f;
            bool fadeFlatCaps = !closed && cap == ArcCap.Flat && featherWidth > Mathf.Epsilon;
            float capInsetAngle = fadeFlatCaps
                ? GetCapInsetAngle(
                    radiusX,
                    radiusY,
                    clampedThickness,
                    featherWidth,
                    absoluteSweep)
                : 0f;

            int segmentCount = GetSegmentCount(Mathf.Max(radiusX, radiusY), absoluteSweep);
            int pointCount = GetPointCount(
                segmentCount,
                absoluteSweep,
                capInsetAngle,
                closed,
                fadeFlatCaps);
            buffer.EnsureCapacity(pointCount);

            BuildContours(
                rect.center,
                radiusX,
                radiusY,
                clampedThickness,
                startAngle,
                clampedSweep,
                absoluteSweep,
                featherWidth,
                capInsetAngle,
                closed,
                fadeFlatCaps,
                buffer,
                pointCount);
            AddArcStrip(
                vertexHelper,
                buffer,
                color,
                pointCount,
                closed,
                featherWidth > Mathf.Epsilon,
                fadeFlatCaps);

            if (!closed && cap == ArcCap.Round)
            {
                AddRoundCap(
                    vertexHelper,
                    rect.center,
                    radiusX,
                    radiusY,
                    clampedThickness,
                    startAngle,
                    Mathf.Sign(clampedSweep),
                    true,
                    color,
                    featherWidth,
                    buffer);
                AddRoundCap(
                    vertexHelper,
                    rect.center,
                    radiusX,
                    radiusY,
                    clampedThickness,
                    startAngle + clampedSweep,
                    Mathf.Sign(clampedSweep),
                    false,
                    color,
                    featherWidth,
                    buffer);
            }
        }

        internal static bool ContainsPoint(
            Vector2 point,
            Rect rect,
            float thickness,
            float startAngle,
            float sweepAngle,
            ArcCap cap,
            bool antiAliasingEnabled,
            float antiAliasingWidth,
            ShapePointBuffer buffer)
        {
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;
            float maximumThickness = Mathf.Max(
                0f,
                Mathf.Min(radiusX, radiusY) - MinimumInnerRadius);
            float clampedThickness = Mathf.Clamp(thickness, 0f, maximumThickness);
            float clampedSweep = Mathf.Clamp(sweepAngle, -360f, 360f);
            float absoluteSweep = Mathf.Abs(clampedSweep);
            if (clampedThickness <= Mathf.Epsilon || absoluteSweep <= Mathf.Epsilon)
            {
                return false;
            }

            bool closed = absoluteSweep >= 360f - Mathf.Epsilon;
            if (closed)
            {
                clampedSweep = Mathf.Sign(clampedSweep) * 360f;
                absoluteSweep = 360f;
            }

            float featherWidth = antiAliasingEnabled
                ? Mathf.Min(antiAliasingWidth, clampedThickness * 0.5f)
                : 0f;
            bool fadeFlatCaps = !closed && cap == ArcCap.Flat && featherWidth > Mathf.Epsilon;
            float capInsetAngle = fadeFlatCaps
                ? GetCapInsetAngle(
                    radiusX,
                    radiusY,
                    clampedThickness,
                    featherWidth,
                    absoluteSweep)
                : 0f;
            int segmentCount = GetSegmentCount(Mathf.Max(radiusX, radiusY), absoluteSweep);
            int pointCount = GetPointCount(
                segmentCount,
                absoluteSweep,
                capInsetAngle,
                closed,
                fadeFlatCaps);
            buffer.EnsureCapacity(pointCount);
            BuildContours(
                rect.center,
                radiusX,
                radiusY,
                clampedThickness,
                startAngle,
                clampedSweep,
                absoluteSweep,
                featherWidth,
                capInsetAngle,
                closed,
                fadeFlatCaps,
                buffer,
                pointCount);

            int connectionCount = closed ? pointCount : pointCount - 1;
            for (int i = 0; i < connectionCount; i++)
            {
                int next = (i + 1) % pointCount;
                if (GeometryRaycast.ContainsQuad(
                        point,
                        buffer.First[i],
                        buffer.First[next],
                        buffer.Fourth[next],
                        buffer.Fourth[i]))
                {
                    return true;
                }
            }

            if (closed || cap != ArcCap.Round)
            {
                return false;
            }

            float capRadius = clampedThickness * 0.5f;
            Vector2 startCapCenter = GetRoundCapCenter(
                rect.center,
                radiusX,
                radiusY,
                capRadius,
                startAngle);
            Vector2 endCapCenter = GetRoundCapCenter(
                rect.center,
                radiusX,
                radiusY,
                capRadius,
                startAngle + clampedSweep);
            return GeometryRaycast.ContainsCircle(point, startCapCenter, capRadius)
                || GeometryRaycast.ContainsCircle(point, endCapCenter, capRadius);
        }

        private static int GetPointCount(
            int segmentCount,
            float absoluteSweep,
            float capInsetAngle,
            bool closed,
            bool fadeFlatCaps)
        {
            if (closed)
            {
                return segmentCount;
            }

            if (!fadeFlatCaps)
            {
                return segmentCount + 1;
            }

            float bodySweep = Mathf.Max(0f, absoluteSweep - capInsetAngle * 2f);
            int bodySegments = bodySweep > Mathf.Epsilon
                ? Mathf.Max(1, Mathf.CeilToInt(segmentCount * bodySweep / absoluteSweep))
                : 0;
            return bodySegments + 3;
        }

        private static void BuildContours(
            Vector2 center,
            float radiusX,
            float radiusY,
            float thickness,
            float startAngle,
            float signedSweep,
            float absoluteSweep,
            float featherWidth,
            float capInsetAngle,
            bool closed,
            bool fadeFlatCaps,
            ShapePointBuffer buffer,
            int pointCount)
        {
            float direction = Mathf.Sign(signedSweep);
            float innerRadiusX = radiusX - thickness;
            float innerRadiusY = radiusY - thickness;

            for (int i = 0; i < pointCount; i++)
            {
                float progress = GetProgressAngle(
                    i,
                    pointCount,
                    absoluteSweep,
                    capInsetAngle,
                    closed,
                    fadeFlatCaps);
                float radians = ToRadians(startAngle + direction * progress);
                float cosine = Mathf.Cos(radians);
                float sine = Mathf.Sin(radians);

                buffer.First[i] = center + new Vector2(cosine * radiusX, sine * radiusY);
                buffer.Second[i] = center + new Vector2(
                    cosine * (radiusX - featherWidth),
                    sine * (radiusY - featherWidth));
                buffer.Third[i] = center + new Vector2(
                    cosine * (innerRadiusX + featherWidth),
                    sine * (innerRadiusY + featherWidth));
                buffer.Fourth[i] = center + new Vector2(
                    cosine * innerRadiusX,
                    sine * innerRadiusY);
            }
        }

        private static float GetProgressAngle(
            int index,
            int pointCount,
            float absoluteSweep,
            float capInsetAngle,
            bool closed,
            bool fadeFlatCaps)
        {
            if (closed)
            {
                return index * 360f / pointCount;
            }

            if (!fadeFlatCaps)
            {
                return index * absoluteSweep / (pointCount - 1);
            }

            if (index == 0)
            {
                return 0f;
            }

            if (index == pointCount - 1)
            {
                return absoluteSweep;
            }

            int bodySegments = pointCount - 3;
            if (bodySegments == 0)
            {
                return absoluteSweep * 0.5f;
            }

            float bodySweep = absoluteSweep - capInsetAngle * 2f;
            return capInsetAngle + (index - 1) * bodySweep / bodySegments;
        }

        private static void AddArcStrip(
            VertexHelper vertexHelper,
            ShapePointBuffer buffer,
            Color32 color,
            int pointCount,
            bool closed,
            bool antiAliasing,
            bool fadeFlatCaps)
        {
            int layerCount = antiAliasing ? 4 : 2;
            int vertexStart = vertexHelper.currentVertCount;
            Color32 transparent = Transparent(color);

            for (int i = 0; i < pointCount; i++)
            {
                if (!antiAliasing)
                {
                    GeometryWriter.AddVertex(vertexHelper, buffer.First[i], color);
                    GeometryWriter.AddVertex(vertexHelper, buffer.Fourth[i], color);
                    continue;
                }

                Color32 solidColor = fadeFlatCaps && (i == 0 || i == pointCount - 1)
                    ? transparent
                    : color;
                GeometryWriter.AddVertex(vertexHelper, buffer.First[i], transparent);
                GeometryWriter.AddVertex(vertexHelper, buffer.Second[i], solidColor);
                GeometryWriter.AddVertex(vertexHelper, buffer.Third[i], solidColor);
                GeometryWriter.AddVertex(vertexHelper, buffer.Fourth[i], transparent);
            }

            int connectionCount = closed ? pointCount : pointCount - 1;
            for (int i = 0; i < connectionCount; i++)
            {
                int next = (i + 1) % pointCount;
                for (int layer = 0; layer < layerCount - 1; layer++)
                {
                    int currentOuter = vertexStart + i * layerCount + layer;
                    int currentInner = currentOuter + 1;
                    int nextOuter = vertexStart + next * layerCount + layer;
                    int nextInner = nextOuter + 1;
                    vertexHelper.AddTriangle(currentOuter, nextOuter, nextInner);
                    vertexHelper.AddTriangle(currentOuter, nextInner, currentInner);
                }
            }
        }

        private static void AddRoundCap(
            VertexHelper vertexHelper,
            Vector2 shapeCenter,
            float radiusX,
            float radiusY,
            float thickness,
            float angle,
            float sweepDirection,
            bool isStart,
            Color32 color,
            float featherWidth,
            ShapePointBuffer buffer)
        {
            float capRadius = thickness * 0.5f;
            int segmentCount = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.PI * capRadius / ArcTargetLength),
                MinimumCapSegments,
                MaximumCapSegments);
            int pointCount = segmentCount + 1;
            buffer.EnsureCapacity(pointCount);

            float radians = ToRadians(angle);
            Vector2 radial = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            float centerRadiusX = radiusX - capRadius;
            float centerRadiusY = radiusY - capRadius;
            Vector2 capCenter = GetRoundCapCenter(
                shapeCenter,
                radiusX,
                radiusY,
                capRadius,
                angle);
            Vector2 clockwiseTangent = new Vector2(
                radial.y * centerRadiusX,
                -radial.x * centerRadiusY).normalized;
            Vector2 pathTangent = clockwiseTangent * sweepDirection;
            Vector2 outsideTangent = isStart ? -pathTangent : pathTangent;
            float solidRadius = Mathf.Max(0f, capRadius - featherWidth);

            for (int i = 0; i < pointCount; i++)
            {
                float capAngle = i * Mathf.PI / segmentCount;
                Vector2 direction = radial * Mathf.Cos(capAngle)
                    + outsideTangent * Mathf.Sin(capAngle);
                buffer.First[i] = capCenter + direction * capRadius;
                buffer.Second[i] = capCenter + direction * solidRadius;
            }

            if (featherWidth > Mathf.Epsilon)
            {
                GeometryWriter.AddStrip(
                    vertexHelper,
                    buffer.First,
                    Transparent(color),
                    buffer.Second,
                    color,
                    pointCount,
                    false);
                GeometryWriter.AddFilledPolygon(
                    vertexHelper,
                    capCenter,
                    buffer.Second,
                    pointCount,
                    color);
                return;
            }

            GeometryWriter.AddFilledPolygon(
                vertexHelper,
                capCenter,
                buffer.First,
                pointCount,
                color);
        }

        private static Vector2 GetRoundCapCenter(
            Vector2 shapeCenter,
            float radiusX,
            float radiusY,
            float capRadius,
            float angle)
        {
            float radians = ToRadians(angle);
            Vector2 radial = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            return shapeCenter + new Vector2(
                radial.x * (radiusX - capRadius),
                radial.y * (radiusY - capRadius));
        }

        private static float GetCapInsetAngle(
            float radiusX,
            float radiusY,
            float thickness,
            float featherWidth,
            float absoluteSweep)
        {
            float centerlineRadius = Mathf.Max(radiusX, radiusY) - thickness * 0.5f;
            if (centerlineRadius <= Mathf.Epsilon)
            {
                return absoluteSweep * 0.5f;
            }

            return Mathf.Min(
                featherWidth / centerlineRadius * Mathf.Rad2Deg,
                absoluteSweep * 0.5f);
        }

        private static int GetSegmentCount(float radius, float absoluteSweep)
        {
            int fullSegments = Mathf.Clamp(
                Mathf.CeilToInt(2f * Mathf.PI * radius / ArcTargetLength),
                MinimumFullSegments,
                MaximumFullSegments);
            fullSegments = (fullSegments + 3) / 4 * 4;
            return Mathf.Max(1, Mathf.CeilToInt(fullSegments * absoluteSweep / 360f));
        }

        private static float ToRadians(float clockwiseAngleFromTop)
        {
            return (90f - clockwiseAngleFromTop) * Mathf.Deg2Rad;
        }

        private static Color32 Transparent(Color32 color)
        {
            color.a = 0;
            return color;
        }
    }
}
