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
            UiKit.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(520, 590), Vector2.zero);

            var title = UiKit.CreateText(panel, "메뉴", 34, new Color(0.95f, 0.9f, 0.75f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(480, 56), new Vector2(0, -22));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);

            var body = UiKit.CreateText(panel,
                "<b>조작</b>\n" +
                "WASD  이동 · Space  점프\n" +
                "마우스  시야 (좌클릭 잠금 · ESC 해제)\n" +
                "Shift  달리기 (스태미나 소모)\n" +
                "E  대화 / 집·트레일러 출입 / 보내기\n" +
                "F  눈을 감고 거울 비추기\n" +
                "Tab  조작키 확인\n\n" +
                "<b>목표</b>\n" +
                $"주민 {GameConfig.RescueGoal} 구출 → 안개 구역 확장\n" +
                $"최종: 주민 {GameConfig.FinalRescueGoal} + 부품 {GameConfig.PartsGoal} → 탈출",
                21, new Color(0.9f, 0.9f, 0.88f), TextAnchor.UpperLeft);
            UiKit.SetRect(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(440, 330), new Vector2(0, -86));
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.supportRichText = true;

            // ---- 설정 (기획 조작표: ESC = 게임 설정 및 메뉴) ----
            var setLabel = UiKit.CreateText(panel, "<b>설정</b>", 21, new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleLeft, true);
            UiKit.SetRect(setLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(440, 28), new Vector2(0, 158));
            setLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            setLabel.alignment = TextAnchor.MiddleLeft;

            var volLabel = UiKit.CreateText(panel, "전체 음량", 19, new Color(0.8f, 0.8f, 0.82f), TextAnchor.MiddleLeft);
            UiKit.SetRect(volLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(130, 24), new Vector2(-150, 122));
            volLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            var volSlider = UiKit.CreateSlider(panel, 0f, 1f, PlayerPrefs.GetFloat("vol", 1f));
            UiKit.SetRect((RectTransform)volSlider.transform, new Vector2(0.5f, 0f), new Vector2(280, 26), new Vector2(70, 122));
            ((RectTransform)volSlider.transform).pivot = new Vector2(0.5f, 0f);
            volSlider.onValueChanged.AddListener(v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat("vol", v);
            });

            var sensLabel = UiKit.CreateText(panel, "마우스 감도", 19, new Color(0.8f, 0.8f, 0.82f), TextAnchor.MiddleLeft);
            UiKit.SetRect(sensLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(130, 24), new Vector2(-150, 86));
            sensLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            var sensSlider = UiKit.CreateSlider(panel, 0.4f, 2.0f, PlayerPrefs.GetFloat("sens", 1f));
            UiKit.SetRect((RectTransform)sensSlider.transform, new Vector2(0.5f, 0f), new Vector2(280, 26), new Vector2(70, 86));
            ((RectTransform)sensSlider.transform).pivot = new Vector2(0.5f, 0f);
            sensSlider.onValueChanged.AddListener(v =>
            {
                Player.ThirdPersonCameraRig.SensitivityScale = v;
                PlayerPrefs.SetFloat("sens", v);
            });

            var closeBtn = UiKit.CreateButton(panel, "계속하기", 22, new Color(0.24f, 0.42f, 0.78f), Color.white);
            UiKit.SetRect((RectTransform)closeBtn.transform, new Vector2(0.5f, 0f), new Vector2(220, 54), new Vector2(0, 22));
            ((RectTransform)closeBtn.transform).pivot = new Vector2(0.5f, 0f);
            closeBtn.onClick.AddListener(Toggle);

            _root.SetActive(false);
        }
    }
}
