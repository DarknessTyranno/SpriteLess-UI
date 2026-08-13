using SpriteLessUI.Geometry;
using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/SpriteLess/SpriteLess Image")]
    public sealed class SpriteLessImage : MaskableGraphic
    {
        [SerializeField] private ProceduralShape m_Shape = ProceduralShape.RoundedRectangle;
        [SerializeField] private SpriteLessRaycastMode m_RaycastMode = SpriteLessRaycastMode.RectTransform;
        [SerializeField] private bool m_UseIndividualCornerRadii;
        [SerializeField, Min(0f)] private float m_CornerRadius = 12f;
        [SerializeField] private CornerRadii m_CornerRadii = new CornerRadii(12f, 12f, 12f, 12f);

        [SerializeField] private CornerOffsets m_CornerOffsets;

        [SerializeField] private ShapeDirection m_Direction = ShapeDirection.Up;
        [SerializeField] private TriangleType m_TriangleType = TriangleType.Isosceles;
        [SerializeField] private TriangleRightAngle m_TriangleRightAngle = TriangleRightAngle.BottomLeft;
        [SerializeField] private ChevronType m_ChevronType = ChevronType.Stretch;
        [SerializeField, Min(0f)] private float m_ChevronThickness = 8f;
        [SerializeField, Range(ChevronGeometry.MinimumSpread, 1f)] private float m_ChevronSpread = 1f;
        [SerializeField] private ArcCap m_ChevronCap = ArcCap.Round;
        [SerializeField] private StrokeJoin m_ChevronJoin = StrokeJoin.Round;

        [SerializeField, Min(0f)] private float m_ArcThickness = 8f;
        [SerializeField] private float m_ArcStartAngle;
        [SerializeField, Range(-360f, 360f)] private float m_ArcSweepAngle = 360f;
        [SerializeField] private ArcCap m_ArcCap = ArcCap.Round;

        [SerializeField] private bool m_BorderEnabled;
        [SerializeField, Min(0f)] private float m_BorderWidth = 1f;
        [SerializeField] private Color m_BorderColor = Color.black;

        [SerializeField] private bool m_EdgeEffectEnabled;
        [SerializeField, Min(0f)] private float m_EdgeEffectWidth = 4f;
        [SerializeField] private Vector2 m_EdgeEffectDirection = Vector2.down;
        [SerializeField] private Color m_EdgeEffectColor = new Color(0f, 0f, 0f, 0.2f);

        [SerializeField] private bool m_AntiAliasingEnabled = true;
        [SerializeField, Min(0f)] private float m_AntiAliasingWidth = 1f;

        private readonly ShapePointBuffer m_PointBuffer = new ShapePointBuffer();

        public ProceduralShape Shape
        {
            get => m_Shape;
            set
            {
                if (m_Shape == value)
                {
                    return;
                }

                m_Shape = value;
                SetVerticesDirty();
            }
        }

        public SpriteLessRaycastMode RaycastMode
        {
            get => m_RaycastMode;
            set => m_RaycastMode = value;
        }

        public bool UseIndividualCornerRadii
        {
            get => m_UseIndividualCornerRadii;
            set
            {
                if (m_UseIndividualCornerRadii == value)
                {
                    return;
                }

                m_UseIndividualCornerRadii = value;
                SetVerticesDirty();
            }
        }

        public float CornerRadius
        {
            get => m_CornerRadius;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(m_CornerRadius, value))
                {
                    return;
                }

                m_CornerRadius = value;
                SetVerticesDirty();
            }
        }

        public CornerRadii CornerRadii
        {
            get => m_CornerRadii;
            set
            {
                value = value.Clamped();
                if (m_CornerRadii.Equals(value))
                {
                    return;
                }

                m_CornerRadii = value;
                SetVerticesDirty();
            }
        }

        public CornerOffsets CornerOffsets
        {
            get => m_CornerOffsets;
            set
            {
                if (m_CornerOffsets.Equals(value))
                {
                    return;
                }

                m_CornerOffsets = value;
                SetVerticesDirty();
            }
        }

        public ShapeDirection Direction
        {
            get => m_Direction;
            set
            {
                if (m_Direction == value)
                {
                    return;
                }

                m_Direction = value;
                SetVerticesDirty();
            }
        }

        public TriangleType TriangleType
        {
            get => m_TriangleType;
            set
            {
                if (m_TriangleType == value)
                {
                    return;
                }

                m_TriangleType = value;
                SetVerticesDirty();
            }
        }

        public TriangleRightAngle TriangleRightAngle
        {
            get => m_TriangleRightAngle;
            set
            {
                if (m_TriangleRightAngle == value)
                {
                    return;
                }

                m_TriangleRightAngle = value;
                SetVerticesDirty();
            }
        }

        public ChevronType ChevronType
        {
            get => m_ChevronType;
            set
            {
                if (m_ChevronType == value)
                {
                    return;
                }

                m_ChevronType = value;
                SetVerticesDirty();
            }
        }

        public float ChevronThickness
        {
            get => m_ChevronThickness;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(m_ChevronThickness, value))
                {
                    return;
                }

                m_ChevronThickness = value;
                SetVerticesDirty();
            }
        }

        public float ChevronSpread
        {
            get => m_ChevronSpread;
            set
            {
                value = Mathf.Clamp(value, ChevronGeometry.MinimumSpread, 1f);
                if (Mathf.Approximately(m_ChevronSpread, value))
                {
                    return;
                }

                m_ChevronSpread = value;
                SetVerticesDirty();
            }
        }

        public ArcCap ChevronCap
        {
            get => m_ChevronCap;
            set
            {
                if (m_ChevronCap == value)
                {
                    return;
                }

                m_ChevronCap = value;
                SetVerticesDirty();
            }
        }

        public StrokeJoin ChevronJoin
        {
            get => m_ChevronJoin;
            set
            {
                if (m_ChevronJoin == value)
                {
                    return;
                }

                m_ChevronJoin = value;
                SetVerticesDirty();
            }
        }

        public float ArcThickness
        {
            get => m_ArcThickness;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(m_ArcThickness, value))
                {
                    return;
                }

                m_ArcThickness = value;
                SetVerticesDirty();
            }
        }

        public float ArcStartAngle
        {
            get => m_ArcStartAngle;
            set
            {
                if (Mathf.Approximately(m_ArcStartAngle, value))
                {
                    return;
                }

                m_ArcStartAngle = value;
                SetVerticesDirty();
            }
        }

        public float ArcSweepAngle
        {
            get => m_ArcSweepAngle;
            set
            {
                value = Mathf.Clamp(value, -360f, 360f);
                if (Mathf.Approximately(m_ArcSweepAngle, value))
                {
                    return;
                }

                m_ArcSweepAngle = value;
                SetVerticesDirty();
            }
        }

        public ArcCap ArcCap
        {
            get => m_ArcCap;
            set
            {
                if (m_ArcCap == value)
                {
                    return;
                }

                m_ArcCap = value;
                SetVerticesDirty();
            }
        }

        public bool BorderEnabled
        {
            get => m_BorderEnabled;
            set
            {
                if (m_BorderEnabled == value)
                {
                    return;
                }

                m_BorderEnabled = value;
                SetVerticesDirty();
            }
        }

        public float BorderWidth
        {
            get => m_BorderWidth;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(m_BorderWidth, value))
                {
                    return;
                }

                m_BorderWidth = value;
                SetVerticesDirty();
            }
        }

        public Color BorderColor
        {
            get => m_BorderColor;
            set
            {
                if (m_BorderColor == value)
                {
                    return;
                }

                m_BorderColor = value;
                SetVerticesDirty();
            }
        }

        public bool EdgeEffectEnabled
        {
            get => m_EdgeEffectEnabled;
            set
            {
                if (m_EdgeEffectEnabled == value)
                {
                    return;
                }

                m_EdgeEffectEnabled = value;
                SetVerticesDirty();
            }
        }

        public float EdgeEffectWidth
        {
            get => m_EdgeEffectWidth;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(m_EdgeEffectWidth, value))
                {
                    return;
                }

                m_EdgeEffectWidth = value;
                SetVerticesDirty();
            }
        }

        public Vector2 EdgeEffectDirection
        {
            get => m_EdgeEffectDirection;
            set
            {
                if (m_EdgeEffectDirection == value)
                {
                    return;
                }

                m_EdgeEffectDirection = value;
                SetVerticesDirty();
            }
        }

        public Color EdgeEffectColor
        {
            get => m_EdgeEffectColor;
            set
            {
                if (m_EdgeEffectColor == value)
                {
                    return;
                }

                m_EdgeEffectColor = value;
                SetVerticesDirty();
            }
        }

        public bool AntiAliasingEnabled
        {
            get => m_AntiAliasingEnabled;
            set
            {
                if (m_AntiAliasingEnabled == value)
                {
                    return;
                }

                m_AntiAliasingEnabled = value;
                SetVerticesDirty();
            }
        }

        public float AntiAliasingWidth
        {
            get => m_AntiAliasingWidth;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(m_AntiAliasingWidth, value))
                {
                    return;
                }

                m_AntiAliasingWidth = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            PopulateMesh(vertexHelper);
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera))
            {
                return false;
            }

            if (m_RaycastMode == SpriteLessRaycastMode.RectTransform)
            {
                return true;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint)
                && ContainsLocalPoint(localPoint);
        }

        internal bool ContainsLocalPoint(Vector2 localPoint)
        {
            Rect rect = GetPixelAdjustedRect();
            switch (m_Shape)
            {
                case ProceduralShape.RoundedRectangle:
                    return GeometryRaycast.ContainsRoundedRectangle(
                        localPoint,
                        rect,
                        GetActiveCornerRadii());

                case ProceduralShape.Circle:
                    return GeometryRaycast.ContainsEllipse(
                        localPoint,
                        rect.center,
                        rect.width * 0.5f,
                        rect.height * 0.5f);

                case ProceduralShape.Quadrilateral:
                    m_PointBuffer.EnsureCapacity(4);
                    QuadrilateralGeometry.BuildPerimeter(
                        rect,
                        m_CornerOffsets,
                        m_PointBuffer.First);
                    return GeometryRaycast.ContainsPolygon(
                        localPoint,
                        m_PointBuffer.First,
                        4);

                case ProceduralShape.Arc:
                    return ArcGeometry.ContainsPoint(
                        localPoint,
                        rect,
                        m_ArcThickness,
                        m_ArcStartAngle,
                        m_ArcSweepAngle,
                        m_ArcCap,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_PointBuffer);

                case ProceduralShape.Triangle:
                    m_PointBuffer.EnsureCapacity(TriangleGeometry.PointCount);
                    TriangleGeometry.BuildPerimeter(
                        rect,
                        m_TriangleType,
                        m_Direction,
                        m_TriangleRightAngle,
                        m_PointBuffer.First);
                    return GeometryRaycast.ContainsPolygon(
                        localPoint,
                        m_PointBuffer.First,
                        TriangleGeometry.PointCount);

                case ProceduralShape.Chevron:
                    return ChevronGeometry.ContainsPoint(
                        localPoint,
                        rect,
                        m_ChevronType,
                        m_Direction,
                        m_ChevronThickness,
                        m_ChevronSpread,
                        m_ChevronCap,
                        m_ChevronJoin,
                        m_PointBuffer);

                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        internal void PopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= Mathf.Epsilon || rect.height <= Mathf.Epsilon)
            {
                return;
            }

            Color32 fillColor = color;
            Color32 borderColor = m_BorderColor;

            switch (m_Shape)
            {
                case ProceduralShape.RoundedRectangle:
                    RoundedRectangleGeometry.Build(
                        vertexHelper,
                        rect,
                        GetActiveCornerRadii(),
                        fillColor,
                        m_BorderEnabled,
                        m_BorderWidth,
                        borderColor,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Circle:
                    CircleGeometry.Build(
                        vertexHelper,
                        rect,
                        fillColor,
                        m_BorderEnabled,
                        m_BorderWidth,
                        borderColor,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Quadrilateral:
                    QuadrilateralGeometry.Build(
                        vertexHelper,
                        rect,
                        m_CornerOffsets,
                        fillColor,
                        m_BorderEnabled,
                        m_BorderWidth,
                        borderColor,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Arc:
                    ArcGeometry.Build(
                        vertexHelper,
                        rect,
                        m_ArcThickness,
                        m_ArcStartAngle,
                        m_ArcSweepAngle,
                        m_ArcCap,
                        fillColor,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Triangle:
                    TriangleGeometry.Build(
                        vertexHelper,
                        rect,
                        m_TriangleType,
                        m_Direction,
                        m_TriangleRightAngle,
                        fillColor,
                        m_BorderEnabled,
                        m_BorderWidth,
                        borderColor,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Chevron:
                    ChevronGeometry.Build(
                        vertexHelper,
                        rect,
                        m_ChevronType,
                        m_Direction,
                        m_ChevronThickness,
                        m_ChevronSpread,
                        m_ChevronCap,
                        m_ChevronJoin,
                        fillColor,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_PointBuffer);
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            if (m_EdgeEffectEnabled)
            {
                PopulateEdgeEffect(vertexHelper, rect);
            }

            ApplyRectUv(vertexHelper, rect);
        }

        private static void ApplyRectUv(VertexHelper vertexHelper, Rect rect)
        {
            UIVertex vertex = default;
            for (int i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                vertex.uv0 = new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y));
                vertexHelper.SetUIVertex(vertex, i);
            }
        }

        private void PopulateEdgeEffect(VertexHelper vertexHelper, Rect rect)
        {
            Color32 effectColor = m_EdgeEffectColor;
            switch (m_Shape)
            {
                case ProceduralShape.RoundedRectangle:
                    EdgeEffectGeometry.BuildRoundedRectangle(
                        vertexHelper,
                        rect,
                        GetActiveCornerRadii(),
                        m_BorderEnabled,
                        m_BorderWidth,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_EdgeEffectWidth,
                        m_EdgeEffectDirection,
                        effectColor,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Circle:
                    EdgeEffectGeometry.BuildCircle(
                        vertexHelper,
                        rect,
                        m_BorderEnabled,
                        m_BorderWidth,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_EdgeEffectWidth,
                        m_EdgeEffectDirection,
                        effectColor,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Quadrilateral:
                    EdgeEffectGeometry.BuildQuadrilateral(
                        vertexHelper,
                        rect,
                        m_CornerOffsets,
                        m_BorderEnabled,
                        m_BorderWidth,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_EdgeEffectWidth,
                        m_EdgeEffectDirection,
                        effectColor,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Triangle:
                    EdgeEffectGeometry.BuildTriangle(
                        vertexHelper,
                        rect,
                        m_TriangleType,
                        m_Direction,
                        m_TriangleRightAngle,
                        m_BorderEnabled,
                        m_BorderWidth,
                        m_AntiAliasingEnabled,
                        m_AntiAliasingWidth,
                        m_EdgeEffectWidth,
                        m_EdgeEffectDirection,
                        effectColor,
                        m_PointBuffer);
                    break;

                case ProceduralShape.Arc:
                case ProceduralShape.Chevron:
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        private CornerRadii GetActiveCornerRadii()
        {
            return m_UseIndividualCornerRadii
                ? m_CornerRadii
                : CornerRadii.Uniform(m_CornerRadius);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_CornerRadius = Mathf.Max(0f, m_CornerRadius);
            m_CornerRadii = m_CornerRadii.Clamped();
            m_ArcThickness = Mathf.Max(0f, m_ArcThickness);
            m_ArcSweepAngle = Mathf.Clamp(m_ArcSweepAngle, -360f, 360f);
            m_ChevronThickness = Mathf.Max(0f, m_ChevronThickness);
            m_ChevronSpread = Mathf.Clamp(m_ChevronSpread, ChevronGeometry.MinimumSpread, 1f);
            m_BorderWidth = Mathf.Max(0f, m_BorderWidth);
            m_EdgeEffectWidth = Mathf.Max(0f, m_EdgeEffectWidth);
            m_AntiAliasingWidth = Mathf.Max(0f, m_AntiAliasingWidth);
            base.OnValidate();
        }
#endif
    }
}
