using System.Linq;
using Photon.Pun;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 낮/밤 사이클 관리 (일수 제한 없음 — 기획서 루프).
    /// 낮(타이머) → 해질녘 정산 → 밤(야행성·배회 도플) → 아침, 다음 일차 반복.
    /// 마스터가 타이머 권한을 갖고 페이즈 전환을 브로드캐스트하며,
    /// 종료 시각은 PhotonNetwork.Time 기준이라 전 클라이언트 시계가 일치한다.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PhaseDirector : MonoBehaviourPun
    {
        public static PhaseDirector Instance { get; private set; }
        public static bool IsNight { get; private set; }
        public static int DayNumber { get; private set; } = 1;

        private const int NightRoamerCount = 2;

        private double _phaseEndTime;
        private bool _cycleRunning;
        private bool _settling;

        private float _dayLightIntensity = 1f;
        private Color _dayLightColor = Color.white;
        private Light _sun;

        public float RemainingSeconds => _cycleRunning ? Mathf.Max(0f, (float)(_phaseEndTime - PhotonNetwork.Time)) : 0f;

        private void Awake()
        {
            Instance = this;
            IsNight = false;
            DayNumber = 1;
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                _sun = lightGo.GetComponent<Light>();
                _dayLightIntensity = _sun.intensity;
                _dayLightColor = _sun.color;
            }
        }

        /// <summary>마스터 전용: 게임 시작 시 1일차 낮 개시.</summary>
        public void StartCycleIfMaster()
        {
            if (!PhotonNetwork.IsMasterClient || _cycleRunning) return;
            photonView.RPC(nameof(RpcBeginDay), RpcTarget.All, 1, PhotonNetwork.Time + GameConfig.DayDurationSeconds);
        }

        private void Update()
        {
            // 타이머 만료 → 마스터가 정산+페이즈 전환
            if (!PhotonNetwork.IsMasterClient || !_cycleRunning || _settling) return;
            var jd = Judgement.JudgementDirector.Instance;
            if (jd == null || jd.GameEnded) return;
            if (RemainingSeconds <= 0f)
            {
                _settling = true;
                jd.SettleAndSwitchPhase();
            }
        }

        /// <summary>마스터 전용: 정산(계속) 후 다음 페이즈로 전환.</summary>
        public void SwitchPhase()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!IsNight)
                photonView.RPC(nameof(RpcBeginNight), RpcTarget.All, PhotonNetwork.Time + GameConfig.NightDurationSeconds);
            else
                photonView.RPC(nameof(RpcBeginDay), RpcTarget.All, DayNumber + 1, PhotonNetwork.Time + GameConfig.DayDurationSeconds);
        }

        [PunRPC]
        private void RpcBeginDay(int day, double endTime)
        {
            IsNight = false;
            DayNumber = day;
            _phaseEndTime = endTime;
            _cycleRunning = true;
            _settling = false;

            // ---- 아침 연출 ----
            if (_sun != null)
            {
                _sun.intensity = _dayLightIntensity;
                _sun.color = _dayLightColor;
            }
            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.fog = false;
            RenderSettings.ambientIntensity = 1f;

            // 주간 시민 기상, 야행성은 잠들기
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.IsResolved) continue;
                c.gameObject.SetActive(!c.IsNocturnal);
            }

            // 밤 배회 도플갱어 소멸 (마스터)
            if (PhotonNetwork.IsMasterClient)
            {
                foreach (var chaser in FindObjectsByType<DoppelChaser>(FindObjectsSortMode.None))
                    PhotonNetwork.Destroy(chaser.gameObject);
            }

            if (day > 1) UI.ToastUI.Show($"{day}일차 아침이 밝았다. 아직 구조를 기다리는 주민들이 있다.");
        }

        [PunRPC]
        private void RpcBeginNight(double endTime)
        {
            IsNight = true;
            _phaseEndTime = endTime;
            _cycleRunning = true;
            _settling = false;

            // ---- 밤 연출 ----
            if (_sun != null)
            {
                _sun.intensity = 0.25f;
                _sun.color = new Color(0.55f, 0.65f, 1f);
            }
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.12f);
            RenderSettings.fogDensity = 0.028f;
            RenderSettings.ambientIntensity = 0.35f;

            // 주간 시민은 집으로(잠들기), 야행성 기상
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.IsResolved) continue;
                c.gameObject.SetActive(c.IsNocturnal);
            }

            UI.ToastUI.Show("밤이 찾아왔다... 야행성 주민이 깨어나고, 놈들이 마을을 배회한다.");

            // 밤 배회 도플갱어 스폰 (마스터)
            if (PhotonNetwork.IsMasterClient)
            {
                Vector3[] spawns = { new Vector3(-16f, 0f, 12f), new Vector3(16f, 0f, 10f) };
                for (int i = 0; i < NightRoamerCount && i < spawns.Length; i++)
                    PhotonNetwork.InstantiateRoomObject("DoppelChaser", spawns[i], Quaternion.identity);
            }
        }
    }
}
