using System;
using UnityEngine;

namespace LastLantern
{
    /// <summary>Тёмный шар стрелка. Kinematic-тело с триггером, живёт в пуле.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float lifetime = 5f;

        Rigidbody2D body;
        float damage;
        float life;
        Action<Projectile> release;
        bool active;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }

        public void Launch(Vector2 velocity, float projectileDamage, Action<Projectile> releaseCallback)
        {
            damage = projectileDamage;
            release = releaseCallback;
            life = lifetime;
            active = true;
            body.position = transform.position;
            body.SetVelocity(velocity);
        }

        void Update()
        {
            if (!active) return;
            life -= Time.deltaTime;
            if (life <= 0f) Despawn();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!active) return;
            if (other.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
                Despawn();
            }
        }

        public void Despawn()
        {
            if (!active) return;
            active = false;
            body.SetVelocity(Vector2.zero);
            var callback = release;
            release = null;
            if (callback != null) callback(this);
            else gameObject.SetActive(false);
        }
    }
}
