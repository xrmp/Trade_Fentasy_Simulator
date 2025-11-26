using UnityEngine;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Панель инвентаря")]
    public GameObject inventoryPanel;
    public Transform inventoryContent;
    public GameObject inventoryItemPrefab;

    [Header("Статистика")]
    public TMP_Text totalWeightText;
    public TMP_Text totalValueText;
    public TMP_Text usedCapacityText;

    private Dictionary<Entity, GameObject> _inventoryItems = new Dictionary<Entity, GameObject>();

    void Update()
    {
        if (inventoryPanel.activeInHierarchy)
        {
            UpdateInventoryUI();
        }
    }

    public void OpenInventory()
    {
        inventoryPanel.SetActive(true);
        UpdateInventoryUI();
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        ClearInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        ClearInventoryUI();

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag));

        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();

        // Обновляем статистику
        UpdateInventoryStats(playerEntity, entityManager);

        // Обновляем список товаров
        if (entityManager.HasBuffer<InventoryBuffer>(playerEntity))
        {
            var inventory = entityManager.GetBuffer<InventoryBuffer>(playerEntity);
            int totalValue = 0;

            foreach (var item in inventory)
            {
                if (item.Quantity > 0)
                {
                    AddInventoryItemUI(item.GoodEntity, item.Quantity, entityManager);
                    totalValue += GetGoodValue(item.GoodEntity, entityManager) * item.Quantity;
                }
            }

            totalValueText.text = $"Общая стоимость: {totalValue}";
        }
    }

    private void UpdateInventoryStats(Entity playerEntity, EntityManager entityManager)
    {
        if (entityManager.HasComponent<PlayerConvoy>(playerEntity))
        {
            var convoy = entityManager.GetComponentData<PlayerConvoy>(playerEntity);
            usedCapacityText.text = $"Грузоподъемность: {convoy.UsedCapacity}/{convoy.TotalCapacity}";

            // Расчет общего веса
            if (entityManager.HasBuffer<InventoryBuffer>(playerEntity))
            {
                var inventory = entityManager.GetBuffer<InventoryBuffer>(playerEntity);
                int totalWeight = 0;

                foreach (var item in inventory)
                {
                    var goodData = entityManager.GetComponentData<GoodData>(item.GoodEntity);
                    totalWeight += goodData.WeightPerUnit * item.Quantity;
                }

                totalWeightText.text = $"Общий вес: {totalWeight}";
            }
        }
    }

    private void AddInventoryItemUI(Entity goodEntity, int quantity, EntityManager entityManager)
    {
        if (!entityManager.HasComponent<GoodData>(goodEntity)) return;

        var goodData = entityManager.GetComponentData<GoodData>(goodEntity);
        var itemUI = Instantiate(inventoryItemPrefab, inventoryContent);

        var texts = itemUI.GetComponentsInChildren<TMP_Text>();
        texts[0].text = goodData.Name.ToString();
        texts[1].text = $"Количество: {quantity}";
        texts[2].text = $"Вес: {goodData.WeightPerUnit * quantity}";
        texts[3].text = $"Стоимость: {goodData.BaseValue * quantity}";

        _inventoryItems[goodEntity] = itemUI;
    }

    private int GetGoodValue(Entity goodEntity, EntityManager entityManager)
    {
        if (entityManager.HasComponent<GoodData>(goodEntity))
        {
            var goodData = entityManager.GetComponentData<GoodData>(goodEntity);
            return goodData.BaseValue;
        }
        return 0;
    }

    private void ClearInventoryUI()
    {
        foreach (var item in _inventoryItems.Values)
        {
            Destroy(item);
        }
        _inventoryItems.Clear();
    }
}