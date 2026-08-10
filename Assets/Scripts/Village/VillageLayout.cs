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

        /// <summary>상업·의료 구역 집들 (2일차 해금 — 레이아웃 적용 시 채워짐).</summary>
        public static readonly List<Transform> CommercialHouses = new();

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
            Vector3 zoneCenter = FogBoundary.Center;
            var placed = new List<Vector3> { trailer };

            // 집 9채: 구역별 밴드 배치 — 거주(1일차 경계 안) / 상업·의료(경계 밖, 2일차 해금)
            string[] bandByHouse = { "거주", "거주", "거주", "상업", "상업", "상업", "거주", "거주", "상업" }; // speciesByHouse 순서와 동일
            var houses = new List<Transform>();
            foreach (Transform h in housesRoot.transform) houses.Add(h);
            for (int hi = 0; hi < houses.Count; hi++)
            {
                var h = houses[hi];
                bool residential = hi < bandByHouse.Length && bandByHouse[hi] == "거주";
                float rMin = residential ? 8f : FogBoundary.Day1Radius + 4f;   // 상업은 1일차 경계 밖
                float rMax = residential ? FogBoundary.Day1Radius - 3f : FogBoundary.FullRadius - 2.5f;
                Vector3 pos = zoneCenter + new Vector3(0f, 0f, rMin);
                bool ok = false;
                for (int attempt = 0; attempt < 80 && !ok; attempt++)
                {
                    float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                    float radius = rMin + (float)rng.NextDouble() * (rMax - rMin);
                    pos = zoneCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    if (Mathf.Abs(pos.x) > 32f || Mathf.Abs(pos.z) > 32f) continue;
                    if ((pos - trailer).magnitude < 10f) continue;   // 안전구역 주변 비움
                    if ((pos - plaza).magnitude < 7f) continue;      // 광장 비움
                    ok = true;
                    foreach (var p in placed)
                        if ((pos - p).magnitude < 9f) { ok = false; break; } // 집 간 최소 9m
                }
                placed.Add(pos);
                h.position = pos;
                Vector3 dir = (plaza - pos).normalized;
                float jitter = (float)(rng.NextDouble() * 80.0 - 40.0); // 광장 방향 ±40도
                h.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z)) * Quaternion.Euler(0f, jitter, 0f);
            }

            // 야행성 시민: 외곽 밴드 (밤에 경계가 열리면 도달 가능)
            var nocturnalIds = new[] { 11, 12, 13 };
            foreach (int nid in nocturnalIds)
            {
                var noct = FindCitizen(nid);
                if (noct == null) continue;
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                    float radius = FogBoundary.FullRadius - 5f + (float)rng.NextDouble() * 3f;
                    Vector3 pos = zoneCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    if (Mathf.Abs(pos.x) > 32f || Mathf.Abs(pos.z) > 32f) continue;
                    bool clear = true;
                    foreach (var p in placed)
                        if ((pos - p).magnitude < 7f) { clear = false; break; }
                    if (!clear) continue;
                    noct.transform.position = pos;
                    noct.transform.rotation = Quaternion.LookRotation((zoneCenter - pos).normalized);
                    placed.Add(pos);
                    break;
                }
            }

            // 주간 동물을 자기 집 앞으로 이동 (집 순서 ↔ 동물 id 매핑은 씬 구축 규칙과 동일)
            int[] idByHouse = { 0, 3, 5, 7, 9, 10, 1, 4, 8 };
            string[] speciesByHouse = { "강아지", "고양이", "토끼", "돼지", "곰", "양", "강아지", "고양이", "돼지" };
            CommercialHouses.Clear();
            for (int i = 0; i < houses.Count && i < idByHouse.Length; i++)
            {
                var citizen = FindCitizen(idByHouse[i]);
                if (citizen == null) continue;
                var h = houses[i];
                citizen.transform.position = h.position + h.forward * 4.5f;
                citizen.transform.rotation = h.rotation;
                // 상업·의료 구역(돼지·곰·양) = 2일차 해금 대상 (기획 5절)
                if (speciesByHouse[i] == "돼지" || speciesByHouse[i] == "곰" || speciesByHouse[i] == "양")
                    CommercialHouses.Add(h);
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
