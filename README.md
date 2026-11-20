# Last Lantern

2D top-down survival / roguelite-арена на Unity. Ночью лес заполняют тени, а ты хранитель последнего фонаря.
Продержись 5 минут до рассвета.

- Unity 2022.3 LTS (подойдёт и Unity 6), URP 2D (Light 2D), новая Input System, Cinemachine 2.x, TextMeshPro
- Namespace: `LastLantern`
- Ассеты платить не нужно: спрайты (пиксель-арт 32×32) и звуки генерируются кодом

## Как запустить

1. **Unity Hub → Add → Add project from disk** и выбрать эту папку. Если Hub спросит версию редактора, выбрать любую 2022.3.x или Unity 6.
2. Открыть проект. Дальше всё идёт само, нужно только соглашаться в диалогах:
   1. `PackageInstaller` ставит URP, Input System, Cinemachine 2.9.7, uGUI/TextMeshPro и пакет Visual Studio.
   2. Unity предлагает включить новую Input System и перезапуститься: нажать **Yes / Перезапустить**.
   3. Импортируются шрифты TextMeshPro.
   4. `ProjectSetup` собирает URP 2D, слои, спрайты, материалы, данные (ScriptableObject), префабы и сцены `MainMenu` и `Game`, а потом настраивает Build Settings.
3. Когда появится окно «Готово!», открыть сцену `Assets/Scenes/MainMenu` и нажать **Play**.

Пересобрать сцены и префабы можно через меню **Last Lantern → Собрать проект**. Данные в `Assets/Data` при пересборке не перезаписываются, поэтому твои настройки баланса сохранятся.

## Управление

| Действие | Клавиатура | Геймпад |
|---|---|---|
| Движение | WASD / стрелки | Левый стик / крестовина |
| Пауза | Esc | Start |
| Атака | автоматическая вспышка | — |

## Геймплей

- **Игрок**: 100 HP, скорость 5. После урона 1 секунда неуязвимости, спрайт в это время мигает.
- **Фонарь**: Light 2D радиусом 6, который медленно гаснет. Масло из убитых теней восстанавливает его.
- **Вспышка**: срабатывает раз в 1.2 с и бьёт всех врагов в радиусе фонаря.
- **Тени**: бегун (20 HP, урон 5), тяжеловес (80 HP, урон 15), стрелок (держит дистанцию, стреляет раз в 2 с). В свете фонаря все они замедляются на 40%.
- **Волны**: каждые 30 секунд враги появляются чаще, их больше и они крепче. К концу забега их до ~200.
- **Уровни**: на каждом уровне игра встаёт на паузу и предлагает 3 случайные карточки улучшений.
- **Победа**: таймер дошёл до 5:00 («Рассвет»). **Поражение**: здоровье упало до 0.

## Где что настраивать

| Что | Где |
|---|---|
| Здоровье, урон, скорость, награда врагов | `Assets/Data/Enemies/*.asset` |
| Карточки улучшений | `Assets/Data/Upgrades/*.asset` (можно добавлять свои через Create → Last Lantern → Upgrade Data) |
| Волны (частота, количество, состав) | объект `Systems` → `WaveManager` |
| Длительность забега | `Systems` → `GameManager` |
| Скорость и HP игрока, радиус фонаря, урон и частота вспышки | префаб `Assets/Prefabs/Player` |
| Звуки и музыка | объект `AudioManager` (появляется в Play Mode): перетащи свои клипы в поля, пустые поля генерируются кодом |

## Архитектура

```
Assets/Scripts
├─ Player/   PlayerController, PlayerHealth, LanternLight, FlashAttack
├─ Enemies/  EnemyBase, EnemyChaser, EnemyShooter, EnemySpawner, EnemyPool, Projectile, ProjectilePool
├─ Systems/  GameManager (состояния), WaveManager, ExperienceSystem, UpgradeManager, AudioManager,
│            GameEvents (шина событий), GameInput (Input Actions), ComponentPool, OilDrop, EffectsManager
├─ Data/     EnemyData, UpgradeData (ScriptableObject)
└─ UI/       HUD, UpgradeUI, PauseUI, GameOverUI, MainMenuUI
Assets/Editor  ProjectSetup (сборка сцен и префабов), ArtGenerator (спрайты), UIBuilder, Installer/PackageInstaller
```

- Системы не держат жёстких ссылок друг на друга, а общаются через `GameEvents` (C# events).
- Враги, снаряды и капли масла живут в пулах (Object Pooling), и вся смерть теней рисуется одной системой частиц. Это нужно, чтобы держать 60 FPS при 150+ врагах.
- Код игры собран в asmdef с `defineConstraints`: пока пакеты не установлены, он просто не компилируется, и поэтому при первом открытии нет красных ошибок и окна Safe Mode.

## Сборка под Windows

**File → Build Settings → Windows**. Сцены `MainMenu` и `Game` уже добавлены, остаётся нажать **Build**.

## Если что-то пошло не так

- **Свет фонаря не виден, всё чёрное или розовое**: в Edit → Project Settings → Graphics должен стоять `Assets/Settings/URP-2D-Pipeline`. Если не помогает, создай новый проект из шаблона **2D (URP)**, скопируй в него папки `Assets/Scripts`, `Assets/Editor` и запусти **Last Lantern → Собрать проект**. Если URP 2D уже настроен, скрипт его не трогает.
- **Русский текст показывается квадратиками**: импортируй Window → TextMeshPro → Import TMP Essential Resources. Кириллицу даёт динамический fallback-шрифт LiberationSans.
- **Меню Last Lantern не появилось**: значит, ещё не установлены пакеты. Выбери Last Lantern → Проверить и установить пакеты (этот пункт работает всегда) или подожди окончания импорта.
- **После первого открытия**: закоммить сгенерированные `.meta`, `Packages/packages-lock.json` и `ProjectSettings/`.
