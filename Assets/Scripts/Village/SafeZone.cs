using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 트레일러 안전구역 (기획: 밤의 거점). 배회 도플갱어가 침입하지 못하고,
    /// 밤에 이 안에 있는 플레이어는 서서히 회복된다 — "나갈 것인가, 버틸 것인가"의 선택지.
    /// </summary>
    public static class SafeZone
    {
        // 트레일러 컨테이너 내부 (월드 고정 박스) — 안에 들어가야 안전하다
        private static readonly Vector3 BoxMin = new(-1.05f, 0f, -25.2f);
        private static readonly Vector3 BoxMax = new(1.05f, 2.6f, -18.8f);

        public static Vector3 Center => new(0f, 0f, -22f);

        public static bool Contains(Vector3 p) =>
            p.x >= BoxMin.x && p.x <= BoxMax.x
            && p.y >= BoxMin.y && p.y <= BoxMax.y
            && p.z >= BoxMin.z && p.z <= BoxMax.z;
    }
}
