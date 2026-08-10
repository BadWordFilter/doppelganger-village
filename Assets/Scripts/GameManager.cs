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
            Vector3 pos = new Vector3(r.x, 0.1f, r.y - 14f); // 트레일러(남쪽) 앞
            PhotonNetwork.Instantiate("PlayerAvatar", pos, Quaternion.identity);
            // 마스터만 실제 동작: 도플갱어 배정 + 마을 레이아웃 시드 + 낮/밤 사이클 시작
            Village.VillageDirector.EnsureAssigned();
            Village.VillageLayout.EnsureSeedAssigned();
            if (Village.PhaseDirector.Instance != null)
                Village.PhaseDirector.Instance.StartCycleIfMaster();
        }
    }
}
