using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 돌변한 도플갱어 추격자. 3상태 FSM: 배회 → 감지 → 추격.
    /// 마스터 클라이언트만 AI를 굴리고, 위치는 PhotonTransformView로 전 클라이언트 동기화.
    /// 잡히면 HP 감소 + 짧은 기절 (사망/관전 시스템은 스코프 외 — CLAUDE.md).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class DoppelChaser : MonoBehaviourPun
    {
        private enum State { Roam, Chase }

        private State _state = State.Roam;
        private NavMeshAgent _agent;
        private Vector3 _home;
        private float _repathTimer;
        private float _attackCooldown;
        private float _lifetime;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _home = transform.position;
        }

        private void Start()
        {
            // 원격 클라이언트에서는 에이전트를 끄고 TransformView 동기화만 받는다
            if (!PhotonNetwork.IsMasterClient) _agent.enabled = false;
        }

        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient || !_agent.enabled || !_agent.isOnNavMesh) return;

            // 수명 소진 → 소멸 (돌변 개체가 무한히 쌓이지 않도록)
            _lifetime += Time.deltaTime;
            if (_lifetime > GameConfig.ChaserLifetimeSeconds)
            {
                PhotonNetwork.Destroy(gameObject);
                return;
            }

            _attackCooldown -= Time.deltaTime;

            var target = NearestAlivePlayer(out float dist);
            switch (_state)
            {
                case State.Roam:
                    _agent.speed = GameConfig.ChaserRoamSpeed;
                    _repathTimer -= Time.deltaTime;
                    if (_repathTimer <= 0f || (!_agent.pathPending && _agent.remainingDistance < 1f))
                    {
                        Vector2 r = Random.insideUnitCircle * 10f;
                        Vector3 dest = _home + new Vector3(r.x, 0f, r.y);
                        if (SafeZone.Contains(dest))
                        {
                            _repathTimer = 0.5f; // 안전구역 안쪽은 배회 목적지에서 제외
                            break;
                        }
                        _agent.SetDestination(dest);
                        _repathTimer = 4f;
                    }
                    if (target != null && dist < GameConfig.ChaserDetectRadius)
                        _state = State.Chase;
                    break;

                case State.Chase:
                    _agent.speed = GameConfig.ChaserChaseSpeed;
                    if (target == null || dist > GameConfig.ChaserLoseRadius)
                    {
                        _state = State.Roam;
                        break;
                    }
                    _agent.SetDestination(target.transform.position);
                    if (dist < GameConfig.ChaserAttackRange && _attackCooldown <= 0f)
                    {
                        _attackCooldown = 2f;
                        photonView.RPC(nameof(RpcCaught), RpcTarget.All, target.photonView.OwnerActorNr);
                    }
                    break;
            }
        }

        private Player.PlayerController NearestAlivePlayer(out float dist)
        {
            dist = float.MaxValue;
            Player.PlayerController best = null;
            foreach (var p in Object.FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None))
            {
                if (p.CurrentHp <= 0f) continue;
                if (SafeZone.Contains(p.transform.position)) continue; // 거점 안 플레이어는 노리지 못한다
                float d = Vector3.Distance(p.transform.position, transform.position);
                if (d < dist)
                {
                    dist = d;
                    best = p;
                }
            }
            return best;
        }

        [PunRPC]
        private void RpcCaught(int actorNumber)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber) return;
            var local = Player.PlayerController.Local;
            if (local == null) return;
            local.TakeDamage(GameConfig.ChaserHitDamage);
            local.Stun(GameConfig.StunDuration);
            UI.ToastUI.Show("도플갱어에게 붙잡혔다! 도망쳐!");
        }
    }
}
