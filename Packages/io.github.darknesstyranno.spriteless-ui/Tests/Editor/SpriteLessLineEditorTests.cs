using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpriteLessUI.Tests
{
    public sealed class SpriteLessLineEditorTests
    {
        [UnityTest]
        public IEnumerator SelectingLineRepaintsSceneWithoutEditorAccessWarnings() => CheckSelection(1);

        [UnityTest]
        public IEnumerator SelectingMultipleLinesRepaintsSceneWithoutEditorAccessWarnings() => CheckSelection(2);

        [UnityTest]
        public IEnumerator ShiftDragFirstEndpointPreservesOriginalAndUndoesOnce() => CheckDrag(0, true);

        [UnityTest]
        public IEnumerator ShiftDragLastEndpointPreservesOriginalAndUndoesOnce() => CheckDrag(2, true);

        [UnityTest]
        public IEnumerator ShiftClickDoesNotInsertPoint() => CheckDrag(2, true, steps: 0);

        [UnityTest]
        public IEnumerator ShiftDragBelowThresholdDoesNotInsertPoint() => CheckDrag(2, true, tiny: true);

        [UnityTest]
        public IEnumerator ShiftDragMiddlePointOnlyMovesIt() => CheckDrag(1, true);

        [UnityTest]
        public IEnumerator NormalEndpointDragOnlyMovesIt() => CheckDrag(2, false);

        [UnityTest]
        public IEnumerator EscapeCancelsEndpointExtrusion() => CheckDrag(2, true, cancel: true);

        [UnityTest]
        public IEnumerator MaximumPointCountPreventsEndpointExtrusion() => CheckDrag(255, true, atLimit: true);

        private static IEnumerator CheckDrag(int index, bool shift, int steps = 2, bool cancel = false,
            bool tiny = false, bool atLimit = false)
        {
            Object[] previousSelection = Selection.objects;
            Tool previousTool = Tools.current;
            SceneView view = SceneView.lastActiveSceneView;
            bool createdView = !view;
            if (createdView) view = EditorWindow.GetWindow<SceneView>();
            Vector3 previousPivot = view.pivot;
            Quaternion previousRotation = view.rotation;
            float previousSize = view.size;
            bool previousOrtho = view.orthographic;
            var go = new GameObject("Endpoint drag test", typeof(RectTransform), typeof(SpriteLessLine));
            var line = go.GetComponent<SpriteLessLine>();
            var original = new Vector2[atLimit ? SpriteLessLine.MaximumPoints : 3];
            original[0] = new Vector2(0.1f, 0.25f);
            for (int i = 1; i < original.Length - 1; i++) original[i] = new Vector2(0.5f, 0.75f);
            original[original.Length - 1] = new Vector2(0.9f, 0.25f);
            line.SetPoints(original);
            line.rectTransform.sizeDelta = new Vector2(200, 100);
            Vector2 mouse = Vector2.zero;
            bool projected = false;
            void Project(SceneView current)
            {
                if (current != view) return;
                Vector3 world = line.rectTransform.TransformPoint(line.rectTransform.rect.min
                    + Vector2.Scale(original[index], line.rectTransform.rect.size));
                mouse = GUIUtility.GUIToScreenPoint(HandleUtility.WorldToGUIPoint(world)) - view.position.position;
                projected = true;
            }
            SceneView.duringSceneGui += Project;
            try
            {
                // Test the point handles, not a Transform gizmo overlapping the middle point.
                Tools.current = Tool.None;
                view.orthographic = true;
                view.LookAtDirect(Vector3.zero, Quaternion.identity, 150f);
                Selection.activeGameObject = go;
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                for (int frame = 0; frame < 3; frame++)
                {
                    view.Repaint();
                    yield return null;
                    view.SendEvent(new Event { type = EventType.Layout });
                }
                Assert.That(projected, Is.True, "The Scene view must provide real handle coordinates.");
                SceneView.duringSceneGui -= Project;
                EventModifiers modifiers = shift ? EventModifiers.Shift : EventModifiers.None;
                view.SendEvent(new Event { type = EventType.MouseMove, mousePosition = mouse, modifiers = modifiers });
                view.SendEvent(new Event { type = EventType.Layout, mousePosition = mouse, modifiers = modifiers });
                view.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = mouse, modifiers = modifiers });
                Vector2 end = mouse;
                Vector2 delta = tiny ? new Vector2(0.25f, 0.25f) : new Vector2(24, -16);
                for (int step = 0; step < steps; step++)
                {
                    end += delta;
                    // Releasing Shift after grab must not switch the gesture back to moving the old point.
                    view.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = end,
                        delta = delta, modifiers = step == 0 ? modifiers : EventModifiers.None });
                    view.Repaint();
                    yield return null;
                }
                if (cancel)
                {
                    view.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape });
                    for (int frame = 0; frame < 60 && line.Points.Count != original.Length; frame++) yield return null;
                }
                view.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = end });
                Undo.FlushUndoRecordObjects();
                Assert.That(line.transform.localPosition, Is.EqualTo(Vector3.zero), "Point editing must not move the whole object.");

                if (steps == 0 || cancel || tiny || atLimit)
                    CollectionAssert.AreEqual(original, line.Points);
                else
                {
                    bool extended = shift && (index == 0 || index == original.Length - 1);
                    Assert.That(line.Points.Count, Is.EqualTo(original.Length + (extended ? 1 : 0)));
                    int movedIndex = extended && index != 0 ? original.Length : index;
                    Assert.That(Vector2.Distance(line.Points[movedIndex], original[index]), Is.GreaterThan(0.01f));
                    for (int i = 0; i < original.Length; i++)
                        if (extended || i != index)
                            Assert.That(line.Points[extended && index == 0 ? i + 1 : i], Is.EqualTo(original[i]));
                    if (extended)
                    {
                        var result = new List<Vector2>(line.Points);
                        Undo.PerformUndo();
                        CollectionAssert.AreEqual(original, line.Points, "One Undo must remove the entire extension.");
                        Undo.PerformRedo();
                        CollectionAssert.AreEqual(result, line.Points);
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                SceneView.duringSceneGui -= Project;
                view.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = mouse });
                Tools.current = previousTool;
                Selection.objects = previousSelection;
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                Undo.ClearUndo(line);
                Object.DestroyImmediate(go);
                if (atLimit) view.RemoveNotification();
                if (createdView) view.Close();
                else
                {
                    view.orthographic = previousOrtho;
                    view.LookAtDirect(previousPivot, previousRotation, previousSize);
                }
            }
        }

        private static IEnumerator CheckSelection(int count)
        {
            Object[] previousSelection = Selection.objects;
            var objects = new List<GameObject>();
            SceneView view = SceneView.lastActiveSceneView;
            bool createdView = !view;
            if (createdView) view = EditorWindow.GetWindow<SceneView>();
            var errors = new List<string>();
            int sceneEvents = 0;
            void Observe(string message, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert
                    || message.Contains("should not be used inside OnSceneGUI")) errors.Add(message);
            }
            void OnScene(SceneView current) { if (current == view) sceneEvents++; }
            Application.logMessageReceived += Observe;
            SceneView.duringSceneGui += OnScene;
            try
            {
                var selected = new Object[count];
                for (int i = 0; i < count; i++)
                {
                    var go = new GameObject("Line editor test", typeof(RectTransform), typeof(SpriteLessLine));
                    objects.Add(go);
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
                    selected[i] = go;
                }
                Selection.objects = selected;
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                bool lineEditorFound = false;
                foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
                    if (editor.target is SpriteLessLine)
                    {
                        Assert.That(editor.GetType().FullName, Is.EqualTo("SpriteLessUI.Editor.SpriteLessLineEditor"));
                        lineEditorFound = true;
                    }
                Assert.That(lineEditorFound, Is.True, "The custom Line inspector must be active for this regression.");
                for (int frame = 0; frame < 4; frame++)
                {
                    view.Repaint();
                    yield return null;
                    view.SendEvent(new Event { type = EventType.Layout });
                }
                Assert.That(sceneEvents, Is.GreaterThan(0), "Scene GUI must actually run, not only create a mesh.");
                Assert.That(errors, Is.Empty);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                SceneView.duringSceneGui -= OnScene;
                Application.logMessageReceived -= Observe;
                Selection.objects = previousSelection;
                foreach (var go in objects) if (go) Object.DestroyImmediate(go);
                if (createdView && view) view.Close();
            }
        }
    }
}
