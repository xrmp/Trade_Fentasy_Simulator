using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;

namespace UI.Managers
{
    public class ConvoyUIManager : MonoBehaviour
    {
        [Header("Основная информация")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI foodText;
        [SerializeField] private TextMeshProUGUI guardsText;
        [SerializeField] private TextMeshProUGUI moraleText;
        [SerializeField] private Slider moraleSlider;

        [Header("Грузоподъемность")]
        [SerializeField] private TextMeshProUGUI capacityText;
        [SerializeField] private Slider capacitySlider;
        [SerializeField] private Image capacityFillImage;

        [Header("Список повозок")]
        [SerializeField] private Transform wagonsContainer;
        [SerializeField] private GameObject wagonUIPrefab;

        [Header("Скорость и позиция")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI terrainText;

        [Header("Цвета индикаторов")]
        [SerializeField] private Color normalCapacityColor = Color.green;
        [SerializeField] private Color warningCapacityColor = Color.yellow;
        [SerializeField] private Color criticalCapacityColor = Color.red;

        [Header("Тестовые данные")]
        [SerializeField] private bool useTestData = true;
        [SerializeField] private int testGold = 1000;
        [SerializeField] private int testFood = 100;
        [SerializeField] private int testGuards = 5;
        [SerializeField] private float testMorale = 0.8f;
        [SerializeField] private int testCapacity = 500;
        [SerializeField] private int testUsedCapacity = 150;
        [SerializeField] private float testSpeed = 5.2f;
        [SerializeField] private string testPosition = "(10, 10)";
        [SerializeField] private string testTerrain = "Равнины";

        private EntityManager _entityManager;
        private World _ecsWorld;
        private List<GameObject> _wagonUIInstances = new List<GameObject>();
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.2f; // Обновление каждые 200ms

        private void Awake()
        {
            Debug.Log("🚛 ConvoyUIManager: Инициализация...");

            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld != null)
            {
                _entityManager = _ecsWorld.EntityManager;
            }

            // Автоматически находим UI элементы если не назначены
            AutoAssignUIElements();
        }

        private void AutoAssignUIElements()
        {
            if (goldText == null) goldText = GameObject.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
            if (foodText == null) foodText = GameObject.Find("FoodText")?.GetComponent<TextMeshProUGUI>();
            if (guardsText == null) guardsText = GameObject.Find("GuardsText")?.GetComponent<TextMeshProUGUI>();
            if (moraleText == null) moraleText = GameObject.Find("MoraleText")?.GetComponent<TextMeshProUGUI>();
            if (moraleSlider == null) moraleSlider = GameObject.Find("MoraleSlider")?.GetComponent<Slider>();
            if (capacityText == null) capacityText = GameObject.Find("CapacityText")?.GetComponent<TextMeshProUGUI>();
            if (capacitySlider == null) capacitySlider = GameObject.Find("CapacitySlider")?.GetComponent<Slider>();
            if (speedText == null) speedText = GameObject.Find("SpeedText")?.GetComponent<TextMeshProUGUI>();
            if (positionText == null) positionText = GameObject.Find("PositionText")?.GetComponent<TextMeshProUGUI>();
            if (terrainText == null) terrainText = GameObject.Find("TerrainText")?.GetComponent<TextMeshProUGUI>();
            if (wagonsContainer == null) wagonsContainer = GameObject.Find("WagonsContainer")?.transform;
        }

        private void Update()
        {
            // Оптимизация: обновляем не каждый кадр, а с интервалом
            if (Time.time - _lastUpdateTime >= UPDATE_INTERVAL)
            {
                UpdateConvoyUI();
                _lastUpdateTime = Time.time;
            }
        }

        private void UpdateConvoyUI()
        {
            // ЕСЛИ ТЕСТОВЫЙ РЕЖИМ - используем тестовые данные
            if (useTestData)
            {
                UpdateUIWithTestData();
                return;
            }

            // ИНАЧЕ - обычная логика с ECS
            if (_entityManager == null)
            {
                // Пытаемся найти EntityManager если он еще не инициализирован
                _ecsWorld = World.DefaultGameObjectInjectionWorld;
                if (_ecsWorld != null) _entityManager = _ecsWorld.EntityManager;
                return;
            }

            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag));
            if (playerQuery.IsEmpty)
            {
                // Игрок еще не создан, используем тестовые данные
                UpdateUIWithTestData();
                playerQuery.Dispose();
                return;
            }

            var playerEntity = playerQuery.GetSingletonEntity();

            // Обновляем ресурсы
            if (_entityManager.HasComponent<ConvoyResources>(playerEntity))
            {
                UpdateResourcesUI(playerEntity);
            }

            // Обновляем информацию об обозе
            if (_entityManager.HasComponent<PlayerConvoy>(playerEntity))
            {
                UpdateConvoyInfoUI(playerEntity);
            }

