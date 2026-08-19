using UnityEngine;

namespace SpriteLessUI.Geometry
{
    internal sealed class ShapePointBuffer
    {
        private Vector2[] m_First = new Vector2[0];
        private Vector2[] m_Second = new Vector2[0];
        private Vector2[] m_Third = new Vector2[0];
        private Vector2[] m_Fourth = new Vector2[0];
        private int[] m_Indices = new int[0];
        private int[] m_Triangles = new int[0];

        public Vector2[] First => m_First;
        public Vector2[] Second => m_Second;
        public Vector2[] Third => m_Third;
        public Vector2[] Fourth => m_Fourth;
        public int[] Indices => m_Indices;
        public int[] Triangles => m_Triangles;

        public void EnsureCapacity(int pointCount)
        {
            if (m_First.Length >= pointCount)
            {
                return;
            }

            m_First = new Vector2[pointCount];
            m_Second = new Vector2[pointCount];
            m_Third = new Vector2[pointCount];
            m_Fourth = new Vector2[pointCount];
            m_Indices = new int[pointCount];
            m_Triangles = new int[Mathf.Max(0, (pointCount - 2) * 3)];
        }
    }
}
