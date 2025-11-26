using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MapMovementSystem : ISystem
{
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
        _random = new Random(12345); // Инициализируем Random один раз
    }

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
        // === ЛОГИКА ПОТРЕБЛЕНИЯ ПИЩИ ===
        var foodConsumption = resources.FoodConsumptionRate * deltaTime;

        if (resources.Food > 0)
        {
            resources.Food = (int)math.max(0, resources.Food - foodConsumption);

            if (convoy.CurrentSpeedModifier < 1.0f)
            {
                convoy.CurrentSpeedModifier = math.min(1.0f,
                    convoy.CurrentSpeedModifier + 0.1f * deltaTime);
            }
        }
        else
        {
            ApplyStarvationEffects(ref resources, ref convoy, deltaTime);
        }

        // === ЛОГИКА ДВИЖЕНИЯ ===
        var terrainModifier = GetTerrainSpeedModifier(position.CurrentTerrainType);
        var currentSpeed = convoy.BaseSpeed * terrainModifier * convoy.CurrentSpeedModifier;

        travelState.TravelProgress += (currentSpeed * deltaTime) /
                                    math.max(1f, travelState.TotalTravelTime);

        // === ИНТЕРПОЛЯЦИЯ ПОЗИЦИИ ===
        if (!travelState.DestinationReached && travelState.TravelProgress <= 1.0f)
        {
            position.WorldPosition = math.lerp(
                travelState.StartPosition,
                travelState.Destination,
                travelState.TravelProgress
            );
        }

        position.GridPosition = new int2(
            (int)math.round(position.WorldPosition.x),
            (int)math.round(position.WorldPosition.z)
        );

        UpdateCurrentTerrain(ref position);

        // === ПРОВЕРКА ПРИБЫТИЯ ===
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

        // Используем mathematics.Random вместо UnityEngine.Random
        if (resources.Morale <= 0.3f && _random.NextFloat() < 0.01f * deltaTime)
        {
            resources.Guards = math.max(1, resources.Guards - 1);
        }
    }

    private void UpdateCurrentTerrain(ref MapPosition position)
    {
        var hash = math.hash(position.GridPosition);
        var terrainValue = hash % 6;

        position.CurrentTerrainType = terrainValue switch
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