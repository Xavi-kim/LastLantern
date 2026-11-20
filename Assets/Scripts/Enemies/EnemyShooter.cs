using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Тень-стрелок: держит дистанцию, кружит вокруг игрока и раз в N секунд стреляет тёмным шаром.
    /// </summary>
    public class EnemyShooter : EnemyBase
    {
        [Tooltip("Допуск вокруг желаемой дистанции, в котором стрелок не приближается и не отходит")]
        [SerializeField, Min(0f)] float distanceTolerance = 0.8f;
        [Tooltip("Дальше этого расстояния не стреляет (игрок за экраном)")]
        [SerializeField, Min(1f)] float maxShootDistance = 11f;
        [Tooltip("Доля скорости при кружении вокруг игрока")]
        [SerializeField, Range(0f, 1f)] float strafeFactor = 0.4f;

        float fireTimer;
        float strafeSign = 1f;

        protected override void OnSpawned()
        {
            fireTimer = data.fireInterval * Random.Range(0.5f, 1f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
        }

        protected override Vector2 DesiredVelocity()
        {
            Vector2 toPlayer = PlayerPosition - body.position;
            float distance = toPlayer.magnitude;
            if (distance < 0.001f) return Vector2.zero;

            Vector2 dir = toPlayer / distance;
            float speed = CurrentSpeed();

            if (distance > data.preferredDistance + distanceTolerance) return dir * speed;   // подойти
            if (distance < data.preferredDistance - distanceTolerance) return -dir * speed;  // отойти

            var perpendicular = new Vector2(-dir.y, dir.x) * strafeSign;
            return perpendicular * (speed * strafeFactor);                                   // кружить
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || !HasPlayer) return;

            fireTimer -= Time.deltaTime;
            if (fireTimer > 0f) return;
            fireTimer = data.fireInterval;

            Vector2 toPlayer = PlayerPosition - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > maxShootDistance * maxShootDistance) return;

            var pool = ProjectilePool.Instance;
            if (pool) pool.Spawn(transform.position, toPlayer.normalized * data.projectileSpeed, data.projectileDamage);
        }
    }
}
