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

        private CharacterController _cc;
        private ThirdPersonCameraRig _rig;
        private float _verticalVel;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (!photonView.IsMine) return;
            Local = this;
            _rig = ThirdPersonCameraRig.AttachTo(transform);
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

            // 이동 입력 (대화/정산 UI 또는 게임 종료 시 입력 차단 — 스태미나 회복은 유지)
            bool uiLocked = UI.DialogueUI.IsOpen || UI.SettlementUI.IsShowing
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

            _verticalVel = _cc.isGrounded ? -1f : _verticalVel - 20f * Time.deltaTime;
            _cc.Move((move + Vector3.up * _verticalVel) * Time.deltaTime);
        }

        /// <summary>과잉 심문 공격 등으로 피해를 받는다. 0이 되면 감염 판정 → 전원 감염 시 패배.</summary>
        public void TakeDamage(float amount)
        {
            if (!photonView.IsMine || CurrentHp <= 0f) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            if (CurrentHp <= 0f)
            {
                Debug.Log("[Player] HP 0 — 도플갱어에게 감염되었다");
                UI.ToastUI.Show("의식이 흐려진다... 도플갱어에게 감염되었다.");
                if (Judgement.JudgementDirector.Instance != null)
                    Judgement.JudgementDirector.Instance.NotifyLocalInfected();
            }
        }

        /// <summary>구급상자 등으로 회복.</summary>
        public void Heal(float amount)
        {
            if (!photonView.IsMine || CurrentHp <= 0f) return;
            CurrentHp = Mathf.Min(GameConfig.MaxHp, CurrentHp + amount);
        }
    }
}
