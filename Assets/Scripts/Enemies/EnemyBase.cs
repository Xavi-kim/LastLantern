using System;
using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Базовый класс тени: здоровье, урон касанием, замедление в свете, возврат в пул.
    /// Как именно двигаться — решают наследники (EnemyChaser, EnemyShooter).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected EnemyData data;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField, Min(0f)] float hitFlashTime = 0.08f;
        [SerializeField] Color hitFlashColor = new Color(1f, 0.95f, 0.7f, 1f);

        protected Rigidbody2D body;
        float health;
        Action<EnemyBase> release;
        Color baseColor = Color.white;
        float flashTimer;
        bool alive;
        int playerLayer = -1;

        public EnemyData Data => data;
        public bool IsAlive => alive;

        protected static bool HasPlayer => PlayerController.PlayerTransform != null;

        protected static Vector2 PlayerPosition =>
            PlayerController.PlayerTransform ? (Vector2)PlayerController.PlayerTransform.position : Vector2.zero;

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer) baseColor = spriteRenderer.color;
            playerLayer = LayerMask.NameToLayer("Player");
        }

        /// <summary>Вызывается пулом при появлении врага.</summary>
        public void Spawn(EnemyData enemyData, Vector2 position, float healthMultiplier, Action<EnemyBase> releaseCallback)
        {
            data = enemyData;
            release = releaseCallback;
            health = data.maxHealth * Mathf.Max(0.01f, healthMultiplier);
            alive = true;
            flashTimer = 0f;
            if (spriteRenderer) spriteRenderer.color = baseColor;

            transform.position = position;
            body.position = position;
            body.SetVelocity(Vector2.zero);
            OnSpawned();
        }

        protected virtual void OnSpawned() { }

        /// <summary>Текущая скорость с учётом замедления в свете фонаря.</summary>
        protected float CurrentSpeed()
        {
            float speed = data.moveSpeed;
            var lantern = LanternLight.Current;
            if (lantern && lantern.IsLit(body.position)) speed *= 1f - data.lightSlow;
            return speed;
        }

        /// <summary>Куда и с какой скоростью хочет двигаться враг.</summary>
        protected abstract Vector2 DesiredVelocity();

        protected virtual void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f && spriteRenderer) spriteRenderer.color = baseColor;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!alive || !HasPlayer || data == null)
            {
                body.SetVelocity(Vector2.zero);
                return;
            }
            body.SetVelocity(DesiredVelocity());
        }

        public void TakeDamage(float amount)
        {
            if (!alive || amount <= 0f) return;

            health -= amount;
            flashTimer = hitFlashTime;
            if (spriteRenderer) spriteRenderer.color = hitFlashColor;

            if (health <= 0f) Die();
        }

        void Die()
        {
            if (!alive) return;
            alive = false;
            GameEvents.RaiseEnemyKilled(data, transform.position);
            Despawn();
        }

        /// <summary>Убирает врага без награды (например, на рассвете).</summary>
        public void Despawn()
        {
            alive = false;
            var callback = release;
            release = null;
            if (callback != null) callback(this);
            else gameObject.SetActive(false);
        }

        void OnCollisionEnter2D(Collision2D collision) => TryHurtPlayer(collision);
        void OnCollisionStay2D(Collision2D collision) => TryHurtPlayer(collision);

        void TryHurtPlayer(Collision2D collision)
        {
            if (!alive || data == null) return;
            // Быстрый отсев: враги часто толкают друг друга, их не проверяем
            if (playerLayer >= 0 && collision.gameObject.layer != playerLayer) return;
            if (collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
                playerHealth.TakeDamage(data.contactDamage);
        }
    }
}
