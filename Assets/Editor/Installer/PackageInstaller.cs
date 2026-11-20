using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace LastLantern.Installer
{
    /// <summary>
    /// Ставит нужные пакеты при первом открытии проекта.
    /// Лежит в отдельной сборке без зависимостей, поэтому работает даже тогда,
    /// когда основной код ещё не может скомпилироваться (пакетов нет).
    /// Версии не указываются — Unity сама выбирает подходящие для твоего редактора.
    /// </summary>
    [InitializeOnLoad]
    static class PackageInstaller
    {
        // Пакет → как его проверить (по наличию в проекте)
        static readonly string[] RequiredPackages =
        {
            "com.unity.ugui",
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.ide.visualstudio",
        };

        // Cinemachine фиксируем на ветке 2.x: код использует её API (CinemachineVirtualCamera)
        const string CinemachinePackage = "com.unity.cinemachine";
        const string CinemachineVersion = "2.9.7";

        static AddAndRemoveRequest request;

        static PackageInstaller()
        {
            EditorApplication.delayCall += Check;
        }

        [MenuItem("Last Lantern/Проверить и установить пакеты", priority = 100)]
        static void CheckFromMenu() => Check();

        static void Check()
        {
            if (request != null && !request.IsCompleted) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var missing = new List<string>();
            foreach (var package in RequiredPackages)
                if (!IsInstalled(package)) missing.Add(package);

            if (!IsInstalled(CinemachinePackage))
                missing.Add(CinemachinePackage + "@" + CinemachineVersion);

#if !UNITY_6000_0_OR_NEWER
            // В Unity 2022 TextMeshPro — отдельный пакет (в Unity 6 он уже внутри ugui 2.x)
            bool tmpAvailable = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro") != null;
            if (!tmpAvailable && !IsInstalled("com.unity.textmeshpro"))
                missing.Add("com.unity.textmeshpro");
#endif

            if (missing.Count == 0) return;

            Debug.Log("[Last Lantern] Устанавливаю пакеты: " + string.Join(", ", missing));
            request = Client.AddAndRemove(missing.ToArray(), null);
            EditorApplication.update += WaitForRequest;
        }

        static bool IsInstalled(string packageName)
        {
            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + packageName) != null) return true;
            // Встроенные пакеты (например, ugui) не всегда находятся по пути — проверяем ещё manifest.json
            try
            {
                string manifest = System.IO.File.ReadAllText("Packages/manifest.json");
                return manifest.Contains("\"" + packageName + "\"");
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void WaitForRequest()
        {
            if (request == null || !request.IsCompleted) return;
            EditorApplication.update -= WaitForRequest;

            if (request.Status == StatusCode.Failure)
                Debug.LogError("[Last Lantern] Не удалось установить пакеты: " + request.Error.message);
            else
                Debug.Log("[Last Lantern] Пакеты установлены. После компиляции проект соберётся автоматически.");
        }
    }
}
