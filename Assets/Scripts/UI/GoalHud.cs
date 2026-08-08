using DoppelgangerVillage.Judgement;
using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>우상단 탈출 목표 게이지: 구출 주민 / 수리 부품.</summary>
    public class GoalHud : MonoBehaviour
    {
        private Text _text;

        private void Start()
        {
            var canvas = UiKit.CreateCanvas("GoalCanvas", 5);
            var panel = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.35f), "GoalPanel");
            UiKit.SetRect(panel, new Vector2(1f, 1f), new Vector2(360, 52), new Vector2(-20, -20));
            panel.pivot = new Vector2(1f, 1f);
            _text = UiKit.CreateText(panel, "", 22, new Color(0.95f, 0.92f, 0.8f), TextAnchor.MiddleCenter, true);
            UiKit.Stretch(_text.rectTransform);
            UpdateText(0, 0);

            var director = JudgementDirector.Instance != null ? JudgementDirector.Instance : GetComponent<JudgementDirector>();
            if (director != null) director.ProgressChanged += UpdateText;
        }

        private void UpdateText(int rescued, int parts)
        {
            if (_text != null)
                _text.text = $"구출 주민 {rescued}/{GameConfig.RescueGoal}   ·   수리 부품 {parts}/{GameConfig.PartsGoal}";
        }
    }
}
