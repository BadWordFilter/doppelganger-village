using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>화면 상단 중앙의 이벤트 토스트 (드랍·판정 결과). 잠시 표시 후 사라진다.</summary>
    public class ToastUI : MonoBehaviour
    {
        private static ToastUI _instance;
        private Text _text;
        private CanvasGroup _group;
        private Coroutine _fade;

        private void Awake()
        {
            _instance = this;
        }

        public static void Show(string message)
        {
            if (_instance == null) return;
            _instance.ShowInternal(message);
        }

        private void ShowInternal(string message)
        {
            if (_text == null)
            {
                var canvas = UiKit.CreateCanvas("ToastCanvas", 15);
                var panel = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.55f), "Toast");
                UiKit.SetRect(panel, new Vector2(0.5f, 1f), new Vector2(760, 54), new Vector2(0, -90));
                _group = panel.gameObject.AddComponent<CanvasGroup>();
                _text = UiKit.CreateText(panel, "", 24, new Color(1f, 0.97f, 0.85f), TextAnchor.MiddleCenter, true);
                UiKit.Stretch(_text.rectTransform);
            }
            _text.text = message;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            _group.alpha = 1f;
            yield return new WaitForSeconds(2.2f);
            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                _group.alpha = 1f - t / 0.8f;
                yield return null;
            }
            _group.alpha = 0f;
        }
    }
}
