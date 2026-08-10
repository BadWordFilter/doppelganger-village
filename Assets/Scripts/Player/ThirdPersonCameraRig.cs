using UnityEngine;
using UnityEngine.InputSystem;

namespace DoppelgangerVillage.Player
{
    /// <summary>
    /// 메인 카메라를 로컬 플레이어 뒤에 붙이는 3인칭 마우스 궤도 카메라.
    /// 좌클릭으로 커서 잠금, ESC로 해제 (WebGL 포인터락 규칙 준수).
    /// </summary>
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        public float Yaw { get; private set; }

        /// <summary>ESC 메뉴 설정에서 조절하는 마우스 감도 배율 (PlayerPrefs 저장).</summary>
        public static float SensitivityScale = 1f;

        private const float Distance = 3.8f;
        private const float PivotHeight = 1.05f; // 2등신 치비 플레이어 기준
        private const float Sensitivity = 0.12f;

        private float _pitch = 18f;
        private Transform _target;

        /// <summary>메인 카메라에 리그를 붙이고 대상을 지정한다.</summary>
        public static ThirdPersonCameraRig AttachTo(Transform target)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            var rig = cam.GetComponent<ThirdPersonCameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<ThirdPersonCameraRig>();
            rig._target = target;
            rig.Yaw = target.eulerAngles.y;
            return rig;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null) return;

            // 커서 잠금/해제 (브라우저는 사용자 입력 프레임에서만 잠금 허용, UI가 떠 있으면 잠그지 않음)
            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked
                && _target != null && !UI.DialogueUI.IsOpen && !UI.SettlementUI.IsShowing && !UI.IntroNoteUI.IsShowing && !UI.MenuUI.IsOpen)
                Cursor.lockState = CursorLockMode.Locked;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;

            if (_target == null || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 delta = mouse.delta.ReadValue();
            float sens = Sensitivity * SensitivityScale;
            Yaw += delta.x * sens;
            _pitch = Mathf.Clamp(_pitch - delta.y * sens, -15f, 60f);
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            var rot = Quaternion.Euler(_pitch, Yaw, 0f);
            Vector3 pivot = _target.position + Vector3.up * PivotHeight;

            // 카메라 충돌: 벽·집 뒤로 넘어가지 않고 막힌 지점까지 당겨온다
            Vector3 back = -(rot * Vector3.forward);
            float dist = Distance;
            if (Physics.SphereCast(pivot, 0.25f, back, out RaycastHit hit, Distance, ~0, QueryTriggerInteraction.Ignore))
            {
                // 자기 자신(플레이어 캡슐)은 시작 지점과 겹쳐 있어 SphereCast가 무시한다
                if (hit.transform != _target && !hit.transform.IsChildOf(_target))
                    dist = Mathf.Max(0.45f, hit.distance - 0.12f);
            }
            transform.position = pivot + back * dist;
            transform.rotation = rot;

            // 달리기 속도감: FOV 확장
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                bool running = PlayerController.Local != null && PlayerController.Local.IsRunning;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, running ? 66f : 60f, 6f * Time.deltaTime);
            }
        }
    }
}
