using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Генератор простых звуков прямо в коде — чтобы у игры был звук без внешних файлов.
    /// </summary>
    public static class SynthAudio
    {
        const int Rate = 44100;
        const float TwoPi = Mathf.PI * 2f;

        static AudioClip Build(string name, float[] data, int rate = Rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Огибающая: быстрая атака, затухание к концу.</summary>
        static float Envelope(float u, float attack)
        {
            float a = attack > 0f ? Mathf.Min(1f, u / attack) : 1f;
            float d = 1f - u;
            return a * d * d;
        }

        /// <summary>Вспышка: восходящий тон + шорох.</summary>
        public static AudioClip Flash()
        {
            int n = (int)(Rate * 0.28f);
            var data = new float[n];
            var rnd = new System.Random(1);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float freq = 500f + 1400f * u;
                phase += TwoPi * freq / Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                data[i] = (Mathf.Sin(phase) * 0.35f + noise * 0.25f * (1f - u)) * Envelope(u, 0.03f);
            }
            return Build("sfx_flash", data);
        }

        /// <summary>Смерть тени: падающий «квадратный» тон с шумом.</summary>
        public static AudioClip EnemyDeath()
        {
            int n = (int)(Rate * 0.3f);
            var data = new float[n];
            var rnd = new System.Random(2);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float freq = 320f * (1f - u) + 70f;
                phase += TwoPi * freq / Rate;
                float square = Mathf.Sign(Mathf.Sin(phase)) * 0.22f;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.18f;
                data[i] = (square + noise) * Envelope(u, 0.01f);
            }
            return Build("sfx_enemy_death", data);
        }

        /// <summary>Подбор масла: короткий звонкий «плинк».</summary>
        public static AudioClip Oil()
        {
            int n = (int)(Rate * 0.14f);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float freq = Mathf.Lerp(880f, 1320f, u);
                phase += TwoPi * freq / Rate;
                data[i] = Mathf.Sin(phase) * 0.5f * Envelope(u, 0.01f);
            }
            return Build("sfx_oil", data);
        }

        /// <summary>Новый уровень: восходящее арпеджио до-мажор.</summary>
        public static AudioClip LevelUp()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int n = (int)(Rate * 0.48f);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                int index = Mathf.Min(notes.Length - 1, (int)(u * notes.Length));
                float local = (u * notes.Length) % 1f;
                phase += TwoPi * notes[index] / Rate;
                float env = Mathf.Min(1f, local * 40f) * (1f - local * 0.6f) * (1f - u * 0.4f);
                data[i] = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * 0.3f * env;
            }
            return Build("sfx_level_up", data);
        }

        /// <summary>Урон по игроку: глухой удар.</summary>
        public static AudioClip Hurt()
        {
            int n = (int)(Rate * 0.22f);
            var data = new float[n];
            var rnd = new System.Random(3);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float freq = Mathf.Lerp(180f, 60f, u);
                phase += TwoPi * freq / Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.3f;
                data[i] = (Mathf.Sin(phase) * 0.6f + noise * (1f - u)) * Envelope(u, 0.005f);
            }
            return Build("sfx_hurt", data);
        }

        /// <summary>
        /// Фоновая музыка: тёмный «эмбиент»-аккорд с медленным дыханием, 16 секунд без шва.
        /// Все частоты дают целое число периодов за 16 секунд, поэтому петля бесшовная.
        /// </summary>
        public static AudioClip Music()
        {
            const int rate = 22050;
            const float length = 16f;
            float[] freqs = { 55f, 82.5f, 110f, 165f, 220f };
            float[] amps = { 0.5f, 0.35f, 0.3f, 0.18f, 0.08f };
            int n = (int)(rate * length);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float sum = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float lfo = 0.5f + 0.5f * Mathf.Sin(TwoPi * t / 8f + k * 1.3f);
                    sum += Mathf.Sin(TwoPi * freqs[k] * t) * amps[k] * (0.55f + 0.45f * lfo);
                }
                float breath = 0.75f + 0.25f * Mathf.Sin(TwoPi * t / 16f);
                data[i] = sum * 0.22f * breath;
            }
            return Build("music_night", data, rate);
        }
    }
}
