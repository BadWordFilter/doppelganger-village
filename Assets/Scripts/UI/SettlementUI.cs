using DoppelgangerVillage.Judgement;
using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 해질녘 정산 / 승리 / 패배 화면. 잠입한 도플갱어가 공개되고 구출 주민 수가 차감된다.
    /// </summary>
    public class SettlementUI : MonoBehaviour
    {
        public static bool IsShowing { get; private set; }

        private GameObject _root;
        private Text _title;
        private Text _body;
        private Button _continueBtn;

        private void Start()
        {
            var director = GetComponent<JudgementDirector>();
            if (director != null) director.SettlementShown += Show;
        }

        private void EnsureBuilt()
        {
            if (_root != null) return;
            var canvas = UiKit.CreateCanvas("SettlementCanvas", 30);
            var dim = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.75f), "Dim");
            UiKit.Stretch(dim);
            _root = dim.gameObject;

            var panel = UiKit.CreatePanel(dim, new Color(0.08f, 0.09f, 0.13f, 0.98f), "Panel");
            UiKit.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(720, 560), Vector2.zero);

            _title = UiKit.CreateText(panel, "", 44, new Color(0.95f, 0.9f, 0.75f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(680, 70), new Vector2(0, -30));
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);

            _body = UiKit.CreateText(panel, "", 26, new Color(0.9f, 0.9f, 0.88f), TextAnchor.UpperCenter);
            UiKit.SetRect(_body.rectTransform, new Vector2(0.5f, 1f), new Vector2(640, 360), new Vector2(0, -120));
            _body.rectTransform.pivot = new Vector2(0.5f, 1f);

            _continueBtn = UiKit.CreateButton(panel, "탐색 계속하기", 24, new Color(0.24f, 0.42f, 0.78f), Color.white);
            UiKit.SetRect((RectTransform)_continueBtn.transform, new Vector2(0.5f, 0f), new Vector2(300, 60), new Vector2(0, 30));
            ((RectTransform)_continueBtn.transform).pivot = new Vector2(0.5f, 0f);
            _continueBtn.onClick.AddListener(Hide);

            _root.SetActive(false);
        }

        public void Show(JudgementDirector.Settlement s)
        {
            EnsureBuilt();
            IsShowing = true;
            _root.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            DialogueUI.Instance?.Close();

            string header = s.outcome switch
            {
                1 => "탈출 성공!",
                2 => s.remaining == 0 ? "탈출 실패..." : "전원 감염... 게임 오버",
                _ => "해질녘 정산",
            };
            _title.text = header;

            string infiltLine = s.infiltrators > 0
                ? $"<color=#ff8f8f>잠입한 도플갱어  {s.infiltrators}마리  →  구출 주민 -{s.infiltrators}</color>"
                : "잠입한 도플갱어  없음";

            string outcomeLine = s.outcome switch
            {
                1 => "\n<color=#a8e6a1>트레일러를 수리해 저주받은 마을을 탈출했다!\n생존자들과 진짜 주민들이 무사히 떠났다.</color>",
                2 => s.remaining == 0
                    ? "\n<color=#ff8f8f>더 이상 구조할 주민이 없다...\n트레일러는 끝내 움직이지 못했다.</color>"
                    : "\n<color=#ff8f8f>모든 생존자가 도플갱어에게 잠식당했다.</color>",
                _ => "\n아직 탈출 조건이 부족하다. 남은 주민을 계속 가려내자.",
            };

            _body.text =
                $"트레일러로 보낸 주민  {s.trueRescued + s.infiltrators}마리\n" +
                $"{infiltLine}\n" +
                $"<b>최종 구출  {s.finalRescued} / {GameConfig.RescueGoal}</b>\n\n" +
                $"수리 부품  {s.parts} / {GameConfig.PartsGoal}\n" +
                $"퇴치한 도플갱어  {s.banished}마리   ·   도망친 주민  {s.fled}마리\n" +
                $"남은 판정 대상  {s.remaining}마리\n" +
                outcomeLine;

            _continueBtn.gameObject.SetActive(s.outcome == 0);
            if (s.outcome != 0)
            {
                var note = UiKit.CreateText((RectTransform)_root.transform.GetChild(0), "다시 플레이하려면 페이지를 새로고침하세요", 18, new Color(0.6f, 0.6f, 0.65f));
                UiKit.SetRect(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(500, 30), new Vector2(0, 40));
                note.rectTransform.pivot = new Vector2(0.5f, 0f);
            }
        }

        private void Hide()
        {
            IsShowing = false;
            if (_root != null) _root.SetActive(false);
        }
    }
}
