using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Managers;

namespace UI.Managers
{
    public class BootstrapUIManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject bootstrapPanel;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button saveGameButton;
        [SerializeField] private TextMeshProUGUI gameStateText;
        [SerializeField] private TextMeshProUGUI systemStatusText;

        private GameManager _gameManager;
        private ECSBootstrap _ecsBootstrap;

        private void Awake()
        {
            Debug.Log("🎮 BootstrapUIManager: Инициализация...");

            _gameManager = FindAnyObjectByType<GameManager>();
            _ecsBootstrap = FindAnyObjectByType<ECSBootstrap>();

            SetupUIEvents();
        }

        private void Start()
        {
            UpdateSystemStatus();
        }

        private void Update()
        {
            UpdateGameStateDisplay();
        }

        private void SetupUIEvents()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClick);

            if (loadGameButton != null)
                loadGameButton.onClick.AddListener(OnLoadGameClick);

            if (saveGameButton != null)
                saveGameButton.onClick.AddListener(OnSaveGameClick);
        }

        private void OnNewGameClick()
        {
            Debug.Log("🎯 Bootstrap: Запуск новой игры");
            _gameManager?.StartNewGame();
        }

        private void OnLoadGameClick()
        {
            Debug.Log("💾 Bootstrap: Загрузка игры");
            _gameManager?.LoadGame("test_save");
        }

        private void OnSaveGameClick()
        {
            Debug.Log("💾 Bootstrap: Сохранение игры");
            _gameManager?.SaveGame("test_save");
        }

        private void UpdateGameStateDisplay()
        {
            if (gameStateText != null && _gameManager != null)
            {
                gameStateText.text = $"Состояние: {_gameManager.currentGameState}";
            }
        }

        private void UpdateSystemStatus()
        {
            if (systemStatusText != null)
            {
                string status = "Системы:\n";

                if (_ecsBootstrap != null)
                {
                    status += $"ECS: {(_ecsBootstrap.AreKeySystemsInitialized() ? "✅" : "❌")}\n";
                }

                if (_gameManager != null)
                {
                    status += $"GameManager: ✅\n";
                }

                systemStatusText.text = status;
            }
        }

        public void ShowPanel(bool show)
        {
            if (bootstrapPanel != null)
                bootstrapPanel.SetActive(show);
        }
    }
}