using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Автоматическая вспышка света раз в N секунд: урон всем врагам в радиусе фонаря.
    /// </summary>
    public class FlashAttack : MonoBehaviour
    {
        [SerializeField] LanternLight lantern;
        [Tooltip("Кольцо, которое разлетается при вспышке (необязательно)")]
        [SerializeField] SpriteRenderer flashRing;

        [Header("Параметры")]
        [SerializeField, Min(0f)] float damage = 10f;
        [SerializeField, Min(0.05f)] float interval = 1.2f;
        [SerializeField, Min(0.05f)] float minInterval = 0.3f;
        [SerializeField] LayerMask enemyLayers = ~0;
        [SerializeField, Min(0.01f)] float ringDuration = 0.3f;

        readonly List<Collider2D> hits = new List<Collider2D>(256);
        ContactFilter2D filter;
        float timer;
        float ringTimer;
        Color ringColor = Color.white;

        public float Damage => damage;
        public float Interval => interval;

        void Awake()
        {
            if (!lantern) lantern = GetComponent<LanternLight>();

            filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayers);
            filter.useTriggers = false;

            if (flashRing)
            {
                ringColor = flashRing.color;
                ringColor.a = 1f;
                flashRing.enabled = false;
            }
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                timer = 0f;
                Fire();
            }
            AnimateRing();
        }

        void Fire()
        {
            float radius = lantern ? lantern.Radius : 6f;

            hits.Clear();
            Physics2D.OverlapCircle(transform.position, radius, filter, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                if (hit && hit.TryGetComponent(out EnemyBase enemy))
                    enemy.TakeDamage(damage);
            }

            if (lantern) lantern.Pulse();
            ringTimer = ringDuration;
            GameEvents.RaiseFlashFired(radius);
        }

        void AnimateRing()
        {
            if (!flashRing) return;

            if (ringTimer > 0f)
            {
                ringTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(ringTimer / ringDuration);
                float diameter = (lantern ? lantern.Radius : 6f) * 2f * Mathf.Lerp(0.2f, 1f, t);
                flashRing.enabled = true;
                flashRing.transform.localScale = new Vector3(diameter, diameter, 1f);
                var c = ringColor;
                c.a = 1f - t;
                flashRing.color = c;
            }
            else if (flashRing.enabled)
            {
                flashRing.enabled = false;
            }
        }

        public void AddDamage(float amount)
        {
            damage += amount;
        }

        /// <summary>Сокращает интервал на долю (0.15 = вспышка на 15% чаще).</summary>
        public void ReduceInterval(float fraction)
        {
            interval = Mathf.Max(minInterval, interval * (1f - Mathf.Clamp01(fraction)));
        }
    }
}
