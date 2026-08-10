using DoppelgangerVillage.Judgement;
using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>우상단 탈출 목표 게이지: 구출 주민 / 수리 부품. 게임 시작(스폰) 후에만 표시.</summary>
    public class GoalHud : MonoBehaviour
    {
        private Text _text;
        private GameObject _panel;
        private int _rescued, _parts, _shownGoal = -1;

        private void Start()
        {
            var canvas = UiKit.CreateCanvas("GoalCanvas", 5);
            var panel = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.35f), "GoalPanel");
            UiKit.SetRect(panel, new Vector2(1f, 1f), new Vector2(360, 52), new Vector2(-20, -20));
            panel.pivot = new Vector2(1f, 1f);
            _panel = panel.gameObject;
            _text = UiKit.CreateText(panel, "", 22, new Color(0.95f, 0.92f, 0.8f), TextAnchor.MiddleCenter, true);
            UiKit.Stretch(_text.rectTransform);
            _panel.SetActive(false);

            var director = JudgementDirector.Instance != null ? JudgementDirector.Instance : GetComponent<JudgementDirector>();
            if (director != null) director.ProgressChanged += (r, p) => { _rescued = r; _parts = p; Refresh(); };
        }

        private void Update()
        {
            bool show = Photon.Pun.PhotonNetwork.InRoom && Player.PlayerController.Local != null;
            if (_panel != null && _panel.activeSelf != show) _panel.SetActive(show);
            // 구역 확장으로 목표가 바뀌면 갱신
            int goal = Village.FogBoundary.Expanded ? GameConfig.FinalRescueGoal : GameConfig.RescueGoal;
            if (goal != _shownGoal) Refresh();
        }

        private void Refresh()
        {
            _shownGoal = Village.FogBoundary.Expanded ? GameConfig.FinalRescueGoal : GameConfig.RescueGoal;
            if (_text != null)
                _text.text = $"구출 주민 {_rescued}/{_shownGoal}   ·   수리 부품 {_parts}/{GameConfig.PartsGoal}";
        }
    }
}
