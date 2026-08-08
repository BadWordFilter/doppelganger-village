using System.Linq;
using Photon.Pun;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 도플갱어 배정 관리. 마스터 클라이언트가 룸 입장 시 1회 랜덤 배정하고
    /// 룸 CustomProperties로 공유한다 — 늦게 합류한 클라이언트도 동일 결과를 읽는다.
    /// </summary>
    public static class VillageDirector
    {
        private const string DoppelKey = "doppelIds";

        /// <summary>마스터 전용: 아직 배정 전이면 도플갱어 2~3마리를 랜덤 배정.</summary>
        public static void EnsureAssigned()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(DoppelKey)) return;

            // 야행성(시작 시 비활성) 시민도 배정 대상에 포함 — 밤에도 추리가 성립해야 한다
            var ids = Object.FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(a => a.CitizenId)
                .ToList();
            if (ids.Count == 0)
            {
                Debug.LogWarning("[Village] 배정할 동물이 없습니다.");
                return;
            }

            int count = Mathf.Min(Random.Range(GameConfig.MinDoppelgangers, GameConfig.MaxDoppelgangers + 1), ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                int j = Random.Range(i, ids.Count);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }
            int[] chosen = ids.Take(count).ToArray();
            PhotonNetwork.CurrentRoom.SetCustomProperties(new PhotonHashtable { { DoppelKey, chosen } });
            Debug.Log($"[Village] 도플갱어 {count}마리 배정 완료 (id: {string.Join(",", chosen)})");
        }

        public static bool IsDoppelganger(int citizenId)
        {
            if (!PhotonNetwork.InRoom) return false;
            return PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(DoppelKey, out object v)
                   && v is int[] ids
                   && System.Array.IndexOf(ids, citizenId) >= 0;
        }
    }
}
