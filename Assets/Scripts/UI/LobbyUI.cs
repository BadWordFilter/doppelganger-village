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
            // 씬 리로드 후 이미 접속돼 있으면 콜백이 다시 오지 않는다 — 버튼이 영영 잠기는 것 방지
            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
            {
                OnStatusChanged("서버 접속 완료");
                SetInteractable(true);
            }
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

            // 닉네임 입력 (선택)
            _nickInput = UiKit.CreateInput(_panelRoot.transform, "닉네임 (선택)", 24);
            _nickInput.characterLimit = 10;
            _nickInput.contentType = UnityEngine.UI.InputField.ContentType.Standard;
            UiKit.SetRect((RectTransform)_nickInput.transform, new Vector2(0.5f, 0.58f), new Vector2(280, 52), Vector2.zero);

            _createBtn = UiKit.CreateButton(_panelRoot.transform, "방 만들기", 30, new Color(0.80f, 0.28f, 0.24f), Color.white);
            UiKit.SetRect((RectTransform)_createBtn.transform, new Vector2(0.5f, 0.48f), new Vector2(320, 70), Vector2.zero);
            _createBtn.onClick.AddListener(() =>
            {
                ApplyNickname();
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
                ApplyNickname();
                SetInteractable(false);
                ConnectionManager.Instance.JoinRoomByCode(code);
            });

            _status = UiKit.CreateText(_panelRoot.transform, "", 22, new Color(0.90f, 0.85f, 0.60f));
            UiKit.SetRect(_status.rectTransform, new Vector2(0.5f, 0.27f), new Vector2(1000, 36), Vector2.zero);

            // ---- 캐릭터 색 커스터마이징 (선택은 플레이어 프로퍼티로 동기화) ----
            var colorLabel = UiKit.CreateText(_panelRoot.transform, "캐릭터 색", 22, new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(colorLabel.rectTransform, new Vector2(0.5f, 0.20f), new Vector2(300, 32), new Vector2(-180, 0));
            for (int i = 0; i < Player.PlayerController.ShirtPalette.Length; i++)
            {
                int idx = i;
                var sw = UiKit.CreateButton(_panelRoot.transform, "", 18, Player.PlayerController.ShirtPalette[i], Color.white);
                UiKit.SetRect((RectTransform)sw.transform, new Vector2(0.5f, 0.20f), new Vector2(44, 44), new Vector2(-60 + i * 54, 0));
                sw.onClick.AddListener(() =>
                {
                    PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "shirt", idx } });
                    OnStatusChanged($"캐릭터 색 선택 완료 ({idx + 1}번)");
                });
            }

            // ---- 열린 방 목록 ----
            var listLabel = UiKit.CreateText(_panelRoot.transform, "열린 방", 22, new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(listLabel.rectTransform, new Vector2(0.5f, 0.13f), new Vector2(300, 32), new Vector2(-320, 0));
            _roomListRoot = new GameObject("RoomList", typeof(RectTransform)).GetComponent<RectTransform>();
            _roomListRoot.SetParent(_panelRoot.transform, false);
            UiKit.SetRect(_roomListRoot, new Vector2(0.5f, 0.13f), new Vector2(700, 50), new Vector2(90, 0));
            ConnectionManager.Instance.RoomListChanged += RefreshRoomList;

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
            // 스폰·게임 시작은 대기실(WaitingRoomUI)에서 방장이 [게임 시작]을 눌렀을 때
        }

        public override void OnPlayerEnteredRoom(PunPlayer newPlayer) => UpdateBadge();
        public override void OnPlayerLeftRoom(PunPlayer otherPlayer) => UpdateBadge();

        private void UpdateBadge()
        {
            if (!PhotonNetwork.InRoom || _roomBadge == null) return;
            _roomBadge.text = $"룸 코드 {PhotonNetwork.CurrentRoom.Name}  ({PhotonNetwork.CurrentRoom.PlayerCount}/{ConnectionManager.MaxPlayersPerRoom}명)";
        }

        private RectTransform _roomListRoot;
        private InputField _nickInput;

        private void ApplyNickname()
        {
            string nick = _nickInput != null ? _nickInput.text?.Trim() : null;
            if (!string.IsNullOrEmpty(nick)) PhotonNetwork.NickName = nick;
        }

        /// <summary>열린 방 목록 버튼 갱신 (최대 4개).</summary>
        private void RefreshRoomList(System.Collections.Generic.List<Photon.Realtime.RoomInfo> rooms)
        {
            if (_roomListRoot == null || PhotonNetwork.InRoom) return;
            for (int i = _roomListRoot.childCount - 1; i >= 0; i--)
                Destroy(_roomListRoot.GetChild(i).gameObject);
            if (rooms.Count == 0)
            {
                var none = UiKit.CreateText(_roomListRoot, "지금은 열린 방이 없어요 — 방을 만들어보세요", 19, new Color(0.55f, 0.55f, 0.6f));
                UiKit.SetRect(none.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600, 30), Vector2.zero);
                return;
            }
            for (int i = 0; i < rooms.Count && i < 4; i++)
            {
                var info = rooms[i];
                var btn = UiKit.CreateButton(_roomListRoot, $"{info.Name}  ({info.PlayerCount}/{info.MaxPlayers})", 20, new Color(0.20f, 0.30f, 0.45f), Color.white);
                UiKit.SetRect((RectTransform)btn.transform, new Vector2(0f, 0.5f), new Vector2(160, 44), new Vector2(10 + i * 172, 0));
                ((RectTransform)btn.transform).pivot = new Vector2(0f, 0.5f);
                string code = info.Name;
                btn.onClick.AddListener(() =>
                {
                    SetInteractable(false);
                    ConnectionManager.Instance.JoinRoomByCode(code);
                });
            }
        }
    }
}
