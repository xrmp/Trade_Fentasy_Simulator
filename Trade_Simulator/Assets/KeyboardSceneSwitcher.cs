using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyboardSceneSwitcher : MonoBehaviour
{
    [Header("Настройки перехода")]
    [SerializeField] private bool enableKeyboardNavigation = true;
    [SerializeField] private KeyCode previousSceneKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode nextSceneKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode menuKey = KeyCode.DownArrow;
    [SerializeField] private float keyCooldown = 0.5f;

    private float _lastKeyPressTime;

    private void Update()
    {
        if (!enableKeyboardNavigation) return;

        // Защита от слишком частых нажатий
        if (Time.time - _lastKeyPressTime < keyCooldown) return;

        if (Input.GetKeyDown(previousSceneKey))
        {
            LoadPreviousScene();
            _lastKeyPressTime = Time.time;
        }
        else if (Input.GetKeyDown(nextSceneKey))
        {
            LoadNextScene();
            _lastKeyPressTime = Time.time;
        }
        else if (Input.GetKeyDown(menuKey))
        {
            LoadMainMenu();
            _lastKeyPressTime = Time.time;
        }
    }

    private void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = (currentIndex + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(nextIndex);
        Debug.Log($"➡️ Переход к сцене: {nextIndex}");
    }

    private void LoadPreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int previousIndex = (currentIndex - 1 + SceneManager.sceneCountInBuildSettings) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(previousIndex);
        Debug.Log($"⬅️ Переход к сцене: {previousIndex}");
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
        Debug.Log("🔻 Возврат в главное меню");
    }
}