using System;
using UnityEngine;

namespace SpriteLessUI.Geometry
{
    /// <summary>Circular corner fillets shared by regular and authored polygons.</summary>
    public static class PolygonRounding
    {
        public const int MaximumSegments = 16;

        public static int Segments(float radius, float detail = 1f) => radius <= 0f ? 0
            : Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(radius * Mathf.Max(1f, detail)) * 0.8f), 2, MaximumSegments);

        public static int Build(Vector2[] source, int count, float radius, Vector2[] destination, float detail = 1f)
        {
            int segments = Segments(radius, detail);
            if (destination.Length < count * (segments + 1)) throw new ArgumentException("Contour buffer is too small.");
            int written = 0;
            for (int i = 0; i < count; i++)
                written += WriteCorner(source[(i + count - 1) % count], source[i], source[(i + 1) % count],
                    radius, segments, destination, written);
            return written;
        }

        public static int WriteCorner(Vector2 previous, Vector2 current, Vector2 next, float radius,
            int segments, Vector2[] destination, int offset)
        {
            if (segments == 0) { destination[offset] = current; return 1; }
            Vector2 toPrevious = previous - current, toNext = next - current;
            float previousLength = toPrevious.magnitude, nextLength = toNext.magnitude;
            Vector2 a = toPrevious.normalized, b = toNext.normalized;
            float halfAngle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(a, b), -1f, 1f)) * 0.5f;
            float tangent = Mathf.Tan(halfAngle);
            if (radius <= 0f || previousLength < 0.000001f || nextLength < 0.000001f
                || halfAngle < 0.0001f || halfAngle > Mathf.PI * 0.5f - 0.0001f)
            {
                for (int j = 0; j <= segments; j++) destination[offset + j] = current;
                return segments + 1;
            }
            float cut = Mathf.Min(radius / tangent, Mathf.Min(previousLength, nextLength) * 0.499f);
            float effectiveRadius = cut * tangent;
            Vector2 center = current + (a + b).normalized * (effectiveRadius / Mathf.Sin(halfAngle));
            Vector2 start = current + a * cut - center;
            Vector2 end = current + b * cut - center;
            float angle = Mathf.Atan2(start.y, start.x);
            float sweep = Vector2.SignedAngle(start, end) * Mathf.Deg2Rad;
            for (int j = 0; j <= segments; j++)
            {
                float step = angle + sweep * j / segments;
                destination[offset + j] = center + new Vector2(Mathf.Cos(step), Mathf.Sin(step)) * effectiveRadius;
            }
            return segments + 1;
        }
    }
}
