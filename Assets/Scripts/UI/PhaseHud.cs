using DoppelgangerVillage.Village;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>상단 중앙 일차·페이즈·남은 시간 HUD ("1일차 · 낮 04:32").</summary>
    public class PhaseHud : MonoBehaviour
    {
        private Text _text;

        private void Start()
        {
            var canvas = UiKit.CreateCanvas("PhaseCanvas", 5);
            var panel = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.35f), "PhasePanel");
            UiKit.SetRect(panel, new Vector2(0.5f, 1f), new Vector2(300, 46), new Vector2(0, -20));
            panel.pivot = new Vector2(0.5f, 1f);
            _text = UiKit.CreateText(panel, "", 24, new Color(0.95f, 0.92f, 0.8f), TextAnchor.MiddleCenter, true);
            UiKit.Stretch(_text.rectTransform);
            panel.gameObject.SetActive(false);
            _panel = panel.gameObject;
        }

        private GameObject _panel;

        private void Update()
        {
            if (_text == null || PhaseDirector.Instance == null) return;
            bool show = PhotonNetwork.InRoom && Player.PlayerController.Local != null;
            if (_panel != null && _panel.activeSelf != show) _panel.SetActive(show);
            if (!show) return;

            float remain = PhaseDirector.Instance.RemainingSeconds;
            int m = Mathf.FloorToInt(remain / 60f);
            int s = Mathf.FloorToInt(remain % 60f);
            string phase = PhaseDirector.IsNight ? "밤" : "낮";
            _text.text = $"{PhaseDirector.DayNumber}일차 · {phase} {m:00}:{s:00}";
            _text.color = PhaseDirector.IsNight ? new Color(0.75f, 0.8f, 1f) : new Color(0.95f, 0.92f, 0.8f);
        }
    }
}
