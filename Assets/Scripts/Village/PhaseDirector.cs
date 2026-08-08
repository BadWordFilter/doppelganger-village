using Photon.Pun;
using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 낮/밤 페이즈 전환. 깨어 있는 동물이 소진되고 정산이 끝나면 마스터가 밤을 개시한다.
    /// 밤: 하늘·조명이 어두워지고 야행성 시민(올빼미·박쥐·늑대)이 깨어나며, 도플갱어가 배회한다.
    /// 슬라이스에서 밤은 1회 — 야행성까지 소진되면 자동으로 최종 정산(승/패)이 발동된다.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PhaseDirector : MonoBehaviourPun
    {
        public static PhaseDirector Instance { get; private set; }
        public static bool IsNight { get; private set; }

        private const int NightRoamerCount = 2; // 밤에 배회하는 도플갱어 수

        private void Awake()
        {
            Instance = this;
            IsNight = false;
        }

        /// <summary>마스터 전용: 밤 개시를 전 클라이언트에 브로드캐스트.</summary>
        public void BeginNight()
        {
            if (!PhotonNetwork.IsMasterClient || IsNight) return;
            photonView.RPC(nameof(RpcNightStart), RpcTarget.All);
        }

        [PunRPC]
        private void RpcNightStart()
        {
            if (IsNight) return;
            IsNight = true;

            // ---- 조명·하늘 연출 ----
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                var light = lightGo.GetComponent<Light>();
                light.intensity = 0.25f;
                light.color = new Color(0.55f, 0.65f, 1f); // 차가운 달빛
            }
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f); // 짙은 밤하늘
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.12f);
            RenderSettings.fogDensity = 0.028f;
            RenderSettings.ambientIntensity = 0.35f;

            // ---- 야행성 시민 기상 ----
            int woken = 0;
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.IsNocturnal && !c.IsResolved && !c.gameObject.activeSelf)
                {
                    c.gameObject.SetActive(true);
                    woken++;
                }
            }

            UI.ToastUI.Show("밤이 찾아왔다... 야행성 주민들이 깨어난다. 놈들도 함께.");
            Debug.Log($"[Phase] 밤 개시 — 야행성 {woken}마리 기상");

            // ---- 밤 배회 도플갱어 (마스터 소유 룸 오브젝트) ----
            if (PhotonNetwork.IsMasterClient)
            {
                Vector3[] spawns = { new Vector3(-18f, 0f, 14f), new Vector3(18f, 0f, 12f) };
                for (int i = 0; i < NightRoamerCount && i < spawns.Length; i++)
                    PhotonNetwork.InstantiateRoomObject("DoppelChaser", spawns[i], Quaternion.identity);
            }
        }
    }
}
