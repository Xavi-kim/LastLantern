using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastLantern
{
    public enum GameState
    {
        Playing,
        Paused,
        LevelUp,
        Victory,
        Defeat
    }

    /// <summary>
    /// Состояния забега, таймер до рассвета, счёт убийств, пауза, перезапуск и выход в меню.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Забег")]
        [Tooltip("Длительность забега в секундах (300 = 5 минут до рассвета)")]
        [SerializeField, Min(10f)] float runDuration = 300f;

        [Header("Сцены")]
        [SerializeField] string menuSceneName = "MainMenu";
        [SerializeField] string gameSceneName = "Game";

        public GameState State { get; private set; } = GameState.Playing;
        public float Elapsed { get; private set; }
        public float RunDuration => runDuration;
        public int Kills { get; private set; }

        bool IsFinished => State == GameState.Victory || State == GameState.Defeat;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void OnEnable()
        {
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        void OnDisable()
        {
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        void Start()
        {
            SetState(GameState.Playing);
            GameEvents.RaiseKillsChanged(Kills);
            GameEvents.RaiseTimerChanged(Elapsed, runDuration);
        }

        void Update()
        {
            // Пауза по Esc / Start
            if (GameInput.Pause.WasPressedThisFrame())
            {
                if (State == GameState.Playing) SetState(GameState.Paused);
                else if (State == GameState.Paused) SetState(GameState.Playing);
            }

            if (State != GameState.Playing) return;

            Elapsed = Mathf.Min(runDuration, Elapsed + Time.deltaTime);
            GameEvents.RaiseTimerChanged(Elapsed, runDuration);

            if (Elapsed >= runDuration)
                SetState(GameState.Victory); // Рассвет!
        }

        /// <summary>Меняет состояние. После победы/поражения состояние больше не меняется.</summary>
        public void SetState(GameState newState)
        {
            if (IsFinished && newState != State) return;

            State = newState;
            Time.timeScale = newState == GameState.Playing ? 1f : 0f;
            GameEvents.RaiseStateChanged(newState);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }

        public void ToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
        }

        void OnPlayerDied()
        {
            SetState(GameState.Defeat);
        }

        void OnEnemyKilled(EnemyData data, Vector3 position)
        {
            Kills++;
            GameEvents.RaiseKillsChanged(Kills);
        }
    }
}
