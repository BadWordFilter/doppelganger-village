using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 대사 텍스트 속 울음소리·행동 묘사를 실제 사운드와 모션으로 공연한다.
    /// 텍스트는 자막 역할로 유지 — 판별 기믹이 문장 정독에 의존하기 때문.
    /// 소리는 로컬 플레이어와의 거리로 감쇠 (가까이 있는 플레이어에게만 들림).
    /// </summary>
    public static class AnimalPerformance
    {
        /// <summary>종별 울음 재생 (거리 감쇠).</summary>
        public static void Cry(AnimalCitizen citizen, float baseVolume = 0.8f)
        {
            if (citizen == null) return;
            float vol = AttenuatedVolume(citizen, baseVolume);
            if (vol <= 0.02f) return;
            UI.SfxDirector.PlayCry(citizen.AnimalType, vol);
        }

        /// <summary>공포 SFX 재생 (거리 감쇠).</summary>
        public static void Horror(AnimalCitizen citizen, string clip, float baseVolume = 0.75f)
        {
            if (citizen == null) return;
            float vol = AttenuatedVolume(citizen, baseVolume);
            if (vol > 0.02f) UI.SfxDirector.Play(clip, vol);
        }

        private static float AttenuatedVolume(AnimalCitizen citizen, float baseVolume)
        {
            var local = Player.PlayerController.Local;
            if (local == null) return 0f;
            float dist = Vector3.Distance(local.transform.position, citizen.transform.position);
            return baseVolume * Mathf.Clamp01(1f - (dist - 3f) / 12f);
        }

        /// <summary>
        /// 답변 공연: 의성어 → 실제 울음, 공포 소리 묘사 → 실제 SFX, 행동 묘사 → 실제 모션.
        /// 모든 클라이언트에서 호출된다 (질문 RPC 이벤트 경유).
        /// </summary>
        public static void Perform(AnimalCitizen citizen, string text, bool abnormal)
        {
            if (citizen == null || string.IsNullOrEmpty(text)) return;

            // 1) 울음 의성어 → 종별 실제 울음
            if (ContainsAny(text, "멍멍", "왈왈", "컹", "야옹", "냐옹", "골골", "그르릉", "꿀꿀", "메에", "매애",
                    "아우", "하울", "부엉", "끼익", "찍찍", "짖"))
                Cry(citizen, abnormal ? 0.55f : 0.85f);

            // 2) 공포 소리 묘사 → 실제 SFX (이상 답변에서만)
            if (abnormal)
            {
                if (ContainsAny(text, "비명", "지른다", "절규")) Horror(citizen, "scream");
                else if (ContainsAny(text, "뼈", "부러지", "으깨", "꺾이", "우두둑")) Horror(citizen, "crack");
                else if (ContainsAny(text, "웃음", "웃고")) Horror(citizen, "laugh");
                else if (ContainsAny(text, "기계", "사이렌", "파열음")) Horror(citizen, "sting", 0.45f);
            }

            // 3) 행동 묘사 → 순한 실제 모션 (정상 답변에서만 — 이상 답변은 StageDirectionActor 공포 모션 몫)
            if (abnormal || citizen.IsActing) return;
            if (ContainsAny(text, "폴짝", "깡충", "껑충", "뛰어", "뛰다", "뛰면"))
                StageDirectionActor.PlayCute(citizen, StageDirectionActor.CuteMotion.Hop);
            else if (text.Contains("꼬리"))
                StageDirectionActor.PlayCute(citizen, StageDirectionActor.CuteMotion.Wag);
            else if (ContainsAny(text, "갸웃", "갸우뚱", "기울이"))
                StageDirectionActor.PlayCute(citizen, StageDirectionActor.CuteMotion.Tilt);
            else if (text.Contains("끄덕"))
                StageDirectionActor.PlayCute(citizen, StageDirectionActor.CuteMotion.Nod);
            else if (ContainsAny(text, "기지개", "하품"))
                StageDirectionActor.PlayCute(citizen, StageDirectionActor.CuteMotion.Stretch);
            else if (ContainsAny(text, "날개", "파닥", "푸드덕"))
                StageDirectionActor.PlayCute(citizen, StageDirectionActor.CuteMotion.Flutter);
        }

        private static bool ContainsAny(string text, params string[] keys)
        {
            foreach (var k in keys)
                if (text.Contains(k)) return true;
            return false;
        }
    }
}
