using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Geometry
{
    internal static class GeometryWriter
    {
        private static readonly Vector2 WhiteTextureUv = new Vector2(0.5f, 0.5f);

        public static void AddFilledPolygon(
            VertexHelper vertexHelper,
            Vector2 center,
            Vector2[] points,
            int pointCount,
            Color32 color)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, color);

            int perimeterStart = vertexHelper.currentVertCount;
            for (int i = 0; i < pointCount; i++)
            {
                AddVertex(vertexHelper, points[i], color);
            }

            for (int i = 0; i < pointCount; i++)
            {
                int next = (i + 1) % pointCount;
                vertexHelper.AddTriangle(centerIndex, perimeterStart + i, perimeterStart + next);
            }
        }

        public static bool AddFilledPolygonTriangulated(
            VertexHelper vertexHelper,
            Vector2[] points,
            int pointCount,
            Color32 color,
            int[] indexBuffer,
            int[] triangleBuffer)
        {
            if (pointCount < 3)
            {
                return false;
            }

            for (int i = 0; i < pointCount; i++)
            {
                indexBuffer[i] = i;
            }

            float signedArea = SignedArea(points, pointCount);
            if (Mathf.Abs(signedArea) <= Mathf.Epsilon)
            {
                return false;
            }

            bool clockwise = signedArea < 0f;
            int remaining = pointCount;
            int guard = pointCount * pointCount;
            int triangleIndexCount = 0;
            while (remaining > 2 && guard-- > 0)
            {
                bool earFound = false;
                for (int i = 0; i < remaining; i++)
                {
                    int previous = indexBuffer[(i + remaining - 1) % remaining];
                    int current = indexBuffer[i];
                    int next = indexBuffer[(i + 1) % remaining];
                    if (!IsConvex(points[previous], points[current], points[next], clockwise)
                        || ContainsPoint(
                            points,
                            indexBuffer,
                            remaining,
                            previous,
                            current,
                            next))
                    {
                        continue;
                    }

                    triangleBuffer[triangleIndexCount++] = previous;
                    triangleBuffer[triangleIndexCount++] = current;
                    triangleBuffer[triangleIndexCount++] = next;
                    RemoveIndex(indexBuffer, remaining, i);
                    remaining--;
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    return false;
                }
            }

            if (remaining > 2)
            {
                return false;
            }

            int vertexStart = vertexHelper.currentVertCount;
            for (int i = 0; i < pointCount; i++)
            {
                AddVertex(vertexHelper, points[i], color);
            }

            for (int i = 0; i < triangleIndexCount; i += 3)
            {
                vertexHelper.AddTriangle(
                    vertexStart + triangleBuffer[i],
                    vertexStart + triangleBuffer[i + 1],
                    vertexStart + triangleBuffer[i + 2]);
            }

            return true;
        }

        public static void AddRing(
            VertexHelper vertexHelper,
            Vector2[] outerPoints,
            Color32 outerColor,
            Vector2[] innerPoints,
            Color32 innerColor,
            int pointCount)
        {
            AddStrip(
                vertexHelper,
                outerPoints,
                outerColor,
                innerPoints,
                innerColor,
                pointCount,
                true);
        }

        public static void AddStrip(
            VertexHelper vertexHelper,
            Vector2[] outerPoints,
            Color32 outerColor,
            Vector2[] innerPoints,
            Color32 innerColor,
            int pointCount,
            bool closed)
        {
            int outerStart = vertexHelper.currentVertCount;
            for (int i = 0; i < pointCount; i++)
            {
                AddVertex(vertexHelper, outerPoints[i], outerColor);
            }

            int innerStart = vertexHelper.currentVertCount;
            for (int i = 0; i < pointCount; i++)
            {
                AddVertex(vertexHelper, innerPoints[i], innerColor);
            }

            int connectionCount = closed ? pointCount : pointCount - 1;
            for (int i = 0; i < connectionCount; i++)
            {
                int next = (i + 1) % pointCount;
                int outer = outerStart + i;
                int outerNext = outerStart + next;
                int inner = innerStart + i;
                int innerNext = innerStart + next;

                vertexHelper.AddTriangle(outer, outerNext, innerNext);
                vertexHelper.AddTriangle(outer, innerNext, inner);
            }
        }

        public static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color32 color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = WhiteTextureUv;
            vertexHelper.AddVert(vertex);
        }

        private static bool IsConvex(
            Vector2 previous,
            Vector2 current,
            Vector2 next,
            bool clockwise)
        {
            float cross = Cross(current - previous, next - current);
            return clockwise ? cross < -Mathf.Epsilon : cross > Mathf.Epsilon;
        }

        private static bool ContainsPoint(
            Vector2[] points,
            int[] indices,
            int indexCount,
            int first,
            int second,
            int third)
        {
            for (int i = 0; i < indexCount; i++)
            {
                int candidate = indices[i];
                if (candidate == first || candidate == second || candidate == third)
                {
                    continue;
                }

                if (IsStrictlyInsideTriangle(
                        points[candidate],
                        points[first],
                        points[second],
                        points[third]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStrictlyInsideTriangle(
            Vector2 point,
            Vector2 first,
            Vector2 second,
            Vector2 third)
        {
            float firstCross = Cross(second - first, point - first);
            float secondCross = Cross(third - second, point - second);
            float thirdCross = Cross(first - third, point - third);
            bool positive = firstCross > Mathf.Epsilon
                && secondCross > Mathf.Epsilon
                && thirdCross > Mathf.Epsilon;
            bool negative = firstCross < -Mathf.Epsilon
                && secondCross < -Mathf.Epsilon
                && thirdCross < -Mathf.Epsilon;
            return positive || negative;
        }

        private static void RemoveIndex(int[] indices, int count, int index)
        {
            for (int i = index; i < count - 1; i++)
            {
                indices[i] = indices[i + 1];
            }
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
    }
}
