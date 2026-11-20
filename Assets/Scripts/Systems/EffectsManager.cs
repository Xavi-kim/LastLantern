using Cinemachine;
using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Эффекты: частицы при смерти тени (одна система частиц на всех — дёшево)
    /// и лёгкое дрожание камеры при получении урона.
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        [Header("Частицы смерти")]
        [SerializeField] ParticleSystem deathParticles;
        [SerializeField, Min(1)] int particlesPerDeath = 14;

        [Header("Дрожание камеры")]
        [SerializeField] CinemachineCameraOffset cameraOffset;
        [SerializeField, Min(0f)] float shakeDuration = 0.22f;
        [SerializeField, Min(0f)] float shakeStrength = 0.25f;

        float shakeTimer;

        void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.PlayerDamaged += OnPlayerDamaged;
        }

        void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.PlayerDamaged -= OnPlayerDamaged;
        }

        void OnEnemyKilled(EnemyData data, Vector3 position)
        {
            if (!deathParticles) return;
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true
            };
            deathParticles.Emit(emit, particlesPerDeath);
        }

        void OnPlayerDamaged(float amount)
        {
            shakeTimer = shakeDuration;
        }

        void Update()
        {
            if (!cameraOffset) return;

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float k = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
                Vector2 offset = Random.insideUnitCircle * (shakeStrength * k);
                cameraOffset.m_Offset = new Vector3(offset.x, offset.y, 0f);
            }
            else if (cameraOffset.m_Offset != Vector3.zero)
            {
                cameraOffset.m_Offset = Vector3.zero;
            }
        }
    }
}
