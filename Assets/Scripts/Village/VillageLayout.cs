using System.Collections.Generic;
using Photon.Pun;
using Unity.AI.Navigation;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 시드 기반 마을 랜덤 배치 (플레이테스트: "집이 뭉쳐 있다").
    /// 마스터가 시드를 룸 프로퍼티로 공유 → 전 클라이언트가 동일 레이아웃 생성 → NavMesh 런타임 재베이크.
    /// 매 게임 다른 마을이 된다.
    /// </summary>
    public class VillageLayout : MonoBehaviourPunCallbacks
    {
        private const string SeedKey = "layoutSeed";
        private bool _applied;

        /// <summary>마스터 전용: 시드 미배정이면 배정 (도플갱어 배정과 같은 타이밍).</summary>
        public static void EnsureSeedAssigned()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SeedKey)) return;
            int seed = Random.Range(1, 999999);
            PhotonNetwork.CurrentRoom.SetCustomProperties(new PhotonHashtable { { SeedKey, seed } });
        }

        public override void OnJoinedRoom() => TryApply();
        public override void OnRoomPropertiesUpdate(PhotonHashtable changed) => TryApply();

        private void TryApply()
        {
            if (_applied || !PhotonNetwork.InRoom) return;
            if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SeedKey, out object v) || v is not int seed) return;
            _applied = true;
            Apply(seed);
        }

        private void Apply(int seed)
        {
            var rng = new System.Random(seed);
            var housesRoot = GameObject.Find("Village/Houses");
            if (housesRoot == null) return;

            Vector3 trailer = new Vector3(0f, 0f, -22f);
            Vector3 plaza = new Vector3(0f, 0f, 2f);
            var placed = new List<Vector3> { trailer };

            // 집 9채: 트레일러·광장·서로에게서 최소 거리를 지키며 산개 배치
            var houses = new List<Transform>();
            foreach (Transform h in housesRoot.transform) houses.Add(h);
            foreach (var h in houses)
            {
                Vector3 pos = Vector3.zero;
                bool ok = false;
                for (int attempt = 0; attempt < 60 && !ok; attempt++)
                {
                    float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                    float radius = 11f + (float)rng.NextDouble() * 17f; // 11~28m
                    pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius * 0.9f + 2f);
                    if (Mathf.Abs(pos.x) > 30f || Mathf.Abs(pos.z) > 30f) continue;
                    if ((pos - trailer).magnitude < 13f) continue;   // 안전구역 주변 비움
                    if ((pos - plaza).magnitude < 8f) continue;      // 광장 비움
                    ok = true;
                    foreach (var p in placed)
                        if ((pos - p).magnitude < 10f) { ok = false; break; } // 집 간 최소 10m
                }
                placed.Add(pos);
                h.position = pos;
                Vector3 dir = (plaza - pos).normalized;
                float jitter = (float)(rng.NextDouble() * 80.0 - 40.0); // 광장 방향 ±40도
                h.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z)) * Quaternion.Euler(0f, jitter, 0f);
            }

            // 주간 동물을 자기 집 앞으로 이동 (집 순서 ↔ 동물 id 매핑은 씬 구축 규칙과 동일)
            int[] idByHouse = { 0, 3, 5, 7, 9, 10, 1, 4, 8 };
            for (int i = 0; i < houses.Count && i < idByHouse.Length; i++)
            {
                var citizen = FindCitizen(idByHouse[i]);
                if (citizen == null) continue;
                var h = houses[i];
                citizen.transform.position = h.position + h.forward * 4.5f;
                citizen.transform.rotation = h.rotation;
            }

            // NavMesh 런타임 재베이크 (이동한 집들이 장애물로 반영되도록)
            var ground = GameObject.Find("Ground");
            var surface = ground != null ? ground.GetComponent<NavMeshSurface>() : null;
            if (surface != null) surface.BuildNavMesh();

            Debug.Log($"[Village] 레이아웃 시드 {seed} 적용 — 집 {houses.Count}채 산개 배치, NavMesh 재베이크");
        }

        private static AnimalCitizen FindCitizen(int id)
        {
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.CitizenId == id) return c;
            return null;
        }
    }
}
