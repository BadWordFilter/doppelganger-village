using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>바닥에 떨어진 드랍 아이템 표식 — 회전·부유 연출 + E 줍기 대상 (기획: 부품 줍기).</summary>
    public class PickupMarker : MonoBehaviour
    {
        public int CitizenId;

        private float _baseY;

        private void Start()
        {
            _baseY = transform.position.y;
        }

        private void Update()
        {
            transform.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);
            var p = transform.position;
            transform.position = new Vector3(p.x, _baseY + Mathf.Sin(Time.time * 2.4f) * 0.06f, p.z);
        }
    }
}
