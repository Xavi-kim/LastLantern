using UnityEngine;

namespace LastLantern
{
    /// <summary>Здоровье игрока: урон, 1 сек неуязвимости с миганием, лечение, смерть.</summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;
        [SerializeField, Min(0f)] float invulnerabilityTime = 1f;
        [SerializeField, Min(0.02f)] float blinkInterval = 0.1f;
        [SerializeField] SpriteRenderer spriteRenderer;

        float current;
        float invulnerableTimer;
        bool dead;

        public float Current => current;
        public float Max => maxHealth;
        public bool IsDead => dead;

        void Awake()
        {
            current = maxHealth;
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        void Start()
        {
            GameEvents.RaisePlayerHealthChanged(current, maxHealth);
        }

        void Update()
        {
            if (invulnerableTimer <= 0f) return;

            invulnerableTimer -= Time.deltaTime;
            if (spriteRenderer)
            {
                bool visible = invulnerableTimer <= 0f || Mathf.FloorToInt(invulnerableTimer / blinkInterval) % 2 == 0;
                spriteRenderer.enabled = visible;
            }
        }

        public void TakeDamage(float amount)
        {
            if (dead || invulnerableTimer > 0f || amount <= 0f) return;

            current = Mathf.Max(0f, current - amount);
            invulnerableTimer = invulnerabilityTime;
            GameEvents.RaisePlayerDamaged(amount);
            GameEvents.RaisePlayerHealthChanged(current, maxHealth);

            if (current <= 0f)
            {
                dead = true;
                if (spriteRenderer) spriteRenderer.enabled = true;
                GameEvents.RaisePlayerDied();
            }
        }

        public void Heal(float amount)
        {
            if (dead || amount <= 0f) return;
            current = Mathf.Min(maxHealth, current + amount);
            GameEvents.RaisePlayerHealthChanged(current, maxHealth);
        }

        public void AddMaxHealth(float amount)
        {
            if (dead || amount <= 0f) return;
            maxHealth += amount;
            current += amount;
            GameEvents.RaisePlayerHealthChanged(current, maxHealth);
        }
    }
}
