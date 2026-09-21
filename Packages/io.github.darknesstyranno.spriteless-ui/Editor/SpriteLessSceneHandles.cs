using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpriteLessUI.Editor
{
    /// <summary>Shared Scene-view guides for editable SpriteLess paths.</summary>
    public static class SpriteLessSceneHandles
    {
        private static readonly Vector3[] Segment = new Vector3[2];
        private static readonly GUIContent LabelContent = new GUIContent();
        private static GUIStyle s_LabelStyle;

        public static void DrawPointLabels(RectTransform rect, IReadOnlyList<Vector2> points, int selectedIndex)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (s_LabelStyle == null)
                s_LabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset()
                };

            Color previousColor = GUI.color;
            Handles.BeginGUI();
            try
            {
                GUI.color = Color.white;
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 world = rect.TransformPoint(rect.rect.min + Vector2.Scale(points[i], rect.rect.size));
                    if (Camera.current && Camera.current.WorldToViewportPoint(world).z <= 0f) continue;
                    Vector2 screen = HandleUtility.WorldToGUIPoint(world);
                    // GUI-point offsets stay readable at any Scene zoom or display scaling.
                    var labelRect = new Rect(screen.x + 10f, screen.y - 22f, 40f, 18f);
                    LabelContent.text = i.ToString();
                    s_LabelStyle.normal.textColor = Color.black;
                    DrawLabel(labelRect, new Vector2(-1f, 0f));
                    DrawLabel(labelRect, new Vector2(1f, 0f));
                    DrawLabel(labelRect, new Vector2(0f, -1f));
                    DrawLabel(labelRect, new Vector2(0f, 1f));
                    s_LabelStyle.normal.textColor = i == selectedIndex ? Color.yellow : Color.white;
                    DrawLabel(labelRect, Vector2.zero);
                }
            }
            finally
            {
                GUI.color = previousColor;
                Handles.EndGUI();
            }
        }

        private static void DrawLabel(Rect rect, Vector2 offset)
        {
            rect.position += offset;
            // Draw only: labels must not allocate controls or interfere with point dragging.
            s_LabelStyle.Draw(rect, LabelContent, false, false, false, false);
        }

        public static void DrawPath(RectTransform rect, IReadOnlyList<Vector2> points, bool closed)
        {
            if (Event.current.type != EventType.Repaint) return;

            int segmentCount = closed ? points.Count : points.Count - 1;
            // Draw all guides before the handles so lines cannot cover their borders.
            using (new Handles.DrawingScope(Color.white))
            {
                for (int i = 0; i < segmentCount; i++)
                {
                    Segment[0] = rect.TransformPoint(rect.rect.min + Vector2.Scale(points[i], rect.rect.size));
                    Segment[1] = rect.TransformPoint(rect.rect.min + Vector2.Scale(points[(i + 1) % points.Count], rect.rect.size));
                    Handles.color = Color.black;
                    Handles.DrawAAPolyLine(4f, Segment);
                    Handles.color = Color.white;
                    Handles.DrawAAPolyLine(2f, Segment);
                }
            }
        }

        public static void PointCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType != EventType.Repaint)
            {
                // Register the original pick area exactly once; the border is visual only.
                Handles.DotHandleCap(controlId, position, rotation, size, eventType);
                return;
            }

            Color fill = Handles.color;
            using (new Handles.DrawingScope(Color.black))
            {
                Handles.DotHandleCap(controlId, position, rotation, size, eventType);
                Handles.color = fill;
                Handles.DotHandleCap(controlId, position, rotation, size * 0.72f, eventType);
            }
        }

        public static void InsertCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType != EventType.Repaint)
            {
                Handles.RectangleHandleCap(controlId, position, rotation, size, eventType);
                return;
            }

            PointCap(controlId, position, rotation, size, eventType);
        }
    }
}
