using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastLantern
{
    /// <summary>
    /// Фонарь: Light 2D вокруг игрока. Радиус медленно уменьшается, масло его восстанавливает.
    /// Радиус фонаря = радиус атаки вспышкой и зона, где тени замедляются.
    /// </summary>
    public class LanternLight : MonoBehaviour
    {
        public static LanternLight Current { get; private set; }

        [SerializeField] Light2D light2D;

        [Header("Радиус")]
        [SerializeField, Min(0.5f)] float startRadius = 6f;
        [SerializeField, Min(0.5f)] float minRadius = 2.5f;
        [SerializeField, Min(1f)] float maxRadius = 8f;
        [Tooltip("На сколько единиц в секунду гаснет фонарь без масла")]
        [SerializeField, Min(0f)] float shrinkPerSecond = 0.06f;
        [Tooltip("Скорость плавного изменения видимого радиуса")]
        [SerializeField, Min(0.1f)] float smoothing = 5f;

        [Header("Визуал")]
        [SerializeField, Range(0f, 1f)] float innerRadiusRatio = 0.35f;
        [SerializeField, Min(0f)] float flickerAmount = 0.06f;
        [SerializeField, Min(0f)] float flashBoost = 1.2f;
        [SerializeField, Min(0.01f)] float flashFadeTime = 0.25f;

        float radius;
        float visualRadius;
        float baseIntensity = 1f;
        float boost;

        public float Radius => radius;
        public float MaxRadius => maxRadius;

        void Awake()
        {
            Current = this;
            if (!light2D) light2D = GetComponentInChildren<Light2D>();
            radius = visualRadius = startRadius;
            if (light2D) baseIntensity = light2D.intensity;
            ApplyToLight();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Update()
        {
            radius = Mathf.Max(minRadius, radius - shrinkPerSecond * Time.deltaTime);
            visualRadius = Mathf.Lerp(visualRadius, radius, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

            if (boost > 0f)
                boost = Mathf.Max(0f, boost - flashBoost * Time.deltaTime / flashFadeTime);

            ApplyToLight();
        }

        void ApplyToLight()
        {
            if (!light2D) return;
            light2D.pointLightOuterRadius = visualRadius;
            light2D.pointLightInnerRadius = visualRadius * innerRadiusRatio;
            float flicker = (Mathf.PerlinNoise(Time.time * 3f, 0.37f) - 0.5f) * 2f * flickerAmount;
            light2D.intensity = baseIntensity * (1f + flicker) + boost;
        }

        /// <summary>Капля масла: увеличивает радиус (не больше максимума).</summary>
        public void AddOil(float amount)
        {
            radius = Mathf.Min(maxRadius, radius + amount);
        }

        /// <summary>Улучшение «+радиус света».</summary>
        public void IncreaseMaxRadius(float amount)
        {
            maxRadius += amount;
            radius = Mathf.Min(maxRadius, radius + amount);
        }

        /// <summary>Яркий всплеск света при вспышке.</summary>
        public void Pulse()
        {
            boost = flashBoost;
        }

        /// <summary>Находится ли точка в круге света.</summary>
        public bool IsLit(Vector2 point)
        {
            return (point - (Vector2)transform.position).sqrMagnitude <= radius * radius;
        }
    }
}
