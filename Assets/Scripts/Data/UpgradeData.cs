using UnityEngine;

namespace LastLantern
{
    public enum UpgradeType
    {
        FlashDamage,   // + урон вспышки (value = единицы урона)
        AttackSpeed,   // вспышка чаще (value = доля, 0.15 = на 15%)
        LightRadius,   // + максимальный радиус фонаря
        MoveSpeed,     // + скорость движения
        MaxHealth,     // + максимальное здоровье (и столько же лечит)
        Heal           // лечение
    }

    /// <summary>Карточка улучшения. Создаётся через Create → Last Lantern → Upgrade Data.</summary>
    [CreateAssetMenu(menuName = "Last Lantern/Upgrade Data", fileName = "Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string title = "Улучшение";
        [TextArea(2, 4)] public string description = "Описание";
        public UpgradeType type;
        public float value = 1f;
    }
}
