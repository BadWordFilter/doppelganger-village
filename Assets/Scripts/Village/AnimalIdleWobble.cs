using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 동물 유휴 모션: 잔잔한 숨쉬기 스쿼시 + 이따금 통통 뛰기.
    /// 순수 연출(로컬 코스메틱)이라 네트워크 동기화 불필요.
    /// </summary>
    public class AnimalIdleWobble : MonoBehaviour
    {
        private Transform _body;
        private Vector3 _baseScale;
        private float _phase;
        private float _hopTimer;
        private float _hopHeight;

        private void Start()
        {
            _body = transform.Find("Body");
            if (_body != null) _baseScale = _body.localScale;
            _phase = Random.value * 10f;
            _hopTimer = Random.Range(3f, 9f);
            _baseRotation = transform.rotation;
        }

        private AnimalCitizen _citizen;
        private Quaternion _baseRotation;

        private void Update()
        {
            if (_body == null) return;

            // 숨쉬기 스쿼시
            float breathe = 1f + Mathf.Sin(Time.time * 2.2f + _phase) * 0.03f;
            _body.localScale = new Vector3(_baseScale.x, _baseScale.y * breathe, _baseScale.z);

            // 가까운 플레이어를 천천히 바라봄 (판정 전 개체만, 연출 중엔 정지)
            if (_citizen == null) _citizen = GetComponent<AnimalCitizen>();
            if (_citizen != null && !_citizen.IsResolved && !_citizen.IsActing)
            {
                var local = Player.PlayerController.Local;
                if (local != null)
                {
                    Vector3 to = local.transform.position - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude < 4.5f * 4.5f && to.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 2.2f * Time.deltaTime);
                    else
                        transform.rotation = Quaternion.Slerp(transform.rotation, _baseRotation, 1.2f * Time.deltaTime);
                }
            }

            // 이따금 통통
            _hopTimer -= Time.deltaTime;
            if (_hopTimer <= 0f)
            {
                _hopTimer = Random.Range(4f, 10f);
                _hopHeight = 0.14f;
            }
            if (_hopHeight > 0f)
            {
                _hopHeight = Mathf.Max(0f, _hopHeight - Time.deltaTime * 0.5f);
                var p = transform.localPosition;
                transform.localPosition = new Vector3(p.x, Mathf.PingPong(_hopHeight * 4f, 0.14f), p.z);
            }
        }
    }
}
