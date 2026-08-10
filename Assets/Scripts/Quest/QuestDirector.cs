using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using PunPlayer = Photon.Realtime.Player;

namespace DoppelgangerVillage.Quest
{
    /// <summary>
    /// 개인 과제 시스템 (기획 6절): 플레이어마다 '특정 시민 구출'과 '전담 수리 부품'이 결합된 임무.
    /// 할당받은 동물을 구출하면 전담 부품이 확정적으로 드랍된다.
    /// 마스터가 입장 순서대로 배정하고 버퍼드 RPC로 전 클라이언트(늦은 합류 포함)에 공유한다.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class QuestDirector : MonoBehaviourPunCallbacks
    {
        public static QuestDirector Instance { get; private set; }

        public class Quest
        {
            public string species;
            public int targetCount;
            public string partName;
            public int progress;
            public bool Done => progress >= targetCount;
        }

        private static readonly string[] PartNames = { "엔진 오일", "타이어", "통신 안테나", "연료 펌프" };
        private static readonly string[] DaySpecies = { "강아지", "고양이", "토끼", "돼지", "곰", "양" };

        private readonly Dictionary<int, Quest> _quests = new(); // actorNumber → 과제 (전 클라이언트 동일)

        public event Action QuestsChanged;

        public Quest LocalQuest =>
            PhotonNetwork.InRoom && _quests.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out var q) ? q : null;

        private void Awake()
        {
            Instance = this;
        }

        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.IsMasterClient)
                AssignFor(PhotonNetwork.LocalPlayer.ActorNumber);
        }

        public override void OnPlayerEnteredRoom(PunPlayer newPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
                AssignFor(newPlayer.ActorNumber);
        }

        private void AssignFor(int actorNumber)
        {
            if (_quests.ContainsKey(actorNumber)) return;
            string species = DaySpecies[UnityEngine.Random.Range(0, DaySpecies.Length)];
            string part = PartNames[UnityEngine.Random.Range(0, PartNames.Length)];
            photonView.RPC(nameof(RpcAssign), RpcTarget.AllBuffered, actorNumber, species, 1, part);
        }

        [PunRPC]
        private void RpcAssign(int actorNumber, string species, int targetCount, string partName)
        {
            _quests[actorNumber] = new Quest { species = species, targetCount = targetCount, partName = partName };
            QuestsChanged?.Invoke();
        }

        /// <summary>
        /// 마스터 전용: 구출 성공 등록. 판정자의 전담 대상이면 진행도 갱신 + 전담 부품 확정 여부 반환.
        /// </summary>
        public bool RegisterRescueAndCheckDedicatedPart(int actorNumber, string species)
        {
            if (!PhotonNetwork.IsMasterClient) return false;
            if (!_quests.TryGetValue(actorNumber, out var q) || q.Done || q.species != species) return false;
            photonView.RPC(nameof(RpcProgress), RpcTarget.AllBuffered, actorNumber, q.progress + 1);
            return true; // 기획: 전담 동물 구출 = 전담 부품 확정 드랍
        }

        [PunRPC]
        private void RpcProgress(int actorNumber, int progress)
        {
            if (!_quests.TryGetValue(actorNumber, out var q)) return;
            q.progress = progress;
            QuestsChanged?.Invoke();
            if (PhotonNetwork.InRoom && actorNumber == PhotonNetwork.LocalPlayer.ActorNumber && q.Done)
            {
                UI.SfxDirector.Play("quest"); // 기획: 과제 달성 시 경쾌한 효과음
                UI.ToastUI.Show($"개인 과제 완수! 전담 부품 '{q.partName}' 확보 — 트레일러가 당신을 기억할 것이다.");
            }
        }
    }
}
