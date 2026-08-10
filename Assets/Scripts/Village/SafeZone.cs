using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 트레일러 안전구역 (기획: 밤의 거점). 배회 도플갱어가 침입하지 못하고,
    /// 밤에 이 안에 있는 플레이어는 서서히 회복된다 — "나갈 것인가, 버틸 것인가"의 선택지.
    /// </summary>
    public static class SafeZone
    {
        private static Vector3? _center;

        public static Vector3 Center
        {
            get
            {
                if (_center == null)
                {
                    var marker = GameObject.Find("SafeZoneMarker");
                    _center = marker != null ? marker.transform.position : new Vector3(0f, 0f, -18.5f);
                }
                return _center.Value;
            }
        }

        public static bool Contains(Vector3 position)
        {
            Vector3 d = position - Center;
            d.y = 0f;
            return d.sqrMagnitude <= GameConfig.SafeZoneRadius * GameConfig.SafeZoneRadius;
        }
    }
}
