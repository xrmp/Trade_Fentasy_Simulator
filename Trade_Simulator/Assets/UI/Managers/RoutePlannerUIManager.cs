using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

namespace UI.Managers
{

    public class RoutePlannerUIManager : MonoBehaviour
    {
        [Header("Основная панель")]
        [SerializeField] private GameObject routePlannerPanel;
        [SerializeField] private Button openPlannerButton;
        [SerializeField] private Button closePlannerButton;
        [SerializeField] private Button confirmRouteButton;
        [SerializeField] private Button cancelRouteButton;

        [Header("Информация о маршруте")]
        [SerializeField] private TextMeshProUGUI startPointText;
        [SerializeField] private TextMeshProUGUI endPointText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI foodRequiredText;
        [SerializeField] private TextMeshProUGUI riskLevelText;
        [SerializeField] private Slider riskSlider;

        [Header("Выбор точек")]
        [SerializeField] private TMP_Dropdown citiesDropdown;
        [SerializeField] private Button setStartPointButton;
        [SerializeField] private Button setEndPointButton;
        [SerializeField] private Button clearRouteButton;

        [Header("Предупреждения")]
        [SerializeField] private GameObject warningsPanel;
        [SerializeField] private TextMeshProUGUI warningsText;

        private EntityManager _entityManager;
        private World _ecsWorld;
        private RoutePlan _currentRoutePlan;
        private float3 _startPosition;
        private float3 _endPosition;
        private bool _isPlanningMode = false;

        private void Awake()
        {
            Debug.Log("🗺️ RoutePlannerUIManager: Инициализация...");

            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld != null)
            {
                _entityManager = _ecsWorld.EntityManager;
            }

            SetupUIEvents();
            PopulateCitiesDropdown();
        }

        private void SetupUIEvents()
        {
            if (openPlannerButton != null)
                openPlannerButton.onClick.AddListener(OpenPlanner);

            if (closePlannerButton != null)
                closePlannerButton.onClick.AddListener(ClosePlanner);

            if (confirmRouteButton != null)
                confirmRouteButton.onClick.AddListener(ConfirmRoute);

            if (cancelRouteButton != null)
                cancelRouteButton.onClick.AddListener(CancelRoute);

            if (setStartPointButton != null)
                setStartPointButton.onClick.AddListener(SetStartFromCurrent);

            if (setEndPointButton != null)
                setEndPointButton.onClick.AddListener(SetEndFromSelection);

            if (clearRouteButton != null)
                clearRouteButton.onClick.AddListener(ClearRoute);

            if (citiesDropdown != null)
                citiesDropdown.onValueChanged.AddListener(OnCitySelected);
        }

        private void PopulateCitiesDropdown()
        {
            if (citiesDropdown == null) return;

            citiesDropdown.ClearOptions();
            var cityNames = new List<string> { "Выберите город..." };

            var cityQuery = _entityManager.CreateEntityQuery(typeof(City));
            var cities = cityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var cityEntity in cities)
            {
                var city = _entityManager.GetComponentData<City>(cityEntity);
                cityNames.Add(city.Name.ToString());
            }

