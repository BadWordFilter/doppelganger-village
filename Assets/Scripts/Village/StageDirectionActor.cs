using System.Collections;
using UnityEngine;

namespace DoppelgangerVillage.Village
{
    /// <summary>
    /// 연출 지문(괄호 답변)을 캐릭터의 실제 모션으로 재생한다.
    /// 지문 키워드로 모션 프로파일을 고르는 절차적 애니메이션 — 근처의 모든 플레이어에게 보인다.
    /// </summary>
    public static class StageDirectionActor
    {
        /// <summary>
        /// 도플갱어 본색 노출: 점 눈이 커지고 검붉게 물들며 얼굴이 무너진다 (레퍼런스의 공포 문법).
        /// 돌변(과잉 심문)·퇴치 연출에서 호출.
        /// </summary>
        /// <summary>이름으로 깊은 탐색 (Blender 임포트 모델은 눈이 Head 하위에 있을 수 있음).</summary>
        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>머티리얼 색 설정 — URP(_BaseColor)와 glTFast(baseColorFactor) 셰이더 모두 지원.</summary>
        public static void Tint(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", c);
            else m.color = c;
        }

        public static Color GetTint(Material m)
        {
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("baseColorFactor")) return m.GetColor("baseColorFactor");
            return m.color;
        }

        public static void DistortFace(AnimalCitizen citizen)
        {
            if (citizen == null) return;
            foreach (var name in new[] { "EyeL", "EyeR", "BigEyeL", "BigEyeR" })
            {
                var eye = FindDeep(citizen.transform, name);
                if (eye == null) continue;
                eye.localScale = new Vector3(eye.localScale.x * 2.4f, eye.localScale.y * 2.8f, eye.localScale.z);
                var r = eye.GetComponent<MeshRenderer>();
                if (r != null) Tint(r.material, new Color(0.35f, 0.02f, 0.02f));
            }
        }

        /// <summary>지문 텍스트에 맞는 기괴한 모션을 재생. abnormal=false면 순한 모션(고개 돌리기 등).</summary>
        public static void Play(AnimalCitizen citizen, string direction, bool abnormal)
        {
            if (citizen == null || !citizen.gameObject.activeInHierarchy) return;
            var host = Dialogue.DialogueDirector.Instance;
            if (host == null) return;
            host.StartCoroutine(Run(citizen, direction ?? "", abnormal));
        }

        private static IEnumerator Run(AnimalCitizen citizen, string text, bool abnormal)
        {
            citizen.IsActing = true;
            var root = citizen.transform;
            var head = FindDeep(root, "Head");
            Vector3 rootPos = root.localPosition;
            Quaternion rootRot = root.localRotation;
            Vector3 rootScale = root.localScale;
            Quaternion headRot = head != null ? head.localRotation : Quaternion.identity;

            if (!abnormal)
            {
                // 정상 지문: 자연스러운 고개 돌리기
                yield return RotateHead(head, 150f, 1.2f);
            }
            else if (Contains(text, "회전", "꺾이며", "뒤집히"))
            {
                // 목이 기괴하게 회전 (720도)
                yield return RotateHead(head, 720f, 1.8f);
            }
            else if (Contains(text, "기어", "벽을 타고", "거미"))
            {
                // 허리가 꺾인 채 기어다니는 자세
                yield return BendAndScuttle(root, rootPos, rootRot);
            }
            else if (Contains(text, "직립", "내려다본다", "일어"))
            {
                // 부자연스럽게 솟아올라 내려다봄
                yield return RiseUp(root, rootScale);
            }
            else if (Contains(text, "갈라지", "찢어지", "벌리"))
            {
                // 머리가 갈라질 듯 크게 벌어짐
                yield return SplitHead(head, root);
            }
            else
            {
                // 기본: 온몸 경련 + 고개 홱 돌아감
                yield return JitterAndSnap(root, head, rootPos);
            }

            // 원상 복구
            if (root != null)
            {
                root.localPosition = rootPos;
                root.localRotation = rootRot;
                root.localScale = rootScale;
            }
            if (head != null) head.localRotation = headRot;
            if (citizen != null) citizen.IsActing = false;
        }

        /// <summary>순한 일상 모션 (울음·행동 묘사가 대사에 있을 때 실제로 수행).</summary>
        public enum CuteMotion { Hop, Wag, Tilt, Nod, Stretch, Flutter }

        public static void PlayCute(AnimalCitizen citizen, CuteMotion motion)
        {
            if (citizen == null || !citizen.gameObject.activeInHierarchy || citizen.IsActing) return;
            var host = Dialogue.DialogueDirector.Instance;
            if (host == null) return;
            host.StartCoroutine(RunCute(citizen, motion));
        }

