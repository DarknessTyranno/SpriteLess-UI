using System;
using UnityEngine;

namespace SpriteLessUI
{
    [Serializable]
    public struct CornerRadii : IEquatable<CornerRadii>
    {
        [SerializeField, Min(0f)] private float m_TopLeft;
        [SerializeField, Min(0f)] private float m_TopRight;
        [SerializeField, Min(0f)] private float m_BottomRight;
        [SerializeField, Min(0f)] private float m_BottomLeft;

        public CornerRadii(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            m_TopLeft = Mathf.Max(0f, topLeft);
            m_TopRight = Mathf.Max(0f, topRight);
            m_BottomRight = Mathf.Max(0f, bottomRight);
            m_BottomLeft = Mathf.Max(0f, bottomLeft);
        }

        public float TopLeft
        {
            readonly get => m_TopLeft;
            set => m_TopLeft = Mathf.Max(0f, value);
        }

        public float TopRight
        {
            readonly get => m_TopRight;
            set => m_TopRight = Mathf.Max(0f, value);
        }

        public float BottomRight
        {
            readonly get => m_BottomRight;
            set => m_BottomRight = Mathf.Max(0f, value);
        }

        public float BottomLeft
        {
            readonly get => m_BottomLeft;
            set => m_BottomLeft = Mathf.Max(0f, value);
        }

        public readonly float Max => Mathf.Max(
            Mathf.Max(m_TopLeft, m_TopRight),
            Mathf.Max(m_BottomRight, m_BottomLeft));

        public static CornerRadii Uniform(float radius)
        {
            radius = Mathf.Max(0f, radius);
            return new CornerRadii(radius, radius, radius, radius);
        }

        internal readonly CornerRadii Clamped()
        {
            return new CornerRadii(m_TopLeft, m_TopRight, m_BottomRight, m_BottomLeft);
        }

        internal readonly CornerRadii Inset(float amount)
        {
            amount = Mathf.Max(0f, amount);
            return new CornerRadii(
                Mathf.Max(0f, m_TopLeft - amount),
                Mathf.Max(0f, m_TopRight - amount),
                Mathf.Max(0f, m_BottomRight - amount),
                Mathf.Max(0f, m_BottomLeft - amount));
        }

        internal readonly CornerRadii Normalized(float width, float height)
        {
            CornerRadii radii = Clamped();
            width = Mathf.Max(0f, width);
            height = Mathf.Max(0f, height);

            float scale = 1f;
            scale = LimitScale(scale, width, radii.m_TopLeft + radii.m_TopRight);
            scale = LimitScale(scale, width, radii.m_BottomLeft + radii.m_BottomRight);
            scale = LimitScale(scale, height, radii.m_TopLeft + radii.m_BottomLeft);
            scale = LimitScale(scale, height, radii.m_TopRight + radii.m_BottomRight);

            return radii.Scaled(scale);
        }

        internal readonly CornerRadii Scaled(float scale)
        {
            return new CornerRadii(
                m_TopLeft * scale,
                m_TopRight * scale,
                m_BottomRight * scale,
                m_BottomLeft * scale);
        }

        public readonly bool Equals(CornerRadii other)
        {
            return m_TopLeft.Equals(other.m_TopLeft)
                && m_TopRight.Equals(other.m_TopRight)
                && m_BottomRight.Equals(other.m_BottomRight)
                && m_BottomLeft.Equals(other.m_BottomLeft);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is CornerRadii other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(m_TopLeft, m_TopRight, m_BottomRight, m_BottomLeft);
        }

        private static float LimitScale(float current, float available, float requested)
        {
            if (requested <= 0f)
            {
                return current;
            }

            return Mathf.Min(current, available / requested);
        }
    }
}

