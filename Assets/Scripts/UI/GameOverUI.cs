using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastLantern
{
    /// <summary>Экран результатов: победа (рассвет) или поражение, время, убийства, уровень.</summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;

        [Header("Цвета")]
        [SerializeField] Color victoryColor = new Color(1f, 0.8f, 0.35f);
        [SerializeField] Color defeatColor = new Color(0.72f, 0.55f, 1f);

        int kills;
        int level = 1;

        void Awake()
        {
            if (panel) panel.SetActive(false);
            if (restartButton) restartButton.onClick.AddListener(OnRestart);
            if (menuButton) menuButton.onClick.AddListener(OnMenu);
        }

        void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.KillsChanged += OnKillsChanged;
            GameEvents.ExperienceChanged += OnExperienceChanged;
        }

        void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.KillsChanged -= OnKillsChanged;
            GameEvents.ExperienceChanged -= OnExperienceChanged;
        }

        void OnKillsChanged(int value) => kills = value;
        void OnExperienceChanged(int xp, int needed, int newLevel) => level = newLevel;

        void OnStateChanged(GameState state)
        {
            if (state != GameState.Victory && state != GameState.Defeat)
            {
                if (panel) panel.SetActive(false);
                return;
            }

            bool victory = state == GameState.Victory;
            if (titleText)
            {
                titleText.text = victory ? "Рассвет!" : "Фонарь погас";
                titleText.color = victory ? victoryColor : defeatColor;
            }
            if (subtitleText)
                subtitleText.text = victory ? "Ты продержался до утра" : "Тени поглотили лес";

            float time = GameManager.Instance ? GameManager.Instance.Elapsed : 0f;
            if (statsText)
                statsText.text = $"Время: {HUD.FormatTime(time)}\nУбито теней: {kills}\nУровень: {level}";

            if (panel) panel.SetActive(true);
            if (restartButton && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }

        void OnRestart()
        {
            if (GameManager.Instance) GameManager.Instance.Restart();
        }

        void OnMenu()
        {
            if (GameManager.Instance) GameManager.Instance.ToMenu();
        }
    }
}
