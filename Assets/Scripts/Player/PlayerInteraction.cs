using DoppelgangerVillage.Dialogue;
using DoppelgangerVillage.UI;
using DoppelgangerVillage.Village;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DoppelgangerVillage.Player
{
    /// <summary>
    /// 로컬 플레이어의 상호작용: 근처의 판정 전 동물을 감지해 E키로 대화를 연다.
    /// </summary>
    public class PlayerInteraction : MonoBehaviourPun
    {
        private const float InteractRadius = 3.0f;

        private static readonly Collider[] _buffer = new Collider[16];

        private void Update()
        {
            if (!photonView.IsMine) return;

            var target = FindNearestCitizen();
            bool nearDoor = !Village.TrailerInterior.Contains(transform.position)
                && Vector3.Distance(transform.position, Village.TrailerInterior.VillageDoor) < 3.0f;
            bool nearExit = Village.TrailerInterior.Contains(transform.position)
                && Vector3.Distance(transform.position, Village.TrailerInterior.InteriorExit) < 3.0f;

            string hint = null;
            if (!DialogueUI.IsOpen)
            {
                if (target != null) hint = "E — 대화하기";
                else if (nearDoor) hint = "E — 트레일러 안으로";
                else if (nearExit) hint = "E — 밖으로 나가기";
            }
            InteractionHint.Show(hint);

            if (DialogueUI.IsOpen) return;
            var kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;

            if (target != null) { DialogueUI.Instance.Open(target); return; }
            if (nearDoor) { Teleport(Village.TrailerInterior.EntrySpawn); return; }
            if (nearExit) Teleport(Village.TrailerInterior.ExitToVillage);
        }

        /// <summary>트레일러 내부 룸 출입 (CC 껐다 켜며 순간이동 — 위치는 PhotonTransformView가 동기화).</summary>
        private void Teleport(Vector3 to)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = to;
            if (cc != null) cc.enabled = true;
        }

        private AnimalCitizen FindNearestCitizen()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, InteractRadius, _buffer);
            AnimalCitizen best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = _buffer[i].GetComponentInParent<AnimalCitizen>();
                if (c == null || c.IsResolved) continue;
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
