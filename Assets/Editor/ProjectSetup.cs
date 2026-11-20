using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastLantern.EditorTools
{
    /// <summary>
    /// Собирает весь проект в редакторе: URP 2D, слои, спрайты, материалы, данные (ScriptableObject),
    /// префабы, сцены MainMenu и Game, Build Settings.
    /// Запускается сам при первом открытии проекта, повторно — через меню Last Lantern.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string SpritesDir = "Assets/Sprites";
        const string SettingsDir = "Assets/Settings";
        const string MaterialsDir = "Assets/Settings/Materials";
        const string EnemyDataDir = "Assets/Data/Enemies";
        const string UpgradeDataDir = "Assets/Data/Upgrades";
        const string PrefabsDir = "Assets/Prefabs";
        const string EnemyPrefabsDir = "Assets/Prefabs/Enemies";
        const string ScenesDir = "Assets/Scenes";
        public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string GameScenePath = "Assets/Scenes/Game.unity";
        const string AutoRunKey = "LastLantern.AutoSetupTried";

        static readonly string[] LayerNames = { "Player", "Enemy", "EnemyProjectile", "Pickup" };

        class Materials { public Material lit, unlit, particles; }
        class Data { public EnemyData runner, heavy, shooter; public readonly List<UpgradeData> upgrades = new List<UpgradeData>(); }
        class Prefabs { public GameObject player; public Projectile projectile; public OilDrop oil; }

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(GameScenePath)) return;
            if (SessionState.GetBool(AutoRunKey, false)) return;
            SessionState.SetBool(AutoRunKey, true);
            Run(false);
        }

        [MenuItem("Last Lantern/Собрать проект (сцены, префабы, данные)", priority = 0)]
        static void RunFromMenu() => Run(true);

        [MenuItem("Last Lantern/Открыть главное меню", priority = 20)]
        static void OpenMenuScene() => OpenScene(MenuScenePath);

        [MenuItem("Last Lantern/Открыть игровую сцену", priority = 21)]
        static void OpenGameScene() => OpenScene(GameScenePath);

        static void OpenScene(string path)
        {
            if (!File.Exists(path)) { EditorUtility.DisplayDialog("Last Lantern", "Сцены ещё нет — сначала соберите проект.", "OK"); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }

        static void Run(bool interactive)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Last Lantern", "Выйдите из Play Mode и повторите.", "OK");
                return;
            }
            if (!EnsureInputBackend()) return;
            if (!EnsureTmpEssentials()) return;
            if (interactive && File.Exists(GameScenePath) &&
                !EditorUtility.DisplayDialog("Last Lantern",
                    "Сцены, префабы и спрайты будут пересозданы.\nДанные врагов и улучшений (Assets/Data) сохранятся.\n\nПродолжить?",
                    "Да", "Отмена"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                Progress("Папки, слои, настройки", 0.05f);
                EnsureFolders();
                SetupLayers();
                SetupEditorAndPlayer();

                Progress("URP 2D", 0.15f);
                bool pipelineOk = SetupPipeline();

                // Временная пустая сцена, чтобы собирать префабы не в чужой сцене
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                Progress("Спрайты", 0.3f);
                var art = ArtGenerator.CreateAll(SpritesDir);
                UIBuilder.WhiteSprite = art.white;

                Progress("Материалы", 0.4f);
                var mats = CreateMaterials(art);

                Progress("Данные (ScriptableObject)", 0.5f);
                var data = CreateData();

                Progress("Префабы", 0.6f);
                var prefabs = CreatePrefabs(art, mats, data);

                Progress("Сцена Game", 0.75f);
                BuildGameScene(art, mats, data, prefabs);

                Progress("Сцена MainMenu", 0.9f);
                BuildMenuScene();

                SetupBuildSettings();
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(MenuScenePath);

                string message = "Готово! Нажми Play в сцене MainMenu.";
                if (!pipelineOk) message += "\n\nВнимание: не удалось настроить URP 2D — свет фонаря может не отображаться. См. README.";
                Debug.Log("[Last Lantern] " + message);
                EditorUtility.DisplayDialog("Last Lantern", message, "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Last Lantern", "Сборка прервалась: " + e.Message + "\nПодробности в Console.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void Progress(string info, float value) => EditorUtility.DisplayProgressBar("Last Lantern", info, value);

        // ------------------------------------------------------------------ Предусловия

        /// <summary>Включает новую Input System в Player Settings (нужен перезапуск редактора).</summary>
        static bool EnsureInputBackend()
        {
#if ENABLE_INPUT_SYSTEM
            return true;
#else
            var playerSettings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            var so = new SerializedObject(playerSettings);
            var handler = so.FindProperty("activeInputHandler");
            if (handler != null)
            {
                handler.intValue = 1; // Input System Package (New)
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (EditorUtility.DisplayDialog("Last Lantern",
                    "Нужно включить новую Input System. Редактор перезапустится, после этого проект соберётся сам.",
                    "Перезапустить", "Позже"))
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            return false;
#endif
        }

        /// <summary>Импортирует TMP Essential Resources (шрифты TextMeshPro) и перезапускает сборку.</summary>
        static bool EnsureTmpEssentials()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            var importer = Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor");
            var method = importer?.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static);
            if (method != null && method.GetParameters().Length == 3)
            {
                try
                {
                    AssetDatabase.importPackageCompleted += OnTmpImported;
                    method.Invoke(null, new object[] { true, false, false });
                    return false;
                }
                catch (Exception e)
                {
                    AssetDatabase.importPackageCompleted -= OnTmpImported;
                    Debug.LogWarning("[Last Lantern] Автоимпорт TMP не удался: " + e.Message);
                }
            }

            EditorUtility.DisplayDialog("Last Lantern",
                "Импортируй шрифты TextMeshPro: Window → TextMeshPro → Import TMP Essential Resources,\nзатем меню Last Lantern → Собрать проект.",
                "OK");
            return false;
        }

        static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            EditorApplication.delayCall += () => Run(false);
        }

        // ------------------------------------------------------------------ Настройки проекта

        static void EnsureFolders()
        {
            foreach (var dir in new[] { SpritesDir, MaterialsDir, EnemyDataDir, UpgradeDataDir, EnemyPrefabsDir, ScenesDir })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        static void SetupLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            foreach (var name in LayerNames)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).stringValue == name) { exists = true; break; }
                if (exists) continue;

                for (int i = 6; i < layers.arraySize; i++)
                {
                    var slot = layers.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                    slot.stringValue = name;
                    break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Матрица столкновений: снаряды бьют только игрока, подбираемое ни с чем не сталкивается
            int player = LayerMask.NameToLayer("Player");
            int projectile = LayerMask.NameToLayer("EnemyProjectile");
            int pickup = LayerMask.NameToLayer("Pickup");
            for (int i = 0; i < 32; i++)
            {
                if (projectile >= 0) Physics2D.IgnoreLayerCollision(projectile, i, i != player);
                if (pickup >= 0) Physics2D.IgnoreLayerCollision(pickup, i, true);
            }
        }

        static int Layer(string name)
        {
            int layer = LayerMask.NameToLayer(name);
            return layer < 0 ? 0 : layer;
        }

        static void SetupEditorAndPlayer()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "Xavi-kim";
            PlayerSettings.productName = "Last Lantern";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;
        }

        static void SetupBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
        }

        // ------------------------------------------------------------------ URP 2D

        static bool SetupPipeline()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing && Uses2DRenderer(existing))
                return true;

            try
            {
                string rendererPath = SettingsDir + "/URP-2D-Renderer.asset";
                var rendererData = Create2DRendererData(rendererPath);
                if (rendererData == null) return false;

                var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, SettingsDir + "/URP-2D-Pipeline.asset");

                GraphicsSettings.defaultRenderPipeline = pipeline;
                int current = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
                QualitySettings.SetQualityLevel(current, false);
                AssetDatabase.SaveAssets();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Last Lantern] Ошибка настройки URP 2D: " + e);
                return false;
            }
        }

        static bool Uses2DRenderer(UniversalRenderPipelineAsset asset)
        {
            var list = new SerializedObject(asset).FindProperty("m_RendererDataList");
            return list != null && list.arraySize > 0 && list.GetArrayElementAtIndex(0).objectReferenceValue is Renderer2DData;
        }

        /// <summary>
        /// Создаёт 2D Renderer так же, как это делает сам URP (внутренний метод CreateRendererAsset),
        /// а если его нет в этой версии — вручную с подгрузкой ресурсов по умолчанию.
        /// </summary>
        static ScriptableRendererData Create2DRendererData(string path)
        {
            var method = typeof(UniversalRenderPipelineAsset).GetMethod("CreateRendererAsset",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (method != null)
            {
                try
                {
                    var parameters = method.GetParameters();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var p = parameters[i];
                        if (i == 0 && p.ParameterType == typeof(string)) args[i] = path;
                        else if (p.ParameterType.IsEnum) args[i] = Enum.Parse(p.ParameterType, "_2DRenderer");
                        else if (p.ParameterType == typeof(bool)) args[i] = false;
                        else if (p.ParameterType == typeof(string)) args[i] = "Renderer";
                        else args[i] = p.HasDefaultValue ? p.DefaultValue : null;
                    }
                    if (method.Invoke(null, args) is Renderer2DData created) return created;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Last Lantern] Стандартное создание 2D Renderer не сработало, создаю вручную: " + e.Message);
                }
            }

            var data = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(data, path);
            var reloader = Type.GetType("UnityEngine.Rendering.ResourceReloader, Unity.RenderPipelines.Core.Runtime");
            var reload = reloader?.GetMethod("ReloadAllNullIn", new[] { typeof(object), typeof(string) });
            reload?.Invoke(null, new object[] { data, "Packages/com.unity.render-pipelines.universal" });
            EditorUtility.SetDirty(data);
            return data;
        }

        // ------------------------------------------------------------------ Материалы и данные

        static Materials CreateMaterials(GameArt art)
        {
            return new Materials
            {
                lit = MaterialAsset("SpriteLit", "Universal Render Pipeline/2D/Sprite-Lit-Default", null),
                unlit = MaterialAsset("SpriteUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default", null),
                particles = MaterialAsset("DeathParticles", "Universal Render Pipeline/2D/Sprite-Unlit-Default", art.soft.texture),
            };
        }

        static Material MaterialAsset(string name, string shaderName, Texture texture)
        {
            var shader = Shader.Find(shaderName);
            if (!shader) throw new Exception("Не найден шейдер " + shaderName + " (установлен ли URP?)");

            string path = $"{MaterialsDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (texture) material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        static T LoadOrCreate<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Data CreateData()
        {
            var d = new Data();
            d.runner = LoadOrCreate<EnemyData>($"{EnemyDataDir}/Runner.asset", e =>
            {
                e.displayName = "Тень-бегун"; e.maxHealth = 20f; e.contactDamage = 5f; e.moveSpeed = 3.6f;
                e.xpReward = 1; e.oilAmount = 0.25f;
            });
            d.heavy = LoadOrCreate<EnemyData>($"{EnemyDataDir}/Heavy.asset", e =>
            {
                e.displayName = "Тень-тяжеловес"; e.maxHealth = 80f; e.contactDamage = 15f; e.moveSpeed = 1.4f;
                e.xpReward = 4; e.oilAmount = 0.8f;
            });
            d.shooter = LoadOrCreate<EnemyData>($"{EnemyDataDir}/Shooter.asset", e =>
            {
                e.displayName = "Тень-стрелок"; e.maxHealth = 30f; e.contactDamage = 5f; e.moveSpeed = 2.4f;
                e.preferredDistance = 6.5f; e.fireInterval = 2f; e.projectileSpeed = 6f; e.projectileDamage = 8f;
                e.xpReward = 2; e.oilAmount = 0.4f;
            });

            d.upgrades.Add(Upgrade("FlashDamage", "Яркая вспышка", "+5 к урону вспышки", UpgradeType.FlashDamage, 5f));
            d.upgrades.Add(Upgrade("AttackSpeed", "Частое мерцание", "Вспышка срабатывает на 15% чаще", UpgradeType.AttackSpeed, 0.15f));
            d.upgrades.Add(Upgrade("LightRadius", "Широкий свет", "+1 к максимальному радиусу фонаря", UpgradeType.LightRadius, 1f));
            d.upgrades.Add(Upgrade("MoveSpeed", "Лёгкие шаги", "+0.6 к скорости движения", UpgradeType.MoveSpeed, 0.6f));
            d.upgrades.Add(Upgrade("MaxHealth", "Крепкое сердце", "+20 к максимальному здоровью", UpgradeType.MaxHealth, 20f));
            d.upgrades.Add(Upgrade("Heal", "Тёплый отдых", "Восстанавливает 35 здоровья", UpgradeType.Heal, 35f));
            return d;
        }

        static UpgradeData Upgrade(string file, string title, string description, UpgradeType type, float value)
        {
            return LoadOrCreate<UpgradeData>($"{UpgradeDataDir}/{file}.asset", u =>
            {
                u.title = title; u.description = description; u.type = type; u.value = value;
            });
        }

        // ------------------------------------------------------------------ Префабы

        static Prefabs CreatePrefabs(GameArt art, Materials mats, Data data)
        {
            var result = new Prefabs { player = CreatePlayerPrefab(art, mats) };

            CreateEnemyPrefab<EnemyChaser>("Enemy_Runner", data.runner, art.runner, mats.lit, 1f, 1f);
            CreateEnemyPrefab<EnemyChaser>("Enemy_Heavy", data.heavy, art.heavy, mats.lit, 1.6f, 4f);
            CreateEnemyPrefab<EnemyShooter>("Enemy_Shooter", data.shooter, art.shooter, mats.lit, 1f, 1f);

            // Снаряд
            var proj = NewObject("Projectile", null, Layer("EnemyProjectile"));
            var projBody = proj.AddComponent<Rigidbody2D>();
            projBody.bodyType = RigidbodyType2D.Kinematic;
            projBody.gravityScale = 0f;
            var projCollider = proj.AddComponent<CircleCollider2D>();
            projCollider.isTrigger = true;
            projCollider.radius = 0.2f;
            AddSprite(proj, "Sprite", art.projectile, mats.unlit, 15, 1f);
            proj.AddComponent<Projectile>();
            result.projectile = SavePrefab(proj, $"{PrefabsDir}/Projectile.prefab").GetComponent<Projectile>();

            // Капля масла
            var oil = NewObject("OilDrop", null, Layer("Pickup"));
            var oilSprite = AddSprite(oil, "Sprite", art.oil, mats.unlit, 4, 1f);
            var oilDrop = oil.AddComponent<OilDrop>();
            Set(oilDrop, "visual", oilSprite.transform);
            result.oil = SavePrefab(oil, $"{PrefabsDir}/OilDrop.prefab").GetComponent<OilDrop>();

            return result;
        }

        static GameObject CreatePlayerPrefab(GameArt art, Materials mats)
        {
            var go = NewObject("Player", null, Layer("Player"));
            go.tag = "Player";

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.mass = 5f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<CircleCollider2D>().radius = 0.38f;

            var sprite = AddSprite(go, "Sprite", art.player, mats.lit, 10, 1f);

            // Фонарь — Light 2D
            var lightGo = NewObject("Lantern", go.transform, go.layer);
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.78f, 0.45f);
            light.intensity = 1.15f;
            light.pointLightOuterRadius = 6f;
            light.pointLightInnerRadius = 2f;
            light.falloffIntensity = 0.6f;
            ApplyToAllSortingLayers(light);

            // Кольцо вспышки
            var ring = AddSprite(go, "FlashRing", art.ring, mats.unlit, 20, 1f);
            ring.color = new Color(1f, 0.9f, 0.55f, 0f);
            ring.enabled = false;

            var controller = go.AddComponent<PlayerController>();
            Set(controller, "spriteRenderer", sprite);
            var health = go.AddComponent<PlayerHealth>();
            Set(health, "spriteRenderer", sprite);
            var lantern = go.AddComponent<LanternLight>();
            Set(lantern, "light2D", light);
            var flash = go.AddComponent<FlashAttack>();
            Set(flash, "lantern", lantern);
            Set(flash, "flashRing", ring);
            SetInt(flash, "enemyLayers", 1 << Layer("Enemy"));

            return SavePrefab(go, $"{PrefabsDir}/Player.prefab");
        }

        static void CreateEnemyPrefab<T>(string name, EnemyData data, Sprite sprite, Material material, float scale, float mass)
            where T : EnemyBase
        {
            var go = NewObject(name, null, Layer("Enemy"));
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.mass = mass;
            go.AddComponent<CircleCollider2D>().radius = 0.42f * scale;

            var sr = AddSprite(go, "Sprite", sprite, material, 5, scale);
            var enemy = go.AddComponent<T>();
            Set(enemy, "data", data);
            Set(enemy, "spriteRenderer", sr);

            var asset = SavePrefab(go, $"{EnemyPrefabsDir}/{name}.prefab");
            data.prefab = asset.GetComponent<EnemyBase>();
            EditorUtility.SetDirty(data);
        }

        // ------------------------------------------------------------------ Сцена Game

        static void BuildGameScene(GameArt art, Materials mats, Data data, Prefabs prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Камера + Cinemachine
            var cam = CreateCamera(new Color(0.02f, 0.03f, 0.06f));
            cam.gameObject.AddComponent<CinemachineBrain>();

            // Глобальный ночной свет
            var globalLightGo = NewObject("Global Light 2D", null, 0);
            var globalLight = globalLightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = new Color(0.5f, 0.6f, 1f);
            globalLight.intensity = 0.25f;
            ApplyToAllSortingLayers(globalLight);

            // Земля и кусты
            var ground = NewObject("Ground", null, 0);
            var groundSr = ground.AddComponent<SpriteRenderer>();
            groundSr.sprite = art.ground;
            groundSr.sharedMaterial = mats.lit;
            groundSr.drawMode = SpriteDrawMode.Tiled;
            groundSr.size = new Vector2(240f, 240f);
            groundSr.sortingOrder = -100;

            var decor = NewObject("Decor", null, 0);
            var rnd = new System.Random(7);
            for (int i = 0; i < 260; i++)
            {
                float angle = (float)(rnd.NextDouble() * Math.PI * 2);
                float distance = 4f + (float)rnd.NextDouble() * 110f;
                var bush = AddSprite(decor, "Bush", art.bush, mats.lit, -50, 0.8f + (float)rnd.NextDouble() * 0.9f);
                bush.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
                bush.flipX = rnd.NextDouble() < 0.5;
            }

            // Игрок
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.player);
            player.transform.position = Vector3.zero;

            // Виртуальная камера, следующая за игроком
            var vcamGo = NewObject("CM Follow Camera", null, 0);
            vcamGo.transform.position = new Vector3(0f, 0f, -10f);
            var vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
            vcam.Follow = player.transform;
            var lens = vcam.m_Lens;
            lens.OrthographicSize = 9f;
            vcam.m_Lens = lens;
            var framing = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            framing.m_XDamping = 0.4f;
            framing.m_YDamping = 0.4f;
            framing.m_CameraDistance = 10f;
            var cameraOffset = vcamGo.AddComponent<CinemachineCameraOffset>();

            // Системы
            var systems = NewObject("Systems", null, 0);
            systems.AddComponent<GameManager>();
            systems.AddComponent<WaveManager>();
            systems.AddComponent<ExperienceSystem>();
            var upgrades = systems.AddComponent<UpgradeManager>();
            SetList(upgrades, "allUpgrades", data.upgrades.ToArray());
            Set(upgrades, "player", player.GetComponent<PlayerController>());
            Set(upgrades, "health", player.GetComponent<PlayerHealth>());
            Set(upgrades, "flash", player.GetComponent<FlashAttack>());
            Set(upgrades, "lantern", player.GetComponent<LanternLight>());

            var spawner = systems.AddComponent<EnemySpawner>();
            Set(spawner, "runner", data.runner);
            Set(spawner, "heavy", data.heavy);
            Set(spawner, "shooter", data.shooter);
            Set(spawner, "targetCamera", cam);

            // Пулы
            var enemyPool = NewObject("EnemyPool", null, 0).AddComponent<EnemyPool>();
            SetList(enemyPool, "prewarmTypes", new Object[] { data.runner, data.heavy, data.shooter });
            var projectilePool = NewObject("ProjectilePool", null, 0).AddComponent<ProjectilePool>();
            Set(projectilePool, "prefab", prefabs.projectile);
            var oilSpawner = NewObject("OilDrops", null, 0).AddComponent<OilDropSpawner>();
            Set(oilSpawner, "prefab", prefabs.oil);

            // Эффекты
            var effects = NewObject("Effects", null, 0);
            var particles = CreateDeathParticles(effects.transform, mats.particles);
            var effectsManager = effects.AddComponent<EffectsManager>();
            Set(effectsManager, "deathParticles", particles);
            Set(effectsManager, "cameraOffset", cameraOffset);

            BuildGameUI();
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static ParticleSystem CreateDeathParticles(Transform parent, Material material)
        {
            var go = NewObject("DeathParticles", parent, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.3f, 0.9f), new Color(0.2f, 0.06f, 0.3f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4000;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 30;
            return ps;
        }

        static void BuildGameUI()
        {
            var canvas = UIBuilder.Canvas("UI");
            var root = canvas.transform;

            // ---- HUD ----
            var hudRoot = UIBuilder.Stretch(UIBuilder.Node("HUD", root));
            var hud = hudRoot.gameObject.AddComponent<HUD>();

            var healthFill = UIBuilder.Bar(hudRoot, "HealthBar", new Vector2(40f, -40f), new Vector2(420f, 34f), new Color(0.85f, 0.25f, 0.25f));
            var healthText = UIBuilder.Text(healthFill.transform.parent, "Value", "100 / 100", 24f, Color.white);
            UIBuilder.Stretch(healthText.rectTransform);

            var xpFill = UIBuilder.Bar(hudRoot, "XpBar", new Vector2(40f, -84f), new Vector2(420f, 18f), UIBuilder.Warm);
            xpFill.fillAmount = 0f;
            var levelText = UIBuilder.Text(hudRoot, "Level", "Ур. 1", 30f, UIBuilder.Warm, TextAlignmentOptions.Left);
            UIBuilder.Place(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(476f, -72f), new Vector2(200f, 36f));

            var timerText = UIBuilder.Text(hudRoot, "Timer", "05:00", 64f, UIBuilder.Warm);
            UIBuilder.Place(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(400f, 80f));
            timerText.fontStyle = FontStyles.Bold;
            var timerLabel = UIBuilder.Text(hudRoot, "TimerLabel", "до рассвета", 24f, UIBuilder.Pale);
            UIBuilder.Place(timerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(400f, 32f));

            var killsText = UIBuilder.Text(hudRoot, "Kills", "Убито: 0", 32f, Color.white, TextAlignmentOptions.Right);
            UIBuilder.Place(killsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(420f, 40f));
            var waveText = UIBuilder.Text(hudRoot, "Wave", "Волна 1", 28f, UIBuilder.Pale, TextAlignmentOptions.Right);
            UIBuilder.Place(waveText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -80f), new Vector2(420f, 36f));

            Set(hud, "healthFill", healthFill);
            Set(hud, "healthText", healthText);
            Set(hud, "xpFill", xpFill);
            Set(hud, "levelText", levelText);
            Set(hud, "timerText", timerText);
            Set(hud, "killsText", killsText);
            Set(hud, "waveText", waveText);

            // ---- Выбор улучшения ----
            var upgradeRoot = UIBuilder.Stretch(UIBuilder.Node("UpgradeUI", root));
            var upgradeUi = upgradeRoot.gameObject.AddComponent<UpgradeUI>();
            var upgradePanel = UIBuilder.Image(upgradeRoot, "Panel", UIBuilder.Dim);
            UIBuilder.Stretch(upgradePanel.rectTransform);
            var upgradeTitle = UIBuilder.Text(upgradePanel.transform, "Title", "Новый уровень! Выбери улучшение", 54f, UIBuilder.Warm);
            UIBuilder.Center(upgradeTitle.rectTransform, new Vector2(0f, 330f), new Vector2(1400f, 80f));

            var cardsRow = UIBuilder.Center(UIBuilder.Node("Cards", upgradePanel.transform), new Vector2(0f, -20f), new Vector2(1300f, 480f));
            var row = cardsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 40f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var cardButtons = new Object[3];
            var cardTitles = new Object[3];
            var cardDescriptions = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                var card = UIBuilder.Button(cardsRow, "Card" + (i + 1), null, new Vector2(380f, 460f));
                var title = UIBuilder.Text(card.transform, "Title", "Улучшение", 40f, UIBuilder.Warm);
                UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(330f, 130f));
                title.fontStyle = FontStyles.Bold;
                var description = UIBuilder.Text(card.transform, "Description", "Описание", 30f, Color.white);
                UIBuilder.Center(description.rectTransform, new Vector2(0f, -60f), new Vector2(330f, 240f));
                cardButtons[i] = card;
                cardTitles[i] = title;
                cardDescriptions[i] = description;
            }
            Set(upgradeUi, "panel", upgradePanel.gameObject);
            SetList(upgradeUi, "cardButtons", cardButtons);
            SetList(upgradeUi, "cardTitles", cardTitles);
            SetList(upgradeUi, "cardDescriptions", cardDescriptions);

            // ---- Пауза ----
            var pauseRoot = UIBuilder.Stretch(UIBuilder.Node("PauseUI", root));
            var pauseUi = pauseRoot.gameObject.AddComponent<PauseUI>();
            var pausePanel = UIBuilder.Image(pauseRoot, "Panel", UIBuilder.Dim);
            UIBuilder.Stretch(pausePanel.rectTransform);
            var pauseBox = UIBuilder.VerticalBox(pausePanel.transform, "Box", new Vector2(600f, 440f), 28f, UIBuilder.Navy);
            var pauseTitle = UIBuilder.Text(pauseBox, "Title", "Пауза", 64f, UIBuilder.Warm);
            pauseTitle.rectTransform.sizeDelta = new Vector2(520f, 90f);
            var continueButton = UIBuilder.Button(pauseBox, "Continue", "Продолжить", new Vector2(420f, 90f));
            var pauseMenuButton = UIBuilder.Button(pauseBox, "Menu", "В меню", new Vector2(420f, 90f));
            Set(pauseUi, "panel", pausePanel.gameObject);
            Set(pauseUi, "continueButton", continueButton);
            Set(pauseUi, "menuButton", pauseMenuButton);

            // ---- Результаты ----
            var overRoot = UIBuilder.Stretch(UIBuilder.Node("GameOverUI", root));
            var overUi = overRoot.gameObject.AddComponent<GameOverUI>();
            var overPanel = UIBuilder.Image(overRoot, "Panel", UIBuilder.Dim);
            UIBuilder.Stretch(overPanel.rectTransform);
            var overBox = UIBuilder.VerticalBox(overPanel.transform, "Box", new Vector2(760f, 660f), 22f, UIBuilder.Navy);
            var overTitle = UIBuilder.Text(overBox, "Title", "Рассвет!", 76f, UIBuilder.Warm);
            overTitle.rectTransform.sizeDelta = new Vector2(680f, 100f);
            overTitle.fontStyle = FontStyles.Bold;
            var overSubtitle = UIBuilder.Text(overBox, "Subtitle", "Ты продержался до утра", 32f, UIBuilder.Pale);
            overSubtitle.rectTransform.sizeDelta = new Vector2(680f, 50f);
            var overStats = UIBuilder.Text(overBox, "Stats", "Время: 00:00", 36f, Color.white);
            overStats.rectTransform.sizeDelta = new Vector2(680f, 180f);
            var restartButton = UIBuilder.Button(overBox, "Restart", "Заново", new Vector2(420f, 90f));
            var overMenuButton = UIBuilder.Button(overBox, "Menu", "В меню", new Vector2(420f, 90f));
            Set(overUi, "panel", overPanel.gameObject);
            Set(overUi, "titleText", overTitle);
            Set(overUi, "subtitleText", overSubtitle);
            Set(overUi, "statsText", overStats);
            Set(overUi, "restartButton", restartButton);
            Set(overUi, "menuButton", overMenuButton);

            // Панели скрываются скриптами в Awake; в сцене выключаем, чтобы не мешали в редакторе
            upgradePanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            overPanel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Сцена MainMenu

        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color(0.03f, 0.04f, 0.09f));

            var canvas = UIBuilder.Canvas("UI");
            var root = canvas.transform;
            var menu = canvas.gameObject.AddComponent<MainMenuUI>();

            var glow = UIBuilder.Image(root, "Glow", new Color(1f, 0.75f, 0.3f, 0.08f));
            UIBuilder.Center(glow.rectTransform, new Vector2(0f, 260f), new Vector2(1500f, 320f));

            var title = UIBuilder.Text(root, "Title", "LAST LANTERN", 130f, UIBuilder.Warm);
            UIBuilder.Center(title.rectTransform, new Vector2(0f, 290f), new Vector2(1600f, 170f));
            title.fontStyle = FontStyles.Bold;
            var subtitle = UIBuilder.Text(root, "Subtitle", "Тени заполнили лес. Продержись до рассвета.", 36f, UIBuilder.Pale);
            UIBuilder.Center(subtitle.rectTransform, new Vector2(0f, 180f), new Vector2(1400f, 60f));

            // Главная панель
            var mainPanel = UIBuilder.Center(UIBuilder.Node("MainPanel", root), new Vector2(0f, -140f), new Vector2(600f, 380f));
            var layout = mainPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var play = UIBuilder.Button(mainPanel, "Play", "Играть", new Vector2(460f, 96f), 42f);
            var settings = UIBuilder.Button(mainPanel, "Settings", "Настройки", new Vector2(460f, 96f), 42f);
            var exit = UIBuilder.Button(mainPanel, "Exit", "Выход", new Vector2(460f, 96f), 42f);

            // Настройки
            var settingsBox = UIBuilder.VerticalBox(root, "SettingsPanel", new Vector2(760f, 480f), 28f, UIBuilder.Navy);
            settingsBox.anchoredPosition = new Vector2(0f, -120f);
            var settingsTitle = UIBuilder.Text(settingsBox, "Title", "Настройки", 56f, UIBuilder.Warm);
            settingsTitle.rectTransform.sizeDelta = new Vector2(680f, 80f);
            var volumeLabel = UIBuilder.Text(settingsBox, "VolumeLabel", "Громкость", 32f, Color.white);
            volumeLabel.rectTransform.sizeDelta = new Vector2(680f, 50f);
            var volume = UIBuilder.Slider(settingsBox, "Volume", new Vector2(480f, 40f), 0.8f);
            var back = UIBuilder.Button(settingsBox, "Back", "Назад", new Vector2(420f, 90f));
            settingsBox.gameObject.SetActive(false);

            var hint = UIBuilder.Text(root, "Hint", "WASD / стрелки / стик — движение    Esc / Start — пауза    Атака — автоматическая", 26f, UIBuilder.Pale);
            UIBuilder.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(1600f, 40f));

            Set(menu, "mainPanel", mainPanel.gameObject);
            Set(menu, "settingsPanel", settingsBox.gameObject);
            Set(menu, "playButton", play);
            Set(menu, "settingsButton", settings);
            Set(menu, "exitButton", exit);
            Set(menu, "backButton", back);
            Set(menu, "volumeSlider", volume);

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        // ------------------------------------------------------------------ Общие помощники

        static Camera CreateCamera(Color background)
        {
            var go = NewObject("Main Camera", null, 0);
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.AddComponent<AudioListener>();
            return cam;
        }

        static void CreateEventSystem()
        {
            var go = NewObject("EventSystem", null, 0);
            go.AddComponent<EventSystem>();
            go.AddComponent<UIInputFix>(); // до модуля — чтобы успеть назначить действия
            go.AddComponent<InputSystemUIInputModule>();
        }

        static GameObject NewObject(string name, Transform parent, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            if (parent) go.transform.SetParent(parent, false);
            return go;
        }

        static SpriteRenderer AddSprite(GameObject parent, string name, Sprite sprite, Material material, int order, float scale)
        {
            var go = NewObject(name, parent.transform, parent.layer);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = material;
            sr.sortingOrder = order;
            return sr;
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        /// <summary>Свет должен влиять на все Sorting Layers (иначе созданный кодом Light 2D может ничего не освещать).</summary>
        static void ApplyToAllSortingLayers(Light2D light)
        {
            var so = new SerializedObject(light);
            var property = so.FindProperty("m_ApplyToSortingLayers");
            if (property == null) return;
            var layers = SortingLayer.layers;
            property.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
                property.GetArrayElementAtIndex(i).intValue = layers[i].id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Запись в приватные [SerializeField] поля через SerializedObject
        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) { Debug.LogError($"[Last Lantern] Нет поля {field} в {target.GetType().Name}"); return; }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) { Debug.LogError($"[Last Lantern] Нет поля {field} в {target.GetType().Name}"); return; }
            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetList(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null || !property.isArray) { Debug.LogError($"[Last Lantern] Нет списка {field} в {target.GetType().Name}"); return; }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
