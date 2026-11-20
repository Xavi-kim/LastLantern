using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastLantern.EditorTools
{
    /// <summary>Помощники для сборки UI кодом в едином тёмно-синем / тёплом-жёлтом стиле.</summary>
    public static class UIBuilder
    {
        public static readonly Color Navy = new Color(0.05f, 0.07f, 0.15f, 0.95f);
        public static readonly Color NavyLight = new Color(0.12f, 0.16f, 0.3f, 1f);
        public static readonly Color Warm = new Color(1f, 0.8f, 0.35f, 1f);
        public static readonly Color Pale = new Color(0.78f, 0.83f, 0.95f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0.02f, 0.65f);

        public static Sprite WhiteSprite;

        public static Canvas Canvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Center(RectTransform rt, Vector2 position, Vector2 size)
        {
            var c = new Vector2(0.5f, 0.5f);
            return Place(rt, c, c, position, size);
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Image(Transform parent, string name, Color color)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Кнопка: тёмно-синяя, при наведении/выделении — тёплая.</summary>
        public static Button Button(Transform parent, string name, string label, Vector2 size, float fontSize = 36f)
        {
            var img = Image(parent, name, Color.white);
            img.rectTransform.sizeDelta = size;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = NavyLight;
            colors.highlightedColor = new Color(0.42f, 0.33f, 0.16f);
            colors.selectedColor = new Color(0.42f, 0.33f, 0.16f);
            colors.pressedColor = new Color(0.6f, 0.45f, 0.18f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.25f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(img.transform, "Label", label, fontSize, Warm);
                Stretch(t.rectTransform, 8f);
            }
            return button;
        }

        /// <summary>Панель с вертикальной раскладкой детей по центру.</summary>
        public static RectTransform VerticalBox(Transform parent, string name, Vector2 size, float spacing, Color background)
        {
            var img = Image(parent, name, background);
            Center(img.rectTransform, Vector2.zero, size);
            var layout = img.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return img.rectTransform;
        }

        /// <summary>Горизонтальная полоса с заливкой (здоровье, опыт).</summary>
        public static Image Bar(Transform parent, string name, Vector2 position, Vector2 size, Color fill)
        {
            var bg = Image(parent, name, new Color(0f, 0f, 0f, 0.55f));
            Place(bg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            var fillImg = Image(bg.transform, "Fill", fill);
            Stretch(fillImg.rectTransform, 3f);
            fillImg.type = UnityEngine.UI.Image.Type.Filled;
            fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;
            return fillImg;
        }

        public static Slider Slider(Transform parent, string name, Vector2 size, float value)
        {
            var root = Node(name, parent);
            root.sizeDelta = size;
            var slider = root.gameObject.AddComponent<Slider>();

            var bg = Image(root, "Background", new Color(0f, 0f, 0f, 0.5f));
            Stretch(bg.rectTransform);

            var fillArea = Node("Fill Area", root);
            Stretch(fillArea, 8f);
            var fill = Image(fillArea, "Fill", Warm);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Node("Handle Slide Area", root);
            Stretch(handleArea, 8f);
            var handle = Image(handleArea, "Handle", Color.white);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(24f, 16f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            return slider;
        }
    }
}
