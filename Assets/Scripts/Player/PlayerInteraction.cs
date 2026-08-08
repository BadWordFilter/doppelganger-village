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
            InteractionHint.Show(target != null && !DialogueUI.IsOpen ? "E — 대화하기" : null);

            if (DialogueUI.IsOpen) return;
            var kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame || target == null) return;
            DialogueUI.Instance.Open(target);
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
