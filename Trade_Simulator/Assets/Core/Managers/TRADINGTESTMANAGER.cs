using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;

namespace UI.Managers
{
    public class TradingTestManager : MonoBehaviour
    {
        [Header("Тестовые данные")]
        [SerializeField] private int testPlayerGold = 1000;
        [SerializeField] private List<TestGood> testGoods = new List<TestGood>();

        [Header("UI References")]
        [SerializeField] private Button openMarketButton;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private Button testTradeButton;
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private MarketUIManager marketManager;
        [SerializeField] private ConvoyUIManager convoyManager;

        private EntityManager _entityManager;
        private Entity _testMarketEntity;

        [System.Serializable]
        public class TestGood
        {
            public string name;
            public int basePrice;
            public int weight;
            public GoodCategory category;
            public int playerQuantity;
        }

        private void Start()
        {
            // ИСПРАВЛЕНИЕ: проверяем что World существует перед получением EntityManager
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager; // Теперь не nullable
            }

            CreateTestGoods();
            SetupUIEvents();
            UpdateDebugInfo();
        }

        private void CreateTestGoods()
        {
            testGoods = new List<TestGood>
            {
                new TestGood { name = "Зерно", basePrice = 10, weight = 1, category = GoodCategory.RawMaterials, playerQuantity = 50 },
                new TestGood { name = "Древесина", basePrice = 15, weight = 2, category = GoodCategory.RawMaterials, playerQuantity = 20 },
                new TestGood { name = "Вино", basePrice = 50, weight = 2, category = GoodCategory.Luxury, playerQuantity = 5 },
                new TestGood { name = "Ткань", basePrice = 25, weight = 3, category = GoodCategory.Crafts, playerQuantity = 15 }
            };
        }

        private void SetupUIEvents()
        {
            if (openMarketButton != null)
                openMarketButton.onClick.AddListener(OpenTestMarket);

            if (addGoldButton != null)
                addGoldButton.onClick.AddListener(AddTestGold);

            if (testTradeButton != null)
                testTradeButton.onClick.AddListener(TestTrade);
        }

        public void OpenTestMarket()
        {
            // ИСПРАВЛЕНИЕ: проверяем что EntityManager инициализирован
            if (_entityManager == null)
            {
                debugText.text = "❌ ECS мир не инициализирован!";
                return;
            }

            // Создаем тестовый рынок
            _testMarketEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(_testMarketEntity, new CityMarket
            {
                CityEntity = Entity.Null,
                PriceMultiplier = 1.0f,
                TradeVolume = 1.0f
            });

            // Добавляем тестовые товары
            var priceBuffer = _entityManager.AddBuffer<GoodPriceBuffer>(_testMarketEntity);

            foreach (var testGood in testGoods)
            {
                var goodEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(goodEntity, new GoodData
                {
                    Name = testGood.name,
                    BaseValue = testGood.basePrice,
                    WeightPerUnit = testGood.weight,
                    Category = testGood.category,
                    ProfitPerKm = testGood.basePrice * 0.01f,
                    DecayRate = 0.1f
                });

                priceBuffer.Add(new GoodPriceBuffer
                {
                    GoodEntity = goodEntity,
                    Price = testGood.basePrice,
                    Supply = 1.0f,
                    Demand = 1.0f
                });
            }

            // Открываем рынок
            if (marketManager != null)
            {
                marketManager.OpenMarket(_testMarketEntity);
                debugText.text = "✅ Рынок открыт!\nДоступно товаров: " + testGoods.Count;
            }
        }

        private void AddTestGold()
        {
            testPlayerGold += 500;
            UpdateDebugInfo();

            // Обновляем ConvoyUI
            if (convoyManager != null)
            {
                convoyManager.UpdateTestData(testPlayerGold, 100, 500, 150);
            }
        }

        private void TestTrade()
        {
            // Тестовая сделка - покупаем 10 единиц зерна
            if (testGoods.Count > 0)
            {
                var grain = testGoods[0];
                int quantity = 10;
                int totalCost = grain.basePrice * quantity;

                if (testPlayerGold >= totalCost)
                {
                    testPlayerGold -= totalCost;
                    grain.playerQuantity += quantity;

                    debugText.text = $"✅ Тестовая сделка!\nКуплено {quantity} {grain.name} за {totalCost}G";

                    // Обновляем UI
                    if (convoyManager != null)
                    {
                        convoyManager.UpdateTestData(testPlayerGold, 100, 500, 150 + (grain.weight * quantity));
                    }
                }
            }
        }

        private void UpdateDebugInfo()
        {
            if (debugText != null)
            {
                debugText.text = $"💰 Золото: {testPlayerGold}G\n";
                debugText.text += $"📦 Товаров: {testGoods.Count}\n";
                debugText.text += $"🏪 Рынок: {(marketManager != null && marketManager.IsMarketOpen() ? "Открыт" : "Закрыт")}";
                debugText.text += $"\n🎯 ECS: {(_entityManager != null ? "✅" : "❌")}";
            }
        }
    }
}