using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct RoutePlanningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка запросов на планирование маршрута
        foreach (var (routePlan, entity) in
                 SystemAPI.Query<RefRW<RoutePlan>>().WithEntityAccess())
        {
            if (!routePlan.ValueRO.IsValid)
            {
                CalculateRoute(ref routePlan.ValueRW, ref state);
                routePlan.ValueRW.IsValid = true;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void CalculateRoute(ref RoutePlan route, ref SystemState state)
    {
        var distance = math.distance(route.StartPosition, route.EndPosition);

        // Упрощенный расчет маршрута
        route.TotalDistance = distance;
        route.EstimatedTime = distance / 5.0f; // Базовая скорость 5 единиц/секунду
        route.FoodRequired = route.EstimatedTime * 2.0f; // 2 единицы пищи в секунду
        route.RiskLevel = math.clamp(distance / 100f, 0.1f, 0.9f);

        Debug.Log($"🗺️ Маршрут рассчитан: {route.TotalDistance:F1} единиц, " +
                 $"{route.EstimatedTime:F1} секунд, риск: {route.RiskLevel:P0}");
    }
}

// Система для запуска путешествия
public partial struct TravelStartSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (travelState, playerConvoy, mapPosition, entity) in
                 SystemAPI.Query<RefRW<TravelState>, RefRO<PlayerConvoy>,
                 RefRO<MapPosition>>().WithEntityAccess())
        {
            if (!travelState.ValueRO.IsTraveling && travelState.ValueRO.Destination.x != 0)
            {
                StartTravel(ref travelState.ValueRW, playerConvoy.ValueRO,
                           mapPosition.ValueRO, ref state);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void StartTravel(ref TravelState travelState, PlayerConvoy convoy,
                           MapPosition position, ref SystemState state)
    {
        var distance = math.distance(position.WorldPosition, travelState.Destination);
        var travelTime = distance / convoy.MoveSpeed;

        travelState.IsTraveling = true;
        travelState.TravelProgress = 0f;
        travelState.TotalTravelTime = travelTime;
        travelState.DestinationReached = false;

        Debug.Log($"🚀 Начато путешествие: {travelTime:F1} секунд");
    }
}