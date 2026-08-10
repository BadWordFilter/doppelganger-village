using DoppelgangerVillage.Network;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using PunPlayer = Photon.Realtime.Player;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 룸 대기실 — 방을 만들면 바로 시작하지 않고 친구를 기다린다.
    /// 룸 코드를 크게 표시하고, 방장이 [게임 시작]을 누르면 전원 동시 시작 (버퍼드 — 늦은 합류자는 즉시 게임 진입).
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class WaitingRoomUI : MonoBehaviourPunCallbacks
    {
        public static bool IsWaiting { get; private set; }

        private GameObject _root;
        private Text _codeText;
        private Text _playersText;
        private Button _startBtn;
        private Text _hintText;
        private bool _gameStarted;

        private void Start()
        {
            var cm = ConnectionManager.Instance;
            if (cm != null) cm.RoomEntered += OnRoomEntered;
        }

        private void OnRoomEntered(string code)
        {
            if (_gameStarted) return;
            if (_root == null) Build();
            IsWaiting = true;
            _root.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            RefreshInfo();
        }

        private void Build()
        {
            var canvas = UiKit.CreateCanvas("WaitingCanvas", 25);
            var dim = UiKit.CreatePanel(canvas.transform, new Color(0.05f, 0.06f, 0.10f, 0.96f), "Dim");
            UiKit.Stretch(dim);
            _root = dim.gameObject;

            var title = UiKit.CreateText(dim, "대기실", 40, new Color(0.95f, 0.9f, 0.75f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(title.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(600, 60), Vector2.zero);

            _codeText = UiKit.CreateText(dim, "", 76, new Color(1f, 0.95f, 0.6f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(_codeText.rectTransform, new Vector2(0.5f, 0.70f), new Vector2(700, 100), Vector2.zero);

            var codeHint = UiKit.CreateText(dim, "친구에게 이 룸 코드를 알려주세요", 22, new Color(0.7f, 0.7f, 0.75f));
            UiKit.SetRect(codeHint.rectTransform, new Vector2(0.5f, 0.61f), new Vector2(700, 34), Vector2.zero);

            _playersText = UiKit.CreateText(dim, "", 26, new Color(0.9f, 0.9f, 0.88f), TextAnchor.UpperCenter);
            UiKit.SetRect(_playersText.rectTransform, new Vector2(0.5f, 0.50f), new Vector2(600, 180), Vector2.zero);
            _playersText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _playersText.supportRichText = true;

            // 캐릭터 색 선택 (대기실에서 변경 — 전 참가자에게 실시간 반영)
            var colorLabel = UiKit.CreateText(dim, "내 캐릭터 색", 22, new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleCenter, true);
            UiKit.SetRect(colorLabel.rectTransform, new Vector2(0.5f, 0.30f), new Vector2(300, 32), new Vector2(-200, 0));
            for (int i = 0; i < Player.PlayerController.ShirtPalette.Length; i++)
            {
                int idx = i;
                var sw = UiKit.CreateButton(dim, "", 18, Player.PlayerController.ShirtPalette[i], Color.white);
                UiKit.SetRect((RectTransform)sw.transform, new Vector2(0.5f, 0.30f), new Vector2(46, 46), new Vector2(-60 + i * 56, 0));
                sw.onClick.AddListener(() =>
                    PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "shirt", idx } }));
            }

            _startBtn = UiKit.CreateButton(dim, "게임 시작", 28, new Color(0.80f, 0.28f, 0.24f), Color.white);
            UiKit.SetRect((RectTransform)_startBtn.transform, new Vector2(0.5f, 0.18f), new Vector2(300, 68), Vector2.zero);
            _startBtn.onClick.AddListener(() => photonView.RPC(nameof(RpcStartGame), RpcTarget.AllBuffered));

            _hintText = UiKit.CreateText(dim, "방장이 게임을 시작하길 기다리는 중...", 22, new Color(0.7f, 0.7f, 0.75f));
            UiKit.SetRect(_hintText.rectTransform, new Vector2(0.5f, 0.10f), new Vector2(700, 34), Vector2.zero);

            _root.SetActive(false);
        }

        private void RefreshInfo()
        {
            if (_root == null || !_root.activeSelf || !PhotonNetwork.InRoom) return;
            _codeText.text = PhotonNetwork.CurrentRoom.Name;
            string players = "";
            foreach (var p in PhotonNetwork.PlayerList)
            {
                int shirtIdx = p.CustomProperties.TryGetValue("shirt", out object v) && v is int i
                    ? Mathf.Clamp(i, 0, Player.PlayerController.ShirtPalette.Length - 1) : 0;
                string hex = ColorUtility.ToHtmlStringRGB(Player.PlayerController.ShirtPalette[shirtIdx]);
                players += $"<color=#{hex}>■</color> " + (p.IsMasterClient ? "👑 " : "") + p.NickName
                    + (p.IsLocal ? "  <color=#8fd18f>(나)</color>" : "") + "\n";
            }
            _playersText.text = $"참가자 {PhotonNetwork.CurrentRoom.PlayerCount}/{ConnectionManager.MaxPlayersPerRoom}\n\n{players}";
            bool isMaster = PhotonNetwork.IsMasterClient;
            _startBtn.gameObject.SetActive(isMaster);
            _hintText.gameObject.SetActive(!isMaster);
        }

        public override void OnPlayerEnteredRoom(PunPlayer newPlayer) => RefreshInfo();
        public override void OnPlayerLeftRoom(PunPlayer otherPlayer) => RefreshInfo();
        public override void OnMasterClientSwitched(PunPlayer newMasterClient) => RefreshInfo();
        public override void OnPlayerPropertiesUpdate(PunPlayer targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) => RefreshInfo();

        [PunRPC]
        private void RpcStartGame(PhotonMessageInfo info)
        {
            if (_gameStarted) return;
            _gameStarted = true;
            IsWaiting = false;
            if (_root != null) _root.SetActive(false);
            GameManager.Instance.SpawnLocalPlayer();
            IntroNoteUI.Show();
        }
    }
}
