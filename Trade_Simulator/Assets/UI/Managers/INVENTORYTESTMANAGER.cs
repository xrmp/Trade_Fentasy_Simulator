using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace UI.Managers
{
    public class InventoryTestManager : MonoBehaviour
    {
        [Header("Тестовые данные ресурсов")]
        [SerializeField] private int testGold = 1000;
        [SerializeField] private int testFood = 100;
        [SerializeField] private int testGuards = 5;
        [SerializeField] private float testMorale = 0.8f;
        [SerializeField] private int testCapacity = 500;
        [SerializeField] private int testUsedCapacity = 150;
        [SerializeField] private float testSpeed = 5.2f;
        [SerializeField] private string testPosition = "(10, 10)";
        [SerializeField] private string testTerrain = "Равнины";

        [Header("UI Elements - Ресурсы")]
        [SerializeField] private Button addGoldButton;
        [SerializeField] private Button removeGoldButton;
        [SerializeField] private Button addFoodButton;
        [SerializeField] private Button removeFoodButton;
        [SerializeField] private Button increaseLoadButton;
        [SerializeField] private Button decreaseLoadButton;
        [SerializeField] private Button increaseMoraleButton;
        [SerializeField] private Button decreaseMoraleButton;

        [Header("UI Elements - Повозки")]
        [SerializeField] private Button addWagonButton;
        [SerializeField] private Button removeWagonButton;
        [SerializeField] private Button breakWagonButton;
        [SerializeField] private Button repairWagonButton;

        [Header("UI Elements - Позиция")]
        [SerializeField] private Button changePositionButton;
        [SerializeField] private Button changeTerrainButton;
        [SerializeField] private TextMeshProUGUI debugText;

        [Header("Ссылки")]
        [SerializeField] private ConvoyUIManager convoyManager;

        private List<string> _terrains = new List<string> { "Равнины", "Лес", "Горы", "Пустыня", "Река", "Дорога" };
        private int _currentTerrainIndex = 0;
        private int _positionX = 10;
        private int _positionY = 10;

        private List<TestWagon> _testWagons = new List<TestWagon>();

        [System.Serializable]
        public class TestWagon
        {
            public WagonType type;
            public int health;
            public int maxHealth;
            public bool isBroken;
            public int currentLoad;
            public int loadCapacity;
        }

        private void Start()
        {
            CreateInitialTestWagons();
            SetupUIEvents();
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void CreateInitialTestWagons()
        {
            _testWagons = new List<TestWagon>
            {
                new TestWagon { type = WagonType.BasicCart, health = 100, maxHealth = 100, isBroken = false, currentLoad = 50, loadCapacity = 500 },
                new TestWagon { type = WagonType.TradeWagon, health = 75, maxHealth = 150, isBroken = false, currentLoad = 80, loadCapacity = 800 }
            };
        }

        private void SetupUIEvents()
        {
            // Ресурсы
            if (addGoldButton != null) addGoldButton.onClick.AddListener(() => UpdateGold(100));
            if (removeGoldButton != null) removeGoldButton.onClick.AddListener(() => UpdateGold(-100));
            if (addFoodButton != null) addFoodButton.onClick.AddListener(() => UpdateFood(50));
            if (removeFoodButton != null) removeFoodButton.onClick.AddListener(() => UpdateFood(-50));
            if (increaseLoadButton != null) increaseLoadButton.onClick.AddListener(() => UpdateLoad(50));
            if (decreaseLoadButton != null) decreaseLoadButton.onClick.AddListener(() => UpdateLoad(-50));
            if (increaseMoraleButton != null) increaseMoraleButton.onClick.AddListener(() => UpdateMorale(0.1f));
            if (decreaseMoraleButton != null) decreaseMoraleButton.onClick.AddListener(() => UpdateMorale(-0.1f));

            // Повозки
            if (addWagonButton != null) addWagonButton.onClick.AddListener(AddTestWagon);
            if (removeWagonButton != null) removeWagonButton.onClick.AddListener(RemoveTestWagon);
            if (breakWagonButton != null) breakWagonButton.onClick.AddListener(BreakRandomWagon);
            if (repairWagonButton != null) repairWagonButton.onClick.AddListener(RepairAllWagons);

            // Позиция
            if (changePositionButton != null) changePositionButton.onClick.AddListener(ChangePosition);
            if (changeTerrainButton != null) changeTerrainButton.onClick.AddListener(ChangeTerrain);
        }

        private void UpdateGold(int amount)
        {
            testGold = Mathf.Max(0, testGold + amount);
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void UpdateFood(int amount)
        {
            testFood = Mathf.Max(0, testFood + amount);
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void UpdateLoad(int amount)
        {
            testUsedCapacity = Mathf.Clamp(testUsedCapacity + amount, 0, testCapacity);
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void UpdateMorale(float amount)
        {
            testMorale = Mathf.Clamp01(testMorale + amount);
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void AddTestWagon()
        {
            var wagonTypes = new[] { WagonType.BasicCart, WagonType.TradeWagon, WagonType.HeavyWagon };
            var randomType = wagonTypes[Random.Range(0, wagonTypes.Length)];

            _testWagons.Add(new TestWagon
            {
                type = randomType,
                health = 100,
                maxHealth = 100,
                isBroken = false,
                currentLoad = 0,
                loadCapacity = randomType == WagonType.BasicCart ? 500 :
                              randomType == WagonType.TradeWagon ? 800 : 1200
            });

            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void RemoveTestWagon()
        {
            if (_testWagons.Count > 0)
            {
                _testWagons.RemoveAt(_testWagons.Count - 1);
                UpdateConvoyUI();
                UpdateDebugInfo();
            }
        }

        private void BreakRandomWagon()
        {
            if (_testWagons.Count > 0)
            {
                var randomWagon = _testWagons[Random.Range(0, _testWagons.Count)];
                randomWagon.health = 0;
                randomWagon.isBroken = true;
                UpdateConvoyUI();
                UpdateDebugInfo();
            }
        }

        private void RepairAllWagons()
        {
            foreach (var wagon in _testWagons)
            {
                wagon.health = wagon.maxHealth;
                wagon.isBroken = false;
            }
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void ChangePosition()
        {
            _positionX += Random.Range(-5, 6);
            _positionY += Random.Range(-5, 6);
            testPosition = $"({_positionX}, {_positionY})";
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void ChangeTerrain()
        {
            _currentTerrainIndex = (_currentTerrainIndex + 1) % _terrains.Count;
            testTerrain = _terrains[_currentTerrainIndex];
            UpdateConvoyUI();
            UpdateDebugInfo();
        }

        private void UpdateConvoyUI()
        {
            if (convoyManager != null)
            {

                convoyManager.UpdateTestData(
                    testGold,
                    testFood,
                    testGuards,
                    testMorale,
                    testCapacity,
                    testUsedCapacity,
                    testSpeed
                );

                convoyManager.UpdateTestPosition(testPosition, testTerrain);
            }
        }

        private void UpdateDebugInfo()
        {
            if (debugText != null)
            {
                debugText.text = $"🎒 ТЕСТ ИНВЕНТАРЯ\n";
                debugText.text += $"💰 Золото: {testGold}G\n";
                debugText.text += $"🍖 Провиант: {testFood}\n";
                debugText.text += $"📦 Груз: {testUsedCapacity}/{testCapacity}\n";
                debugText.text += $"😊 Мораль: {testMorale:P0}\n";
                debugText.text += $"🚛 Повозок: {_testWagons.Count}\n";
                debugText.text += $"📍 Позиция: {testPosition}\n";
                debugText.text += $"🌍 Местность: {testTerrain}";
            }
        }

        // Методы для доступа из ConvoyUIManager (если понадобятся)
        public List<TestWagon> GetTestWagons() => _testWagons;
        public float GetTestMorale() => testMorale;
        public string GetTestPosition() => testPosition;
        public string GetTestTerrain() => testTerrain;
        public float GetTestSpeed() => testSpeed;
    }
}