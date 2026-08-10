using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 트레일러 안 구조 주민의 한마디 (E 말 걸기 — 대화 이어가기 없음).
    /// 잠입한 도플갱어(정체 노출 상태)는 섬뜩한 한마디를 한다.
    /// </summary>
    public static class RescuedChatter
    {
        private static readonly string[] Lines =
        {
            "구해줘서 정말 고마워요... 흑흑.",
            "무서웠어요. 그놈이 제 목소리로 말을 걸었어요.",
            "여기는 따뜻하네요. 바깥은... 아직 위험해요.",
            "아직 못 나온 친구들이 있어요. 부탁할게요.",
            "밖에 나가면 안 돼요. 놈들이 얼굴을 훔쳐가요.",
            "트레일러가 고쳐지면 정말 떠날 수 있는 거죠?",
            "옆집 애도 아직 마을에 있어요... 데려와 주세요.",
            "그놈들, 대화를 하다 보면 어딘가 이상해요. 꼭 기억하세요.",
            "거울이 무서워요. 하지만 놈들에겐 더 무섭겠죠.",
            "이제야 좀 숨을 쉴 수 있겠어요.",
            "제 이웃이 언제부턴가 밤에 안 자더라고요... 그게 시작이었어요.",
            "고마워요. 당신은 은인이에요.",
            "부품은 다 모였나요? 빨리 떠나고 싶어요.",
            "밤에는 절대 혼자 다니지 마세요.",
            "여기 있으면 안전한 거... 맞죠?",
            "아직도 심장이 두근거려요.",
            "그놈이 우리 집 침대에서 자고 있었어요. 제 모습으로요.",
            "구해주셔서... 말로 다 못 해요.",
            "남은 애들도 꼭... 꼭 부탁해요.",
            "바깥에서 소리가 들릴 때마다 소름이 돋아요.",
            "집에 두고 온 게 많지만... 목숨보다 소중하진 않죠.",
            "당신들이 와서 정말 다행이에요.",
        };

        /// <summary>한마디 말 걸기. 도플갱어는 침묵("......") 또는 폭소 둘 중 하나.</summary>
        public static void Talk(AnimalCitizen citizen)
        {
            if (citizen == null) return;
            if (citizen.IsDoppelganger)
            {
                bool silent = Random.value < 0.5f;
                if (silent)
                {
                    UI.ToastUI.Show($"{citizen.AnimalType}: “......”");
                }
                else
                {
                    UI.ToastUI.Show($"{citizen.AnimalType}: “히히... 히히히히... 히히히히히히!”");
                    AnimalPerformance.Horror(citizen, "laugh", 0.65f);
                }
                return;
            }
            string line = Lines[Random.Range(0, Lines.Length)];
            UI.ToastUI.Show($"{citizen.AnimalType}: “{line}”");
            AnimalPerformance.Cry(citizen, 0.5f);
            StageDirectionActor.PlayCute(citizen,
                Random.value < 0.5f ? StageDirectionActor.CuteMotion.Nod : StageDirectionActor.CuteMotion.Tilt);
        }
    }
}
