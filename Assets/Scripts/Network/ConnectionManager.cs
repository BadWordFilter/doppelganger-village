using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace DoppelgangerVillage.Network
{
    /// <summary>
    /// PUN 연결과 4자리 룸 코드 입장을 관리한다.
    /// 마스터 클라이언트가 모든 판정 권한을 가진다 (멀티 아키텍처 규칙).
    /// 1인으로도 룸 생성·시작이 가능해야 한다 (심사위원 혼자 테스트).
    /// </summary>
    public class ConnectionManager : MonoBehaviourPunCallbacks
    {
        /// <summary>룸 최대 인원. 본선에서 동물 수와 함께 확장 가능하도록 상수로 분리.</summary>
        public const int MaxPlayersPerRoom = 4;

        private const int MaxCreateRetries = 3; // 룸 코드 충돌 시 재생성 횟수

        public static ConnectionManager Instance { get; private set; }

        /// <summary>UI 표시용 상태 메시지 (접속 중 / 완료 / 오류)</summary>
        public event Action<string> StatusChanged;

        /// <summary>게임 룸 입장 완료. 인자는 룸 코드.</summary>
        public event Action<string> RoomEntered;

        /// <summary>룸 입장 실패. 인자는 사유.</summary>
        public event Action<string> RoomJoinFailed;

        /// <summary>열린 방 목록 갱신 (로비 UI 표시용).</summary>
        public event Action<List<RoomInfo>> RoomListChanged;

        private int _createRetries;
        private readonly Dictionary<string, RoomInfo> _roomCache = new();

        /// <summary>리매치 예약 코드 — 씬 리로드 후 재접속 시 이 코드로 자동 입장.</summary>
        public static string RematchCode;

        private void Awake()
        {
            // 씬 리로드(리매치) 때 새 인스턴스가 이어받는다 — 게임 상태 전체 리셋을 위해 씬과 수명을 같이한다
            Instance = this;
        }

        /// <summary>마스터 서버 접속. 이미 연결돼 있으면 무시.</summary>
        public void Connect()
        {
            if (PhotonNetwork.IsConnected) return;
            PhotonNetwork.NickName = $"플레이어{UnityEngine.Random.Range(100, 1000)}";
            Report("서버 접속 중...");
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>새 룸 생성 (4자리 랜덤 코드).</summary>
        public void CreateRoom()
        {
            _createRetries = 0;
            TryCreateRoom();
        }

        private void TryCreateRoom()
        {
            string code = UnityEngine.Random.Range(1000, 10000).ToString();
            Report($"룸 생성 중... (코드 {code})");
            PhotonNetwork.CreateRoom(code, new RoomOptions { MaxPlayers = MaxPlayersPerRoom });
        }

        /// <summary>룸 코드로 입장.</summary>
        public void JoinRoomByCode(string code)
        {
            Report($"룸 {code} 입장 중...");
            PhotonNetwork.JoinRoom(code);
        }

        private void Report(string message)
        {
            Debug.Log($"[Connection] {message}");
            StatusChanged?.Invoke(message);
        }

        // ---- PUN 콜백 ----

        public override void OnConnectedToMaster()
        {
            Report("서버 접속 완료");
            if (!string.IsNullOrEmpty(RematchCode))
            {
                // 리매치: 같은 팀 전원이 같은 새 코드로 모인다 (첫 도착자가 방을 만든다)
                string code = RematchCode;
                RematchCode = null;
                Report($"같은 팀과 새 게임 준비 중... (코드 {code})");
                PhotonNetwork.JoinOrCreateRoom(code, new RoomOptions { MaxPlayers = MaxPlayersPerRoom }, TypedLobby.Default);
                return;
            }
            PhotonNetwork.JoinLobby(); // 열린 방 목록 수신용
        }

        /// <summary>룸을 떠나 로비로 (씬 리로드로 게임 상태 전체 리셋 — 새로고침 불필요).</summary>
        public static void LeaveToLobby()
        {
            RematchCode = null;
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
            PhotonNetwork.SendAllOutgoingCommands();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            foreach (var r in roomList)
            {
                if (r.RemovedFromList) _roomCache.Remove(r.Name);
                else _roomCache[r.Name] = r;
            }
            RoomListChanged?.Invoke(_roomCache.Values.Where(r => r.IsOpen && r.PlayerCount > 0).ToList());
        }

        public override void OnJoinedRoom()
        {
            string code = PhotonNetwork.CurrentRoom.Name;
            Report($"룸 {code} 입장 완료 ({PhotonNetwork.CurrentRoom.PlayerCount}/{MaxPlayersPerRoom}명, 마스터={PhotonNetwork.IsMasterClient})");
            RoomEntered?.Invoke(code);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (++_createRetries <= MaxCreateRetries)
            {
                TryCreateRoom(); // 코드 충돌 가능성 — 새 코드로 재시도
                return;
            }
            Report($"룸 생성 실패: {message}");
            RoomJoinFailed?.Invoke(message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Report($"입장 실패: 코드를 확인하세요 ({message})");
            RoomJoinFailed?.Invoke(message);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Report($"연결 끊김: {cause}");
        }
    }
}
