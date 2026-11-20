using UnityEngine;

namespace LastLantern
{
    /// <summary>Опыт и уровни. Опыт приходит от подобранных капель масла.</summary>
    public class ExperienceSystem : MonoBehaviour
    {
        [Tooltip("Опыта нужно для 2-го уровня")]
        [SerializeField, Min(1)] int baseXpToLevel = 5;
        [Tooltip("Прибавка к требуемому опыту за каждый уровень")]
        [SerializeField, Min(0)] int flatGrowth = 3;
        [Tooltip("Степень дополнительного роста требований")]
        [SerializeField, Min(1f)] float curvePower = 1.35f;

        int level = 1;
        int xp;
        int needed;

        public int Level => level;

        void OnEnable() => GameEvents.OilCollected += AddExperience;
        void OnDisable() => GameEvents.OilCollected -= AddExperience;

        void Awake()
        {
            needed = XpForLevel(level);
        }

        void Start()
        {
            GameEvents.RaiseExperienceChanged(xp, needed, level);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            xp += amount;

            while (xp >= needed)
            {
                xp -= needed;
                level++;
                needed = XpForLevel(level);
                GameEvents.RaiseLevelUp(level);
            }

            GameEvents.RaiseExperienceChanged(xp, needed, level);
        }

        int XpForLevel(int lvl)
        {
            int extra = Mathf.RoundToInt(Mathf.Pow(lvl - 1, curvePower));
            return baseXpToLevel + flatGrowth * (lvl - 1) + extra;
        }
    }
}
