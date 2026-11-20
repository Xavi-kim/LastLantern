using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastLantern
{
    /// <summary>Меню паузы (Esc): «Продолжить» и «В меню».</summary>
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button continueButton;
        [SerializeField] Button menuButton;

        void Awake()
        {
            if (panel) panel.SetActive(false);
            if (continueButton) continueButton.onClick.AddListener(OnContinue);
            if (menuButton) menuButton.onClick.AddListener(OnMenu);
        }

        void OnEnable() => GameEvents.StateChanged += OnStateChanged;
        void OnDisable() => GameEvents.StateChanged -= OnStateChanged;

        void OnStateChanged(GameState state)
        {
            bool show = state == GameState.Paused;
            if (panel) panel.SetActive(show);
            if (show && continueButton && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }

        void OnContinue()
        {
            if (GameManager.Instance) GameManager.Instance.Resume();
        }

        void OnMenu()
        {
            if (GameManager.Instance) GameManager.Instance.ToMenu();
        }
    }
}
