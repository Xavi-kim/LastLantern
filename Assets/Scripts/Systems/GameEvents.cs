using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Шина событий игры. Системы общаются через неё, а не через жёсткие ссылки друг на друга.
    /// Правило: подписываемся в OnEnable, отписываемся в OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // --- Игрок ---
        public static event Action<float, float> PlayerHealthChanged;      // текущее, максимум
        public static event Action<float> PlayerDamaged;                    // сколько урона получено
        public static event Action PlayerDied;
        public static event Action<float> FlashFired;                       // радиус вспышки

        // --- Враги и лут ---
        public static event Action<EnemyData, Vector3> EnemyKilled;         // тип врага, место смерти
        public static event Action<int> OilCollected;                       // сколько опыта дала капля

        // --- Опыт и улучшения ---
        public static event Action<int, int, int> ExperienceChanged;        // опыт, нужно до уровня, уровень
        public static event Action<int> LevelUp;                            // новый уровень
        public static event Action<IReadOnlyList<UpgradeData>> UpgradeChoicesOffered;
        public static event Action<UpgradeData> UpgradePicked;

        // --- Ход забега ---
        public static event Action<GameState> StateChanged;
        public static event Action<float, float> TimerChanged;              // прошло секунд, длительность забега
        public static event Action<int> KillsChanged;
        public static event Action<int, WaveSettings> WaveStarted;          // номер волны (с 1), её параметры

        public static void RaisePlayerHealthChanged(float current, float max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDamaged(float amount) => PlayerDamaged?.Invoke(amount);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseFlashFired(float radius) => FlashFired?.Invoke(radius);
        public static void RaiseEnemyKilled(EnemyData data, Vector3 position) => EnemyKilled?.Invoke(data, position);
        public static void RaiseOilCollected(int xp) => OilCollected?.Invoke(xp);
        public static void RaiseExperienceChanged(int xp, int needed, int level) => ExperienceChanged?.Invoke(xp, needed, level);
        public static void RaiseLevelUp(int level) => LevelUp?.Invoke(level);
        public static void RaiseUpgradeChoicesOffered(IReadOnlyList<UpgradeData> choices) => UpgradeChoicesOffered?.Invoke(choices);
        public static void RaiseUpgradePicked(UpgradeData upgrade) => UpgradePicked?.Invoke(upgrade);
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
        public static void RaiseTimerChanged(float elapsed, float duration) => TimerChanged?.Invoke(elapsed, duration);
        public static void RaiseKillsChanged(int kills) => KillsChanged?.Invoke(kills);
        public static void RaiseWaveStarted(int wave, WaveSettings settings) => WaveStarted?.Invoke(wave, settings);
    }
}
