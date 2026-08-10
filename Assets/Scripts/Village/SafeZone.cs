using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 트레일러 안전구역 (기획: 밤의 거점). 배회 도플갱어가 침입하지 못하고,
    /// 밤에 이 안에 있는 플레이어는 서서히 회복된다 — "나갈 것인가, 버틸 것인가"의 선택지.
    /// </summary>
    public static class SafeZone
    {
        public static Vector3 Center => new(0f, 0f, -22f); // 마을 쪽 트레일러 (추격자 회피 기준점)

        /// <summary>안전 = 트레일러 내부 룸 안 (어몽어스식 별도 공간).</summary>
        public static bool Contains(Vector3 p) => TrailerInterior.Contains(p);
    }
}
