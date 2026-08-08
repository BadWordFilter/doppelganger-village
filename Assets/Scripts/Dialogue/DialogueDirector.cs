using System;
using System.Collections.Generic;
using System.Linq;
using DoppelgangerVillage.Data;
using DoppelgangerVillage.Village;
using Photon.Pun;
using UnityEngine;

namespace DoppelgangerVillage.Dialogue
{
    /// <summary>
    /// 대화 판정 허브. 질문 요청은 마스터 클라이언트가 받아 확률을 굴리고
    /// 결과를 전 클라이언트에 브로드캐스트한다 (전원 동일 결과 보장 — 멀티 아키텍처 규칙).
    /// 질문 횟수는 동물 1마리당 전 플레이어 공유 3회. 4번째 시도 = 과잉 심문.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class DialogueDirector : MonoBehaviourPun
    {
        public static DialogueDirector Instance { get; private set; }

        [SerializeField] private TextAsset dialogueJson; // Assets/Data/dialogue.json 참조

        public DialogueDatabase Database { get; private set; }

        /// <summary>질문 결과 브로드캐스트: (citizenId, entry, 이상답변 여부, 누적 질문 수, 요청 액터)</summary>
        public event Action<int, DialogueEntry, bool, int, int> QuestionAnswered;

        /// <summary>과잉 심문 발생: (citizenId, 대상 액터)</summary>
        public event Action<int, int> OverInterrogated;

        private Dictionary<int, AnimalCitizen> _citizens;

        private void Awake()
        {
            Instance = this;
            Database = DialogueDatabase.Load(dialogueJson);
        }

        private AnimalCitizen Citizen(int id)
        {
            if (_citizens == null)
                _citizens = FindObjectsByType<AnimalCitizen>(FindObjectsSortMode.None).ToDictionary(c => c.CitizenId);
            return _citizens.TryGetValue(id, out var c) ? c : null;
        }

        /// <summary>해당 동물에게 낼 질문 선택지 3개 (일상/핵심 혼합 — 핵심 최소 1개 보장).</summary>
        public List<DialogueEntry> PickChoices(AnimalCitizen citizen)
        {
            var pool = Database.ForAnimal(citizen.AnimalType).ToList();
            for (int i = 0; i < pool.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, pool.Count);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            var picked = pool.Take(GameConfig.ChoicesPerRound).ToList();
            if (!picked.Any(e => e.IsCore))
            {
                var core = pool.Skip(GameConfig.ChoicesPerRound).FirstOrDefault(e => e.IsCore);
                if (core != null) picked[picked.Count - 1] = core;
            }
            return picked;
        }

        /// <summary>로컬 → 마스터에게 질문 요청.</summary>
        public void Ask(AnimalCitizen citizen, DialogueEntry entry)
        {
            photonView.RPC(nameof(RpcAskQuestion), RpcTarget.MasterClient,
                citizen.CitizenId, entry.id, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [PunRPC]
        private void RpcAskQuestion(int citizenId, int entryId, int actorNumber)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            var citizen = Citizen(citizenId);
            var entry = Database.ById(entryId);
            if (citizen == null || entry == null || citizen.IsResolved) return;

            // 4번째 질문 시도 = 과잉 심문: 도플갱어가 눈치채고 돌변
            if (citizen.QuestionsAsked >= GameConfig.MaxQuestionsPerAnimal)
            {
                photonView.RPC(nameof(RpcOverInterrogation), RpcTarget.All, citizenId, actorNumber);
                return;
            }

            // 확률 굴림: 일상 질문엔 도플갱어도 100% 정상 거짓말. 핵심 질문만 차수별 노출.
            bool abnormal = false;
            if (entry.IsCore && citizen.IsDoppelganger)
            {
                float chance = GameConfig.DoppelRevealChanceByQuestion[
                    Mathf.Clamp(citizen.QuestionsAsked, 0, GameConfig.DoppelRevealChanceByQuestion.Length - 1)];
                abnormal = UnityEngine.Random.value < chance;
            }

            photonView.RPC(nameof(RpcQuestionResult), RpcTarget.All,
                citizenId, entryId, abnormal, citizen.QuestionsAsked + 1, actorNumber);
        }

        [PunRPC]
        private void RpcQuestionResult(int citizenId, int entryId, bool abnormal, int newCount, int actorNumber)
        {
            var citizen = Citizen(citizenId);
            var entry = Database.ById(entryId);
            if (citizen == null || entry == null) return;
            citizen.QuestionsAsked = newCount;
            QuestionAnswered?.Invoke(citizenId, entry, abnormal, newCount, actorNumber);
        }

        [PunRPC]
        private void RpcOverInterrogation(int citizenId, int actorNumber)
        {
            Debug.Log($"[Dialogue] 과잉 심문! 시민 {citizenId} 이(가) 돌변하여 액터 {actorNumber} 을(를) 공격");
            OverInterrogated?.Invoke(citizenId, actorNumber);

            var citizen = Citizen(citizenId);
            if (citizen == null) return;
            StartCoroutine(HideAfter(citizen, 1.3f));
            // 돌변 → 추격자 스폰 (마스터 소유 룸 오브젝트, 전 클라이언트 위치 동기화)
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.InstantiateRoomObject("DoppelChaser", citizen.transform.position, citizen.transform.rotation);
        }

        private System.Collections.IEnumerator HideAfter(AnimalCitizen citizen, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (citizen != null) citizen.gameObject.SetActive(false);
        }
    }
}
