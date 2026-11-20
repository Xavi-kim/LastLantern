using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Звук и музыка. Создаётся автоматически и живёт между сценами.
    /// Если клипы не назначены в Inspector — генерирует простые звуки кодом (SynthAudio),
    /// поэтому игра звучит без внешних ассетов. Свои .wav/.ogg можно просто перетащить в поля.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const string VolumeKey = "LastLantern.Volume";

        public static AudioManager Instance { get; private set; }

        /// <summary>Общая громкость 0..1, сохраняется между запусками.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set
            {
                float v = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, v);
                AudioListener.volume = v;
            }
        }

        [Header("Клипы (пусто = сгенерировать кодом)")]
        [SerializeField] AudioClip flashClip;
        [SerializeField] AudioClip enemyDeathClip;
        [SerializeField] AudioClip oilClip;
        [SerializeField] AudioClip levelUpClip;
        [SerializeField] AudioClip hurtClip;
        [SerializeField] AudioClip musicClip;

        [Header("Громкость")]
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.3f;
        [Tooltip("Минимальная пауза между одинаковыми звуками, чтобы 50 смертей разом не оглушали")]
        [SerializeField, Min(0f)] float minRepeatInterval = 0.05f;

        AudioSource sfxSource;
        AudioSource musicSource;
        float lastDeathTime = -1f;
        float lastOilTime = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (Instance != null || FindFirstObjectByType<AudioManager>() != null) return;
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            if (!flashClip) flashClip = SynthAudio.Flash();
            if (!enemyDeathClip) enemyDeathClip = SynthAudio.EnemyDeath();
            if (!oilClip) oilClip = SynthAudio.Oil();
            if (!levelUpClip) levelUpClip = SynthAudio.LevelUp();
            if (!hurtClip) hurtClip = SynthAudio.Hurt();
            if (!musicClip) musicClip = SynthAudio.Music();

            AudioListener.volume = Volume;
            musicSource.clip = musicClip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.FlashFired += OnFlash;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.OilCollected += OnOil;
            GameEvents.LevelUp += OnLevelUp;
            GameEvents.PlayerDamaged += OnHurt;
        }

        void OnDisable()
        {
            GameEvents.FlashFired -= OnFlash;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.OilCollected -= OnOil;
            GameEvents.LevelUp -= OnLevelUp;
            GameEvents.PlayerDamaged -= OnHurt;
        }

        void OnFlash(float radius) => Play(flashClip, 0.6f);
        void OnLevelUp(int level) => Play(levelUpClip, 0.9f);
        void OnHurt(float amount) => Play(hurtClip, 1f);

        void OnEnemyKilled(EnemyData data, Vector3 position)
        {
            if (Time.unscaledTime - lastDeathTime < minRepeatInterval) return;
            lastDeathTime = Time.unscaledTime;
            Play(enemyDeathClip, 0.7f);
        }

        void OnOil(int xp)
        {
            if (Time.unscaledTime - lastOilTime < minRepeatInterval) return;
            lastOilTime = Time.unscaledTime;
            Play(oilClip, 0.5f);
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip && sfxSource) sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }
    }
}
