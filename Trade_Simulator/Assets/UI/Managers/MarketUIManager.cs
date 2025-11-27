using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;

namespace UI.Managers
{

    public class MarketUIManager : MonoBehaviour
    {
        [Header("Основные элементы")]
        [SerializeField] private GameObject marketPanel;
        [SerializeField] private TextMeshProUGUI cityNameText;
        [SerializeField] private TextMeshProUGUI marketInfoText;

        [Header("Списки товаров")]
        [SerializeField] private Transform goodsForSaleContainer;
        [SerializeField] private Transform playerGoodsContainer;
        [SerializeField] private GameObject goodItemUIPrefab;

        [Header("Торговые операции")]
        [SerializeField] private TextMeshProUGUI selectedGoodText;
        [SerializeField] private TextMeshProUGUI transactionInfoText;
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private TextMeshProUGUI totalPriceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;

        [Header("Информация о товаре")]
        [SerializeField] private TextMeshProUGUI goodNameText;
        [SerializeField] private TextMeshProUGUI goodPriceText;
        [SerializeField] private TextMeshProUGUI goodSupplyDemandText;
        [SerializeField] private TextMeshProUGUI goodWeightText;

        private EntityManager _entityManager;
        private World _ecsWorld; // ДОБАВЛЕНО: объявление переменной
        private Entity _currentMarketEntity;
        private Entity _selectedGoodEntity;
        private List<GameObject> _goodsUIInstances = new List<GameObject>();
        private List<GameObject> _inventoryUIInstances = new List<GameObject>();

        private void Awake()
        {
            Debug.Log("🏪 MarketUIManager: Инициализация...");

            _ecsWorld = World.DefaultGameObjectInjectionWorld; // ТЕПЕРЬ РАБОТАЕТ
            if (_ecsWorld != null)
            {
                _entityManager = _ecsWorld.EntityManager;
            }

            SetupUIEvents();
        }

        private void SetupUIEvents()
        {
            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuyButtonClick);

            if (sellButton != null)
                sellButton.onClick.AddListener(OnSellButtonClick);

            if (quantityInput != null)
                quantityInput.onValueChanged.AddListener(OnQuantityChanged);
        }


        public void OpenMarket(Entity marketEntity)
        {
            _currentMarketEntity = marketEntity;

            if (marketPanel != null)
                marketPanel.SetActive(true);

            UpdateMarketUI();
            Debug.Log($"🏪 MarketUIManager: Открыт рынок города");
        }


        public void CloseMarket()
        {
            if (marketPanel != null)
                marketPanel.SetActive(false);

            ClearGoodsLists();
            Debug.Log("🏪 MarketUIManager: Рынок закрыт");
        }

        private void UpdateMarketUI()
        {
            if (_currentMarketEntity == Entity.Null) return;

            UpdateMarketInfo();
            UpdateGoodsForSale();
            UpdatePlayerInventory();
            UpdateTransactionInfo();
        }

        private void UpdateMarketInfo()
        {
            if (!_entityManager.HasComponent<CityMarket>(_currentMarketEntity)) return;

            var market = _entityManager.GetComponentData<CityMarket>(_currentMarketEntity);

            // Получаем информацию о городе
            if (_entityManager.HasComponent<City>(market.CityEntity))
            {
                var city = _entityManager.GetComponentData<City>(market.CityEntity);

                if (cityNameText != null)
                    cityNameText.text = city.Name.ToString();
            }

            if (marketInfoText != null)
            {
                marketInfoText.text = $"Множитель цен: {market.PriceMultiplier:F2}x\n" +
                                    $"Объем торговли: {market.TradeVolume:F2}";
            }
        }

        private void UpdateGoodsForSale()
        {
            ClearGoodsList(_goodsUIInstances, goodsForSaleContainer);

            if (!_entityManager.HasBuffer<GoodPriceBuffer>(_currentMarketEntity)) return;

            var priceBuffer = _entityManager.GetBuffer<GoodPriceBuffer>(_currentMarketEntity);

            foreach (var priceData in priceBuffer)
            {
                if (!_entityManager.HasComponent<GoodData>(priceData.GoodEntity)) continue;

                var goodData = _entityManager.GetComponentData<GoodData>(priceData.GoodEntity);
                CreateGoodUIItem(priceData.GoodEntity, goodData, priceData, goodsForSaleContainer, _goodsUIInstances, true);
            }
        }

