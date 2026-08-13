using System;
using UnityEngine;

namespace SpriteLessUI
{
    [Serializable]
    public struct CornerOffsets : IEquatable<CornerOffsets>
    {
        [SerializeField] private Vector2 m_TopLeft;
        [SerializeField] private Vector2 m_TopRight;
        [SerializeField] private Vector2 m_BottomRight;
        [SerializeField] private Vector2 m_BottomLeft;

        public CornerOffsets(
            Vector2 topLeft,
            Vector2 topRight,
            Vector2 bottomRight,
            Vector2 bottomLeft)
        {
            m_TopLeft = topLeft;
            m_TopRight = topRight;
            m_BottomRight = bottomRight;
            m_BottomLeft = bottomLeft;
        }

        public Vector2 TopLeft
        {
            readonly get => m_TopLeft;
            set => m_TopLeft = value;
        }

        public Vector2 TopRight
        {
            readonly get => m_TopRight;
            set => m_TopRight = value;
        }

        public Vector2 BottomRight
        {
            readonly get => m_BottomRight;
            set => m_BottomRight = value;
        }

        public Vector2 BottomLeft
        {
            readonly get => m_BottomLeft;
            set => m_BottomLeft = value;
        }

        internal readonly CornerOffsets Scaled(float scale)
        {
            return new CornerOffsets(
                m_TopLeft * scale,
                m_TopRight * scale,
                m_BottomRight * scale,
                m_BottomLeft * scale);
        }

        public readonly bool Equals(CornerOffsets other)
        {
            return m_TopLeft.Equals(other.m_TopLeft)
                && m_TopRight.Equals(other.m_TopRight)
                && m_BottomRight.Equals(other.m_BottomRight)
                && m_BottomLeft.Equals(other.m_BottomLeft);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is CornerOffsets other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(m_TopLeft, m_TopRight, m_BottomRight, m_BottomLeft);
        }
    }
}
