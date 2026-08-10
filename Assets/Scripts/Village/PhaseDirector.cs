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

        private bool _startPending;

        /// <summary>마스터 전용: 게임 시작 시 1일차 낮 개시. PhotonNetwork.Time 서버 동기화를 기다린 뒤 시작한다.</summary>
        public void StartCycleIfMaster()
        {
            if (!PhotonNetwork.IsMasterClient || _cycleRunning || _startPending) return;
            _startPending = true;
            StartCoroutine(StartAfterTimeSync());
        }

        private System.Collections.IEnumerator StartAfterTimeSync()
        {
            yield return new WaitForSeconds(1.5f); // 입장 직후 서버 시계 동기화 대기 (로컬→서버 시간 점프 레이스 방지)
            photonView.RPC(nameof(RpcBeginDay), RpcTarget.AllBuffered, 1, PhotonNetwork.Time + GameConfig.DayDurationSeconds);
        }

        /// <summary>버퍼 재생(늦은 합류)으로 도착한 과거 이벤트인지 — 상태만 적용하고 연출은 생략한다.</summary>
        private static bool IsStale(PhotonMessageInfo info) => PhotonNetwork.Time - info.SentServerTime > 8.0;

        /// <summary>수신한 종료 시각이 내 시계 기준으로 말이 안 되면(과거이거나 지나치게 미래) 로컬 재계산으로 자가 보정.</summary>
        private double SanitizeEndTime(double endTime, float duration)
        {
            double now = PhotonNetwork.Time;
            if (endTime <= now + 1.0 || endTime > now + duration + 30.0)
                return now + duration;
            return endTime;
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
                photonView.RPC(nameof(RpcBeginNight), RpcTarget.AllBuffered, PhotonNetwork.Time + GameConfig.NightDurationSeconds);
            else
                photonView.RPC(nameof(RpcBeginDay), RpcTarget.AllBuffered, DayNumber + 1, PhotonNetwork.Time + GameConfig.DayDurationSeconds);
        }

        [PunRPC]
        private void RpcBeginDay(int day, double endTime, PhotonMessageInfo info)
        {
            bool stale = IsStale(info);
            IsNight = false;
            DayNumber = day;
            _phaseEndTime = SanitizeEndTime(endTime, GameConfig.DayDurationSeconds);
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

            // 주간 시민 기상, 야행성은 잠들기 (외곽 링 시민은 구역 확장 전까지 잠재운다)
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.IsResolved) continue;
                bool outerLocked = c.CitizenId >= GameConfig.OuterIdStart && !FogBoundary.Expanded;
                c.gameObject.SetActive(!c.IsNocturnal && !outerLocked);
            }

            // 밤 배회 도플갱어 소멸 (마스터)
            if (PhotonNetwork.IsMasterClient)
            {
                foreach (var chaser in FindObjectsByType<DoppelChaser>(FindObjectsSortMode.None))
                    PhotonNetwork.Destroy(chaser.gameObject);
            }

            // 일차별 구역 해금 (기획 5절): 안개 경계가 바깥으로 물러나며 구역이 넓어진다
            if (day == 1)
            {
                LockCommercialZone();
                FogBoundary.SetRadius(FogBoundary.Day1Radius, false, this);
            }
            else
            {
                UnlockCommercialZone(day == 2 && !stale);
                FogBoundary.SetRadius(FogBoundary.CurrentMaxRadius, !stale, this);
            }

            UI.SfxDirector.PlayAmbient(false); // 낮 앰비언트
            if (day > 1 && !stale)
            {
                UI.ToastUI.Show($"{day}일차 아침이 밝았다. 아직 구조를 기다리는 주민들이 있다.");
                if (PhotonNetwork.IsMasterClient && Quest.QuestDirector.Instance != null)
                    Quest.QuestDirector.Instance.ReassignAllForNewDay(); // 일차별 과제 갱신
            }
        }

        private static bool IsCommercialSpecies(string type) => type == "돼지" || type == "곰" || type == "양";

        /// <summary>1일차: 경계 밖 상업·의료 구역 동물 비활성 (구역 자체는 FogBoundary가 막는다).</summary>
        private void LockCommercialZone()
        {
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!c.IsNocturnal && !c.IsResolved && IsCommercialSpecies(c.AnimalType))
                    c.gameObject.SetActive(false);
        }

        /// <summary>2일차 아침: 경계가 물러나며 상업·의료 구역 해금.</summary>
        private void UnlockCommercialZone(bool announce)
        {
            // 시민 활성화는 RpcBeginDay의 공통 루프가 이미 처리
            if (announce)
                UI.ToastUI.Show("안개 경계가 멀리 물러난다... 마을이 넓어지고, 낯선 주민들이 보인다.");
        }

        [PunRPC]
        private void RpcBeginNight(double endTime, PhotonMessageInfo info)
        {
            bool stale = IsStale(info);
            IsNight = true;
            _phaseEndTime = SanitizeEndTime(endTime, GameConfig.NightDurationSeconds);
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
            RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.10f);
            RenderSettings.fogDensity = 0.055f; // 기획: 밤에는 시야가 크게 좁아진다
            RenderSettings.ambientIntensity = 0.22f;

            // 주간 시민은 집으로(잠들기), 야행성 기상
            foreach (var c in FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.IsResolved) continue;
                c.gameObject.SetActive(c.IsNocturnal);
            }

            // 밤: 경계가 열린다 — 야행성 구역까지 나갈 수 있지만, 놈들도 배회한다
            FogBoundary.SetRadius(FogBoundary.CurrentMaxRadius, !stale, this);
            UI.SfxDirector.PlayAmbient(true); // 밤 앰비언트
            if (!stale) UI.ToastUI.Show("밤이 찾아왔다... 경계가 열리고, 야행성 주민과 놈들이 깨어난다.");

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
