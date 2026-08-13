using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace SpriteLessUI.Editor
{
    [CustomEditor(typeof(SpriteLessImage)), CanEditMultipleObjects]
    public sealed class SpriteLessImageEditor : GraphicEditor
    {
        private SerializedProperty m_Shape;
        private SerializedProperty m_RaycastMode;
        private SerializedProperty m_UseIndividualCornerRadii;
        private SerializedProperty m_CornerRadius;
        private SerializedProperty m_CornerRadii;
        private SerializedProperty m_CornerOffsets;
        private SerializedProperty m_Direction;
        private SerializedProperty m_TriangleType;
        private SerializedProperty m_TriangleRightAngle;
        private SerializedProperty m_ChevronType;
        private SerializedProperty m_ChevronThickness;
        private SerializedProperty m_ChevronSpread;
        private SerializedProperty m_ChevronCap;
        private SerializedProperty m_ChevronJoin;
        private SerializedProperty m_ArcThickness;
        private SerializedProperty m_ArcStartAngle;
        private SerializedProperty m_ArcSweepAngle;
        private SerializedProperty m_ArcCap;
        private SerializedProperty m_BorderEnabled;
        private SerializedProperty m_BorderWidth;
        private SerializedProperty m_BorderColor;
        private SerializedProperty m_EdgeEffectEnabled;
        private SerializedProperty m_EdgeEffectWidth;
        private SerializedProperty m_EdgeEffectDirection;
        private SerializedProperty m_EdgeEffectColor;
        private SerializedProperty m_AntiAliasingEnabled;
        private SerializedProperty m_AntiAliasingWidth;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Shape = serializedObject.FindProperty("m_Shape");
            m_RaycastMode = serializedObject.FindProperty("m_RaycastMode");
            m_UseIndividualCornerRadii = serializedObject.FindProperty("m_UseIndividualCornerRadii");
            m_CornerRadius = serializedObject.FindProperty("m_CornerRadius");
            m_CornerRadii = serializedObject.FindProperty("m_CornerRadii");
            m_CornerOffsets = serializedObject.FindProperty("m_CornerOffsets");
            m_Direction = serializedObject.FindProperty("m_Direction");
            m_TriangleType = serializedObject.FindProperty("m_TriangleType");
            m_TriangleRightAngle = serializedObject.FindProperty("m_TriangleRightAngle");
            m_ChevronType = serializedObject.FindProperty("m_ChevronType");
            m_ChevronThickness = serializedObject.FindProperty("m_ChevronThickness");
            m_ChevronSpread = serializedObject.FindProperty("m_ChevronSpread");
            m_ChevronCap = serializedObject.FindProperty("m_ChevronCap");
            m_ChevronJoin = serializedObject.FindProperty("m_ChevronJoin");
            m_ArcThickness = serializedObject.FindProperty("m_ArcThickness");
            m_ArcStartAngle = serializedObject.FindProperty("m_ArcStartAngle");
            m_ArcSweepAngle = serializedObject.FindProperty("m_ArcSweepAngle");
            m_ArcCap = serializedObject.FindProperty("m_ArcCap");
            m_BorderEnabled = serializedObject.FindProperty("m_BorderEnabled");
            m_BorderWidth = serializedObject.FindProperty("m_BorderWidth");
            m_BorderColor = serializedObject.FindProperty("m_BorderColor");
            m_EdgeEffectEnabled = serializedObject.FindProperty("m_EdgeEffectEnabled");
            m_EdgeEffectWidth = serializedObject.FindProperty("m_EdgeEffectWidth");
            m_EdgeEffectDirection = serializedObject.FindProperty("m_EdgeEffectDirection");
            m_EdgeEffectColor = serializedObject.FindProperty("m_EdgeEffectColor");
            m_AntiAliasingEnabled = serializedObject.FindProperty("m_AntiAliasingEnabled");
            m_AntiAliasingWidth = serializedObject.FindProperty("m_AntiAliasingWidth");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Shape);
            EditorGUILayout.PropertyField(m_Color, new GUIContent("Fill Color"));
            EditorGUILayout.PropertyField(m_Material);

            bool hasSingleShape = !m_Shape.hasMultipleDifferentValues;
            ProceduralShape shape = hasSingleShape
                ? (ProceduralShape)m_Shape.enumValueIndex
                : default;
            if (hasSingleShape)
            {
                DrawShapeSettings(shape);
            }

            if (!hasSingleShape
                || (shape != ProceduralShape.Arc && shape != ProceduralShape.Chevron))
            {
                EditorGUILayout.Space();
                DrawBorderSettings();

                EditorGUILayout.Space();
                DrawEdgeEffectSettings();
            }

            EditorGUILayout.Space();
            DrawAntiAliasingSettings();

            EditorGUILayout.Space();
            RaycastControlsGUI();
            EditorGUILayout.PropertyField(
                m_RaycastMode,
                new GUIContent(
                    "Raycast Area",
                    "Rect Transform uses the full UI rectangle. Shape uses the generated geometry."));
            MaskableControlsGUI();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawShapeSettings(ProceduralShape shape)
        {
            switch (shape)
            {
                case ProceduralShape.RoundedRectangle:
                    DrawCornerSettings();
                    break;

                case ProceduralShape.Quadrilateral:
                    DrawQuadrilateralSettings();
                    break;

                case ProceduralShape.Arc:
                    DrawArcSettings();
                    break;

                case ProceduralShape.Triangle:
                    DrawTriangleSettings();
                    break;

                case ProceduralShape.Chevron:
                    DrawChevronSettings();
                    break;
            }
        }

        private void DrawTriangleSettings()
        {
            EditorGUILayout.LabelField("Triangle", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_TriangleType, new GUIContent("Type"));
            if (m_TriangleType.hasMultipleDifferentValues)
            {
                return;
            }

            TriangleType type = (TriangleType)m_TriangleType.enumValueIndex;
            if (type == TriangleType.Right)
            {
                EditorGUILayout.PropertyField(
                    m_TriangleRightAngle,
                    new GUIContent("Right Angle Corner"));
                return;
            }

            EditorGUILayout.PropertyField(m_Direction);
        }

        private void DrawChevronSettings()
        {
            EditorGUILayout.LabelField("Chevron", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ChevronType, new GUIContent("Type"));
            EditorGUILayout.PropertyField(m_Direction);
            EditorGUILayout.PropertyField(m_ChevronThickness, new GUIContent("Thickness"));
            EditorGUILayout.PropertyField(
                m_ChevronSpread,
                new GUIContent(
                    "Spread",
                    "Endpoint spread for Stretch. Overall scale for Equilateral."));
            EditorGUILayout.PropertyField(m_ChevronCap, new GUIContent("Cap"));
            EditorGUILayout.PropertyField(m_ChevronJoin, new GUIContent("Join"));
        }

        private void DrawCornerSettings()
        {
            EditorGUILayout.LabelField("Corners", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_UseIndividualCornerRadii, new GUIContent("Individual"));

            using (new EditorGUI.IndentLevelScope())
            {
                if (!m_UseIndividualCornerRadii.hasMultipleDifferentValues
                    && m_UseIndividualCornerRadii.boolValue)
                {
                    EditorGUILayout.PropertyField(m_CornerRadii, true);
                }
                else
                {
                    EditorGUILayout.PropertyField(m_CornerRadius, new GUIContent("Radius"));
                }
            }
        }

        private void DrawQuadrilateralSettings()
        {
            EditorGUILayout.LabelField("Quadrilateral", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                m_CornerOffsets,
                new GUIContent("Corner Offsets", "Pixel offset applied to each RectTransform corner."),
                true);
        }

        private void DrawArcSettings()
        {
            EditorGUILayout.LabelField("Arc", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ArcThickness, new GUIContent("Thickness"));
            EditorGUILayout.PropertyField(
                m_ArcStartAngle,
                new GUIContent("Start Angle", "0 is the top of the RectTransform."));
            EditorGUILayout.PropertyField(
                m_ArcSweepAngle,
                new GUIContent("Sweep Angle", "Positive values sweep clockwise. 360 creates a Ring."));

            if (Mathf.Abs(m_ArcSweepAngle.floatValue) < 360f - Mathf.Epsilon)
            {
                EditorGUILayout.PropertyField(m_ArcCap, new GUIContent("Cap"));
            }
        }

        private void DrawBorderSettings()
        {
            EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_BorderEnabled, new GUIContent("Enabled"));

            if (m_BorderEnabled.hasMultipleDifferentValues || !m_BorderEnabled.boolValue)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_BorderWidth, new GUIContent("Width"));
                EditorGUILayout.PropertyField(m_BorderColor, new GUIContent("Color"));
            }
        }

        private void DrawAntiAliasingSettings()
        {
            EditorGUILayout.LabelField("Anti Aliasing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AntiAliasingEnabled, new GUIContent("Enabled"));

            if (m_AntiAliasingEnabled.hasMultipleDifferentValues || !m_AntiAliasingEnabled.boolValue)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    m_AntiAliasingWidth,
                    new GUIContent("Width", "Inset feather width in UI units."));
            }
        }

        private void DrawEdgeEffectSettings()
        {
            EditorGUILayout.LabelField("Edge Effect", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_EdgeEffectEnabled, new GUIContent("Enabled"));

            if (m_EdgeEffectEnabled.hasMultipleDifferentValues || !m_EdgeEffectEnabled.boolValue)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_EdgeEffectWidth, new GUIContent("Width"));
                EditorGUILayout.PropertyField(
                    m_EdgeEffectDirection,
                    new GUIContent("Direction", "Direction of the shaded edge. (0, -1) shades the bottom."));
                EditorGUILayout.PropertyField(m_EdgeEffectColor, new GUIContent("Color"));
            }
        }
    }
}
