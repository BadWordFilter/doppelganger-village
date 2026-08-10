using DoppelgangerVillage.Dialogue;
using DoppelgangerVillage.UI;
using DoppelgangerVillage.Village;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DoppelgangerVillage.Player
{
    /// <summary>
    /// 로컬 플레이어의 상호작용: 근처의 판정 전 동물 대화, 집·트레일러 출입,
    /// 트레일러 안 구조 주민 말 걸기 — 전부 E 하나로.
    /// </summary>
    public class PlayerInteraction : MonoBehaviourPun
    {
        private const float InteractRadius = 3.0f;

        private static readonly Collider[] _buffer = new Collider[16];

        private void Update()
        {
            if (!photonView.IsMine) return;

            Vector3 pos = transform.position;
            bool inTrailer = TrailerInterior.Contains(pos);
            bool inHouse = HouseInteriors.Contains(pos);

            var target = FindNearestCitizen(resolved: false);
            var rescued = inTrailer ? FindNearestCitizen(resolved: true) : null;
            bool nearTrailerDoor = !inTrailer && !inHouse
                && Vector3.Distance(pos, TrailerInterior.VillageDoor) < 3.0f;
            bool nearTrailerExit = inTrailer
                && Vector3.Distance(pos, TrailerInterior.InteriorExit) < 3.0f;
            Vector3 houseSpawn = Vector3.zero, houseExit = Vector3.zero;
            bool nearHouseDoor = !inTrailer && !inHouse
                && HouseInteriors.TryNearestDoor(pos, out houseSpawn);
            bool nearHouseExit = inHouse && HouseInteriors.TryExit(pos, out houseExit);

            string hint = null;
            if (!DialogueUI.IsOpen)
            {
                if (target != null) hint = "E — 대화하기";
                else if (rescued != null) hint = "E — 말 걸기";
                else if (nearTrailerDoor) hint = "E — 트레일러 안으로";
                else if (nearHouseDoor) hint = "E — 집으로 들어가기";
                else if (nearHouseExit || nearTrailerExit) hint = "E — 밖으로 나가기";
            }
            InteractionHint.Show(hint);

            if (DialogueUI.IsOpen) return;
            var kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;

            if (target != null) { DialogueUI.Instance.Open(target); return; }
            if (rescued != null) { RescuedChatter.Talk(rescued); return; }
            if (nearTrailerDoor) { Teleport(TrailerInterior.EntrySpawn); return; }
            if (nearHouseDoor) { Teleport(houseSpawn); return; }
            if (nearHouseExit) { Teleport(houseExit); return; }
            if (nearTrailerExit) Teleport(TrailerInterior.ExitToVillage);
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
    }
}
