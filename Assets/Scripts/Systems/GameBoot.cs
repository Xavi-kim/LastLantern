using UnityEngine;

namespace LastLantern
{
    /// <summary>Глобальные настройки, которые применяются до загрузки первой сцены.</summary>
    static class GameBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // Синхронизация с частотой монитора + цель 60 FPS, если VSync выключен драйвером
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }
    }
}
