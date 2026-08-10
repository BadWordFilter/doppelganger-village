using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DoppelgangerVillage.Player
{
    /// <summary>
    /// WASD 이동 + Shift 달리기(스태미나) + 중력. 로컬 소유 아바타에서만 입력 처리.
    /// 원격 아바타는 PhotonTransformView가 위치를 동기화한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviourPun
    {
        /// <summary>이 클라이언트가 조종하는 로컬 플레이어 (HUD 등에서 참조)</summary>
        public static PlayerController Local { get; private set; }

        public float CurrentHp { get; private set; } = GameConfig.MaxHp;
        public float CurrentStamina { get; private set; } = GameConfig.MaxStamina;
        public bool IsExhausted { get; private set; }
        public bool IsRunning { get; private set; }

        private float _dangerTimer;

        /// <summary>추격자 근접도 → 심장박동 가속 (0.25초 주기 계산).</summary>
        private void UpdateDanger()
        {
            _dangerTimer -= Time.deltaTime;
            if (_dangerTimer > 0f) return;
            _dangerTimer = 0.25f;
            float danger = 0f;
            if (CurrentHp > 0f && !Village.SafeZone.Contains(transform.position))
            {
                float nearest = float.MaxValue;
                foreach (var ch in FindObjectsByType<Village.DoppelChaser>(FindObjectsSortMode.None))
                {
                    float d = Vector3.Distance(ch.transform.position, transform.position);
                    if (d < nearest) nearest = d;
                }
                if (nearest < 14f) danger = Mathf.Clamp01(1f - (nearest - 2f) / 12f);
            }
            UI.SfxDirector.SetDanger(danger);
        }

        private CharacterController _cc;
        private ThirdPersonCameraRig _rig;
        private float _verticalVel;
        private float _stunTimer;

        private Light _lantern;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            // 랜턴: 밤에 좁아진 시야 속 유일한 불빛 (모든 아바타 공통, 밤에만 켜짐)
            var lanternGo = new GameObject("Lantern");
            lanternGo.transform.SetParent(transform, false);
            lanternGo.transform.localPosition = new Vector3(0f, 1.7f, 0.25f);
            _lantern = lanternGo.AddComponent<Light>();
            _lantern.type = LightType.Point;
            _lantern.range = 11f;
            _lantern.color = new Color(1f, 0.85f, 0.6f);
            _lantern.intensity = 0f;
        }

        private void LateUpdate()
        {
            if (_lantern != null)
                _lantern.intensity = Village.PhaseDirector.IsNight ? 2.4f : 0f;
        }

        /// <summary>커스터마이징 셔츠 팔레트 (로비 스와치와 공유).</summary>
        public static readonly Color[] ShirtPalette =
        {
            new(0.90f, 0.53f, 0.24f), // 주황 (기본)
            new(0.30f, 0.62f, 0.88f), // 하늘
            new(0.38f, 0.72f, 0.42f), // 초록
            new(0.90f, 0.45f, 0.60f), // 분홍
            new(0.62f, 0.45f, 0.85f), // 보라
            new(0.92f, 0.80f, 0.30f), // 노랑
        };

        private void Start()
        {
            ApplyCustomization(); // 모든 아바타 공통 — 소유자의 색 선택 반영
            if (!photonView.IsMine) return;
            Local = this;
            _rig = ThirdPersonCameraRig.AttachTo(transform);
        }

        /// <summary>소유자 플레이어 프로퍼티의 셔츠 색을 아바타에 적용.</summary>
        private void ApplyCustomization()
        {
            int idx = 0;
            if (photonView.Owner != null
                && photonView.Owner.CustomProperties.TryGetValue("shirt", out object v) && v is int i)
                idx = Mathf.Clamp(i, 0, ShirtPalette.Length - 1);
            Color c = ShirtPalette[idx];
            foreach (var partName in new[] { "Body", "ArmL", "ArmR" })
            {
                var part = Village.StageDirectionActor.FindDeep(transform, partName);
                if (part == null) continue;
                var r = part.GetComponent<MeshRenderer>();
                if (r != null) r.material.SetColor("_BaseColor", c);
            }
        }

        private void OnDestroy()
        {
            if (Local == this) Local = null;
        }

        private void Update()
        {
            if (!photonView.IsMine) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            // 이동 입력 (대화/정산 UI·게임 종료·기절 시 입력 차단 — 스태미나 회복은 유지)
            if (_stunTimer > 0f) _stunTimer -= Time.deltaTime;
            bool uiLocked = _stunTimer > 0f || CurrentHp <= 0f // 감염(HP 0) = 사망 판정 — 움직일 수 없다
                || UI.DialogueUI.IsOpen || UI.SettlementUI.IsShowing || UI.IntroNoteUI.IsShowing || UI.MenuUI.IsOpen
                || (Judgement.JudgementDirector.Instance != null && Judgement.JudgementDirector.Instance.GameEnded);
            Vector2 input = Vector2.zero;
            if (!uiLocked)
            {
                if (kb.wKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed) input.x -= 1f;
                input = Vector2.ClampMagnitude(input, 1f);
            }

            // 스태미나: 달리는 동안 소모, 정지·걷기 시 회복, 고갈 시 감속 상태 진입
            bool wantsRun = kb.leftShiftKey.isPressed && input.sqrMagnitude > 0.01f;
            if (wantsRun && !IsExhausted && CurrentStamina > 0f)
            {
                CurrentStamina = Mathf.Max(0f, CurrentStamina - GameConfig.StaminaDrainPerSec * Time.deltaTime);
                if (CurrentStamina <= 0f) IsExhausted = true;
            }
            else
            {
                CurrentStamina = Mathf.Min(GameConfig.MaxStamina, CurrentStamina + GameConfig.StaminaRegenPerSec * Time.deltaTime);
                if (IsExhausted && CurrentStamina >= GameConfig.MaxStamina * 0.3f) IsExhausted = false;
            }
            bool running = wantsRun && !IsExhausted;
            float speed = IsExhausted ? GameConfig.ExhaustedSpeed : (running ? GameConfig.RunSpeed : GameConfig.WalkSpeed);

            // 카메라 기준 이동 방향
            float yaw = _rig != null ? _rig.Yaw : transform.eulerAngles.y;
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Vector3 move = (forward * input.y + right * input.x) * speed;

            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12f * Time.deltaTime);

            if (_cc.isGrounded)
            {
                _verticalVel = -1f;
                if (!uiLocked && kb.spaceKey.wasPressedThisFrame)
                    _verticalVel = GameConfig.JumpSpeed; // 점프
            }
            else
            {
                _verticalVel -= 20f * Time.deltaTime;
            }
            _cc.Move((move + Vector3.up * _verticalVel) * Time.deltaTime);

            IsRunning = running && move.sqrMagnitude > 0.01f;

            // 낙사 방지: 허공으로 떨어지면 트레일러 앞으로 복귀
            if (transform.position.y < -10f)
            {
                _cc.enabled = false;
                transform.position = Village.TrailerInterior.ExitToVillage;
                _cc.enabled = true;
                _verticalVel = 0f;
                UI.ToastUI.Show("아찔했다... 정신을 차리니 트레일러 앞이다.");
            }

            UpdateDanger();
            UpdateSafeZone();
        }

        private bool _safeToastShownTonight;

        /// <summary>밤의 거점(트레일러 안전구역): 안에 있으면 서서히 회복 (기획: 해질녘 거점 복귀).</summary>
        private void UpdateSafeZone()
        {
            if (!Village.PhaseDirector.IsNight)
            {
                _safeToastShownTonight = false;
                return;
            }
            if (!Village.SafeZone.Contains(transform.position)) return;
            if (CurrentHp > 0f && CurrentHp < GameConfig.MaxHp)
                CurrentHp = Mathf.Min(GameConfig.MaxHp, CurrentHp + GameConfig.SafeZoneHpRegenPerSec * Time.deltaTime);
            if (!_safeToastShownTonight)
            {
                _safeToastShownTonight = true;
                UI.ToastUI.Show("트레일러 안은 안전하다... 상처가 아물고, 놈들도 들어오지 못한다.");
            }
        }

        /// <summary>과잉 심문 공격 등으로 피해를 받는다. 0이 되면 감염 판정 → 전원 감염 시 패배.</summary>
        public void TakeDamage(float amount)
        {
            if (!photonView.IsMine || CurrentHp <= 0f) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            UI.SfxDirector.Play("hit");
            if (CurrentHp <= 0f)
            {
                Debug.Log("[Player] HP 0 — 도플갱어에게 감염되었다");
                UI.ToastUI.Show("의식이 흐려진다... 도플갱어에게 감염되었다.");
                UI.ScreenFX.ShowInfected();
                if (Judgement.JudgementDirector.Instance != null)
                    Judgement.JudgementDirector.Instance.NotifyLocalInfected();
            }
        }

        /// <summary>추격자에게 잡히는 등 짧은 기절 — 입력이 잠시 차단된다.</summary>
        public void Stun(float duration)
        {
            _stunTimer = Mathf.Max(_stunTimer, duration);
        }

        /// <summary>구급상자 등으로 회복.</summary>
        public void Heal(float amount)
        {
            if (!photonView.IsMine || CurrentHp <= 0f) return;
            CurrentHp = Mathf.Min(GameConfig.MaxHp, CurrentHp + amount);
        }
    }
}
