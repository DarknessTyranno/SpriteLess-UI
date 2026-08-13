using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpriteLessUI.Editor
{
    internal static class SpriteLessMenu
    {
        private const string MenuRoot = "GameObject/UI (Canvas)/SpriteLess/";

        [MenuItem(MenuRoot + "Rounded Rectangle", false, 2100)]
        private static void CreateRoundedRectangle(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Rounded Rectangle",
                ProceduralShape.RoundedRectangle,
                new Vector2(160f, 80f),
                12f,
                Color.white);
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Circle", false, 2101)]
        private static void CreateCircle(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Circle",
                ProceduralShape.Circle,
                new Vector2(100f, 100f),
                0f,
                Color.white);
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Parallelogram", false, 2102)]
        private static void CreateParallelogram(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Parallelogram",
                ProceduralShape.Quadrilateral,
                new Vector2(160f, 80f),
                0f,
                Color.white);
            graphic.GetComponent<SpriteLessImage>().CornerOffsets = new CornerOffsets(
                new Vector2(24f, 0f),
                Vector2.zero,
                new Vector2(-24f, 0f),
                Vector2.zero);
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Trapezoid", false, 2103)]
        private static void CreateTrapezoid(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Trapezoid",
                ProceduralShape.Quadrilateral,
                new Vector2(160f, 80f),
                0f,
                Color.white);
            graphic.GetComponent<SpriteLessImage>().CornerOffsets = new CornerOffsets(
                new Vector2(16f, 0f),
                new Vector2(-16f, 0f),
                Vector2.zero,
                Vector2.zero);
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Ring", false, 2104)]
        private static void CreateRing(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Ring",
                ProceduralShape.Arc,
                new Vector2(120f, 120f),
                0f,
                Color.white);
            SpriteLessImage image = graphic.GetComponent<SpriteLessImage>();
            image.ArcThickness = 12f;
            image.ArcSweepAngle = 360f;
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Arc", false, 2105)]
        private static void CreateArc(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Arc",
                ProceduralShape.Arc,
                new Vector2(120f, 120f),
                0f,
                Color.white);
            SpriteLessImage image = graphic.GetComponent<SpriteLessImage>();
            image.ArcThickness = 12f;
            image.ArcStartAngle = -135f;
            image.ArcSweepAngle = 270f;
            image.ArcCap = ArcCap.Round;
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Triangle", false, 2106)]
        private static void CreateTriangle(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Triangle",
                ProceduralShape.Triangle,
                new Vector2(100f, 80f),
                0f,
                Color.white);
            graphic.GetComponent<SpriteLessImage>().Direction = ShapeDirection.Up;
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Chevron", false, 2107)]
        private static void CreateChevron(MenuCommand command)
        {
            GameObject graphic = CreateGraphic(
                "Chevron",
                ProceduralShape.Chevron,
                new Vector2(48f, 48f),
                0f,
                Color.white);
            SpriteLessImage image = graphic.GetComponent<SpriteLessImage>();
            image.Direction = ShapeDirection.Right;
            image.ChevronThickness = 6f;
            image.ChevronSpread = 1f;
            image.ChevronCap = ArcCap.Round;
            image.ChevronJoin = StrokeJoin.Round;
            PlaceUnderCanvas(graphic, command);
        }

        [MenuItem(MenuRoot + "Panel", false, 2110)]
        private static void CreatePanel(MenuCommand command)
        {
            GameObject panel = CreateGraphic(
                "Panel",
                ProceduralShape.RoundedRectangle,
                new Vector2(320f, 180f),
                16f,
                new Color(0.16f, 0.18f, 0.22f, 1f));
            PlaceUnderCanvas(panel, command);
        }

        [MenuItem(MenuRoot + "Button", false, 2111)]
        private static void CreateButton(MenuCommand command)
        {
            GameObject buttonObject = CreateGraphic(
                "Button",
                ProceduralShape.RoundedRectangle,
                new Vector2(160f, 40f),
                8f,
                Color.white);
            SpriteLessImage graphic = buttonObject.GetComponent<SpriteLessImage>();
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = graphic;

            GameObject labelObject = ObjectFactory.CreateGameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(buttonObject.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.text = "Button";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>(GetBuiltInFontName());

            PlaceUnderCanvas(buttonObject, command);
        }

        [MenuItem(MenuRoot + "Slider", false, 2112)]
        private static void CreateSlider(MenuCommand command)
        {
            GameObject sliderObject = ObjectFactory.CreateGameObject("Slider", typeof(RectTransform), typeof(Slider));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(200f, 24f);

            SpriteLessImage background = CreateSliderGraphic(
                sliderObject.transform,
                "Background",
                ProceduralShape.RoundedRectangle,
                new Color(0.2f, 0.2f, 0.2f, 1f));
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 8f);
            backgroundRect.anchoredPosition = Vector2.zero;
            background.CornerRadius = 4f;

            RectTransform fillArea = CreateRectTransform("Fill Area", sliderObject.transform);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(5f, -4f);
            fillArea.offsetMax = new Vector2(-5f, 4f);

            SpriteLessImage fill = CreateSliderGraphic(
                fillArea,
                "Fill",
                ProceduralShape.RoundedRectangle,
                new Color(0.2f, 0.55f, 1f, 1f));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.CornerRadius = 4f;

            RectTransform handleArea = CreateRectTransform("Handle Slide Area", sliderObject.transform);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(9f, 0f);
            handleArea.offsetMax = new Vector2(-9f, 0f);

            SpriteLessImage handle = CreateSliderGraphic(
                handleArea,
                "Handle",
                ProceduralShape.Circle,
                Color.white);
            handle.rectTransform.sizeDelta = new Vector2(18f, 18f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;

            PlaceUnderCanvas(sliderObject, command);
        }

        private static GameObject CreateGraphic(
            string name,
            ProceduralShape shape,
            Vector2 size,
            float radius,
            Color color)
        {
            GameObject gameObject = ObjectFactory.CreateGameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(SpriteLessImage));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size;

            SpriteLessImage image = gameObject.GetComponent<SpriteLessImage>();
            image.Shape = shape;
            image.CornerRadius = radius;
            image.color = color;
            return gameObject;
        }

        private static SpriteLessImage CreateSliderGraphic(
            Transform parent,
            string name,
            ProceduralShape shape,
            Color color)
        {
            GameObject gameObject = CreateGraphic(name, shape, Vector2.zero, 0f, color);
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<SpriteLessImage>();
        }

        private static RectTransform CreateRectTransform(string name, Transform parent)
        {
            GameObject gameObject = ObjectFactory.CreateGameObject(name, typeof(RectTransform));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void PlaceUnderCanvas(GameObject element, MenuCommand command)
        {
            GameObject parent = command.context as GameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>(true) == null)
            {
                parent = GetOrCreateCanvas(parent);
            }

            GameObjectUtility.EnsureUniqueNameForSibling(element);
            Undo.SetTransformParent(element.transform, parent.transform, "Create " + element.name);
            RectTransform rectTransform = element.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            SetLayerRecursively(element, parent.layer);
            Undo.RegisterFullObjectHierarchyUndo(element, "Create " + element.name);
            Selection.activeGameObject = element;
        }

        private static GameObject GetOrCreateCanvas(GameObject requestedParent)
        {
            if (requestedParent == null)
            {
                Canvas existingCanvas = FindFirstObjectIncludingInactive<Canvas>();
                if (existingCanvas != null)
                {
                    return existingCanvas.gameObject;
                }
            }

            GameObject canvasObject = ObjectFactory.CreateGameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                canvasObject.layer = uiLayer;
            }

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            if (requestedParent != null)
            {
                Undo.SetTransformParent(canvasObject.transform, requestedParent.transform, "Create Canvas");
            }

            EnsureEventSystem();
            return canvasObject;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectIncludingInactive<EventSystem>() != null)
            {
                return;
            }

            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType == null)
            {
                inputModuleType = typeof(StandaloneInputModule);
            }

            GameObject eventSystem = ObjectFactory.CreateGameObject(
                "EventSystem",
                typeof(EventSystem),
                inputModuleType);
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static T FindFirstObjectIncludingInactive<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<T>(true);
#endif
        }

        private static string GetBuiltInFontName()
        {
#if UNITY_2022_2_OR_NEWER
            return "LegacyRuntime.ttf";
#else
            return "Arial.ttf";
#endif
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                SetLayerRecursively(gameObject.transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