        private static IEnumerator RunCute(AnimalCitizen citizen, CuteMotion motion)
        {
            citizen.IsActing = true;
            var root = citizen.transform;
            var head = FindDeep(root, "Head");
            Vector3 basePos = root.localPosition;
            Quaternion baseRot = root.localRotation;
            Vector3 baseScale = root.localScale;
            Quaternion headRot = head != null ? head.localRotation : Quaternion.identity;

            float t = 0f;
            switch (motion)
            {
                case CuteMotion.Hop: // 폴짝폴짝 3회
                    for (int hop = 0; hop < 3; hop++)
                    {
                        t = 0f;
                        while (t < 0.3f)
                        {
                            t += Time.deltaTime;
                            root.localPosition = basePos + Vector3.up * (Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.3f)) * 0.32f);
                            yield return null;
                        }
                    }
                    break;
                case CuteMotion.Wag: // 꼬리 치듯 엉덩이 씰룩
                    while (t < 1.2f)
                    {
                        t += Time.deltaTime;
                        root.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * 18f) * 9f, 0f);
                        yield return null;
                    }
                    break;
                case CuteMotion.Tilt: // 고개 갸웃
                    if (head != null)
                    {
                        while (t < 0.4f)
                        {
                            t += Time.deltaTime;
                            head.localRotation = headRot * Quaternion.Euler(0f, 0f, 24f * Mathf.SmoothStep(0f, 1f, t / 0.4f));
                            yield return null;
                        }
                        yield return new WaitForSeconds(0.8f);
                    }
                    break;
                case CuteMotion.Nod: // 끄덕끄덕
                    if (head != null)
                    {
                        while (t < 1.0f)
                        {
                            t += Time.deltaTime;
                            head.localRotation = headRot * Quaternion.Euler(Mathf.Abs(Mathf.Sin(t * 6.28f)) * 22f, 0f, 0f);
                            yield return null;
                        }
                    }
                    break;
                case CuteMotion.Stretch: // 기지개
                    while (t < 1.1f)
                    {
                        t += Time.deltaTime;
                        float k = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.1f));
                        root.localScale = new Vector3(baseScale.x * (1f - 0.08f * k), baseScale.y * (1f + 0.18f * k), baseScale.z * (1f - 0.08f * k));
                        yield return null;
                    }
                    break;
                case CuteMotion.Flutter: // 날개 파닥 (박쥐·올빼미)
                    while (t < 1.0f)
                    {
                        t += Time.deltaTime;
                        root.localPosition = basePos + Vector3.up * (0.12f + Mathf.Sin(t * 40f) * 0.03f);
                        root.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 6f);
                        yield return null;
                    }
                    break;
            }

            if (root != null)
            {
                root.localPosition = basePos;
                root.localRotation = baseRot;
                root.localScale = baseScale;
            }
            if (head != null) head.localRotation = headRot;
            if (citizen != null) citizen.IsActing = false;
        }

        private static bool Contains(string text, params string[] keys)
        {
            foreach (var k in keys)
                if (text.Contains(k)) return true;
            return false;
        }

        private static IEnumerator RotateHead(Transform head, float degrees, float duration)
        {
            if (head == null) yield break;
            Quaternion start = head.localRotation;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, t / duration);
                head.localRotation = start * Quaternion.Euler(0f, degrees * eased, 0f);
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);
        }

        private static IEnumerator BendAndScuttle(Transform root, Vector3 basePos, Quaternion baseRot)
        {
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                root.localRotation = baseRot * Quaternion.Euler(-75f * (t / 0.6f), 0f, 0f);
                yield return null;
            }
            t = 0f;
            while (t < 1.8f)
            {
                t += Time.deltaTime;
                root.localPosition = basePos + new Vector3(Mathf.Sin(t * 18f) * 0.12f, Mathf.Abs(Mathf.Sin(t * 22f)) * 0.15f, 0f);
                yield return null;
            }
        }

        private static IEnumerator RiseUp(Transform root, Vector3 baseScale)
        {
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float k = 1f + 0.6f * Mathf.SmoothStep(0f, 1f, t / 1.4f);
                root.localScale = new Vector3(baseScale.x * (2f - k) * 0.9f + baseScale.x * 0.1f, baseScale.y * k, baseScale.z);
                yield return null;
            }
            yield return new WaitForSeconds(0.7f);
        }

        private static IEnumerator SplitHead(Transform head, Transform root)
        {
            if (head == null) yield break;
            Vector3 headPos = head.localPosition;
            float t = 0f;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                head.localPosition = headPos + new Vector3(Mathf.Sin(t * 40f) * 0.05f, 0.18f * Mathf.PingPong(t * 2f, 1f), 0f);
                head.localRotation = Quaternion.Euler(-35f * Mathf.PingPong(t * 2.5f, 1f), 0f, Mathf.Sin(t * 30f) * 8f);
                yield return null;
            }
            head.localPosition = headPos;
        }

        private static IEnumerator JitterAndSnap(Transform root, Transform head, Vector3 basePos)
        {
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                root.localPosition = basePos + (Vector3)(Random.insideUnitCircle * 0.05f);
                if (head != null && t > 0.7f)
                    head.localRotation = Quaternion.Euler(0f, 160f, 25f);
                yield return null;
            }
        }
    }
}
