using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MapMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (position, convoy, travelState, resources) in
                 SystemAPI.Query<RefRW<MapPosition>, RefRW<PlayerConvoy>,
                                RefRW<TravelState>, RefRW<ConvoyResources>>()
                 .WithAll<PlayerTag>())
        {
            if (!travelState.ValueRO.IsTraveling) continue;

            ProcessMovement(ref position.ValueRW, ref convoy.ValueRW,
                          ref travelState.ValueRW, ref resources.ValueRW, deltaTime);
        }
    }

    private void ProcessMovement(ref MapPosition position, ref PlayerConvoy convoy,
                               ref TravelState travelState, ref ConvoyResources resources, float deltaTime)
    {
        // Потребление пищи
        var foodConsumption = resources.FoodConsumptionRate * deltaTime;

        if (resources.Food > 0)
        {
            resources.Food = (int)math.max(0, resources.Food - foodConsumption);

            // Восстанавливаем скорость если была снижена из-за голода
            if (convoy.CurrentSpeedModifier < 1.0f)
            {
                convoy.CurrentSpeedModifier = math.min(1.0f,
                    convoy.CurrentSpeedModifier + 0.1f * deltaTime);
            }
        }
        else
        {
            // Штрафы за голод
            ApplyStarvationEffects(ref resources, ref convoy, deltaTime);
        }

        // Расчет скорости с учетом местности
        var terrainModifier = GetTerrainSpeedModifier(position.CurrentTerrain);
        var currentSpeed = convoy.BaseSpeed * terrainModifier * convoy.CurrentSpeedModifier;

        // Обновление прогресса
        travelState.TravelProgress += (currentSpeed * deltaTime) /
                                    math.max(1f, travelState.TotalTravelTime);

        // Интерполяция позиции
        if (!travelState.DestinationReached && travelState.TravelProgress <= 1.0f)
        {
            position.WorldPosition = math.lerp(
                travelState.StartPosition,
                travelState.Destination,
                travelState.TravelProgress
            );
        }

        // Обновление сеточной позиции
        position.GridPosition = new int2(
            (int)math.round(position.WorldPosition.x),
            (int)math.round(position.WorldPosition.z)
        );

        // Обновление текущей местности
        UpdateCurrentTerrain(ref position);

        // Проверка прибытия
        if (travelState.TravelProgress >= 1.0f && !travelState.DestinationReached)
        {
            travelState.IsTraveling = false;
            travelState.DestinationReached = true;
            travelState.TravelProgress = 1.0f;

            position.WorldPosition = travelState.Destination;
            position.GridPosition = new int2(
                (int)math.round(travelState.Destination.x),
                (int)math.round(travelState.Destination.z)
            );
        }
    }

    private void ApplyStarvationEffects(ref ConvoyResources resources, ref PlayerConvoy convoy, float deltaTime)
    {
        resources.Morale = math.max(0.1f, resources.Morale - 0.05f * deltaTime);
        convoy.CurrentSpeedModifier = math.max(0.3f, convoy.CurrentSpeedModifier - 0.1f * deltaTime);
    }

    private void UpdateCurrentTerrain(ref MapPosition position)
    {
        // Упрощенная логика определения местности
        var hash = math.hash(position.GridPosition);
        var terrainValue = hash % 6;

        position.CurrentTerrain = terrainValue switch
        {
            0 => TerrainType.Plains,
            1 => TerrainType.Forest,
            2 => TerrainType.Mountains,
            3 => TerrainType.Road,
            4 => TerrainType.Desert,
            5 => TerrainType.River,
            _ => TerrainType.Plains
        };
    }

    private float GetTerrainSpeedModifier(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Road => 1.2f,
            TerrainType.Plains => 1.0f,
            TerrainType.Forest => 0.7f,
            TerrainType.Mountains => 0.5f,
            TerrainType.Desert => 0.6f,
            TerrainType.River => 0.4f,
            _ => 1.0f
        };
    }
}