            // Обновляем позицию
            if (_entityManager.HasComponent<MapPosition>(playerEntity))
            {
                UpdatePositionUI(playerEntity);
            }

            // Обновляем список повозок
            UpdateWagonsList();

            playerQuery.Dispose();
        }

        private void UpdateUIWithTestData()
        {
            // ОБНОВЛЯЕМ РЕСУРСЫ
            if (goldText != null)
                goldText.text = $"💰 ЗОЛОТО: {testGold}G";

            if (foodText != null)
                foodText.text = $"🍖 ПРОВИАНТ: {testFood}";

            if (guardsText != null)
                guardsText.text = $"🛡️ ОХРАНА: {testGuards}";

            if (moraleText != null)
                moraleText.text = $"😊 МОРАЛЬ: {testMorale:P0}";

            if (moraleSlider != null)
            {
                moraleSlider.value = testMorale;
                UpdateSliderColor(moraleSlider, testMorale, 0.7f, 0.4f);
            }

            // ОБНОВЛЯЕМ ГРУЗОПОДЪЕМНОСТЬ
            if (capacityText != null)
                capacityText.text = $"📦 ГРУЗ: {testUsedCapacity}/{testCapacity}";

            if (capacitySlider != null)
            {
                capacitySlider.maxValue = testCapacity;
                capacitySlider.value = testUsedCapacity;

                // Обновляем цвет индикатора загрузки
                var loadRatio = (float)testUsedCapacity / testCapacity;
                if (capacityFillImage != null)
                {
                    capacityFillImage.color = loadRatio < 0.7f ? normalCapacityColor :
                                            loadRatio < 0.9f ? warningCapacityColor : criticalCapacityColor;
                }
            }

            // ОБНОВЛЯЕМ СТАТУС
            if (speedText != null)
                speedText.text = $"🚀 СКОРОСТЬ: {testSpeed:F1} u/s";

            if (positionText != null)
                positionText.text = $"📍 ПОЗИЦИЯ: {testPosition}";

            if (terrainText != null)
                terrainText.text = $"🌲 МЕСТНОСТЬ: {testTerrain}";

            // ОБНОВЛЯЕМ ТЕСТОВЫЕ ПОВОЗКИ
            UpdateTestWagons();
        }

        private void UpdateResourcesUI(Entity playerEntity)
        {
            var resources = _entityManager.GetComponentData<ConvoyResources>(playerEntity);

            if (goldText != null)
                goldText.text = $"💰 ЗОЛОТО: {resources.Gold}G";

            if (foodText != null)
                foodText.text = $"🍖 ПРОВИАНТ: {resources.Food}";

            if (guardsText != null)
                guardsText.text = $"🛡️ ОХРАНА: {resources.Guards}";

            if (moraleText != null)
                moraleText.text = $"😊 МОРАЛЬ: {resources.Morale:P0}";

            if (moraleSlider != null)
            {
                moraleSlider.value = resources.Morale;
                UpdateSliderColor(moraleSlider, resources.Morale, 0.7f, 0.4f);
            }
        }

        private void UpdateConvoyInfoUI(Entity playerEntity)
        {
            var convoy = _entityManager.GetComponentData<PlayerConvoy>(playerEntity);

            if (capacityText != null)
                capacityText.text = $"📦 ГРУЗ: {convoy.UsedCapacity}/{convoy.TotalCapacity}";

            if (capacitySlider != null)
            {
                capacitySlider.maxValue = convoy.TotalCapacity;
                capacitySlider.value = convoy.UsedCapacity;

                // Обновляем цвет индикатора загрузки
                var loadRatio = (float)convoy.UsedCapacity / convoy.TotalCapacity;
                if (capacityFillImage != null)
                {
                    capacityFillImage.color = loadRatio < 0.7f ? normalCapacityColor :
                                            loadRatio < 0.9f ? warningCapacityColor : criticalCapacityColor;
                }
            }

            if (speedText != null)
            {
                var currentSpeed = convoy.MoveSpeed * convoy.CurrentSpeedModifier;
                speedText.text = $"🚀 СКОРОСТЬ: {currentSpeed:F1} u/s";

                // Изменяем цвет если скорость снижена
                speedText.color = convoy.CurrentSpeedModifier < 1.0f ? Color.yellow : Color.white;
            }
        }

        private void UpdatePositionUI(Entity playerEntity)
        {
            var position = _entityManager.GetComponentData<MapPosition>(playerEntity);

            if (positionText != null)
                positionText.text = $"📍 ПОЗИЦИЯ: ({position.GridPosition.x}, {position.GridPosition.y})";

            if (terrainText != null)
                terrainText.text = $"🌲 МЕСТНОСТЬ: {GetTerrainName(position.CurrentTerrain)}";
        }

