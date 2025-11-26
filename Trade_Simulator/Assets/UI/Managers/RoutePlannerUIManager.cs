using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;

public class RoutePlannerUIManager : MonoBehaviour
{
    [Header("Панель планировщика")]
    public GameObject routePanel;
    public TMP_InputField destinationX;
    public TMP_InputField destinationY;
    public TMP_Text routeInfoText;
    public TMP_Text riskAssessmentText;
    public TMP_Text foodRequiredText;
    public TMP_Text timeRequiredText;

    [Header("Кнопки")]
    public Button planRouteButton;
    public Button startTravelButton;
    public Button cancelButton;

    [Header("Предупреждения")]
    public GameObject insufficientFoodWarning;
    public GameObject highRiskWarning;

    private Entity _routePlanEntity = Entity.Null;

    void Start()
    {
        planRouteButton.onClick.AddListener(PlanRoute);
        startTravelButton.onClick.AddListener(StartTravel);
        cancelButton.onClick.AddListener(CancelRoute);

        routePanel.SetActive(false);
    }

    void Update()
    {
        if (routePanel.activeInHierarchy)
        {
            UpdateRouteWarnings();
        }
    }

    public void OpenRoutePlanner()
    {
        routePanel.SetActive(true);
        ClearRouteInfo();
    }

    private void PlanRoute()
    {
        if (!int.TryParse(destinationX.text, out int x) || !int.TryParse(destinationY.text, out int y))
        {
            routeInfoText.text = "❌ Ошибка: введите корректные координаты";
            return;
        }

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Удаляем старый план маршрута
        if (_routePlanEntity != Entity.Null && entityManager.Exists(_routePlanEntity))
        {
            entityManager.DestroyEntity(_routePlanEntity);
        }

        // Создаем новый план маршрута
        _routePlanEntity = entityManager.CreateEntity();

        var startPos = GetPlayerPosition();
        var endPos = new float3(x * 10f, 0, y * 10f);

        entityManager.AddComponentData(_routePlanEntity, new RoutePlan
        {
            StartPosition = startPos,
            EndPosition = endPos,
            IsValid = false
        });

        routeInfoText.text = "📐 Расчет маршрута...";
    }

    private void StartTravel()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (_routePlanEntity == Entity.Null || !entityManager.Exists(_routePlanEntity))
        {
            routeInfoText.text = "❌ Сначала рассчитайте маршрут";
            return;
        }

        var routePlan = entityManager.GetComponentData<RoutePlan>(_routePlanEntity);

        if (!routePlan.IsValid)
        {
            routeInfoText.text = "❌ Маршрут еще не рассчитан";
            return;
        }

        // Проверяем достаточно ли пищи
        var playerFood = GetPlayerFood();
        if (playerFood < routePlan.FoodRequired)
        {
            routeInfoText.text = "❌ Недостаточно провианта для путешествия";
            return;
        }

        // Устанавливаем точку назначения для игрока
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag));
        if (!playerQuery.IsEmpty)
        {
            var playerEntity = playerQuery.GetSingletonEntity();
            var travelState = entityManager.GetComponentData<TravelState>(playerEntity);

            travelState.Destination = routePlan.EndPosition;
            travelState.DestinationReached = false;
            travelState.IsTraveling = true;
            travelState.StartPosition = GetPlayerPosition();

            entityManager.SetComponentData(playerEntity, travelState);

            routePanel.SetActive(false);
        }

        // Очищаем план маршрута
        entityManager.DestroyEntity(_routePlanEntity);
        _routePlanEntity = Entity.Null;
    }

    // ИЗМЕНИТЬ НА PUBLIC!
    public void CancelRoute()
    {
        if (_routePlanEntity != Entity.Null && World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (entityManager.Exists(_routePlanEntity))
            {
                entityManager.DestroyEntity(_routePlanEntity);
            }
            _routePlanEntity = Entity.Null;
        }

        routePanel.SetActive(false);
    }

    private void UpdateRouteWarnings()
    {
        if (_routePlanEntity == Entity.Null || !World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (!entityManager.Exists(_routePlanEntity)) return;

        var routePlan = entityManager.GetComponentData<RoutePlan>(_routePlanEntity);

        if (routePlan.IsValid)
        {
            routeInfoText.text = $"✅ Маршрут готов!\nДистанция: {routePlan.TotalDistance:F1}";
            timeRequiredText.text = $"Время: {routePlan.EstimatedTime:F1} сек";
            foodRequiredText.text = $"Провиант: {routePlan.FoodRequired:F1}";
            riskAssessmentText.text = $"Риск: {routePlan.RiskLevel:P0}";

            var playerFood = GetPlayerFood();
            insufficientFoodWarning.SetActive(playerFood < routePlan.FoodRequired);
            highRiskWarning.SetActive(routePlan.RiskLevel > 0.7f);

            startTravelButton.interactable = playerFood >= routePlan.FoodRequired;
        }
    }

    private void ClearRouteInfo()
    {
        routeInfoText.text = "Введите координаты назначения (X Y)";
        timeRequiredText.text = "";
        foodRequiredText.text = "";
        riskAssessmentText.text = "";
        insufficientFoodWarning.SetActive(false);
        highRiskWarning.SetActive(false);
        startTravelButton.interactable = false;
    }

    private float3 GetPlayerPosition()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return float3.zero;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(MapPosition));

        if (!playerQuery.IsEmpty)
        {
            var position = playerQuery.GetSingleton<MapPosition>();
            return position.WorldPosition;
        }

        return float3.zero;
    }

    private int GetPlayerFood()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return 0;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));

        if (!playerQuery.IsEmpty)
        {
            var resources = playerQuery.GetSingleton<ConvoyResources>();
            return resources.Food;
        }

        return 0;
    }
}