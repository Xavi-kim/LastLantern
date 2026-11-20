using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastLantern
{
    /// <summary>Главное меню: «Играть», «Настройки» (громкость), «Выход».</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] string gameSceneName = "Game";

        [Header("Панели")]
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject settingsPanel;

        [Header("Кнопки")]
        [SerializeField] Button playButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button exitButton;
        [SerializeField] Button backButton;

        [Header("Настройки")]
        [SerializeField] Slider volumeSlider;

        void Awake()
        {
            Time.timeScale = 1f;

            if (playButton) playButton.onClick.AddListener(Play);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettings);
            if (exitButton) exitButton.onClick.AddListener(Exit);
            if (backButton) backButton.onClick.AddListener(ShowMain);

            if (volumeSlider)
            {
                volumeSlider.SetValueWithoutNotify(AudioManager.Volume);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
        }

        void Start()
        {
            ShowMain();
        }

        void Play()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        void ShowMain()
        {
            if (mainPanel) mainPanel.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(false);
            Select(playButton ? playButton.gameObject : null);
        }

        void ShowSettings()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(true);
            Select(volumeSlider ? volumeSlider.gameObject : null);
        }

        void OnVolumeChanged(float value)
        {
            AudioManager.Volume = value;
        }

        void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Select(GameObject target)
        {
            if (target && EventSystem.current) EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
