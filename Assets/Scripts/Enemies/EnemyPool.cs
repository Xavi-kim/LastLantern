using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>Пул врагов: отдельный под-пул для каждого типа (EnemyData).</summary>
    public class EnemyPool : MonoBehaviour
    {
        public static EnemyPool Instance { get; private set; }

        [Tooltip("Какие типы заранее создать при старте, чтобы не было рывков во время игры")]
        [SerializeField] EnemyData[] prewarmTypes = new EnemyData[0];
        [SerializeField, Min(0)] int prewarmPerType = 40;

        readonly Dictionary<EnemyData, ComponentPool<EnemyBase>> pools = new Dictionary<EnemyData, ComponentPool<EnemyBase>>();
        readonly List<EnemyBase> active = new List<EnemyBase>(256);

        public int ActiveCount => active.Count;

        void Awake()
        {
            Instance = this;
            foreach (var type in prewarmTypes)
            {
                if (type == null || type.prefab == null || pools.ContainsKey(type)) continue;
                pools[type] = new ComponentPool<EnemyBase>(type.prefab, transform, prewarmPerType);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        ComponentPool<EnemyBase> PoolFor(EnemyData type)
        {
            if (!pools.TryGetValue(type, out var pool))
            {
                pool = new ComponentPool<EnemyBase>(type.prefab, transform, 0);
                pools[type] = pool;
            }
            return pool;
        }

        public EnemyBase Spawn(EnemyData type, Vector2 position, float healthMultiplier)
        {
            if (type == null || type.prefab == null) return null;
            var enemy = PoolFor(type).Get(position);
            active.Add(enemy);
            enemy.Spawn(type, position, healthMultiplier, Release);
            return enemy;
        }

        void Release(EnemyBase enemy)
        {
            active.Remove(enemy);
            if (enemy.Data != null) PoolFor(enemy.Data).Release(enemy);
            else enemy.gameObject.SetActive(false);
        }

        /// <summary>Убрать всех врагов (рассвет).</summary>
        public void DespawnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                active[i].Despawn();
        }
    }
}