        private void UpdatePlayerInventory()
        {
            ClearGoodsList(_inventoryUIInstances, playerGoodsContainer);

            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag));
            if (playerQuery.IsEmpty) return;

            var playerEntity = playerQuery.GetSingletonEntity();

            if (!_entityManager.HasBuffer<InventoryBuffer>(playerEntity)) return;

            var inventory = _entityManager.GetBuffer<InventoryBuffer>(playerEntity);

            foreach (var item in inventory)
            {
                if (item.Quantity <= 0) continue;
                if (!_entityManager.HasComponent<GoodData>(item.GoodEntity)) continue;

                var goodData = _entityManager.GetComponentData<GoodData>(item.GoodEntity);
                CreateGoodUIItem(item.GoodEntity, goodData, default, playerGoodsContainer, _inventoryUIInstances, false);
            }

            playerQuery.Dispose();
        }

        private void CreateGoodUIItem(Entity goodEntity, GoodData goodData, GoodPriceBuffer priceData,
                                    Transform container, List<GameObject> list, bool isForSale)
        {
            if (goodItemUIPrefab == null || container == null) return;

            var goodUI = Instantiate(goodItemUIPrefab, container);
            var goodItem = goodUI.GetComponent<MarketGoodItemUI>();

            if (goodItem != null)
            {
                if (isForSale)
                {
                    goodItem.SetupForSale(goodEntity, goodData, priceData, OnGoodSelected);
                }
                else
                {
                    goodItem.SetupForInventory(goodEntity, goodData, OnGoodSelected);
                }
            }

            list.Add(goodUI);
        }

        private void ClearGoodsList(List<GameObject> list, Transform container)
        {
            foreach (var item in list)
            {
                if (item != null)
                    Destroy(item);
            }
            list.Clear();
        }

        private void ClearGoodsLists()
        {
            ClearGoodsList(_goodsUIInstances, goodsForSaleContainer);
            ClearGoodsList(_inventoryUIInstances, playerGoodsContainer);
        }

        private void OnGoodSelected(Entity goodEntity, GoodData goodData, GoodPriceBuffer priceData)
        {
            _selectedGoodEntity = goodEntity;
            UpdateSelectedGoodInfo(goodData, priceData);
            UpdateTransactionInfo();
        }

        private void UpdateSelectedGoodInfo(GoodData goodData, GoodPriceBuffer priceData)
        {
            if (goodNameText != null)
                goodNameText.text = goodData.Name.ToString();

            if (goodPriceText != null)
                goodPriceText.text = $"{priceData.Price}G";

            if (goodSupplyDemandText != null)
                goodSupplyDemandText.text = $"Спрос: {priceData.Demand:F2} | Предложение: {priceData.Supply:F2}";

            if (goodWeightText != null)
                goodWeightText.text = $"{goodData.WeightPerUnit}кг/ед.";

            if (selectedGoodText != null)
                selectedGoodText.text = $"Выбран: {goodData.Name}";
        }

        private void OnQuantityChanged(string quantityStr)
        {
            UpdateTransactionInfo();
        }

        private void UpdateTransactionInfo()
        {
            if (_selectedGoodEntity == Entity.Null || quantityInput == null) return;

            if (!int.TryParse(quantityInput.text, out int quantity) || quantity <= 0)
            {
                totalPriceText.text = "0G";
                return;
            }

            // Получаем цену товара
            int pricePerUnit = GetGoodPrice(_selectedGoodEntity);
            int totalPrice = pricePerUnit * quantity;

            if (totalPriceText != null)
                totalPriceText.text = $"{totalPrice}G";

            // Проверяем возможность операции
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources), typeof(PlayerConvoy));
            if (playerQuery.IsEmpty) return;

            var playerEntity = playerQuery.GetSingletonEntity();
            var resources = _entityManager.GetComponentData<ConvoyResources>(playerEntity);
            var convoy = _entityManager.GetComponentData<PlayerConvoy>(playerEntity);

            // Проверяем возможность покупки
            if (buyButton != null)
            {
                var goodData = _entityManager.GetComponentData<GoodData>(_selectedGoodEntity);
                var totalWeight = goodData.WeightPerUnit * quantity;
                bool canAfford = resources.Gold >= totalPrice;
                bool hasCapacity = convoy.UsedCapacity + totalWeight <= convoy.TotalCapacity;

                buyButton.interactable = canAfford && hasCapacity;
            }

            // Проверяем возможность продажи
            if (sellButton != null)
            {
                bool hasEnoughGoods = HasEnoughGoodsInInventory(_selectedGoodEntity, quantity);
                sellButton.interactable = hasEnoughGoods;
            }

            playerQuery.Dispose();
        }

        private int GetGoodPrice(Entity goodEntity)
        {
            if (!_entityManager.HasBuffer<GoodPriceBuffer>(_currentMarketEntity)) return 0;

            var priceBuffer = _entityManager.GetBuffer<GoodPriceBuffer>(_currentMarketEntity);
            foreach (var priceData in priceBuffer)
            {
                if (priceData.GoodEntity == goodEntity)
                    return priceData.Price;
            }

            return 0;
        }

        private bool HasEnoughGoodsInInventory(Entity goodEntity, int quantity)
        {
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag));
            if (playerQuery.IsEmpty) return false;

            var playerEntity = playerQuery.GetSingletonEntity();
            if (!_entityManager.HasBuffer<InventoryBuffer>(playerEntity)) return false;

            var inventory = _entityManager.GetBuffer<InventoryBuffer>(playerEntity);
            foreach (var item in inventory)
            {
                if (item.GoodEntity == goodEntity)
                    return item.Quantity >= quantity;
            }

            playerQuery.Dispose();
            return false;
        }

        private void OnBuyButtonClick()
        {
            if (_selectedGoodEntity == Entity.Null || quantityInput == null) return;

            if (!int.TryParse(quantityInput.text, out int quantity) || quantity <= 0) return;

            // Создаем транзакцию покупки
            CreateTradeTransaction(_selectedGoodEntity, quantity, true);
        }

        private void OnSellButtonClick()
        {
            if (_selectedGoodEntity == Entity.Null || quantityInput == null) return;

            if (!int.TryParse(quantityInput.text, out int quantity) || quantity <= 0) return;

            // Создаем транзакцию продажи
            CreateTradeTransaction(_selectedGoodEntity, quantity, false);
        }

        private void CreateTradeTransaction(Entity goodEntity, int quantity, bool isBuy)
        {
            var transactionEntity = _entityManager.CreateEntity();

            int pricePerUnit = GetGoodPrice(goodEntity);
            int totalPrice = pricePerUnit * quantity;

            _entityManager.AddComponentData(transactionEntity, new TradeTransaction
            {
                GoodEntity = goodEntity,
                MarketEntity = _currentMarketEntity,
                Quantity = quantity,
                TotalPrice = totalPrice,
                IsBuy = isBuy,
                Status = TradeStatus.Pending
            });

            Debug.Log($"💰 MarketUIManager: Создана транзакция - {(isBuy ? "Покупка" : "Продажа")} {quantity} ед.");

            // Обновляем UI после транзакции
            UpdateMarketUI();
        }


        public bool IsMarketOpen()
        {
            return marketPanel != null && marketPanel.activeInHierarchy;
        }
    }

    public class MarketGoodItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI goodNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button selectButton;

        private Entity _goodEntity;
        private System.Action<Entity, GoodData, GoodPriceBuffer> _onSelected;

        public void SetupForSale(Entity goodEntity, GoodData goodData, GoodPriceBuffer priceData,
                               System.Action<Entity, GoodData, GoodPriceBuffer> onSelected)
        {
            _goodEntity = goodEntity;
            _onSelected = onSelected;

            if (goodNameText != null)
                goodNameText.text = goodData.Name.ToString();

            if (priceText != null)
                priceText.text = $"{priceData.Price}G";

            if (quantityText != null)
                quantityText.text = $"Наличие: {priceData.Supply:F1}";

            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelect);
        }

        public void SetupForInventory(Entity goodEntity, GoodData goodData,
                                    System.Action<Entity, GoodData, GoodPriceBuffer> onSelected)
        {
            _goodEntity = goodEntity;
            _onSelected = onSelected;

            if (goodNameText != null)
                goodNameText.text = goodData.Name.ToString();

            if (priceText != null)
                priceText.text = $"{goodData.BaseValue}G";

            if (quantityText != null)
                quantityText.text = "В инвентаре";

            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelect);
        }

        private void OnSelect()
        {
            // Получаем данные товара для передачи
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (entityManager.HasComponent<GoodData>(_goodEntity))
            {
                var goodData = entityManager.GetComponentData<GoodData>(_goodEntity);
                _onSelected?.Invoke(_goodEntity, goodData, default);
            }
        }
    }
}