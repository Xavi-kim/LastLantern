using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// При повышении уровня ставит игру на паузу и предлагает 3 случайные карточки улучшений.
    /// Выбор приходит из UI событием UpgradePicked.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [SerializeField] List<UpgradeData> allUpgrades = new List<UpgradeData>();
        [SerializeField, Min(1)] int choicesCount = 3;

        [Header("Игрок (найдутся сами, если пусто)")]
        [SerializeField] PlayerController player;
        [SerializeField] PlayerHealth health;
        [SerializeField] FlashAttack flash;
        [SerializeField] LanternLight lantern;

        readonly List<UpgradeData> currentChoices = new List<UpgradeData>();
        readonly List<UpgradeData> pool = new List<UpgradeData>();
        int pendingLevelUps;
        bool showing;

        void Start()
        {
            if (!player) player = FindFirstObjectByType<PlayerController>();
            if (!health) health = FindFirstObjectByType<PlayerHealth>();
            if (!flash) flash = FindFirstObjectByType<FlashAttack>();
            if (!lantern) lantern = FindFirstObjectByType<LanternLight>();
        }

        void OnEnable()
        {
            GameEvents.LevelUp += OnLevelUp;
            GameEvents.UpgradePicked += OnUpgradePicked;
        }

        void OnDisable()
        {
            GameEvents.LevelUp -= OnLevelUp;
            GameEvents.UpgradePicked -= OnUpgradePicked;
        }

        void OnLevelUp(int level)
        {
            pendingLevelUps++;
            if (!showing) ShowNext();
        }

        void ShowNext()
        {
            var gm = GameManager.Instance;
            if (pendingLevelUps <= 0 || gm == null) return;
            if (gm.State == GameState.Victory || gm.State == GameState.Defeat) return;

            RollChoices();
            if (currentChoices.Count == 0) { pendingLevelUps = 0; return; }

            pendingLevelUps--;
            showing = true;
            gm.SetState(GameState.LevelUp);
            GameEvents.RaiseUpgradeChoicesOffered(currentChoices);
        }

        void RollChoices()
        {
            pool.Clear();
            foreach (var upgrade in allUpgrades)
            {
                if (upgrade == null) continue;
                // Лечение не предлагаем при полном здоровье
                if (upgrade.type == UpgradeType.Heal && health && health.Current >= health.Max) continue;
                pool.Add(upgrade);
            }

            // Перемешивание Фишера–Йетса
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            currentChoices.Clear();
            for (int i = 0; i < pool.Count && i < choicesCount; i++)
                currentChoices.Add(pool[i]);
        }

        void OnUpgradePicked(UpgradeData upgrade)
        {
            if (!showing) return;
            Apply(upgrade);
            showing = false;

            if (GameManager.Instance) GameManager.Instance.SetState(GameState.Playing);
            ShowNext(); // если за раз набралось несколько уровней
        }

        void Apply(UpgradeData upgrade)
        {
            if (upgrade == null) return;
            switch (upgrade.type)
            {
                case UpgradeType.FlashDamage:
                    if (flash) flash.AddDamage(upgrade.value);
                    break;
                case UpgradeType.AttackSpeed:
                    if (flash) flash.ReduceInterval(upgrade.value);
                    break;
                case UpgradeType.LightRadius:
                    if (lantern) lantern.IncreaseMaxRadius(upgrade.value);
                    break;
                case UpgradeType.MoveSpeed:
                    if (player) player.AddMoveSpeed(upgrade.value);
                    break;
                case UpgradeType.MaxHealth:
                    if (health) health.AddMaxHealth(upgrade.value);
                    break;
                case UpgradeType.Heal:
                    if (health) health.Heal(upgrade.value);
                    break;
            }
        }
    }
}
