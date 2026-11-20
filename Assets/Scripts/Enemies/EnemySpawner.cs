using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Спавнит теней по кругу за пределами экрана. Частоту, количество и состав берёт
    /// из текущей волны (событие WaveStarted). На рассвете убирает всех теней.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Типы врагов")]
        [SerializeField] EnemyData runner;
        [SerializeField] EnemyData heavy;
        [SerializeField] EnemyData shooter;

        [Header("Спавн")]
        [SerializeField] Camera targetCamera;
        [Tooltip("Насколько дальше края экрана появляются тени")]
        [SerializeField, Min(0f)] float spawnMargin = 2f;

        WaveSettings wave;
        float timer;
        bool running = true;

        void OnEnable()
        {
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.StateChanged -= OnStateChanged;
        }

        void OnWaveStarted(int number, WaveSettings settings)
        {
            wave = settings;
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Victory)
            {
                running = false;
                // Рассвет: тени исчезают
                if (EnemyPool.Instance) EnemyPool.Instance.DespawnAll();
                if (ProjectilePool.Instance) ProjectilePool.Instance.DespawnAll();
            }
            else if (state == GameState.Defeat)
            {
                running = false;
            }
        }

        void Update()
        {
            if (!running || wave == null || !EnemyPool.Instance || !PlayerController.PlayerTransform) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = wave.spawnInterval;

            for (int i = 0; i < wave.spawnBatch; i++)
            {
                if (EnemyPool.Instance.ActiveCount >= wave.maxAlive) break;
                SpawnOne();
            }
        }

        void SpawnOne()
        {
            EnemyData type = PickType();
            if (type == null) return;

            float angle = Random.value * Mathf.PI * 2f;
            Vector2 center = PlayerController.PlayerTransform.position;
            Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SpawnRadius();
            EnemyPool.Instance.Spawn(type, position, wave.healthMultiplier);
        }

        /// <summary>Радиус окружности, гарантированно лежащей за краем экрана.</summary>
        float SpawnRadius()
        {
            if (!targetCamera) targetCamera = Camera.main;
            if (!targetCamera) return 20f;
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;
            return Mathf.Sqrt(halfHeight * halfHeight + halfWidth * halfWidth) + spawnMargin;
        }

        EnemyData PickType()
        {
            float r = runner ? wave.runnerWeight : 0f;
            float h = heavy ? wave.heavyWeight : 0f;
            float s = shooter ? wave.shooterWeight : 0f;
            float total = r + h + s;
            if (total <= 0f) return runner;

            float roll = Random.value * total;
            if (roll < r) return runner;
            if (roll < r + h) return heavy;
            return shooter;
        }
    }
}
