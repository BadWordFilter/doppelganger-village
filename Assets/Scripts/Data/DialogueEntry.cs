using System;

namespace DoppelgangerVillage.Data
{
    /// <summary>
    /// dialogue.json의 대화 항목 1개. 기획서 대화 테이블(1~67)과 1:1 대응.
    /// </summary>
    [Serializable]
    public class DialogueEntry
    {
        public int id;
        public string animal;       // "강아지" | "고양이" | "토끼"
        public string type;         // "일상" | "핵심"
        public string question;     // 플레이어의 질문 (선택지)
        public string normalAnswer; // 시민의 답변 (정상)
        public string doppelAnswer; // 도플갱어의 답변 (이상 징후)

        /// <summary>핵심 질문 여부. 일상 질문에는 도플갱어가 100% 정상적으로 거짓말한다.</summary>
        public bool IsCore => type == "핵심";

        /// <summary>괄호로 시작하는 연출 지문형 답변 여부. UI에서 지문 스타일로 구분 표시한다.</summary>
        public static bool IsStageDirection(string answer) =>
            !string.IsNullOrEmpty(answer) && answer.StartsWith("(");
    }
}
