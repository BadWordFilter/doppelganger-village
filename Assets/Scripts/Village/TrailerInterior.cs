using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 트레일러 내부 공간 (어몽어스식 별도 룸 — 맵 밖 원거리에 배치, 문 앞 E로 출입).
    /// 구출되어 보내진 시민들이 이 안에 모여 있다 — 잠입한 도플갱어도 태연히 섞여서.
    /// </summary>
    public static class TrailerInterior
    {
        public static readonly Vector3 Center = new(200f, 0f, 200f);

        /// <summary>내부 진입 시 플레이어 스폰 위치 (입구 안쪽).</summary>
        public static readonly Vector3 EntrySpawn = new(200f, 0.4f, 195.6f);

        /// <summary>밖으로 나올 때 위치 (마을 트레일러 문 앞).</summary>
        public static readonly Vector3 ExitToVillage = new(0f, 0.3f, -17.6f);

        /// <summary>마을 트레일러 문 앞 상호작용 지점.</summary>
        public static readonly Vector3 VillageDoor = new(0f, 0f, -18.4f);

        /// <summary>내부 출구 상호작용 지점 (입구 매트).</summary>
        public static readonly Vector3 InteriorExit = new(200f, 0f, 195.2f);

        public static bool Contains(Vector3 p) =>
            p.x >= 192f && p.x <= 208f && p.z >= 194f && p.z <= 206f && p.y >= -1f && p.y <= 4.5f;

        /// <summary>시민 id 기반 결정적 배치 슬롯 (전 클라이언트 동일).</summary>
        public static Vector3 CitizenSlot(int citizenId)
        {
            const int cols = 5;
            int idx = ((citizenId % 20) + 20) % 20;
            int row = idx / cols;
            int col = idx % cols;
            return new Vector3(195.6f + col * 2.2f, 0f, 203.8f - row * 1.9f);
        }
    }
}
