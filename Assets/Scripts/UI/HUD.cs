using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastLantern
{
    /// <summary>HUD: полоса здоровья, полоса опыта и уровень, таймер до рассвета, убийства, волна.</summary>
    public class HUD : MonoBehaviour
    {
        [Header("Здоровье")]
        [SerializeField] Image healthFill;
        [SerializeField] TMP_Text healthText;

        [Header("Опыт")]
        [SerializeField] Image xpFill;
        [SerializeField] TMP_Text levelText;

        [Header("Забег")]
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text killsText;
        [SerializeField] TMP_Text waveText;

        int shownSecond = -1;

        void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.ExperienceChanged += OnExperienceChanged;
            GameEvents.TimerChanged += OnTimerChanged;
            GameEvents.KillsChanged += OnKillsChanged;
            GameEvents.WaveStarted += OnWaveStarted;
        }

        void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.ExperienceChanged -= OnExperienceChanged;
            GameEvents.TimerChanged -= OnTimerChanged;
            GameEvents.KillsChanged -= OnKillsChanged;
            GameEvents.WaveStarted -= OnWaveStarted;
        }

        void OnHealthChanged(float current, float max)
        {
            if (healthFill) healthFill.fillAmount = max > 0f ? current / max : 0f;
            if (healthText) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        void OnExperienceChanged(int xp, int needed, int level)
        {
            if (xpFill) xpFill.fillAmount = needed > 0 ? (float)xp / needed : 0f;
            if (levelText) levelText.text = $"Ур. {level}";
        }

        void OnTimerChanged(float elapsed, float duration)
        {
            // Обновляем текст только раз в секунду — без лишнего мусора каждый кадр
            int left = Mathf.CeilToInt(duration - elapsed);
            if (left == shownSecond) return;
            shownSecond = left;
            if (timerText) timerText.text = FormatTime(left);
        }

        void OnKillsChanged(int kills)
        {
            if (killsText) killsText.text = $"Убито: {kills}";
        }

        void OnWaveStarted(int number, WaveSettings settings)
        {
            if (waveText) waveText.text = $"Волна {number}";
        }

        /// <summary>Секунды → «мм:сс».</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
