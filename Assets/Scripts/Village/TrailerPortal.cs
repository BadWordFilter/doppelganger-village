using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 트레일러 내부 룸의 문 너머로 실제 마을(트레일러 앞) 풍경이 보이는 포털 뷰.
    /// 마을 트레일러 문 위치의 카메라가 렌더 텍스처로 찍고, 내부 룸 문 개구부에 띄운다.
    /// 내부에 로컬 플레이어가 있을 때만 카메라를 켠다 (WebGL 성능).
    /// </summary>
    public class TrailerPortal : MonoBehaviour
    {
        private Camera _cam;
        private float _timer;

        private void Start()
        {
            var rt = new RenderTexture(768, 768, 16);

            // 마을 쪽 시점: 트레일러 문에서 마을(광장 방향)을 바라본다
            var camGo = new GameObject("TrailerPortalCam");
            camGo.transform.position = new Vector3(0f, 1.5f, -18.0f);
            camGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            _cam = camGo.AddComponent<Camera>();
            _cam.targetTexture = rt;
            _cam.fieldOfView = 68f;
            _cam.nearClipPlane = 0.2f;
            _cam.farClipPlane = 90f;
            _cam.enabled = false;

            // 내부 룸 문 개구부를 덮는 뷰 (안쪽에서 보인다)
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "TrailerPortalView";
            Destroy(quad.GetComponent<Collider>()); // 충돌은 기존 VoidBlocker 담당
            quad.transform.position = TrailerInterior.Center + new Vector3(0f, 1.7f, -6.05f);
            quad.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 룸 안쪽(+z 시점)에서 보이는 면
            quad.transform.localScale = new Vector3(3.4f, 3.5f, 1f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetTexture("_BaseMap", rt);
            quad.GetComponent<MeshRenderer>().material = mat;
        }

        private void Update()
        {
            if (_cam == null) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.25f;
            var local = Player.PlayerController.Local;
            bool inside = local != null && TrailerInterior.Contains(local.transform.position);
            if (_cam.enabled != inside) _cam.enabled = inside;
        }
    }
}
