using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DoppelgangerVillage.Village;
using Photon.Pun;
using UnityEngine;

namespace DoppelgangerVillage.Judgement
{
    /// <summary>
    /// 판정(구출/퇴치)·드랍·수리 게이지·정산·승패의 마스터 권한 허브.
    /// 판정 직후 정답 여부를 노출하지 않는다 — 도플갱어를 보내도 겉보기는 동일하게 트레일러로 걸어가며,
    /// 정산(해질녘) 때 잠입이 공개되고 구출 주민 수가 차감된다 (기획 규칙).
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class JudgementDirector : MonoBehaviourPun
    {
        public static JudgementDirector Instance { get; private set; }

        public struct Settlement
        {
            public int trueRescued, infiltrators, finalRescued, parts, banished, fled, remaining;
            public int outcome; // 0 계속 / 1 승리(탈출) / 2 패배
        }

        /// <summary>(표시용 구출 수, 부품 수) — 잠입 도플갱어 포함 표시 (플레이어는 알 수 없으므로)</summary>
        public event System.Action<int, int> ProgressChanged;
        public event System.Action<Settlement> SettlementShown;

        public bool GameEnded { get; private set; }

        // ---- 마스터 전용 상태 ----
        private int _trueRescued, _infiltrators, _parts, _banished, _fled;
        private readonly HashSet<int> _infected = new();

        private Vector3 _trailerTarget;

        private void Awake()
        {
            Instance = this;
            var zone = GameObject.Find("SafeZoneMarker");
            _trailerTarget = zone != null ? zone.transform.position : new Vector3(0f, 0f, -14.5f);
        }

        private void Start()
        {
            var dialogue = GetComponent<UI.DialogueUI>();
            if (dialogue != null) dialogue.VerdictChosen += OnVerdictChosen;
        }

        private void OnVerdictChosen(AnimalCitizen citizen, bool mirror)
        {
            photonView.RPC(nameof(RpcRequestVerdict), RpcTarget.MasterClient,
                citizen.CitizenId, mirror, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        private int DisplayRescued => _trueRescued + _infiltrators;

        [PunRPC]
        private void RpcRequestVerdict(int citizenId, bool mirror, int actorNumber)
        {
            if (!PhotonNetwork.IsMasterClient || GameEnded) return;
            var citizen = FindCitizen(citizenId);
            if (citizen == null || citizen.IsResolved) return;

            bool isDoppel = citizen.IsDoppelganger;
            if (!mirror)
            {
                // <트레일러로 이동하세요> — 정답이든 오답이든 겉보기는 동일
                string drop = "";
                if (!isDoppel)
                {
                    _trueRescued++;
                    if (citizen.IsNocturnal)
                    {
                        drop = "part"; // 야행성 시민 구출 성공 = 수리 부품 확정 지급 (기획 규칙)
                        _parts++;
                    }
                    else
                    {
                        float r = Random.value;
                        if (r < GameConfig.PartDropChance) { drop = "part"; _parts++; }
                        else if (r < GameConfig.PartDropChance + GameConfig.MedkitDropChance) drop = "medkit";
                        else if (r < GameConfig.PartDropChance + GameConfig.MedkitDropChance + GameConfig.FoodDropChance) drop = "food";
                    }
                }
                else
                {
                    _infiltrators++; // 오답 — 정산 때 차감
                }
                photonView.RPC(nameof(RpcApplyVerdict), RpcTarget.All, citizenId, 0, drop, actorNumber, DisplayRescued, _parts);
            }
            else
            {
                // <눈을 감고 거울 비추기>
                int kind;
                if (isDoppel) { _banished++; kind = 1; } // 퇴치 성공
                else { _fled++; kind = 2; }              // 진짜에게 거울 — 겁먹고 도주 (구출 실패)
                photonView.RPC(nameof(RpcApplyVerdict), RpcTarget.All, citizenId, kind, "", actorNumber, DisplayRescued, _parts);
            }

            CheckPhase();
        }

        /// <summary>
        /// 마스터: 목표 달성(표시 기준) 또는 깨어 있는 동물 소진 시 정산 발동.
        /// 잠들어 있는 야행성이 남아 있으면 패배 대신 밤 페이즈로 이어진다.
        /// </summary>
        private void CheckPhase()
        {
            int awake = RemainingCitizens();          // 활성(깨어 있는) 미판정
            int all = RemainingCitizensIncludingSleeping();
            bool goalMet = DisplayRescued >= GameConfig.RescueGoal && _parts >= GameConfig.PartsGoal;
            if (!goalMet && awake > 0) return;

            int final = Mathf.Max(0, _trueRescued - _infiltrators);
            bool realGoal = final >= GameConfig.RescueGoal && _parts >= GameConfig.PartsGoal;
            int outcome = realGoal ? 1 : (all == 0 ? 2 : 0);

            photonView.RPC(nameof(RpcSettlement), RpcTarget.All,
                _trueRescued, _infiltrators, final, _parts, _banished, _fled, all, outcome);

            // 정산 차감 반영 후 계속 (기획: 잠입 도플 1마리당 구출 주민 -1)
            if (outcome == 0)
            {
                _trueRescued = final;
                _infiltrators = 0;
                // 깨어 있는 대상이 없고 야행성이 잠들어 있다면 → 밤 페이즈 개시
                if (awake == 0 && all > 0)
                    StartCoroutine(NightAfterSettlement());
            }
        }

        private IEnumerator NightAfterSettlement()
        {
            yield return new WaitForSeconds(4f); // 정산 화면 읽을 시간
            Village.PhaseDirector.Instance.BeginNight();
        }

        [PunRPC]
        private void RpcApplyVerdict(int citizenId, int kind, string drop, int actorNumber, int displayRescued, int parts)
        {
            var citizen = FindCitizen(citizenId);
            if (citizen == null) return;
            citizen.IsResolved = true;

            switch (kind)
            {
                case 0: StartCoroutine(WalkToTrailer(citizen)); break;
                case 1: StartCoroutine(FlashAndVanish(citizen)); break;
                case 2: StartCoroutine(FleeAndVanish(citizen)); break;
            }

            ProgressChanged?.Invoke(displayRescued, parts);

            if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber) return;
            switch (kind)
            {
                case 0:
                    if (drop == "part") UI.ToastUI.Show("수리 부품을 얻었다! 트레일러가 조금 더 온전해진다.");
                    else if (drop == "medkit")
                    {
                        UI.ToastUI.Show("구급상자를 얻었다! 상처를 치료했다.");
                        var local = Player.PlayerController.Local;
                        if (local != null) local.Heal(GameConfig.MedkitHeal);
                    }
                    else if (drop == "food") UI.ToastUI.Show("식량을 얻었다.");
                    else UI.ToastUI.Show("주민이 말없이 트레일러로 향했다...");
                    break;
                case 1: UI.ToastUI.Show("거울에 비친 것은... 도플갱어였다! 퇴치 성공."); break;
                case 2: UI.ToastUI.Show("진짜 주민이었다... 겁에 질려 도망쳐 버렸다."); break;
            }
        }

        [PunRPC]
        private void RpcSettlement(int trueRescued, int infiltrators, int finalRescued, int parts,
            int banished, int fled, int remaining, int outcome)
        {
            if (outcome != 0) GameEnded = true;
            var s = new Settlement
            {
                trueRescued = trueRescued,
                infiltrators = infiltrators,
                finalRescued = finalRescued,
                parts = parts,
                banished = banished,
                fled = fled,
                remaining = remaining,
                outcome = outcome,
            };
            ProgressChanged?.Invoke(finalRescued, parts);
            SettlementShown?.Invoke(s);
        }

        // ---- 감염(HP 0) → 전원 감염 시 패배 ----

        public void NotifyLocalInfected()
        {
            photonView.RPC(nameof(RpcInfected), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [PunRPC]
        private void RpcInfected(int actorNumber)
        {
            if (!PhotonNetwork.IsMasterClient || GameEnded) return;
            _infected.Add(actorNumber);
            if (_infected.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
            {
                int final = Mathf.Max(0, _trueRescued - _infiltrators);
                photonView.RPC(nameof(RpcSettlement), RpcTarget.All,
                    _trueRescued, _infiltrators, final, _parts, _banished, _fled, RemainingCitizens(), 2);
            }
        }

        // ---- 연출 코루틴 (전 클라이언트 동일 수행) ----

        private IEnumerator WalkToTrailer(AnimalCitizen citizen)
        {
            var tr = citizen.transform;
            Vector3 target = _trailerTarget;
            while ((tr.position - target).sqrMagnitude > 1.2f)
            {
                Vector3 dir = (target - tr.position).normalized;
                tr.position += dir * (2.2f * Time.deltaTime);
                tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
                yield return null;
            }
            citizen.gameObject.SetActive(false);
        }

        private IEnumerator FlashAndVanish(AnimalCitizen citizen)
        {
            foreach (var r in citizen.GetComponentsInChildren<MeshRenderer>())
                r.material.SetColor("_BaseColor", new Color(0.9f, 0.15f, 0.1f));
            yield return new WaitForSeconds(0.6f);
            citizen.gameObject.SetActive(false);
        }

        private IEnumerator FleeAndVanish(AnimalCitizen citizen)
        {
            var tr = citizen.transform;
            Vector3 dir = (tr.position - _trailerTarget).normalized;
            dir.y = 0f;
            float t = 0f;
            while (t < 3.5f)
            {
                t += Time.deltaTime;
                tr.position += dir * (7f * Time.deltaTime);
                tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                yield return null;
            }
            citizen.gameObject.SetActive(false);
        }

        // ---- 헬퍼 ----

        private static AnimalCitizen FindCitizen(int id)
        {
            foreach (var c in Object.FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.CitizenId == id) return c;
            return null;
        }

        private static int RemainingCitizens() =>
            Object.FindObjectsByType<AnimalCitizen>(FindObjectsSortMode.None).Count(c => !c.IsResolved);

        private static int RemainingCitizensIncludingSleeping() =>
            Object.FindObjectsByType<AnimalCitizen>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(c => !c.IsResolved);
    }
}
