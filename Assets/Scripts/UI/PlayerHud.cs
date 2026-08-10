using DoppelgangerVillage.Player;
using UnityEngine;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 좌하단 HP·스태미나 게이지 (런타임 생성). 로컬 플레이어 스폰 후 표시된다.
    /// </summary>
    public class PlayerHud : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _hpFill;
        private RectTransform _staminaFill;

        private void Start()
        {
            var canvas = UiKit.CreateCanvas("HudCanvas", 5);

            _root = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.35f), "StatusPanel").gameObject;
            var rootRt = (RectTransform)_root.transform;
            UiKit.SetRect(rootRt, new Vector2(0f, 0f), new Vector2(340, 96), new Vector2(20, 20));
            rootRt.pivot = new Vector2(0f, 0f);

            var hpLabel = UiKit.CreateText(_root.transform, "HP", 18, new Color(1f, 0.75f, 0.75f), TextAnchor.MiddleLeft, true);
            UiKit.SetRect(hpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(60, 24), new Vector2(12, -12));
            hpLabel.rectTransform.pivot = new Vector2(0f, 1f);

            var (hpBar, hpFill) = UiKit.CreateBar(_root.transform, new Color(0.15f, 0.1f, 0.1f, 0.9f), new Color(0.85f, 0.25f, 0.25f));
            UiKit.SetRect(hpBar, new Vector2(0f, 1f), new Vector2(250, 16), new Vector2(76, -16));
            hpBar.pivot = new Vector2(0f, 1f);
            _hpFill = hpFill;

            var stLabel = UiKit.CreateText(_root.transform, "기력", 18, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, true);
            UiKit.SetRect(stLabel.rectTransform, new Vector2(0f, 1f), new Vector2(60, 24), new Vector2(12, -52));
            stLabel.rectTransform.pivot = new Vector2(0f, 1f);

            var (stBar, stFill) = UiKit.CreateBar(_root.transform, new Color(0.1f, 0.12f, 0.15f, 0.9f), new Color(0.35f, 0.75f, 0.95f));
            UiKit.SetRect(stBar, new Vector2(0f, 1f), new Vector2(250, 16), new Vector2(76, -56));
            stBar.pivot = new Vector2(0f, 1f);
            _staminaFill = stFill;

            _root.SetActive(false);
        }

        private void Update()
        {
            var p = PlayerController.Local;
            if (_root == null) return;
            if (p == null || DialogueUI.IsOpen) // 대화 패널이 게이지를 덮으므로 대화 중엔 숨김
            {
                if (_root.activeSelf) _root.SetActive(false);
                return;
            }
            if (!_root.activeSelf) _root.SetActive(true);
            UiKit.SetBarValue(_hpFill, p.CurrentHp / GameConfig.MaxHp);
            UiKit.SetBarValue(_staminaFill, p.CurrentStamina / GameConfig.MaxStamina);
        }
    }
}
