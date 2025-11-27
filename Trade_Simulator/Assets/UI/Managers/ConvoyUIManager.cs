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

        private EntityManager _entityManager;
        private World _ecsWorld;
        private List<GameObject> _wagonUIInstances = new List<GameObject>();

        private void Awake()
        {
            Debug.Log("🚛 ConvoyUIManager: Инициализация...");

            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld != null)
            {
                _entityManager = _ecsWorld.EntityManager;
            }
        }

        private void Update()
        {
            UpdateConvoyUI();
        }

        private void UpdateConvoyUI()
        {
            if (_entityManager == null) return;

            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag));
            if (playerQuery.IsEmpty) return;

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

        private void UpdateResourcesUI(Entity playerEntity)
        {
            var resources = _entityManager.GetComponentData<ConvoyResources>(playerEntity);

            if (goldText != null)
                goldText.text = $"{resources.Gold}G";

            if (foodText != null)
                foodText.text = $"{resources.Food}F";

            if (guardsText != null)
                guardsText.text = $"{resources.Guards}🛡️";

            if (moraleText != null)
                moraleText.text = $"{resources.Morale:P0}";

            if (moraleSlider != null)
            {
                moraleSlider.value = resources.Morale;

                // Изменяем цвет в зависимости от морали
                var moraleFillImage = moraleSlider.fillRect.GetComponent<Image>();
                if (moraleFillImage != null)
                {
                    moraleFillImage.color = resources.Morale >= 0.7f ? Color.green :
                                          resources.Morale >= 0.4f ? Color.yellow : Color.red;
                }
            }
        }

        private void UpdateConvoyInfoUI(Entity playerEntity)
        {
            var convoy = _entityManager.GetComponentData<PlayerConvoy>(playerEntity);

            if (capacityText != null)
                capacityText.text = $"{convoy.UsedCapacity}/{convoy.TotalCapacity}";

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
                speedText.text = $"{currentSpeed:F1} u/s";

                // Изменяем цвет если скорость снижена
                speedText.color = convoy.CurrentSpeedModifier < 1.0f ? Color.yellow : Color.white;
            }
        }

        private void UpdatePositionUI(Entity playerEntity)
        {
            var position = _entityManager.GetComponentData<MapPosition>(playerEntity);

            if (positionText != null)
                positionText.text = $"({position.GridPosition.x}, {position.GridPosition.y})";

            if (terrainText != null)
                terrainText.text = GetTerrainName(position.CurrentTerrain);
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

                // Изменяем цвет в зависимости от состояния
                var fillImage = healthBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    var healthRatio = (float)wagon.Health / wagon.MaxHealth;
                    fillImage.color = healthRatio > 0.5f ? Color.green :
                                    healthRatio > 0.2f ? Color.yellow : Color.red;
                }
            }

            if (statusIndicator != null)
            {
                statusIndicator.color = wagon.IsBroken ? Color.red : Color.green;
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
    }
}