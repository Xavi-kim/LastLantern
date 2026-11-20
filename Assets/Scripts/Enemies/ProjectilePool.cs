using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>Пул снарядов стрелков.</summary>
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [SerializeField] Projectile prefab;
        [SerializeField, Min(0)] int prewarm = 40;

        ComponentPool<Projectile> pool;
        readonly List<Projectile> active = new List<Projectile>(64);

        void Awake()
        {
            Instance = this;
            if (prefab) pool = new ComponentPool<Projectile>(prefab, transform, prewarm);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Spawn(Vector2 position, Vector2 velocity, float damage)
        {
            if (pool == null) return;
            var projectile = pool.Get(position);
            active.Add(projectile);
            projectile.Launch(velocity, damage, Release);
        }

        void Release(Projectile projectile)
        {
            active.Remove(projectile);
            pool.Release(projectile);
        }

        public void DespawnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                active[i].Despawn();
        }
    }
}
