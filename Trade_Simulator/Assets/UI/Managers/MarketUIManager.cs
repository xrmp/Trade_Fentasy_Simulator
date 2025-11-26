using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;

public class MarketUIManager : MonoBehaviour
{
    [Header("Панель рынка")]
    public GameObject marketPanel;
    public Transform goodsListParent;
    public GameObject goodItemPrefab;

    [Header("Информация о товаре")]
    public TMP_Text goodNameText;
    public TMP_Text goodPriceText;
    public TMP_Text goodWeightText;
    public TMP_Text playerQuantityText;

    [Header("Управление")]
    public TMP_InputField quantityInput;
    public Button buyButton;
    public Button sellButton;
    public TMP_Text totalCostText;

    [Header("Город")]
    public TMP_Text cityNameText;

    private Entity _selectedGood = Entity.Null;
    private int _currentQuantity = 1;
    private Dictionary<Entity, GameObject> _goodUIItems = new Dictionary<Entity, GameObject>();

    void Start()
    {
        buyButton.onClick.AddListener(BuyGood);
        sellButton.onClick.AddListener(SellGood);
        quantityInput.onValueChanged.AddListener(OnQuantityChanged);
    }

    void Update()
    {
        if (marketPanel.activeInHierarchy)
        {
            UpdateMarketUI();
        }
    }

    public void OpenMarket()
    {
        marketPanel.SetActive(true);
        PopulateGoodsList();
        UpdateSelectedGoodInfo();
    }

    public void CloseMarket()
    {
        marketPanel.SetActive(false);
        ClearGoodsList();
    }

    private void PopulateGoodsList()
    {
        ClearGoodsList();

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var goodsQuery = entityManager.CreateEntityQuery(typeof(GoodData));
        var goods = goodsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var goodEntity in goods)
        {
            var goodData = entityManager.GetComponentData<GoodData>(goodEntity);
            var goodUI = Instantiate(goodItemPrefab, goodsListParent);

            var texts = goodUI.GetComponentsInChildren<TMP_Text>();
            texts[0].text = goodData.Name.ToString();
            texts[1].text = $"Цена: {GetGoodPrice(goodEntity)}";
            texts[2].text = $"Вес: {goodData.WeightPerUnit}";

            var button = goodUI.GetComponent<Button>();
            button.onClick.AddListener(() => SelectGood(goodEntity));

            _goodUIItems[goodEntity] = goodUI;
        }

        goods.Dispose();

        // Выбираем первый товар по умолчанию
        if (goods.Length > 0)
        {
            SelectGood(goods[0]);
        }
    }

    private void SelectGood(Entity goodEntity)
    {
        _selectedGood = goodEntity;
        UpdateSelectedGoodInfo();
    }

    private void UpdateSelectedGoodInfo()
    {
        if (_selectedGood == Entity.Null) return;

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var goodData = entityManager.GetComponentData<GoodData>(_selectedGood);

        goodNameText.text = goodData.Name.ToString();
        goodPriceText.text = $"Цена: {GetGoodPrice(_selectedGood)}";
        goodWeightText.text = $"Вес за ед.: {goodData.WeightPerUnit}";

        UpdatePlayerInventoryInfo();
        UpdateTransactionButtons();
    }

    private void UpdatePlayerInventoryInfo()
    {
        if (_selectedGood == Entity.Null) return;

        var playerQuantity = GetPlayerGoodQuantity(_selectedGood);
        playerQuantityText.text = $"В инвентаре: {playerQuantity}";
    }

    private void UpdateTransactionButtons()
    {
        if (_selectedGood == Entity.Null) return;

        var totalCost = _currentQuantity * GetGoodPrice(_selectedGood);
        totalCostText.text = $"Общая стоимость: {totalCost}";

        var playerGold = GetPlayerGold();
        var playerQuantity = GetPlayerGoodQuantity(_selectedGood);
        var hasCapacity = HasEnoughCapacity(_selectedGood, _currentQuantity);

        buyButton.interactable = playerGold >= totalCost && hasCapacity;
        sellButton.interactable = playerQuantity >= _currentQuantity;
    }

    private void OnQuantityChanged(string value)
    {
        if (int.TryParse(value, out int quantity))
        {
            _currentQuantity = Mathf.Max(1, quantity);
            quantityInput.text = _currentQuantity.ToString();
            UpdateTransactionButtons();
        }
    }

    private void BuyGood()
    {
        if (_selectedGood == Entity.Null) return;

        var totalCost = _currentQuantity * GetGoodPrice(_selectedGood);

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var transactionEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(transactionEntity, new TradeTransaction
        {
            GoodEntity = _selectedGood,
            Quantity = _currentQuantity,
            TotalPrice = totalCost,
            IsBuy = true
        });

        Debug.Log($"Покупка: {_currentQuantity} ед. товара за {totalCost} золота");
    }

    private void SellGood()
    {
        if (_selectedGood == Entity.Null) return;

        var totalCost = _currentQuantity * GetGoodPrice(_selectedGood);

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var transactionEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(transactionEntity, new TradeTransaction
        {
            GoodEntity = _selectedGood,
            Quantity = _currentQuantity,
            TotalPrice = totalCost,
            IsBuy = false
        });

        Debug.Log($"Продажа: {_currentQuantity} ед. товара за {totalCost} золота");
    }

    private int GetGoodPrice(Entity goodEntity)
    {
        // Упрощенная логика цены - в реальной системе брать из CityMarket
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return 10;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (entityManager.HasComponent<GoodData>(goodEntity))
        {
            var goodData = entityManager.GetComponentData<GoodData>(goodEntity);
            return goodData.BaseValue;
        }

        return 10;
    }

    private int GetPlayerGold()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return 0;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));

        if (!playerQuery.IsEmpty)
        {
            var resources = playerQuery.GetSingleton<ConvoyResources>();
            return resources.Gold;
        }

        return 0;
    }

    private int GetPlayerGoodQuantity(Entity goodEntity)
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return 0;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag));

        if (!playerQuery.IsEmpty)
        {
            var playerEntity = playerQuery.GetSingletonEntity();
            var inventory = entityManager.GetBuffer<InventoryBuffer>(playerEntity);

            foreach (var item in inventory)
            {
                if (item.GoodEntity == goodEntity)
                {
                    return item.Quantity;
                }
            }
        }

        return 0;
    }

    private bool HasEnoughCapacity(Entity goodEntity, int quantity)
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return false;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(PlayerConvoy));

        if (!playerQuery.IsEmpty)
        {
            var convoy = playerQuery.GetSingleton<PlayerConvoy>();
            var goodData = entityManager.GetComponentData<GoodData>(goodEntity);
            var requiredCapacity = goodData.WeightPerUnit * quantity;

            return convoy.UsedCapacity + requiredCapacity <= convoy.TotalCapacity;
        }

        return false;
    }

    private void UpdateMarketUI()
    {
        UpdateSelectedGoodInfo();
    }

    private void ClearGoodsList()
    {
        foreach (var item in _goodUIItems.Values)
        {
            Destroy(item);
        }
        _goodUIItems.Clear();
    }
}