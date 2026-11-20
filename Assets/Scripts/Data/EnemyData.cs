using UnityEngine;

namespace LastLantern
{
    /// <summary>Параметры типа врага. Создаётся через Create → Last Lantern → Enemy Data.</summary>
    [CreateAssetMenu(menuName = "Last Lantern/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Общее")]
        public string displayName = "Тень";
        [Tooltip("Префаб с компонентом EnemyChaser или EnemyShooter")]
        public EnemyBase prefab;

        [Header("Характеристики")]
        [Min(1f)] public float maxHealth = 20f;
        [Tooltip("Урон при касании игрока")]
        [Min(0f)] public float contactDamage = 5f;
        [Min(0f)] public float moveSpeed = 3f;
        [Tooltip("Замедление в свете фонаря (0.4 = на 40%)")]
        [Range(0f, 1f)] public float lightSlow = 0.4f;

        [Header("Награда")]
        [Min(0)] public int xpReward = 1;
        [Tooltip("На сколько капля из этого врага увеличит радиус фонаря")]
        [Min(0f)] public float oilAmount = 0.25f;

        [Header("Только для стрелка")]
        [Min(0f)] public float preferredDistance = 6.5f;
        [Min(0.1f)] public float fireInterval = 2f;
        [Min(0f)] public float projectileSpeed = 6f;
        [Min(0f)] public float projectileDamage = 8f;
    }
}
