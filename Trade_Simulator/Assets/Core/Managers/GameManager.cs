using Unity.Entities;
using UnityEngine;
namespace Core.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Состояние игры")]
        [Tooltip("Текущее состояние игры")]
        public GameState currentGameState = GameState.Initializing;

        [Tooltip("Пауза ли игра")]
        public bool isGamePaused = false;

        [Header("Настройки игры")]
        [Tooltip("Автосохранение включено")]
        public bool autoSaveEnabled = true;

        [Tooltip("Интервал автосохранения в секундах")]
        public float autoSaveInterval = 120f;

        [Header("Ссылки на системы")]
        [Tooltip("Ссылка на бутстрап ECS")]
        public ECSBootstrap ecsBootstrap;

        [Header("Управление сценами")]
        [Tooltip("Главное меню сцена")]
        public string mainMenuScene = "MainMenu";

        [Tooltip("Основная игровая сцена")]
        public string gameScene = "MainGame";

        // События игры
        public System.Action<GameState> OnGameStateChanged;
        public System.Action<bool> OnPauseStateChanged;
        public System.Action<string> OnGameEvent;

        private float _autoSaveTimer;
        private EntityManager _entityManager;
        private World _ecsWorld;

        private void Awake()
        {
            Debug.Log("🎮 GameManager: Инициализация...");

            // Получаем ссылки на ECS
            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld != null)
            {
                _entityManager = _ecsWorld.EntityManager;
            }

            // Находим зависимости
            if (ecsBootstrap == null)
                ecsBootstrap = FindAnyObjectByType<ECSBootstrap>();

            ChangeGameState(GameState.Loading);
        }

        private void Start()
        {
            // Ждем инициализации ECS
            if (_ecsWorld != null && AreECSSystemsReady())
            {
                ChangeGameState(GameState.MainMenu);
            }
            else
            {
                Debug.LogWarning("⚠️ GameManager: ECS системы еще не готовы, отложенная инициализация");
                Invoke(nameof(DelayedInitialization), 1f);
            }
        }

        private void Update()
        {
            if (isGamePaused) return;

            UpdateAutoSave();
            UpdateGameState();
            ProcessInput();
        }

        private void ProcessInput()
        {
            // Базовая обработка ввода (можно вынести в отдельную систему)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }
        }

        private void UpdateAutoSave()
        {
            if (!autoSaveEnabled || currentGameState != GameState.Playing) return;

            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= autoSaveInterval)
            {
                TriggerAutoSave();
                _autoSaveTimer = 0f;
            }
        }

        private void UpdateGameState()
        {
            // Проверяем условия смены состояния игры
            switch (currentGameState)
            {
                case GameState.Playing:
                    CheckGameOverConditions();
                    break;
                case GameState.GameOver:
                    // Обработка состояния проигрыша
                    break;
            }
        }

        private void DelayedInitialization()
        {
            if (AreECSSystemsReady())
            {
                ChangeGameState(GameState.MainMenu);
                Debug.Log("✅ GameManager: Отложенная инициализация завершена");
            }
            else
            {
                Debug.LogError("❌ GameManager: ECS системы не готовы после задержки");
                ChangeGameState(GameState.Error);
            }
        }

        private bool AreECSSystemsReady()
        {
            if (_ecsWorld == null) return false;

            // Проверяем наличие ключевых систем
            var configQuery = _entityManager.CreateEntityQuery(typeof(GameConfig));
            var hasConfig = !configQuery.IsEmpty;
            configQuery.Dispose();

            return hasConfig; // Конфиг должен быть всегда
        }
        public void StartNewGame()
        {
            Debug.Log("🎯 GameManager: Запуск новой игры...");

            ChangeGameState(GameState.Playing);

            // Сбрасываем игровые данные
            ResetGameData();

            // Триггерим событие начала игры
            OnGameEvent?.Invoke("Новая игра началась!");
        }

        public void LoadGame(string saveName = "quicksave")
        {
            Debug.Log($"💾 GameManager: Загрузка игры - {saveName}");

            ChangeGameState(GameState.Loading);

            bool success = SaveGameData(saveName, true);
            if (success)
            {
                ChangeGameState(GameState.Playing);
                OnGameEvent?.Invoke($"Игра загружена: {saveName}");
            }
            else
            {
                ChangeGameState(GameState.MainMenu);
                OnGameEvent?.Invoke("Ошибка загрузки игры");
            }
        }


        public void SaveGame(string saveName = "quicksave")
        {
            if (currentGameState != GameState.Playing)
            {
                Debug.LogWarning("⚠️ GameManager: Нельзя сохранить игру не в игровом состоянии");
                return;
            }

            Debug.Log($"💾 GameManager: Сохранение игры - {saveName}");

            bool success = SaveGameData(saveName, false);
            if (success)
            {
                OnGameEvent?.Invoke($"Игра сохранена: {saveName}");
            }
            else
            {
                OnGameEvent?.Invoke("Ошибка сохранения игры");
            }
        }


        public void SetPause(bool paused)
        {
            if (isGamePaused == paused) return;

            isGamePaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            OnPauseStateChanged?.Invoke(paused);
            OnGameEvent?.Invoke(paused ? "Игра на паузе" : "Игра продолжена");

            Debug.Log($"⏸️ GameManager: Пауза - {paused}");
        }


        public void TogglePause()
        {
            SetPause(!isGamePaused);
        }


        public void ReturnToMainMenu()
        {
            Debug.Log("🏠 GameManager: Возврат в главное меню");

            ChangeGameState(GameState.MainMenu);
            SetPause(false);

            // Очищаем игровые данные
            ClearGameData();
        }


        public void LoadScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }


        public void QuitGame()
        {
            Debug.Log("🚪 GameManager: Выход из игры");

            // Автосохранение при выходе
            if (autoSaveEnabled && currentGameState == GameState.Playing)
            {
                SaveGame("autosave_exit");
            }

            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void ChangeGameState(GameState newState)
        {
            if (currentGameState == newState) return;

            var oldState = currentGameState;
            currentGameState = newState;

            Debug.Log($"🔄 GameManager: Смена состояния {oldState} -> {newState}");

            OnGameStateChanged?.Invoke(newState);

            // Обработка специальных переходов
            HandleStateTransition(oldState, newState);
        }

        private void HandleStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Playing:
                    SetPause(false);
                    break;
                case GameState.Paused:
                    SetPause(true);
                    break;
                case GameState.GameOver:
                    SetPause(true);
                    OnGameEvent?.Invoke("Игра завершена!");
                    break;
            }
        }

        private void ResetGameData()
        {
            // Логика сброса игровых данных будет в системе ECS
            Debug.Log("🔄 GameManager: Сброс игровых данных");

            // Создаем команду для системы сброса
            if (_entityManager != null)
            {
                var resetEntity = _entityManager.CreateEntity();
                _entityManager.AddComponent<ResetGameCommand>(resetEntity);
            }
        }

        private void ClearGameData()
        {
            // Логика очистки игровых данных будет в системе ECS
            Debug.Log("🧹 GameManager: Очистка игровых данных");
        }

        private bool SaveGameData(string saveName, bool isLoad)
        {
            // Упрощенная реализация сохранения/загрузки
            // В реальном проекте здесь будет сложная логика сериализации ECS
            try
            {
                if (isLoad)
                {
                    // Загрузка данных
                    Debug.Log($"📥 Загрузка данных: {saveName}");
                    return true;
                }
                else
                {
                    // Сохранение данных
                    Debug.Log($"📤 Сохранение данных: {saveName}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Ошибка {(isLoad ? "загрузки" : "сохранения")}: {e.Message}");
                return false;
            }
        }

        private void CheckGameOverConditions()
        {
            if (_entityManager == null) return;

            // Проверяем условия завершения игры
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));
            if (playerQuery.IsEmpty) return;

            var resources = playerQuery.GetSingleton<ConvoyResources>();

            // Условие проигрыша: нет золота, еды и охраны
            if (resources.Gold <= 0 && resources.Food <= 0 && resources.Guards <= 0)
            {
                ChangeGameState(GameState.GameOver);
            }

            playerQuery.Dispose();
        }

        private void TriggerAutoSave()
        {
            if (autoSaveEnabled && currentGameState == GameState.Playing)
            {
                SaveGame("autosave");
                Debug.Log("💾 GameManager: Автосохранение выполнено");
            }
        }

        private void HandleEscapeKey()
        {
            Debug.Log("⎋ GameManager: Клавиша Escape");

            if (currentGameState == GameState.Playing)
            {
                TogglePause();
            }
            else if (currentGameState == GameState.Paused)
            {
                TogglePause();
            }
            else if (currentGameState == GameState.MainMenu)
            {
                QuitGame();
            }
        }


        public string GetGameInfo()
        {
            var info = $"🎮 Game State: {currentGameState}\n";
            info += $"⏸️ Paused: {isGamePaused}\n";
            info += $"💾 AutoSave: {autoSaveEnabled} ({_autoSaveTimer:F0}/{autoSaveInterval}s)\n";

            if (_ecsWorld != null)
            {
                info += $"🌍 ECS World: {_ecsWorld.Name}\n";
            }

            return info;
        }
    }


    public enum GameState
    {
        Initializing,   // Инициализация
        Loading,        // Загрузка
        MainMenu,       // Главное меню
        Playing,        // Игровой процесс
        Paused,         // Пауза
        GameOver,       // Конец игры
        Error           // Ошибка
    }


    public struct ResetGameCommand : IComponentData { }
}