        private void UpdateWagonsList()
        {
            // Очищаем старый список
            foreach (var wagonUI in _wagonUIInstances)
            {
                if (wagonUI != null)
                    Destroy(wagonUI);
            }
            _wagonUIInstances.Clear();

            if (wagonsContainer == null || wagonUIPrefab == null) return;

            // Получаем все повозки
            var wagonQuery = _entityManager.CreateEntityQuery(typeof(Wagon));
            var wagons = wagonQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var wagonEntity in wagons)
            {
                var wagon = _entityManager.GetComponentData<Wagon>(wagonEntity);

                // Создаем UI элемент для повозки
                var wagonUI = Instantiate(wagonUIPrefab, wagonsContainer);
                SetupWagonUI(wagonUI, wagon);
                _wagonUIInstances.Add(wagonUI);
            }

            wagons.Dispose();
        }

        private void UpdateTestWagons()
        {
            if (wagonsContainer == null || wagonUIPrefab == null) return;

            // Очищаем старый список
            foreach (var wagonUI in _wagonUIInstances)
            {
                if (wagonUI != null)
                    Destroy(wagonUI);
            }
            _wagonUIInstances.Clear();

            // Создаем тестовые повозки
            var testWagons = new List<Wagon>
            {
                new Wagon { Health = 100, MaxHealth = 100, Type = WagonType.BasicCart, IsBroken = false, CurrentLoad = 50, LoadCapacity = 500 },
                new Wagon { Health = 75, MaxHealth = 150, Type = WagonType.TradeWagon, IsBroken = false, CurrentLoad = 80, LoadCapacity = 800 },
                new Wagon { Health = 25, MaxHealth = 200, Type = WagonType.HeavyWagon, IsBroken = true, CurrentLoad = 20, LoadCapacity = 1200 }
            };

            foreach (var wagon in testWagons)
            {
                var wagonUI = Instantiate(wagonUIPrefab, wagonsContainer);
                SetupWagonUI(wagonUI, wagon);
                _wagonUIInstances.Add(wagonUI);
            }
        }

        private void SetupWagonUI(GameObject wagonUI, Wagon wagon)
        {
            // Находим элементы UI
            var healthText = wagonUI.transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
            var capacityText = wagonUI.transform.Find("CapacityText")?.GetComponent<TextMeshProUGUI>();
            var typeText = wagonUI.transform.Find("TypeText")?.GetComponent<TextMeshProUGUI>();
            var healthBar = wagonUI.transform.Find("HealthBar")?.GetComponent<Slider>();
            var statusIndicator = wagonUI.transform.Find("StatusIndicator")?.GetComponent<Image>();

            if (healthText != null)
                healthText.text = $"{wagon.Health}/{wagon.MaxHealth}";

            if (capacityText != null)
                capacityText.text = $"{wagon.CurrentLoad}/{wagon.LoadCapacity}";

            if (typeText != null)
                typeText.text = GetWagonTypeName(wagon.Type);

            if (healthBar != null)
            {
                healthBar.maxValue = wagon.MaxHealth;
                healthBar.value = wagon.Health;
                UpdateSliderColor(healthBar, (float)wagon.Health / wagon.MaxHealth, 0.5f, 0.2f);
            }

            if (statusIndicator != null)
            {
                statusIndicator.color = wagon.IsBroken ? Color.red : Color.green;
            }
        }

        private void UpdateSliderColor(Slider slider, float value, float warningThreshold, float criticalThreshold)
        {
            var fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = value > warningThreshold ? Color.green :
                                value > criticalThreshold ? Color.yellow : Color.red;
            }
        }

        private string GetTerrainName(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plains => "Равнины",
                TerrainType.Forest => "Лес",
                TerrainType.Mountains => "Горы",
                TerrainType.Desert => "Пустыня",
                TerrainType.River => "Река",
                TerrainType.Road => "Дорога",
                _ => "Неизвестно"
            };
        }

        private string GetWagonTypeName(WagonType type)
        {
            return type switch
            {
                WagonType.BasicCart => "Базовая",
                WagonType.TradeWagon => "Торговая",
                WagonType.HeavyWagon => "Тяжелая",
                WagonType.LuxuryCoach => "Роскошная",
                _ => "Неизвестная"
            };
        }


        public void SetUIVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void RefreshUI()
        {
            UpdateConvoyUI();
            Debug.Log("🔄 ConvoyUIManager: Принудительное обновление UI");
        }

        public void UpdateTestData(int gold, int food, int capacity, int usedCapacity)
        {
            testGold = gold;
            testFood = food;
            testCapacity = capacity;
            testUsedCapacity = usedCapacity;
            RefreshUI();
        }
    }
}