using UnityEngine;
using Unity.Entities;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Менеджеры")]
    public ConvoyUIManager convoyUI;
    public MarketUIManager marketUI;
    public RoutePlannerUIManager routeUI;
    public EventLogUIManager eventLogUI;
    public InventoryUIManager inventoryUI;
    public CombatUIManager combatUI;
    public WagonUIManager wagonUI;

    [Header("Настройки игры")]
    public bool debugMode = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("🎮 GameManager инициализирован");
    }

    private void Update()
    {
        // Глобальные горячие клавиши
        HandleGlobalInput();
    }

    private void HandleGlobalInput()
    {
        // Открытие/закрытие UI панелей
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMarket();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRoutePlanner();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleEventLog();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            ToggleWagonManager();
        }

        // Сохранение/загрузка
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadGame();
        }
    }

    public void ToggleMarket()
    {
        if (marketUI != null)
        {
            if (marketUI.marketPanel.activeInHierarchy)
            {
                marketUI.CloseMarket();
            }
            else
            {
                marketUI.OpenMarket();
            }
        }
    }

    public void ToggleInventory()
    {
        if (inventoryUI != null)
        {
            if (inventoryUI.inventoryPanel.activeInHierarchy)
            {
                inventoryUI.CloseInventory();
            }
            else
            {
                inventoryUI.OpenInventory();
            }
        }
    }

    public void ToggleRoutePlanner()
    {
        if (routeUI != null)
        {
            if (routeUI.routePanel.activeInHierarchy)
            {
                routeUI.CancelRoute();
            }
            else
            {
                routeUI.OpenRoutePlanner();
            }
        }
    }

    public void ToggleEventLog()
    {
        if (eventLogUI != null)
        {
            eventLogUI.ToggleLogPanel();
        }
    }

    public void ToggleWagonManager()
    {
        if (wagonUI != null)
        {
            if (wagonUI.wagonsPanel.activeInHierarchy)
            {
                wagonUI.CloseWagonsManager();
            }
            else
            {
                wagonUI.OpenWagonsManager();
            }
        }
    }

    public void SaveGame()
    {
        Debug.Log("💾 Сохранение игры...");
        // Здесь будет логика сохранения
    }

    public void LoadGame()
    {
        Debug.Log("📂 Загрузка игры...");
        // Здесь будет логика загрузки
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Методы для управления игровым состоянием
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void ShowGameOver()
    {
        // Логика экрана Game Over
        Debug.Log("💀 Игра окончена!");
    }

    public void ShowVictory()
    {
        // Логика экрана победы
        Debug.Log("🎉 Победа!");
    }
}