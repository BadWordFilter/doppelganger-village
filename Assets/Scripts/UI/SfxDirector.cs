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

        private void Awake()
        {
            _instance = this;
            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.55f;
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
