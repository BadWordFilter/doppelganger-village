using Photon.Pun;
using UnityEngine;

namespace DoppelgangerVillage.Network
{
    /// <summary>
    /// PUN 실서버 접속 스모크 테스트 (임시 — 검증 후 삭제).
    /// 접속 → 마스터 서버 도달 시 룸 생성 → 입장 로그까지 자동 진행.
    /// </summary>
    public class ConnectionSmokeTest : MonoBehaviour
    {
        private bool _roomRequested;

        private void Start()
        {
            ConnectionManager.Instance.RoomEntered += code =>
                Debug.Log($"[SmokeTest] SUCCESS: room={code}, isMaster={PhotonNetwork.IsMasterClient}, region={PhotonNetwork.CloudRegion}");
            ConnectionManager.Instance.Connect();
        }

        private void Update()
        {
            if (_roomRequested || !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom) return;
            if (PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.ConnectedToMasterServer) return;
            _roomRequested = true;
            ConnectionManager.Instance.CreateRoom();
        }
    }
}