            citiesDropdown.AddOptions(cityNames);
            cities.Dispose();
        }


        public void OpenPlanner()
        {
            if (routePlannerPanel != null)
                routePlannerPanel.SetActive(true);

            _isPlanningMode = true;
            UpdateRouteInfo();
            Debug.Log("🗺️ RoutePlannerUIManager: Планировщик маршрутов открыт");
        }


        public void ClosePlanner()
        {
            if (routePlannerPanel != null)
                routePlannerPanel.SetActive(false);

            _isPlanningMode = false;
            ClearRoute();
            Debug.Log("🗺️ RoutePlannerUIManager: Планировщик маршрутов закрыт");
        }

        private void SetStartFromCurrent()
        {
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(MapPosition));
            if (playerQuery.IsEmpty) return;

            var playerEntity = playerQuery.GetSingletonEntity();
            var position = _entityManager.GetComponentData<MapPosition>(playerEntity);

            _startPosition = position.WorldPosition;
            UpdateRouteInfo();

            Debug.Log($"📍 RoutePlanner: Начальная точка установлена на текущую позицию");

            playerQuery.Dispose();
        }

        private void SetEndFromSelection()
        {
            if (citiesDropdown == null || citiesDropdown.value == 0) return;

            var cityQuery = _entityManager.CreateEntityQuery(typeof(City));
            var cities = cityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            if (citiesDropdown.value - 1 < cities.Length)
            {
                var cityEntity = cities[citiesDropdown.value - 1];
                var city = _entityManager.GetComponentData<City>(cityEntity);

                _endPosition = city.WorldPosition;
                UpdateRouteInfo();

                Debug.Log($"🎯 RoutePlanner: Конечная точка установлена на {city.Name}");
            }

            cities.Dispose();
        }

        private void OnCitySelected(int index)
        {
            // Город выбран в dropdown, можно установить как конечную точку
            if (index > 0)
            {
                SetEndFromSelection();
            }
        }

        private void ClearRoute()
        {
            _startPosition = float3.zero;
            _endPosition = float3.zero;
            _currentRoutePlan = new RoutePlan { IsValid = false };

            UpdateRouteInfo();
            Debug.Log("🗺️ RoutePlanner: Маршрут очищен");
        }

        private void UpdateRouteInfo()
        {
            // Обновляем информацию о начальной точке
            if (startPointText != null)
            {
                startPointText.text = _startPosition.Equals(float3.zero) ?
                    "Не установлена" : $"({_startPosition.x:F0}, {_startPosition.z:F0})";
            }

            // Обновляем информацию о конечной точке
            if (endPointText != null)
            {
                endPointText.text = _endPosition.Equals(float3.zero) ?
                    "Не установлена" : $"({_endPosition.x:F0}, {_endPosition.z:F0})";
            }

            // Рассчитываем маршрут если обе точки установлены
            if (!_startPosition.Equals(float3.zero) && !_endPosition.Equals(float3.zero))
            {
                CalculateRoute();
                UpdateRouteDetails();
                UpdateWarnings();
            }
            else
            {
                ClearRouteDetails();
            }

            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void CalculateRoute()
        {
            // Рассчитываем базовые параметры маршрута
            var distance = math.distance(_startPosition, _endPosition);
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(PlayerConvoy));

            float speed = 5f; // Базовая скорость
            if (!playerQuery.IsEmpty)
            {
                var playerEntity = playerQuery.GetSingletonEntity();
                var convoy = _entityManager.GetComponentData<PlayerConvoy>(playerEntity);
                speed = convoy.BaseSpeed;
            }

            var estimatedTime = distance / speed;
            var riskLevel = CalculateRiskLevel(_startPosition, _endPosition);
            var foodRequired = CalculateFoodRequired(estimatedTime);

            _currentRoutePlan = new RoutePlan
            {
                StartPosition = _startPosition,
                EndPosition = _endPosition,
                TotalDistance = distance,
                EstimatedTime = estimatedTime,
                FoodRequired = foodRequired,
                RiskLevel = riskLevel,
                IsValid = true
            };

            playerQuery.Dispose();
        }

        private float CalculateRiskLevel(float3 start, float3 end)
        {
            // Упрощенный расчет уровня риска
            // В реальной реализации здесь будет сложная логика анализа местности
            var distance = math.distance(start, end);
            var baseRisk = math.min(distance / 100f, 0.8f); // Риск растет с дистанцией

            // Добавляем случайный фактор
            var random = new Unity.Mathematics.Random((uint)(start.x + start.z + end.x + end.z));
            var randomRisk = random.NextFloat(0.1f, 0.3f);

            return math.min(baseRisk + randomRisk, 1.0f);
        }

        private float CalculateFoodRequired(float travelTime)
        {
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));
            if (playerQuery.IsEmpty) return 0f;

            var playerEntity = playerQuery.GetSingletonEntity();
            var resources = _entityManager.GetComponentData<ConvoyResources>(playerEntity);

            // Еда потребляется в день, время в часах - конвертируем
            var days = travelTime / 24f;
            var foodRequired = resources.FoodConsumptionRate * resources.Guards * days;

            playerQuery.Dispose();
            return foodRequired;
        }

        private void UpdateRouteDetails()
        {
            if (distanceText != null)
                distanceText.text = $"{_currentRoutePlan.TotalDistance:F1} км";

            if (timeText != null)
            {
                var hours = _currentRoutePlan.EstimatedTime;
                var days = hours / 24f;
                timeText.text = $"{days:F1} дней";
            }

            if (foodRequiredText != null)
                foodRequiredText.text = $"{_currentRoutePlan.FoodRequired:F0} ед.";

            if (riskLevelText != null)
            {
                riskLevelText.text = $"{_currentRoutePlan.RiskLevel:P0}";
                riskLevelText.color = _currentRoutePlan.RiskLevel < 0.3f ? Color.green :
                                    _currentRoutePlan.RiskLevel < 0.6f ? Color.yellow : Color.red;
            }

            if (riskSlider != null)
            {
                riskSlider.value = _currentRoutePlan.RiskLevel;

                // Изменяем цвет слайдера в зависимости от уровня риска
                var fillImage = riskSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = _currentRoutePlan.RiskLevel < 0.3f ? Color.green :
                                    _currentRoutePlan.RiskLevel < 0.6f ? Color.yellow : Color.red;
                }
            }
        }

        private void ClearRouteDetails()
        {
            if (distanceText != null) distanceText.text = "-";
            if (timeText != null) timeText.text = "-";
            if (foodRequiredText != null) foodRequiredText.text = "-";
            if (riskLevelText != null) riskLevelText.text = "-";
            if (riskSlider != null) riskSlider.value = 0f;
        }

        private void UpdateWarnings()
        {
            if (warningsPanel == null || warningsText == null) return;

            var warnings = new List<string>();
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));

            if (!playerQuery.IsEmpty)
            {
                var playerEntity = playerQuery.GetSingletonEntity();
                var resources = _entityManager.GetComponentData<ConvoyResources>(playerEntity);

                // Проверяем достаточно ли еды
                if (resources.Food < _currentRoutePlan.FoodRequired)
                {
                    warnings.Add("⚠️ Недостаточно провианта для маршрута");
                }

                // Проверяем уровень риска
                if (_currentRoutePlan.RiskLevel > 0.7f)
                {
                    warnings.Add("⚡ Высокий уровень риска на маршруте");
                }

                // Проверяем мораль
                if (resources.Morale < 0.3f)
                {
                    warnings.Add("😔 Низкая мораль отряда");
                }
            }

            playerQuery.Dispose();

            // Показываем/скрываем панель предупреждений
            bool hasWarnings = warnings.Count > 0;
            warningsPanel.SetActive(hasWarnings);

            if (hasWarnings)
            {
                warningsText.text = string.Join("\n", warnings);
            }
        }

        private void UpdateButtonsState()
        {
            bool hasValidRoute = _currentRoutePlan.IsValid;

            if (confirmRouteButton != null)
                confirmRouteButton.interactable = hasValidRoute;

            if (cancelRouteButton != null)
                cancelRouteButton.interactable = hasValidRoute;
        }

        private void ConfirmRoute()
        {
            if (!_currentRoutePlan.IsValid) return;

            // Создаем команду для системы путешествий
            var routeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(routeEntity, new TravelCommand
            {
                StartPosition = _currentRoutePlan.StartPosition,
                EndPosition = _currentRoutePlan.EndPosition,
                RoutePlan = _currentRoutePlan
            });

            Debug.Log($"✅ RoutePlanner: Маршрут подтвержден. Дистанция: {_currentRoutePlan.TotalDistance:F1} км");

            ClosePlanner();
        }

        private void CancelRoute()
        {
            ClearRoute();
            Debug.Log("❌ RoutePlanner: Планирование маршрута отменено");
        }

        public bool IsPlannerOpen()
        {
            return routePlannerPanel != null && routePlannerPanel.activeInHierarchy;
        }
    }

    public struct TravelCommand : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public RoutePlan RoutePlan;
    }
}