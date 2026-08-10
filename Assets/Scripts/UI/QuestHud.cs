using DoppelgangerVillage.Judgement;
using DoppelgangerVillage.Quest;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 좌상단 개인 과제 체크리스트 (기획 6절: To-do list 형태로 항상 표시, 달성 시 완료 표시).
    /// Tab을 누르는 동안 상세 정보(수집 현황·조작법)가 펼쳐진다 (조작표: Tab = 정보 확인).
    /// </summary>
    public class QuestHud : MonoBehaviour
    {
        private Text _questText;
        private Text _detailText;
        private GameObject _detailPanel;
        private GameObject _root;
        private int _rescued;
        private int _parts;

        private void Start()
        {
            var canvas = UiKit.CreateCanvas("QuestCanvas", 5);

            var panel = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.35f), "QuestPanel");
            UiKit.SetRect(panel, new Vector2(0f, 1f), new Vector2(380, 74), new Vector2(20, -58));
            panel.pivot = new Vector2(0f, 1f);
            _root = panel.gameObject;

            _questText = UiKit.CreateText(panel, "", 19, new Color(0.92f, 0.92f, 0.85f), TextAnchor.UpperLeft, true);
            UiKit.SetRect(_questText.rectTransform, new Vector2(0f, 1f), new Vector2(360, 66), new Vector2(12, -8));
            _questText.rectTransform.pivot = new Vector2(0f, 1f);
            _questText.supportRichText = true;

            var detail = UiKit.CreatePanel(canvas.transform, new Color(0.04f, 0.05f, 0.08f, 0.92f), "DetailPanel");
            UiKit.SetRect(detail, new Vector2(0f, 1f), new Vector2(420, 176), new Vector2(20, -140));
            detail.pivot = new Vector2(0f, 1f);
            _detailPanel = detail.gameObject;
            _detailText = UiKit.CreateText(detail, "", 19, new Color(0.9f, 0.9f, 0.88f), TextAnchor.UpperLeft);
            UiKit.SetRect(_detailText.rectTransform, new Vector2(0f, 1f), new Vector2(392, 174), new Vector2(14, -10));
            _detailText.rectTransform.pivot = new Vector2(0f, 1f);
            _detailText.supportRichText = true;
            _detailPanel.SetActive(false);
            _root.SetActive(false);

            if (QuestDirector.Instance != null) QuestDirector.Instance.QuestsChanged += Refresh;
            var jd = JudgementDirector.Instance != null ? JudgementDirector.Instance : GetComponent<JudgementDirector>();
            if (jd != null) jd.ProgressChanged += (rescued, parts) => { _rescued = rescued; _parts = parts; };
        }

        private void Update()
        {
            bool show = PhotonNetwork.InRoom && Player.PlayerController.Local != null;
            if (_root != null && _root.activeSelf != show) { _root.SetActive(show); Refresh(); }
            if (!show) return;

            var kb = Keyboard.current;
            bool tab = kb != null && kb.tabKey.isPressed;
            if (_detailPanel != null && _detailPanel.activeSelf != tab)
            {
                _detailPanel.SetActive(tab);
                if (tab) RefreshDetail();
            }
        }

        private void Refresh()
        {
            if (_questText == null) return;
            var q = QuestDirector.Instance != null ? QuestDirector.Instance.LocalQuest : null;
            if (q == null)
            {
                _questText.text = "<b>개인 과제</b>  (배정 중...)";
                return;
            }
            string check1 = q.Done ? "<color=#8fd18f>✓</color>" : "□";
            string line1 = q.Done
                ? $"{check1} <color=#8a8a8a>{q.species} 시민 {q.targetCount}마리 구출</color>"
                : $"{check1} {q.species} 시민 {q.targetCount}마리 구출 ({q.progress}/{q.targetCount})";
            string check2 = q.Done ? "<color=#8fd18f>✓</color>" : "□";
            string line2 = q.Done
                ? $"{check2} <color=#8a8a8a>전담 부품 '{q.partName}' 확보</color>"
                : $"{check2} 전담 부품 '{q.partName}' — 전담 시민이 준다";
            _questText.text = $"<b>개인 과제</b>  <color=#777777>[Tab 상세]</color>\n{line1}\n{line2}";
        }

        private void RefreshDetail()
        {
            if (_detailText == null) return;
            // 조작키만 표시 (수집 현황은 우상단 목표 HUD가 담당)
            _detailText.text =
                "<b>조작</b>\n" +
                "WASD 이동 · Space 점프\n" +
                "Shift 달리기 · 좌클릭 시점 잠금\n" +
                "E 대화 / 집·트레일러 출입 / 보내기\n" +
                "F 눈을 감고 거울 비추기\n" +
                "ESC 메뉴 · 커서 해제";
        }
    }
}
