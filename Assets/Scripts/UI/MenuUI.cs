using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>ESC 시스템 메뉴 (조작표 준수) — 조작법 안내와 재시작 안내.</summary>
    public class MenuUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private GameObject _root;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (DialogueUI.IsOpen || SettlementUI.IsShowing || IntroNoteUI.IsShowing) return; // 다른 모달 우선
            Toggle();
        }

        private void Toggle()
        {
            if (_root == null) Build();
            IsOpen = !IsOpen;
            _root.SetActive(IsOpen);
            if (IsOpen) Cursor.lockState = CursorLockMode.None;
        }

        private void Build()
        {
            var canvas = UiKit.CreateCanvas("MenuCanvas", 35);
            var dim = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.7f), "Dim");
            UiKit.Stretch(dim);
            _root = dim.gameObject;

            var panel = UiKit.CreatePanel(dim, new Color(0.08f, 0.09f, 0.13f, 0.98f), "Panel");
            UiKit.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(520, 460), Vector2.zero);

            var title = UiKit.CreateText(panel, "메뉴", 34, new Color(0.95f, 0.9f, 0.75f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(480, 56), new Vector2(0, -22));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);

            var body = UiKit.CreateText(panel,
                "<b>조작</b>\n" +
                "WASD  이동\n" +
                "마우스  시야 (좌클릭 잠금 · ESC 해제)\n" +
                "Shift  달리기 (스태미나 소모)\n" +
                "E  대화 / 트레일러로 보내기\n" +
                "F  눈을 감고 거울 비추기\n" +
                "Tab  개인 과제·수집 현황\n\n" +
                "<b>목표</b>\n" +
                $"진짜 주민 {GameConfig.RescueGoal} + 수리 부품 {GameConfig.PartsGoal} → 탈출\n\n" +
                "다시 시작: 브라우저 새로고침",
                21, new Color(0.9f, 0.9f, 0.88f), TextAnchor.UpperLeft);
            UiKit.SetRect(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(440, 320), new Vector2(0, -86));
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.supportRichText = true;

            var closeBtn = UiKit.CreateButton(panel, "계속하기", 22, new Color(0.24f, 0.42f, 0.78f), Color.white);
            UiKit.SetRect((RectTransform)closeBtn.transform, new Vector2(0.5f, 0f), new Vector2(220, 54), new Vector2(0, 22));
            ((RectTransform)closeBtn.transform).pivot = new Vector2(0.5f, 0f);
            closeBtn.onClick.AddListener(Toggle);

            _root.SetActive(false);
        }
    }
}
