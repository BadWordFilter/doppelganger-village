using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>화면 하단 중앙의 상호작용 힌트 ("E — 대화하기"). 정적 헬퍼로 어디서든 표시.</summary>
    public static class InteractionHint
    {
        private static Text _text;

        public static void Show(string message)
        {
            if (_text == null)
            {
                if (string.IsNullOrEmpty(message)) return;
                var canvas = UiKit.CreateCanvas("HintCanvas", 6);
                _text = UiKit.CreateText(canvas.transform, "", 26, new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleCenter, true);
                UiKit.SetRect(_text.rectTransform, new Vector2(0.5f, 0f), new Vector2(600, 40), new Vector2(0, 170));
            }
            _text.gameObject.SetActive(!string.IsNullOrEmpty(message));
            if (!string.IsNullOrEmpty(message)) _text.text = message;
        }
    }
}
