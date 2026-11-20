using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>Роняет капли масла из убитых врагов. Капли берутся из пула.</summary>
    public class OilDropSpawner : MonoBehaviour
    {
        [SerializeField] OilDrop prefab;
        [SerializeField, Min(0)] int prewarm = 80;
        [SerializeField, Range(0f, 1f)] float dropChance = 1f;

        ComponentPool<OilDrop> pool;
        readonly List<OilDrop> active = new List<OilDrop>();

        void Awake()
        {
            if (prefab) pool = new ComponentPool<OilDrop>(prefab, transform, prewarm);
        }

        void OnEnable() => GameEvents.EnemyKilled += OnEnemyKilled;
        void OnDisable() => GameEvents.EnemyKilled -= OnEnemyKilled;

        void OnEnemyKilled(EnemyData data, Vector3 position)
        {
            if (pool == null || data == null || Random.value > dropChance) return;
            position.z = 0f;
            var drop = pool.Get(position);
            active.Add(drop);
            drop.Init(data.xpReward, data.oilAmount, Release);
        }

        void Release(OilDrop drop)
        {
            active.Remove(drop);
            pool.Release(drop);
        }
    }
}
