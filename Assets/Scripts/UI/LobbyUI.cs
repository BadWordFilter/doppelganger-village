using DoppelgangerVillage.Network;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using PunPlayer = Photon.Realtime.Player;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 룸 코드 로비 (런타임 생성 UI). 접속 → 방 만들기 / 4자리 코드 입장 → 플레이어 스폰.
    /// 1인으로도 방 만들기 즉시 플레이 가능 (심사위원 혼자 테스트).
    /// </summary>
    public class LobbyUI : MonoBehaviourPunCallbacks
    {
        private GameObject _panelRoot;
        private Text _status;
        private Text _roomBadge;
        private Button _createBtn;
        private Button _joinBtn;
        private InputField _codeInput;

        private void Start()
        {
            Build();
            var cm = ConnectionManager.Instance;
            cm.StatusChanged += OnStatusChanged;
            cm.RoomEntered += OnRoomEntered;
            cm.RoomJoinFailed += _ => SetInteractable(true);
            SetInteractable(false);
            cm.Connect();
        }

        private void Build()
        {
            var canvas = UiKit.CreateCanvas("LobbyCanvas", 10);

            _panelRoot = UiKit.CreatePanel(canvas.transform, new Color(0.06f, 0.07f, 0.10f, 0.97f), "LobbyPanel").gameObject;
            UiKit.Stretch((RectTransform)_panelRoot.transform);

            var title = UiKit.CreateText(_panelRoot.transform, "도플갱어 마을 탈출", 64, new Color(0.95f, 0.90f, 0.80f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(title.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(1000, 90), Vector2.zero);

            var subtitle = UiKit.CreateText(_panelRoot.transform, "대화로 도플갱어를 가려내고, 진짜 주민을 구출해 마을에서 탈출하세요", 24, new Color(0.68f, 0.68f, 0.75f));
            UiKit.SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(1100, 40), Vector2.zero);

            _createBtn = UiKit.CreateButton(_panelRoot.transform, "방 만들기", 30, new Color(0.80f, 0.28f, 0.24f), Color.white);
            UiKit.SetRect((RectTransform)_createBtn.transform, new Vector2(0.5f, 0.50f), new Vector2(320, 70), Vector2.zero);
            _createBtn.onClick.AddListener(() =>
            {
                SetInteractable(false);
                ConnectionManager.Instance.CreateRoom();
            });

            _codeInput = UiKit.CreateInput(_panelRoot.transform, "4자리 코드", 28);
            UiKit.SetRect((RectTransform)_codeInput.transform, new Vector2(0.5f, 0.38f), new Vector2(220, 64), new Vector2(-80, 0));

            _joinBtn = UiKit.CreateButton(_panelRoot.transform, "입장", 28, new Color(0.24f, 0.42f, 0.78f), Color.white);
            UiKit.SetRect((RectTransform)_joinBtn.transform, new Vector2(0.5f, 0.38f), new Vector2(130, 64), new Vector2(115, 0));
            _joinBtn.onClick.AddListener(() =>
            {
                string code = _codeInput.text?.Trim();
                if (string.IsNullOrEmpty(code) || code.Length != 4)
                {
                    OnStatusChanged("4자리 룸 코드를 입력하세요");
                    return;
                }
                SetInteractable(false);
                ConnectionManager.Instance.JoinRoomByCode(code);
            });

            _status = UiKit.CreateText(_panelRoot.transform, "", 22, new Color(0.90f, 0.85f, 0.60f));
            UiKit.SetRect(_status.rectTransform, new Vector2(0.5f, 0.27f), new Vector2(1000, 36), Vector2.zero);

            // 입장 후 좌상단 룸 코드 배지 (동료가 코드로 합류할 수 있게 항상 표시)
            _roomBadge = UiKit.CreateText(canvas.transform, "", 24, new Color(1f, 0.95f, 0.70f), TextAnchor.MiddleLeft, true);
            UiKit.SetRect(_roomBadge.rectTransform, new Vector2(0f, 1f), new Vector2(520, 40), new Vector2(20, -24));
            _roomBadge.rectTransform.pivot = new Vector2(0f, 1f);
            _roomBadge.gameObject.SetActive(false);
        }

        private void OnStatusChanged(string msg)
        {
            if (_status != null) _status.text = msg;
            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom) SetInteractable(true);
        }

        private void SetInteractable(bool on)
        {
            if (_createBtn != null) _createBtn.interactable = on;
            if (_joinBtn != null) _joinBtn.interactable = on;
            if (_codeInput != null) _codeInput.interactable = on;
        }

        private void OnRoomEntered(string code)
        {
            _panelRoot.SetActive(false);
            _roomBadge.gameObject.SetActive(true);
            UpdateBadge();
            GameManager.Instance.SpawnLocalPlayer();
            IntroNoteUI.Show(); // 튜토리얼 쪽지 (기획서 원문)
        }

        public override void OnPlayerEnteredRoom(PunPlayer newPlayer) => UpdateBadge();
        public override void OnPlayerLeftRoom(PunPlayer otherPlayer) => UpdateBadge();

        private void UpdateBadge()
        {
            if (!PhotonNetwork.InRoom || _roomBadge == null) return;
            _roomBadge.text = $"룸 코드 {PhotonNetwork.CurrentRoom.Name}  ({PhotonNetwork.CurrentRoom.PlayerCount}/{ConnectionManager.MaxPlayersPerRoom}명)";
        }
    }
}
