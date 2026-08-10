using System.Collections;
using DoppelgangerVillage.Dialogue;
using DoppelgangerVillage.UI;
using DoppelgangerVillage.Village;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DoppelgangerVillage.Player
{
    /// <summary>
    /// 로컬 플레이어의 상호작용 — E 또는 좌클릭(시점 잠금 중) 하나로:
    /// 동물 대화 / 문 두드리기·집 출입 / 트레일러 출입 / 드랍 줍기 / 구조 주민 말 걸기.
    /// 감염자는 상호작용 대신 생존자 습격만 가능 (기획 감염자 플레이).
    /// </summary>
    public class PlayerInteraction : MonoBehaviourPun
    {
        private const float InteractRadius = 3.0f;

        private static readonly Collider[] _buffer = new Collider[24];

        private bool _knocking;
        private float _attackCooldown;

        private void Update()
        {
            if (!photonView.IsMine) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            // E 또는 좌클릭(시점 잠금 중) = 상호작용 (기획 조작표)
            bool pressed = (kb != null && kb.eKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked);

            // 대화가 열려 있거나 방금 닫혔으면 무시 — 판정 E가 곧바로 집 출입 등으로 새는 버그 방지
            if (DialogueUI.IsOpen || Time.unscaledTime - DialogueUI.LastClosedAt < 0.35f)
            {
                InteractionHint.Show(null);
                return;
            }

            var local = PlayerController.Local;
            if (local != null && local.IsInfected)
            {
                UpdateInfected(pressed);
                return;
            }

            Vector3 pos = transform.position;
            bool inTrailer = TrailerInterior.Contains(pos);
            bool inHouse = HouseInteriors.Contains(pos);

            var target = FindNearestCitizen(resolved: false);
            var rescued = inTrailer ? FindNearestCitizen(resolved: true) : null;
            var pickup = FindNearestPickup();
            bool nearTrailerDoor = !inTrailer && !inHouse
                && Vector3.Distance(pos, TrailerInterior.VillageDoor) < 3.0f;
            bool nearTrailerExit = inTrailer
                && Vector3.Distance(pos, TrailerInterior.InteriorExit) < 3.0f;
            Vector3 houseSpawn = Vector3.zero, houseExit = Vector3.zero;
            bool nearHouseDoor = !inTrailer && !inHouse
                && HouseInteriors.TryNearestDoor(pos, out houseSpawn);
            bool nearHouseExit = inHouse && HouseInteriors.TryExit(pos, out houseExit);

            string hint = null;
            if (_knocking) hint = "...";
            else if (target != null) hint = "E — 대화하기";
            else if (pickup != null) hint = "E — 줍기";
            else if (rescued != null) hint = "E — 말 걸기";
            else if (nearTrailerDoor) hint = "E — 트레일러 안으로";
            else if (nearHouseDoor) hint = "E — 문 두드리기";
            else if (nearHouseExit || nearTrailerExit) hint = "E — 밖으로 나가기";
            InteractionHint.Show(hint);

            if (!pressed || _knocking) return;

            if (target != null) { DialogueUI.Instance.Open(target); return; }
            if (pickup != null)
            {
                if (Judgement.JudgementDirector.Instance != null)
                    Judgement.JudgementDirector.Instance.RequestCollect(pickup.CitizenId);
                return;
            }
            if (rescued != null) { RescuedChatter.Talk(rescued); return; }
            if (nearTrailerDoor) { Teleport(TrailerInterior.EntrySpawn); return; }
            if (nearHouseDoor) { StartCoroutine(KnockAndEnter(houseSpawn)); return; }
            if (nearHouseExit) { Teleport(houseExit); return; }
            if (nearTrailerExit) Teleport(TrailerInterior.ExitToVillage);
        }

        /// <summary>기획 조작표의 '문 두드리기' — 노크 후 문이 열리며 들어간다.</summary>
        private IEnumerator KnockAndEnter(Vector3 interiorSpawn)
        {
            _knocking = true;
            SfxDirector.Play("knock", 0.8f);
            yield return new WaitForSeconds(0.75f);
            Teleport(interiorSpawn);
            ToastUI.Show("문이 스르르 열렸다...");
            _knocking = false;
        }

        /// <summary>감염자: 근처 생존자를 습격한다 (느린 이동·습격 쿨다운).</summary>
        private void UpdateInfected(bool pressed)
        {
            _attackCooldown -= Time.deltaTime;
            PlayerController victim = null;
            float best = GameConfig.InfectedAttackRange * GameConfig.InfectedAttackRange;
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (p == PlayerController.Local || p.photonView.IsMine) continue;
                if (p.photonView.Owner != null && p.photonView.Owner.CustomProperties.ContainsKey("infected")) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; victim = p; }
            }
            InteractionHint.Show(victim != null && _attackCooldown <= 0f ? "E — 습격하기" : null);
            if (!pressed || victim == null || _attackCooldown > 0f) return;
            _attackCooldown = GameConfig.InfectedAttackCooldown;
            SfxDirector.Play("hit", 0.7f);
            victim.photonView.RPC("RpcAttacked", victim.photonView.Owner, GameConfig.InfectedAttackDamage);
        }

        /// <summary>내부 룸 출입 (CC 껐다 켜며 순간이동 — 위치는 PhotonTransformView가 동기화).</summary>
        private void Teleport(Vector3 to)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = to;
            if (cc != null) cc.enabled = true;
        }

        private AnimalCitizen FindNearestCitizen(bool resolved)
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, InteractRadius, _buffer);
            AnimalCitizen best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = _buffer[i].GetComponentInParent<AnimalCitizen>();
                if (c == null || c.IsResolved != resolved) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        private PickupMarker FindNearestPickup()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, 2.6f, _buffer);
            PickupMarker best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var m = _buffer[i].GetComponentInParent<PickupMarker>();
                if (m == null) continue;
                float d = (m.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = m;
                }
            }
            return best;
        }
    }
}
