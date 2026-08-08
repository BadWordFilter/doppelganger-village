using System;
using System.Collections.Generic;
using DoppelgangerVillage.Data;
using DoppelgangerVillage.Dialogue;
using DoppelgangerVillage.Village;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using StageDirectionActor = DoppelgangerVillage.Village.StageDirectionActor;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 동물 심문 대화 UI (런타임 생성). 질문 선택지 3개 제시 → 마스터 판정 결과 표시.
    /// 마리당 질문 3회 공유, 4번째 시도는 과잉 심문으로 돌변 공격.
    /// 이상 답변은 색으로 구분하지 않는다 — 내용으로 판단하게 하는 것이 기획 의도.
    /// 괄호로 시작하는 연출 지문만 지문 스타일(이탤릭·보라)로 표시.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        /// <summary>판정 선택: (대상, 거울인가) — 4단계 판정 시스템이 구독한다.</summary>
        public event Action<AnimalCitizen, bool> VerdictChosen;

        private GameObject _root;
        private Text _speciesLabel;
        private Text _countLabel;
        private Text _answerText;
        private Text _warnText;
        private readonly List<Button> _choiceButtons = new();
        private readonly List<Text> _choiceLabels = new();
        private Button _sendBtn, _mirrorBtn;

        private AnimalCitizen _current;
        private List<DialogueEntry> _choices;
        private bool _waiting;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            var d = DialogueDirector.Instance;
            if (d != null)
            {
                d.QuestionAnswered += OnQuestionAnswered;
                d.OverInterrogated += OnOverInterrogated;
            }
        }

        private void EnsureBuilt()
        {
            if (_root != null) return;
            var canvas = UiKit.CreateCanvas("DialogueCanvas", 20);

            var panel = UiKit.CreatePanel(canvas.transform, new Color(0.05f, 0.06f, 0.09f, 0.94f), "DialoguePanel");
            UiKit.SetRect(panel, new Vector2(0.5f, 0f), new Vector2(1500, 430), new Vector2(0, 20));
            panel.pivot = new Vector2(0.5f, 0f);
            _root = panel.gameObject;

            _speciesLabel = UiKit.CreateText(panel, "", 30, new Color(0.95f, 0.9f, 0.75f), TextAnchor.MiddleLeft, true);
            UiKit.SetRect(_speciesLabel.rectTransform, new Vector2(0f, 1f), new Vector2(400, 44), new Vector2(28, -16));
            _speciesLabel.rectTransform.pivot = new Vector2(0f, 1f);

            _countLabel = UiKit.CreateText(panel, "", 24, new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleRight, true);
            UiKit.SetRect(_countLabel.rectTransform, new Vector2(1f, 1f), new Vector2(300, 40), new Vector2(-28, -18));
            _countLabel.rectTransform.pivot = new Vector2(1f, 1f);

            _answerText = UiKit.CreateText(panel, "", 27, new Color(0.92f, 0.92f, 0.9f), TextAnchor.UpperLeft);
            UiKit.SetRect(_answerText.rectTransform, new Vector2(0f, 1f), new Vector2(1050, 120), new Vector2(28, -66));
            _answerText.rectTransform.pivot = new Vector2(0f, 1f);
            _answerText.supportRichText = true;

            _warnText = UiKit.CreateText(panel, "", 20, new Color(0.95f, 0.55f, 0.5f), TextAnchor.MiddleLeft);
            UiKit.SetRect(_warnText.rectTransform, new Vector2(0f, 1f), new Vector2(1050, 30), new Vector2(28, -190));
            _warnText.rectTransform.pivot = new Vector2(0f, 1f);

            for (int i = 0; i < GameConfig.ChoicesPerRound; i++)
            {
                var btn = UiKit.CreateButton(panel, "", 24, new Color(0.16f, 0.18f, 0.24f), new Color(0.92f, 0.92f, 0.9f));
                UiKit.SetRect((RectTransform)btn.transform, new Vector2(0f, 0f), new Vector2(1020, 54), new Vector2(28, 24 + (GameConfig.ChoicesPerRound - 1 - i) * 62));
                ((RectTransform)btn.transform).pivot = new Vector2(0f, 0f);
                var label = btn.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                var lrt = label.rectTransform;
                lrt.offsetMin = new Vector2(18, 0);
                int idx = i;
                btn.onClick.AddListener(() => OnChoiceClicked(idx));
                _choiceButtons.Add(btn);
                _choiceLabels.Add(label);
            }

            _sendBtn = UiKit.CreateButton(panel, "트레일러로 보내기", 22, new Color(0.22f, 0.55f, 0.30f), Color.white);
            UiKit.SetRect((RectTransform)_sendBtn.transform, new Vector2(1f, 0f), new Vector2(360, 64), new Vector2(-28, 110));
            ((RectTransform)_sendBtn.transform).pivot = new Vector2(1f, 0f);
            _sendBtn.onClick.AddListener(() => Verdict(false));

            _mirrorBtn = UiKit.CreateButton(panel, "눈을 감고 거울 비추기", 22, new Color(0.45f, 0.30f, 0.62f), Color.white);
            UiKit.SetRect((RectTransform)_mirrorBtn.transform, new Vector2(1f, 0f), new Vector2(360, 64), new Vector2(-28, 32));
            ((RectTransform)_mirrorBtn.transform).pivot = new Vector2(1f, 0f);
            _mirrorBtn.onClick.AddListener(() => Verdict(true));

            var closeBtn = UiKit.CreateButton(panel, "떠나기", 20, new Color(0.3f, 0.3f, 0.35f), Color.white);
            UiKit.SetRect((RectTransform)closeBtn.transform, new Vector2(1f, 0f), new Vector2(360, 44), new Vector2(-28, 190));
            ((RectTransform)closeBtn.transform).pivot = new Vector2(1f, 0f);
            closeBtn.onClick.AddListener(Close);

            _root.SetActive(false);
        }

        public void Open(AnimalCitizen citizen)
        {
            EnsureBuilt();
            _current = citizen;
            IsOpen = true;
            _root.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            _speciesLabel.text = $"{citizen.AnimalType} 주민";
            _answerText.text = Stylize("(당신을 물끄러미 바라본다)");
            _warnText.text = "";
            RefreshChoices();
        }

        public void Close()
        {
            IsOpen = false;
            _current = null;
            if (_root != null) _root.SetActive(false);
        }

        private void RefreshChoices()
        {
            UpdateCount();
            bool danger = _current.QuestionsAsked >= GameConfig.MaxQuestionsPerAnimal;
            _warnText.text = danger ? "더 캐물었다간... 눈치챌 것 같다." : "";
            _choices = DialogueDirector.Instance.PickChoices(_current);
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                bool has = i < _choices.Count;
                _choiceButtons[i].gameObject.SetActive(has);
                if (has)
                {
                    _choiceLabels[i].text = _choices[i].question;
                    _choiceButtons[i].interactable = true;
                }
            }
        }

        private void OnChoiceClicked(int idx)
        {
            if (_waiting || _current == null || idx >= _choices.Count) return;
            _waiting = true;
            foreach (var b in _choiceButtons) b.interactable = false;
            DialogueDirector.Instance.Ask(_current, _choices[idx]);
        }

        private void OnQuestionAnswered(int citizenId, DialogueEntry entry, bool abnormal, int newCount, int actorNumber)
        {
            // 연출 지문은 텍스트가 아니라 캐릭터 모션으로 — 근처의 모든 클라이언트에서 재생
            string shown = abnormal ? entry.doppelAnswer : entry.normalAnswer;
            if (DialogueEntry.IsStageDirection(shown))
            {
                var target = FindCitizen(citizenId);
                if (target != null) StageDirectionActor.Play(target, shown, abnormal);
            }

            if (_current == null || _current.CitizenId != citizenId) return;
            if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                UpdateCount(); // 다른 플레이어가 질문 소진 — 카운트만 갱신
                return;
            }
            _waiting = false;
            _answerText.text = Stylize(shown);
            RefreshChoices();
        }

        private void OnOverInterrogated(int citizenId, int actorNumber)
        {
            var citizen = _current != null && _current.CitizenId == citizenId ? _current : null;
            // 돌변 연출: 대상 동물을 검붉게 물들인다 (모든 클라이언트 공통)
            var target = citizen != null ? citizen : FindCitizen(citizenId);
            if (target != null)
            {
                foreach (var r in target.GetComponentsInChildren<MeshRenderer>())
                    r.material.SetColor("_BaseColor", new Color(0.45f, 0.08f, 0.08f));
                StageDirectionActor.DistortFace(target); // 눈이 커지고 검붉게 — 얼굴 붕괴
                target.IsResolved = true; // 돌변한 개체는 더 이상 대화 불가
            }

            if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber) return;
            _waiting = false;
            if (_current != null && _current.CitizenId == citizenId)
            {
                _answerText.text = Stylize("(온몸의 관절이 우두둑 꺾이며, 그것이 본색을 드러낸다)");
                _warnText.text = "과잉 심문! 도플갱어가 눈치채고 공격했다!";
            }
            var local = Player.PlayerController.Local;
            if (local != null) local.TakeDamage(GameConfig.OverInterrogationDamage);
            Invoke(nameof(Close), 1.6f);
        }

        private void Verdict(bool mirror)
        {
            if (_current == null) return;
            var target = _current;
            if (mirror) ScreenFX.MirrorFlash(); // 눈 감고 거울 비추기 연출
            VerdictChosen?.Invoke(target, mirror);
            // 판정 처리(구출/퇴치/도주·드랍)는 4단계 JudgementDirector가 구독해 수행
            if (VerdictChosen == null)
                Debug.Log($"[Dialogue] 판정 선택: {(mirror ? "거울" : "트레일러")} → {target.AnimalType} {target.CitizenId} (판정 시스템 미연결)");
            Close();
        }

        private void UpdateCount()
        {
            if (_current != null)
                _countLabel.text = $"질문 {_current.QuestionsAsked}/{GameConfig.MaxQuestionsPerAnimal}";
        }

        private static AnimalCitizen FindCitizen(int id)
        {
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsSortMode.None))
                if (c.CitizenId == id) return c;
            return null;
        }

        /// <summary>괄호로 시작하는 연출 지문은 이탤릭·보라 스타일로 구분 표시 (데이터 규칙).</summary>
        private static string Stylize(string answer)
        {
            if (DialogueEntry.IsStageDirection(answer))
                return $"<i><color=#b8a3e0>{answer}</color></i>";
            return answer;
        }
    }
}
