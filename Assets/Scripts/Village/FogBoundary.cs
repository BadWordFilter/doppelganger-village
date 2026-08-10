using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 맵 경계의 안개 벽 — 일차가 지날수록 바깥으로 물러나며 활동 구역이 넓어진다
    /// (기획 5절 "안개가 걷히며 구역 해금"을 [99일 살아남기]식 확장 경계로 구현).
    /// 벽 세그먼트에는 콜라이더가 있어 경계 밖으로 나갈 수 없다.
    /// </summary>
    public static class FogBoundary
    {
        public static readonly Vector3 Center = new(0f, 0f, -6f);
        public const float Day1Radius = 17f;   // 1일차: 거주 구역 + 트레일러
        public const float FullRadius = 28f;   // 밤부터: 전체 맵 (영구 외곽 경계)

        private const int SegmentCount = 28;
        private const float WallHeight = 9f;

        private static readonly List<Transform> _segments = new();
        private static float _currentRadius = -1f;

        public static void SetRadius(float radius, bool animated, MonoBehaviour host)
        {
            if (Mathf.Approximately(_currentRadius, radius)) return;
            Ensure();
            if (animated && host != null && _currentRadius > 0f)
                host.StartCoroutine(Animate(radius));
            else
                Apply(radius);
        }

        private static void Ensure()
        {
            _segments.RemoveAll(s => s == null);
            if (_segments.Count > 0) return;
            var root = new GameObject("FogBoundary").transform;
            var mat = Resources.Load<Material>("ZoneFog");
            for (int i = 0; i < SegmentCount; i++)
            {
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube); // BoxCollider 유지 — 통과 불가
                seg.name = "FogWall_" + i;
                seg.transform.SetParent(root, false);
                if (mat != null) seg.GetComponent<MeshRenderer>().sharedMaterial = mat;
                _segments.Add(seg.transform);
            }
        }

        private static void Apply(float radius)
        {
            _currentRadius = radius;
            float segWidth = 2f * Mathf.PI * radius / SegmentCount * 1.18f; // 약간 겹치게
            for (int i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                if (seg == null) continue;
                float angle = i / (float)SegmentCount * Mathf.PI * 2f;
                Vector3 dir = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                seg.position = Center + dir * radius + Vector3.up * (WallHeight * 0.5f);
                seg.rotation = Quaternion.LookRotation(dir);
                seg.localScale = new Vector3(segWidth, WallHeight, 1.2f);
            }
        }

        private static IEnumerator Animate(float target)
        {
            float start = _currentRadius;
            float t = 0f;
            const float duration = 5f;
            while (t < duration)
            {
                t += Time.deltaTime;
                Apply(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            Apply(target);
        }
    }
}
