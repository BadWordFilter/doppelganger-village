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

        // ---- 라운드 코너 9-슬라이스 스프라이트 (절차 생성) — 밋밋한 사각 패널 탈피 ----
        private static Sprite _rounded;
        public static Sprite RoundedSprite
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int s = 64, r = 18;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        // 모서리 원 4개 기준 signed distance → 안티앨리어싱 알파
                        float cx = Mathf.Clamp(x + 0.5f, r, s - r);
                        float cy = Mathf.Clamp(y + 0.5f, r, s - r);
                        float dist = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        float a = Mathf.Clamp01(r - dist + 0.5f);
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                _rounded = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(r + 4, r + 4, r + 4, r + 4));
                return _rounded;
            }
        }

        /// <summary>이미지에 라운드 코너 + 부드러운 그림자 적용.</summary>
        public static void Soften(Image img, bool shadow = true)
        {
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            if (shadow)
            {
                var sh = img.gameObject.GetComponent<Shadow>();
                if (sh == null) sh = img.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.35f);
                sh.effectDistance = new Vector2(0f, -3f);
            }
        }

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
            var img = go.GetComponent<Image>();
            img.color = color;
            Soften(img, shadow: color.a > 0.5f); // 반투명 오버레이엔 그림자 생략
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
            var img = go.GetComponent<Image>();
            img.color = bg;
            Soften(img);
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.08f); // 호버 시 살짝 밝게
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var txt = CreateText(go.transform, label, fontSize, textColor, TextAnchor.MiddleCenter, true);
            Stretch(txt.rectTransform);
            return btn;
        }

        public static InputField CreateInput(Transform parent, string placeholder, int fontSize)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.96f, 0.94f, 0.90f, 0.97f); // 따뜻한 종이 톤
            Soften(img);
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
            // 전체 화면 패널엔 라운드 코너·그림자가 어울리지 않는다 — 원복
            var img = rt.GetComponent<Image>();
            if (img != null && img.sprite == _rounded)
            {
                img.sprite = null;
                var sh = rt.GetComponent<Shadow>();
                if (sh != null) Object.Destroy(sh);
            }
        }

        public static void SetRect(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 anchoredPos)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }
    }
}
