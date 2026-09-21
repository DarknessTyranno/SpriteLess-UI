using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace SpriteLessUI.Editor
{
    [CustomEditor(typeof(SpriteLessLine)), CanEditMultipleObjects]
    public sealed class SpriteLessLineEditor : GraphicEditor
    {
        private int m_Selected = -1;
        private EndpointExtrusion m_Extrusion;

        private sealed class EndpointExtrusion
        {
            public SpriteLessLine Line;
            public Vector2[] OriginalPoints;
            public Vector2 Position;
            public Vector2 MouseStart;
            public int PointIndex;
            public int ControlId;
            public int UndoGroup = -1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Color);
            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Thickness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Cap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Join"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Points"), new GUIContent("Normalized Points"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AntiAliasingEnabled"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AntiAliasingWidth"));
            RaycastControlsGUI();
            MaskableControlsGUI();
            serializedObject.ApplyModifiedProperties();
            var line = (SpriteLessLine)target;
            if (!line.ValidateLine(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.HelpBox("Drag points in Scene view; Shift-drag either endpoint to extend the line. Click a segment midpoint to insert a point. Coordinates are relative to the RectTransform (0–1).", MessageType.Info);
            using (new EditorGUI.DisabledScope(targets.Length != 1 || line.Points == null || line.Points.Count <= 2 || m_Selected < 0 || m_Selected >= line.Points.Count))
            {
                if (GUILayout.Button("Remove Selected Point"))
                {
                    var points = new List<Vector2>(line.Points);
                    points.RemoveAt(m_Selected);
                    ApplyPoints(line, points.ToArray(), "Remove line point");
                    m_Selected = -1;
                }
            }
        }

        private void OnSceneGUI()
        {
            var line = (SpriteLessLine)target;
            if (!line) return;
            Event current = Event.current;
            if (m_Extrusion != null && m_Extrusion.Line == line
                && ((current.rawType == EventType.MouseUp && current.button == 0)
                    || (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)))
            {
                FinishExtrusion(current.type == EventType.KeyDown);
                current.Use();
            }
            var rect = line.rectTransform;
            // Scene GUI is invoked for the current target, not the Inspector's targets array.
            if (!rect || rect.rect.width <= 0f || rect.rect.height <= 0f || !line.ValidateLine(out _)) return;
            // Keep the original handle count/order throughout the gesture. Inserting a point
            // must not change the active handle's control ID while the mouse is still down.
            bool extruding = m_Extrusion != null && m_Extrusion.Line == line;
            IReadOnlyList<Vector2> points = extruding ? m_Extrusion.OriginalPoints : line.Points;
            using (new Handles.DrawingScope(Color.white))
            {
                SpriteLessSceneHandles.DrawPath(rect, line.Points, false);
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 point = extruding && i == m_Extrusion.PointIndex ? m_Extrusion.Position : points[i];
                    Vector3 world = rect.TransformPoint(rect.rect.min + Vector2.Scale(point, rect.rect.size));
                    float size = HandleUtility.GetHandleSize(world) * 0.06f;
                    int control = GUIUtility.GetControlID(FocusType.Passive);
                    Handles.color = i == m_Selected ? Color.yellow : Color.white;
                    if (current.type == EventType.MouseDown && current.button == 0 && current.shift && !current.alt
                        && GUIUtility.hotControl == 0 && HandleUtility.nearestControl == control
                        && (i == 0 || i == points.Count - 1))
                    {
                        if (points.Count >= SpriteLessLine.MaximumPoints)
                        {
                            SceneView.currentDrawingSceneView.ShowNotification(new GUIContent("A line supports up to 256 points."));
                            current.Use();
                            return;
                        }
                        m_Extrusion = new EndpointExtrusion
                        {
                            Line = line, OriginalPoints = new List<Vector2>(points).ToArray(),
                            Position = points[i], MouseStart = current.mousePosition,
                            PointIndex = i, ControlId = control
                        };
                        extruding = true;
                    }
                    EditorGUI.BeginChangeCheck();
#if UNITY_2022_1_OR_NEWER
                    Vector3 moved = Handles.FreeMoveHandle(control, world, size, Vector3.zero, SpriteLessSceneHandles.PointCap);
#else
                    Vector3 moved = Handles.FreeMoveHandle(control, world, Quaternion.identity, size, Vector3.zero, SpriteLessSceneHandles.PointCap);
#endif
                    if (GUIUtility.hotControl == control) { m_Selected = i; Repaint(); }
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 local = rect.InverseTransformPoint(moved);
                        var normalized = new Vector2((local.x - rect.rect.xMin) / rect.rect.width, (local.y - rect.rect.yMin) / rect.rect.height);
                        if (extruding)
                            UpdateExtrusion(normalized, current.mousePosition);
                        else
                        {
                            var edited = new List<Vector2>(points);
                            edited[i] = normalized;
                            ApplyPoints(line, edited.ToArray(), "Move line point");
                            points = line.Points;
                        }
                        world = moved;
                        m_Selected = i;
                    }
                    if (i == points.Count - 1) continue;
                    Vector3 next = rect.TransformPoint(rect.rect.min + Vector2.Scale(points[i + 1], rect.rect.size));
                    Handles.color = Color.white;
                    // Still allocate midpoint controls while dragging to keep later IDs stable.
                    using (new EditorGUI.DisabledScope(extruding))
                    if (points.Count < SpriteLessLine.MaximumPoints && Handles.Button((world + next) * 0.5f,
                        Quaternion.identity, size * 0.55f, size * 0.55f, SpriteLessSceneHandles.InsertCap))
                    {
                        var edited = new List<Vector2>(points);
                        edited.Insert(i + 1, (points[i] + points[i + 1]) * 0.5f);
                        ApplyPoints(line, edited.ToArray(), "Insert line point");
                        m_Selected = i + 1;
                        break;
                    }
                }
                if (extruding && m_Extrusion.UndoGroup >= 0 && current.type == EventType.Repaint)
                {
                    Vector3 original = rect.TransformPoint(rect.rect.min + Vector2.Scale(points[m_Extrusion.PointIndex], rect.rect.size));
                    Handles.color = Color.white;
                    SpriteLessSceneHandles.PointCap(0, original, Quaternion.identity,
                        HandleUtility.GetHandleSize(original) * 0.06f, EventType.Repaint);
                }
                int selectedIndex = extruding && m_Extrusion.UndoGroup >= 0
                    ? (m_Extrusion.PointIndex == 0 ? 0 : line.Points.Count - 1)
                    : m_Selected;
                // Labels follow the live point list, not the frozen drag-handle order.
                SpriteLessSceneHandles.DrawPointLabels(rect, line.Points, selectedIndex);
            }
        }

        private void UpdateExtrusion(Vector2 position, Vector2 mousePosition)
        {
            if (m_Extrusion.UndoGroup < 0)
            {
                if ((mousePosition - m_Extrusion.MouseStart).sqrMagnitude < 9f
                    || position == m_Extrusion.Position) return;
                Undo.IncrementCurrentGroup();
                m_Extrusion.UndoGroup = Undo.GetCurrentGroup();
            }
            m_Extrusion.Position = position;
            var edited = new List<Vector2>(m_Extrusion.OriginalPoints);
            edited.Insert(m_Extrusion.PointIndex == 0 ? 0 : edited.Count, position);
            ApplyPoints(m_Extrusion.Line, edited.ToArray(), "Extrude line endpoint");
        }

        private void FinishExtrusion(bool cancel)
        {
            if (m_Extrusion == null) return;
            EndpointExtrusion extrusion = m_Extrusion;
            m_Extrusion = null;
            if (GUIUtility.hotControl == extrusion.ControlId)
            {
                GUIUtility.hotControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
            }
            cancel |= extrusion.Position == extrusion.OriginalPoints[extrusion.PointIndex];
            if (extrusion.UndoGroup >= 0)
            {
                Undo.FlushUndoRecordObjects();
                if (cancel)
                {
                    // Reverting Undo notifies selection listeners. Run outside OnSceneGUI,
                    // where Unity temporarily forbids those listeners from reading Editor.targets.
                    void RevertOnUpdate()
                    {
                        EditorApplication.update -= RevertOnUpdate;
                        Undo.RevertAllDownToGroup(extrusion.UndoGroup);
                    }
                    EditorApplication.update += RevertOnUpdate;
                }
                else Undo.CollapseUndoOperations(extrusion.UndoGroup);
                Undo.IncrementCurrentGroup();
            }
            m_Selected = !cancel && extrusion.UndoGroup >= 0 && extrusion.PointIndex != 0
                ? extrusion.OriginalPoints.Length : extrusion.PointIndex;
            Repaint();
            SceneView.RepaintAll();
        }

        protected override void OnDisable()
        {
            FinishExtrusion(false);
            base.OnDisable();
        }

        private static void ApplyPoints(SpriteLessLine line, Vector2[] points, string undoName)
        {
            Undo.RecordObject(line, undoName);
            line.SetPoints(points);
            if (PrefabUtility.IsPartOfPrefabInstance(line)) PrefabUtility.RecordPrefabInstancePropertyModifications(line);
            EditorUtility.SetDirty(line);
        }
    }
}
