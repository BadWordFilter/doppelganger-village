using System.Collections.Generic;
using UnityEngine;

namespace DoppelgangerVillage.UI
{
    /// <summary>
    /// 프로시저럴 효과음 — 외부 에셋 없이 코드로 합성한 사운드.
    /// dudung(정산 두둥), mirror(거울 섬광), hit(피격), quest(과제 완료 — 기획: "경쾌한 효과음"), sting(돌변).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxDirector : MonoBehaviour
    {
        private static SfxDirector _instance;
        private static readonly Dictionary<string, AudioClip> _clips = new();
        private AudioSource _source;

        private const int SR = 44100;

        private AudioSource _ambientSource;

        private void Awake()
        {
            _instance = this;
            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.55f;

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;
            _ambientSource.loop = true;
            _ambientSource.spatialBlend = 0f;
            _ambientSource.volume = 0.22f;

            StartCoroutine(HeartRoutine());
        }

        /// <summary>낮/밤 앰비언트 루프 전환 (코드 합성 — 낮: 온화한 패드+새소리, 밤: 저음 드론+바람).</summary>
        public static void PlayAmbient(bool night)
        {
            if (_instance == null) return;
            string key = night ? "amb_night" : "amb_day";
            if (!_clips.TryGetValue(key, out var clip))
            {
                var data = night ? AmbientNight() : AmbientDay();
                clip = AudioClip.Create(key, data.Length, 1, SR, false);
                clip.SetData(data, 0);
                _clips[key] = clip;
            }
            _instance.SetScares(night); // 밤에만 공포 원샷 루프
            if (_instance._ambientSource.clip == clip && _instance._ambientSource.isPlaying) return;
            _instance._ambientSource.clip = clip;
            _instance._ambientSource.Play();
        }

        // 낮: 장조 패드 + 드문 새소리 (8초 루프)
        private static float[] AmbientDay()
        {
            int n = SR * 8;
            var d = new float[n];
            var rng = new System.Random(11);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                d[i] = (Mathf.Sin(2f * Mathf.PI * 261.625f * t) + Mathf.Sin(2f * Mathf.PI * 329.75f * t) * 0.7f
                        + Mathf.Sin(2f * Mathf.PI * 392f * t) * 0.5f) * 0.05f * trem;
            }
            for (int c = 0; c < 5; c++) // 새소리 블립
            {
                int start = rng.Next(0, n - SR / 2);
                float f0 = 2100f + rng.Next(0, 900);
                for (int i = 0; i < SR / 7; i++)
                {
                    float t = i / (float)SR;
                    float env = Mathf.Sin(Mathf.PI * i / (SR / 7f));
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * (f0 + Mathf.Sin(t * 60f) * 250f) * t) * env * 0.09f;
                }
            }
            FadeEdges(d);
            return d;
        }

        // 밤: 불협 저음 드론 + 심장박동 + 바람 (8초 루프) — 공포 배경음
        private static float[] AmbientNight()
        {
            int n = SR * 8;
            var d = new float[n];
            var rng = new System.Random(13);
            float noise = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                noise = Mathf.Lerp(noise, (float)rng.NextDouble() * 2f - 1f, 0.02f); // 저역 필터 바람
                float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.125f * t);
                // 단2도 불협 클러스터 (50 + 52.8 + 106Hz)
                d[i] = (Mathf.Sin(2f * Mathf.PI * 50f * t) + Mathf.Sin(2f * Mathf.PI * 52.8f * t)
                        + Mathf.Sin(2f * Mathf.PI * 106f * t) * 0.4f) * 0.06f
                       + noise * 0.05f * swell;
            }
            // 심장박동 (2초 주기 lub-dub × 4)
            void Thump(float at, float amp)
            {
                int start = (int)(at * SR);
                int len = (int)(SR * 0.16f);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * 48f * t) * Mathf.Exp(-t * 22f) * amp;
                }
            }
            for (int b = 0; b < 4; b++)
            {
                Thump(b * 2f, 0.30f);
                Thump(b * 2f + 0.34f, 0.20f);
            }
            FadeEdges(d);
            return d;
        }

        // ---- 추격자 근접 심장박동 (거리 비례 가속) ----
        private float _danger;

        public static void SetDanger(float d01)
        {
            if (_instance != null) _instance._danger = Mathf.Clamp01(d01);
        }

        private System.Collections.IEnumerator HeartRoutine()
        {
            while (true)
            {
                if (_danger <= 0.01f)
                {
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }
                Play("heart", 0.35f + 0.5f * _danger);
                yield return new WaitForSeconds(Mathf.Lerp(1.3f, 0.42f, _danger));
            }
        }

        private static float[] Heart()
        {
            int n = (int)(SR * 0.5f);
            var d = new float[n];
            void Thump(float at, float amp)
            {
                int start = (int)(at * SR);
                int len = (int)(SR * 0.15f);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * 52f * t) * Mathf.Exp(-t * 24f) * amp;
                }
            }
            Thump(0f, 0.9f);
            Thump(0.18f, 0.6f);
            return d;
        }

        // ---- 밤 공포 원샷: 먼 하울링 / 삐걱임 ----
        private Coroutine _scareLoop;

        private void SetScares(bool on)
        {
            if (_scareLoop != null) { StopCoroutine(_scareLoop); _scareLoop = null; }
            if (on) _scareLoop = StartCoroutine(ScareRoutine());
        }

        private System.Collections.IEnumerator ScareRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(12f, 28f));
                Play(Random.value < 0.5f ? "howl" : "creak", 0.5f);
            }
        }

        private static float[] Howl()
        {
            int n = (int)(SR * 2.4f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 2.4f));
                float f = Mathf.Lerp(520f, 250f, t / 2.4f) + Mathf.Sin(t * 11f) * 14f; // 떨어지는 울음 + 비브라토
                d[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f * 0.5f * t) * 0.3f) * env * 0.35f;
            }
            return d;
        }

        private static float[] Creak()
        {
            int n = (int)(SR * 1.4f);
            var d = new float[n];
            var rng = new System.Random(21);
            float noise = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                noise = Mathf.Lerp(noise, (float)rng.NextDouble() * 2f - 1f, 0.35f);
                float ratchet = Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * (9f + t * 5f) * t)) * 0.8f + 0.2f; // 끊기는 마찰
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.4f));
                d[i] = noise * ratchet * env * 0.4f;
            }
            return d;
        }

        private static void FadeEdges(float[] d)
        {
            int fade = SR / 20;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                d[i] *= k;
                d[d.Length - 1 - i] *= k;
            }
        }

        public static void Play(string name, float volume = 1f)
        {
            if (_instance == null) return;
            if (!_clips.TryGetValue(name, out var clip))
            {
                clip = _instance.Build(name);
                if (clip == null) return;
                _clips[name] = clip;
            }
            _instance._source.PlayOneShot(clip, volume);
        }

        private AudioClip Build(string name)
        {
            float[] data = name switch
            {
                "dudung" => Dudung(),
                "mirror" => Mirror(),
                "hit" => Hit(),
                "quest" => Quest(),
                "sting" => Sting(),
                "howl" => Howl(),
                "creak" => Creak(),
                "heart" => Heart(),
                _ => null,
            };
            if (data == null) return null;
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 정산 "두둥" — 저음 북소리 2연타
        private static float[] Dudung()
        {
            int n = (int)(SR * 1.2f);
            var d = new float[n];
            void Thump(int start, float freq, float amp)
            {
                int len = (int)(SR * 0.45f);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    float env = Mathf.Exp(-t * 9f);
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * (freq - t * 30f) * t) * env * amp;
                }
            }
            Thump(0, 82f, 0.85f);
            Thump((int)(SR * 0.32f), 62f, 1f);
            return d;
        }

        // 거울 섬광 — 유리질 상승 반짝임
        private static float[] Mirror()
        {
            int n = (int)(SR * 0.8f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * 5f);
                d[i] = (Mathf.Sin(2f * Mathf.PI * (1200f + t * 2200f) * t) * 0.4f
                        + Mathf.Sin(2f * Mathf.PI * (1800f + t * 3100f) * t) * 0.25f) * env;
            }
            return d;
        }

        // 피격 — 짧은 노이즈 임팩트
        private static float[] Hit()
        {
            int n = (int)(SR * 0.28f);
            var d = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * 26f);
                d[i] = ((float)rng.NextDouble() * 2f - 1f) * env * 0.8f
                       + Mathf.Sin(2f * Mathf.PI * 110f * t) * env * 0.4f;
            }
            return d;
        }

        // 과제 완료 — 경쾌한 2음 딩동
        private static float[] Quest()
        {
            int n = (int)(SR * 0.75f);
            var d = new float[n];
            void Tone(int start, float freq)
            {
                int len = (int)(SR * 0.35f);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    float env = Mathf.Exp(-t * 7f);
                    d[start + i] += (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f
                                     + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.15f) * env;
                }
            }
            Tone(0, 784f);              // G5
            Tone((int)(SR * 0.16f), 1046f); // C6
            return d;
        }

        // 돌변 스팅 — 불협 상승음
        private static float[] Sting()
        {
            int n = (int)(SR * 1.0f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Clamp01(t * 8f) * Mathf.Exp(-t * 2.2f);
                d[i] = (Mathf.Sin(2f * Mathf.PI * (180f + t * 260f) * t) * 0.4f
                        + Mathf.Sin(2f * Mathf.PI * (191f + t * 300f) * t) * 0.4f) * env;
            }
            return d;
        }
    }
}
