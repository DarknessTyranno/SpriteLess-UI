using System;
using System.Collections.Generic;
using SpriteLessUI.Geometry;
using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/SpriteLess/SpriteLess Line")]
    public sealed class SpriteLessLine : MaskableGraphic
    {
        public const int MaximumPoints = 256;

        [SerializeField] private Vector2[] m_Points = { new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f) };
        [SerializeField, Min(0)] private float m_Thickness = 8f;
        [SerializeField] private ArcCap m_Cap = ArcCap.Round;
        [SerializeField] private StrokeJoin m_Join = StrokeJoin.Round;
        [SerializeField] private bool m_AntiAliasingEnabled = true;
        [SerializeField, Min(0)] private float m_AntiAliasingWidth = 1f;
        [NonSerialized] private float m_MeshDetailScale = 1f;
        private readonly PolylineGeometry m_Geometry = new PolylineGeometry();

        /// <summary>Open path in normalized RectTransform coordinates. Use SetPoints to modify it.</summary>
        public IReadOnlyList<Vector2> Points => m_Points;
        public float Thickness { get => m_Thickness; set { m_Thickness = NonNegative(value); SetVerticesDirty(); } }
        public ArcCap Cap { get => m_Cap; set { m_Cap = value; SetVerticesDirty(); } }
        public StrokeJoin Join { get => m_Join; set { m_Join = value; SetVerticesDirty(); } }
        public bool AntiAliasingEnabled { get => m_AntiAliasingEnabled; set { m_AntiAliasingEnabled = value; SetVerticesDirty(); } }
        public float AntiAliasingWidth { get => m_AntiAliasingWidth; set { m_AntiAliasingWidth = NonNegative(value); SetVerticesDirty(); } }

        /// <summary>Non-serialized detail multiplier for temporary high-resolution captures.</summary>
        public float MeshDetailScale
        {
            get => m_MeshDetailScale;
            set { m_MeshDetailScale = IsFinite(value) ? Mathf.Clamp(value, 1f, 8f) : 1f; SetVerticesDirty(); }
        }

        public void SetPoints(Vector2[] points)
        {
            if (!ValidatePoints(points, out string error)) throw new ArgumentException(error, nameof(points));
            m_Points = (Vector2[])points.Clone();
            SetVerticesDirty();
        }

        public bool ValidateLine(out string error) => ValidatePoints(m_Points, out error);

        private static bool ValidatePoints(Vector2[] points, out string error)
        {
            error = null;
            if (points == null || points.Length < 2 || points.Length > MaximumPoints)
                error = "A line needs 2 to 256 points.";
            else
                for (int i = 0; i < points.Length; i++)
                    if (!IsFinite(points[i].x) || !IsFinite(points[i].y))
                    {
                        error = "Line coordinates must be finite numbers.";
                        break;
                    }
            return error == null;
        }

        protected override void OnPopulateMesh(VertexHelper vh) => PopulateMesh(vh);

        internal void PopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0 || !ValidateLine(out _)) return;
            m_Geometry.Build(vh, m_Points, rect, NonNegative(m_Thickness), m_Cap, m_Join, color,
                m_AntiAliasingEnabled ? NonNegative(m_AntiAliasingWidth) : 0f, m_MeshDetailScale);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float NonNegative(float value) => IsFinite(value) ? Mathf.Max(0f, value) : 0f;

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            raycastTarget = false;
        }

        protected override void OnValidate()
        {
            m_Thickness = NonNegative(m_Thickness);
            m_AntiAliasingWidth = NonNegative(m_AntiAliasingWidth);
            base.OnValidate();
        }
#endif
    }
}
