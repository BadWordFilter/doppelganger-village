using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 마을 동물 시민 1마리. 씬에 정적 배치되며, 도플갱어 여부는
    /// 마스터가 배정한 룸 프로퍼티(VillageDirector)를 통해 전 클라이언트 동일하게 판정된다.
    /// </summary>
    public class AnimalCitizen : MonoBehaviour
    {
        [SerializeField] private int citizenId;
        [SerializeField] private string animalType; // "강아지" | "고양이" | "토끼"

        public int CitizenId => citizenId;
        public string AnimalType => animalType;

        /// <summary>이 시민이 도플갱어인지 (마스터 배정 룸 프로퍼티 기준).</summary>
        public bool IsDoppelganger => VillageDirector.IsDoppelganger(citizenId);

        /// <summary>판정 완료 여부 (구출/퇴치/도주 후 재상호작용 방지).</summary>
        public bool IsResolved { get; set; }

        /// <summary>남은 질문 횟수 관리는 대화 시스템(3단계)에서 처리한다.</summary>
        public int QuestionsAsked { get; set; }

        public void Configure(int id, string type)
        {
            citizenId = id;
            animalType = type;
        }
    }
}
