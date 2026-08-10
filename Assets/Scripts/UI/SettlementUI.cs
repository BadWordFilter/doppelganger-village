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
            UiKit.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(640, 400), Vector2.zero);

            _title = UiKit.CreateText(panel, "", 42, new Color(0.95f, 0.9f, 0.75f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(600, 66), new Vector2(0, -28));
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);

            _body = UiKit.CreateText(panel, "", 28, new Color(0.9f, 0.9f, 0.88f), TextAnchor.UpperCenter);
            _body.lineSpacing = 1.35f;
            UiKit.SetRect(_body.rectTransform, new Vector2(0.5f, 1f), new Vector2(580, 210), new Vector2(0, -116));
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
            SfxDirector.Play("dudung"); // 기획: 일일 성과 브리핑 "두둥-" 효과음
            IsShowing = true;
            _root.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            DialogueUI.Instance?.Close();

            string header = s.outcome switch
            {
                1 => "탈출 성공!",
                2 => s.remaining == 0 ? "탈출 실패..." : "전원 감염... 게임 오버",
                _ => Village.PhaseDirector.IsNight ? "아침 정산" : "해질녘 정산",
            };
            _title.text = header;

            // 간결 정산 (플레이테스트: 글이 너무 많음 → 구출·잠입 차감·부품만)
            int goal = Village.FogBoundary.Expanded ? GameConfig.FinalRescueGoal : GameConfig.RescueGoal;
            string infiltLine = s.infiltrators > 0
                ? $"<color=#ff8f8f>잠입한 도플갱어  -{s.infiltrators}</color>\n"
                : "";
            string outcomeLine = s.outcome switch
            {
                1 => "<color=#a8e6a1>트레일러를 수리해 마을을 탈출했다!</color>\n\n",
                2 => s.remaining == 0
                    ? "<color=#ff8f8f>더 이상 구조할 주민이 없다...</color>\n\n"
                    : "<color=#ff8f8f>모든 생존자가 도플갱어에게 잠식당했다.</color>\n\n",
                _ => "",
            };

            _body.text =
                outcomeLine +
                $"<b>구출 주민  {s.finalRescued} / {goal}</b>\n" +
                infiltLine +
                $"수리 부품  {s.parts} / {GameConfig.PartsGoal}";

            // 종료 화면은 버튼 2개가 들어가도록 패널을 키운다
            ((RectTransform)_root.transform.GetChild(0)).sizeDelta = new Vector2(640, s.outcome == 0 ? 400 : 490);

            _continueBtn.gameObject.SetActive(s.outcome == 0);
            if (s.outcome != 0)
            {
                EnsureEndButtons();
                bool master = Photon.Pun.PhotonNetwork.IsMasterClient;
                _rematchBtn.gameObject.SetActive(master);
                _leaveBtn.gameObject.SetActive(true);
                _endNote.gameObject.SetActive(!master);
            }
        }

        private Button _rematchBtn, _leaveBtn;
        private Text _endNote;

        /// <summary>게임 종료 버튼: 같은 팀 리매치(방장) / 로비로 나가기 — 새로고침 없이 초기화.</summary>
        private void EnsureEndButtons()
        {
            if (_rematchBtn != null) return;
            var panel = (RectTransform)_root.transform.GetChild(0);

            _rematchBtn = UiKit.CreateButton(panel, "같은 팀과 다시 하기", 22, new Color(0.22f, 0.55f, 0.30f), Color.white);
            UiKit.SetRect((RectTransform)_rematchBtn.transform, new Vector2(0.5f, 0f), new Vector2(300, 56), new Vector2(0, 96));
            ((RectTransform)_rematchBtn.transform).pivot = new Vector2(0.5f, 0f);
            _rematchBtn.onClick.AddListener(() =>
            {
                var jd = GetComponent<JudgementDirector>();
                if (jd != null) jd.RequestRematch();
            });

            _leaveBtn = UiKit.CreateButton(panel, "로비로 나가기", 22, new Color(0.35f, 0.35f, 0.42f), Color.white);
            UiKit.SetRect((RectTransform)_leaveBtn.transform, new Vector2(0.5f, 0f), new Vector2(300, 56), new Vector2(0, 28));
            ((RectTransform)_leaveBtn.transform).pivot = new Vector2(0.5f, 0f);
            _leaveBtn.onClick.AddListener(Network.ConnectionManager.LeaveToLobby);

            _endNote = UiKit.CreateText(panel, "방장이 [같은 팀과 다시 하기]를 누르면 함께 새 게임이 시작됩니다", 17, new Color(0.65f, 0.65f, 0.7f));
            UiKit.SetRect(_endNote.rectTransform, new Vector2(0.5f, 0f), new Vector2(560, 28), new Vector2(0, 160));
            _endNote.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        private void Hide()
        {
            IsShowing = false;
            if (_root != null) _root.SetActive(false);
        }
    }
}
