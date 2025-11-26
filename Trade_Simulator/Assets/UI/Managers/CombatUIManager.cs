using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;

public class CombatUIManager : MonoBehaviour
{
    [Header("Панель боя")]
    public GameObject combatPanel;
    public TMP_Text combatStatusText;
    public TMP_Text playerPowerText;
    public TMP_Text enemyPowerText;
    public TMP_Text resultText;

    [Header("Кнопки действий")]
    public Button attackButton;
    public Button retreatButton;
    public Button negotiateButton;

    [Header("Статистика боя")]
    public TMP_Text playerLossesText;
    public TMP_Text enemyLossesText;
    public TMP_Text spoilsText;

    private Entity _currentCombatEntity = Entity.Null;

    void Start()
    {
        attackButton.onClick.AddListener(Attack);
        retreatButton.onClick.AddListener(Retreat);
        negotiateButton.onClick.AddListener(Negotiate);

        combatPanel.SetActive(false);
    }

    void Update()
    {
        if (combatPanel.activeInHierarchy)
        {
            UpdateCombatUI();
        }
    }

    public void ShowCombat(Entity combatEntity)
    {
        _currentCombatEntity = combatEntity;
        combatPanel.SetActive(true);
        UpdateCombatUI();
    }

    private void UpdateCombatUI()
    {
        if (_currentCombatEntity == Entity.Null || !World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (!entityManager.Exists(_currentCombatEntity)) return;

        if (entityManager.HasComponent<BanditEncounter>(_currentCombatEntity))
        {
            var encounter = entityManager.GetComponentData<BanditEncounter>(_currentCombatEntity);
            UpdateEncounterUI(encounter, entityManager);
        }
        else if (entityManager.HasComponent<CombatResult>(_currentCombatEntity))
        {
            var result = entityManager.GetComponentData<CombatResult>(_currentCombatEntity);
            UpdateResultUI(result);
        }
    }

    private void UpdateEncounterUI(BanditEncounter encounter, EntityManager entityManager)
    {
        combatStatusText.text = "Обнаружены бандиты!";
        enemyPowerText.text = $"Сила бандитов: {encounter.BanditPower}";

        var playerPower = CalculatePlayerPower(entityManager);
        playerPowerText.text = $"Ваша сила: {playerPower}";

        // Скрываем результат пока бой не завершен
        resultText.gameObject.SetActive(false);
        playerLossesText.gameObject.SetActive(false);
        enemyLossesText.gameObject.SetActive(false);
        spoilsText.gameObject.SetActive(false);
    }

    private void UpdateResultUI(CombatResult result)
    {
        combatStatusText.text = "Бой завершен";
        resultText.gameObject.SetActive(true);
        resultText.text = result.Victory ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
        resultText.color = result.Victory ? Color.green : Color.red;

        playerLossesText.gameObject.SetActive(true);
        playerLossesText.text = $"Потери: {result.PlayerLosses} охраны";

        enemyLossesText.gameObject.SetActive(true);
        enemyLossesText.text = $"Бандитов убито: {result.BanditLosses}";

        spoilsText.gameObject.SetActive(true);
        if (result.Victory && result.GoldLost < 0) // Отрицательные потери = трофеи
        {
            spoilsText.text = $"Трофеи: {-result.GoldLost} золота";
        }
        else
        {
            spoilsText.text = $"Потери: {result.GoldLost} золота";
        }

        // Скрываем кнопки действий после боя
        attackButton.gameObject.SetActive(false);
        retreatButton.gameObject.SetActive(false);
        negotiateButton.gameObject.SetActive(false);
    }

    private void Attack()
    {
        if (_currentCombatEntity == Entity.Null) return;

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (entityManager.HasComponent<BanditEncounter>(_currentCombatEntity))
        {
            var encounter = entityManager.GetComponentData<BanditEncounter>(_currentCombatEntity);
            // Запускаем бой
            Debug.Log("⚔️ Атакуем бандитов!");

            // Здесь будет логика начала боя
        }
    }

    private void Retreat()
    {
        if (_currentCombatEntity == Entity.Null) return;

        Debug.Log("🏃 Отступаем!");

        // Логика отступления
        CloseCombat();
    }

    private void Negotiate()
    {
        if (_currentCombatEntity == Entity.Null) return;

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));

        if (!playerQuery.IsEmpty)
        {
            var resources = playerQuery.GetSingleton<ConvoyResources>();
            var bribeAmount = 50; // Сумма взятки

            if (resources.Gold >= bribeAmount)
            {
                resources.Gold -= bribeAmount;
                entityManager.SetComponentData(playerQuery.GetSingletonEntity(), resources);

                Debug.Log($"💰 Заплатили {bribeAmount} золота бандитам");
                CloseCombat();
            }
            else
            {
                Debug.Log("❌ Недостаточно золота для переговоров");
            }
        }
    }

    private void CloseCombat()
    {
        if (_currentCombatEntity != Entity.Null && World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (entityManager.Exists(_currentCombatEntity))
            {
                entityManager.DestroyEntity(_currentCombatEntity);
            }
        }

        _currentCombatEntity = Entity.Null;
        combatPanel.SetActive(false);

        // Сбрасываем UI
        attackButton.gameObject.SetActive(true);
        retreatButton.gameObject.SetActive(true);
        negotiateButton.gameObject.SetActive(true);
        resultText.gameObject.SetActive(false);
        playerLossesText.gameObject.SetActive(false);
        enemyLossesText.gameObject.SetActive(false);
        spoilsText.gameObject.SetActive(false);
    }

    private int CalculatePlayerPower(EntityManager entityManager)
    {
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));

        if (!playerQuery.IsEmpty)
        {
            var resources = playerQuery.GetSingleton<ConvoyResources>();
            return resources.Guards * 10 + (int)(resources.Morale * 20);
        }

        return 0;
    }
}