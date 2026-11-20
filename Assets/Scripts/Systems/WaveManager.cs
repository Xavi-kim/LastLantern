using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>Параметры одной волны. Всё редактируется в Inspector у WaveManager.</summary>
    [Serializable]
    public class WaveSettings
    {
        [Tooltip("Пауза между пачками спавна, сек")]
        [Min(0.05f)] public float spawnInterval = 1.2f;
        [Tooltip("Сколько врагов появляется за одну пачку")]
        [Min(1)] public int spawnBatch = 2;
        [Tooltip("Максимум живых врагов одновременно")]
        [Min(1)] public int maxAlive = 30;
        [Tooltip("Множитель здоровья врагов")]
        [Min(0.1f)] public float healthMultiplier = 1f;

        [Header("Шансы появления (веса)")]
        [Min(0f)] public float runnerWeight = 1f;
        [Min(0f)] public float heavyWeight;
        [Min(0f)] public float shooterWeight;
    }

    /// <summary>
    /// Каждые 30 секунд запускает следующую, более сильную волну и сообщает об этом событием.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField, Min(1f)] float waveDuration = 30f;
        [SerializeField] List<WaveSettings> waves = CreateDefaultWaves();

        int currentIndex = -1;
        float elapsed;

        public int CurrentWave => currentIndex + 1;

        void Start()
        {
            StartWave(0);
        }

        void Update()
        {
            elapsed += Time.deltaTime; // при паузе timeScale = 0, время волны тоже стоит
            int index = Mathf.FloorToInt(elapsed / waveDuration);
            if (index != currentIndex) StartWave(index);
        }

        void StartWave(int index)
        {
            currentIndex = index;
            WaveSettings settings = waves.Count > 0
                ? waves[Mathf.Min(index, waves.Count - 1)]
                : new WaveSettings();
            GameEvents.RaiseWaveStarted(index + 1, settings);
        }

        /// <summary>10 волн по 30 сек: враги появляются чаще, их больше и они крепче.</summary>
        static List<WaveSettings> CreateDefaultWaves()
        {
            var list = new List<WaveSettings>();
            for (int i = 0; i < 10; i++)
            {
                float t = i / 9f;
                list.Add(new WaveSettings
                {
                    spawnInterval = Mathf.Lerp(1.2f, 0.35f, t),
                    spawnBatch = 2 + i / 2,
                    maxAlive = 30 + i * 19,          // к последней волне ~200 врагов
                    healthMultiplier = 1f + 0.15f * i,
                    runnerWeight = 1f,
                    heavyWeight = i >= 1 ? 0.15f + 0.05f * i : 0f,
                    shooterWeight = i >= 2 ? 0.1f + 0.04f * i : 0f,
                });
            }
            return list;
        }
    }
}
