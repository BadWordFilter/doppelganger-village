using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 전체 화면 연출 (거울 판정 컷: 암전 → 섬광). 최상위 캔버스에 풀스크린 이미지로 구현.
    /// </summary>
    public class ScreenFX : MonoBehaviour
    {
        private static ScreenFX _instance;
        private Image _overlay;

        private void Awake()
        {
            _instance = this;
        }

        /// <summary>거울 비추기: 눈을 감는 암전 → 섬광 → 서서히 밝아짐.</summary>
        public static void MirrorFlash()
        {
            if (_instance == null) return;
            _instance.StartCoroutine(_instance.MirrorRoutine());
        }

        private void EnsureBuilt()
        {
            if (_overlay != null) return;
            var canvas = UiKit.CreateCanvas("FxCanvas", 50);
            var panel = UiKit.CreatePanel(canvas.transform, Color.clear, "Overlay");
            UiKit.Stretch(panel);
            _overlay = panel.GetComponent<Image>();
            _overlay.raycastTarget = false;
        }

        private IEnumerator MirrorRoutine()
        {
            EnsureBuilt();
            // 눈을 감는다 (암전)
            yield return Fade(Color.clear, Color.black, 0.35f);
            yield return new WaitForSeconds(0.25f);
            // 거울 섬광
            _overlay.color = Color.white;
            yield return new WaitForSeconds(0.12f);
            // 서서히 현실로
            yield return Fade(Color.white, Color.clear, 0.6f);
        }

        private IEnumerator Fade(Color from, Color to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _overlay.color = Color.Lerp(from, to, t / duration);
                yield return null;
            }
            _overlay.color = to;
        }
    }
}
