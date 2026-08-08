namespace DoppelgangerVillage
{
    /// <summary>
    /// 승리 수치·확률 테이블·도플갱어 수 등 튜닝 상수 (CLAUDE.md 데이터 규칙).
    /// 마스터 클라이언트가 이 값으로 모든 판정을 굴린다.
    /// </summary>
    public static class GameConfig
    {
        // ---- 승리 조건 ----
        public const int RescueGoal = 4; // 구출해야 하는 진짜 주민 수
        public const int PartsGoal = 4;  // 모아야 하는 트레일러 수리 부품 수

        // ---- 대화 규칙 ----
        public const int MaxQuestionsPerAnimal = 3; // 마리당 질문 제한. 4번째 시도 = 과잉 심문으로 돌변
        public const int ChoicesPerRound = 3;       // 대화 UI에 제시하는 질문 선택지 수 (일상/핵심 혼합)

        /// <summary>
        /// 질문 차수별(1~3번째) 도플갱어의 이상 답변 노출 확률.
        /// 핵심 질문에만 적용 — 일상 질문에는 도플갱어가 100% 정상적으로 거짓말한다.
        /// </summary>
        public static readonly float[] DoppelRevealChanceByQuestion = { 0.10f, 0.30f, 0.60f };

        // ---- 판정 드랍 (진짜 주민 구출 시 — 드랍 없음도 있어 도플갱어가 통계로 안 들키게) ----
        public const float PartDropChance = 0.65f;   // 수리 부품
        public const float MedkitDropChance = 0.10f; // 구급상자 (HP 회복)
        public const float FoodDropChance = 0.10f;   // 식량 (연출용) — 나머지 15%는 드랍 없음
        public const float MedkitHeal = 30f;

        // ---- 배정 ----
        public const int MinDoppelgangers = 3;  // 게임 시작 시 마스터가 랜덤 배정
        public const int MaxDoppelgangers = 4;
        public const int MinAnimals = 10;       // 맵에 등장하는 주간 동물 개체 수 범위
        public const int MaxAnimals = 11;

        // ---- 플레이어 ----
        public const float MaxHp = 100f;
        public const float OverInterrogationDamage = 34f; // 과잉 심문 공격 1회 피해
        public const float MaxStamina = 100f;
        public const float StaminaDrainPerSec = 20f;   // Shift 달리기 소모
        public const float StaminaRegenPerSec = 12f;   // 정지·걷기 시 회복
        public const float WalkSpeed = 4f;
        public const float RunSpeed = 7f;
        public const float ExhaustedSpeed = 2.5f;      // 스태미나 고갈 시 감속
    }
}
