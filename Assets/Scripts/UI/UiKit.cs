using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 런타임 uGUI 생성 헬퍼. 프리팹 대신 코드로 UI를 구축한다 (슬라이스 전략 — 전부 버전 관리됨).
    /// 한글은 Pretendard(OFL, Resources/Fonts)로 렌더링.
    /// </summary>
    public static class UiKit
    {
        private static Font _regular, _bold;
        public static Font FontRegular => _regular != null ? _regular : (_regular = Resources.Load<Font>("Fonts/Pretendard-Regular"));
        public static Font FontBold => _bold != null ? _bold : (_bold = Resources.Load<Font>("Fonts/Pretendard-Bold"));

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        public static RectTransform CreatePanel(Transform parent, Color color, string name = "Panel")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static Text CreateText(Transform parent, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, bool bold = false)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = bold ? FontBold : FontRegular;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button CreateButton(Transform parent, string label, int fontSize, Color bg, Color textColor)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;
            var txt = CreateText(go.transform, label, fontSize, textColor, TextAnchor.MiddleCenter, true);
            Stretch(txt.rectTransform);
            return btn;
        }

        public static InputField CreateInput(Transform parent, string placeholder, int fontSize)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
            var input = go.GetComponent<InputField>();
            var text = CreateText(go.transform, "", fontSize, new Color(0.1f, 0.1f, 0.12f), TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            var ph = CreateText(go.transform, placeholder, fontSize, new Color(0.45f, 0.45f, 0.5f, 0.8f), TextAnchor.MiddleCenter);
            Stretch(ph.rectTransform);
            input.textComponent = text;
            input.placeholder = ph;
            input.characterLimit = 4;
            input.contentType = InputField.ContentType.IntegerNumber;
            return input;
        }

        /// <summary>배경+채움 형태의 게이지 바. 채움은 anchorMax.x 조절 방식.</summary>
        public static (RectTransform root, RectTransform fill) CreateBar(Transform parent, Color bg, Color fillColor)
        {
            var root = CreatePanel(parent, bg, "Bar");
            var fill = CreatePanel(root, fillColor, "Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            return (root, fill);
        }

        public static void SetBarValue(RectTransform fill, float normalized)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void SetRect(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 anchoredPos)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }
    }
}
