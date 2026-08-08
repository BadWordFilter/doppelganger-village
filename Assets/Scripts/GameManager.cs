using Photon.Pun;
using UnityEngine;

namespace DoppelgangerVillage
{
    /// <summary>
    /// 게임 흐름 관리 (슬라이스: 낮 페이즈 1개). 룸 입장 후 로컬 플레이어를 스폰한다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>트레일러 앞 반경 3m 랜덤 위치에 로컬 아바타 스폰 (플레이어 겹침 방지).</summary>
        public void SpawnLocalPlayer()
        {
            Vector2 r = Random.insideUnitCircle * 3f;
            Vector3 pos = new Vector3(r.x, 0.1f, r.y);
            PhotonNetwork.Instantiate("PlayerAvatar", pos, Quaternion.identity);
        }
    }
}
