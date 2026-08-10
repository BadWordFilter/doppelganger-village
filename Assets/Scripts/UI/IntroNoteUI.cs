using UnityEngine;
using UnityEngine.UI;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 게임 시작 시 표시되는 튜토리얼 쪽지 (기획서 원문). 룸 입장 직후 1회 표시.
    /// </summary>
    public class IntroNoteUI : MonoBehaviour
    {
        public static bool IsShowing { get; private set; }
        private static IntroNoteUI _instance;

        private GameObject _root;

        private const string NoteText =
            "이 쪽지를 줍게 될 분이 제발 살아있기를 바랍니다.\n\n" +
            "우리 마을은 주민인지, 도플갱어인지 구별할 수 없는\n괴물들에게 완전히 점령당했습니다.\n\n" +
            "부디 도플갱어에게 먹히지 말고\n진짜 주민들을 찾아 이곳을 탈출하세요.\n\n" +
            "만약 도플갱어라는 확신이 들었다면,\n눈을 감고 거울을 비추십시오.";

        private void Awake()
        {
            _instance = this;
        }

        public static void Show()
        {
            if (_instance == null) return;
            _instance.ShowInternal();
        }

        private void ShowInternal()
        {
            if (_root == null) Build();
            _root.SetActive(true);
            _prologue.SetActive(true); // 도입 서사(기획: 트레일러 사고 고립)부터
            _paper.SetActive(false);
            IsShowing = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private GameObject _prologue;
        private GameObject _paper;

        private void Build()
        {
            var canvas = UiKit.CreateCanvas("IntroNoteCanvas", 40);
            var dim = UiKit.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.82f), "Dim");
            UiKit.Stretch(dim);
            _root = dim.gameObject;

            // ---- 도입 서사 (기획 스토리: 택배 트레일러 사고로 고립) ----
            _prologue = new GameObject("Prologue", typeof(RectTransform)).gameObject;
            var proRt = (RectTransform)_prologue.transform;
            proRt.SetParent(dim, false);
            UiKit.Stretch(proRt);
            var proText = UiKit.CreateText(proRt,
                "<i>택배 트레일러를 타고 마을들을 돌던 중이었다.\n\n" +
                "갑작스러운 사고 — 정신을 차리니\n트레일러는 부서졌고, 우리는 낯선 마을에 고립됐다.\n\n" +
                "마을은... 이상할 정도로 조용하다.</i>",
                26, new Color(0.88f, 0.86f, 0.80f), TextAnchor.MiddleCenter);
            UiKit.SetRect(proText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(900, 300), Vector2.zero);
            var proBtn = UiKit.CreateButton(proRt, "…정신을 차린다", 22, new Color(0.30f, 0.28f, 0.26f), new Color(0.92f, 0.9f, 0.85f));
            UiKit.SetRect((RectTransform)proBtn.transform, new Vector2(0.5f, 0.24f), new Vector2(260, 54), Vector2.zero);
            proBtn.onClick.AddListener(() =>
            {
                _prologue.SetActive(false);
                _paper.SetActive(true);
            });

            // 낡은 종이 느낌의 쪽지
            var paper = UiKit.CreatePanel(dim, new Color(0.91f, 0.86f, 0.72f, 0.98f), "Paper");
            UiKit.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(680, 470), Vector2.zero);
            paper.localEulerAngles = new Vector3(0, 0, 1.5f);
            _paper = paper.gameObject;

            var header = UiKit.CreateText(paper, "< 떨어진 메모장 >", 26, new Color(0.35f, 0.28f, 0.2f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(600, 44), new Vector2(0, -28));
            header.rectTransform.pivot = new Vector2(0.5f, 1f);

            var body = UiKit.CreateText(paper, $"<i>{NoteText}</i>", 22, new Color(0.25f, 0.2f, 0.16f), TextAnchor.UpperCenter);
            UiKit.SetRect(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(600, 310), new Vector2(0, -84));
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.supportRichText = true;

            var btn = UiKit.CreateButton(paper, "쪽지를 내려놓는다", 22, new Color(0.35f, 0.28f, 0.2f), new Color(0.93f, 0.9f, 0.8f));
            UiKit.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0f), new Vector2(280, 54), new Vector2(0, 26));
            ((RectTransform)btn.transform).pivot = new Vector2(0.5f, 0f);
            btn.onClick.AddListener(() =>
            {
                _root.SetActive(false);
                IsShowing = false;
                ToastUI.Show("동물 주민에게 다가가 E 키로 대화하세요. 좌클릭으로 시점을 잠글 수 있습니다.");
            });

            _root.SetActive(false);
        }
    }
}
