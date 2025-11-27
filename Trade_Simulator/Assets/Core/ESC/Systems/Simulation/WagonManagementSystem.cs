using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct WagonManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка износа повозок
        ProcessWagonWear(ref state, ref ecb);

        // Обработка ремонта
        ProcessWagonRepairs(ref state, ref ecb);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ProcessWagonWear(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        foreach (var (wagon, position, entity) in
                 SystemAPI.Query<RefRW<Wagon>, RefRO<MapPosition>>().WithEntityAccess())
        {
            if (wagon.ValueRO.IsBroken) continue;

            // Базовый износ + износ от местности
            var terrainModifier = GetTerrainWearMultiplier(position.ValueRO.CurrentTerrain);
            var wearAmount = wagon.ValueRO.WearRate * terrainModifier * SystemAPI.Time.DeltaTime;

            // Дополнительный износ от перегрузки
            if (wagon.ValueRO.CurrentLoad > wagon.ValueRO.LoadCapacity)
            {
                var overload = (float)wagon.ValueRO.CurrentLoad / wagon.ValueRO.LoadCapacity;
                wearAmount *= overload;
            }

            wagon.ValueRW.Health = (int)math.max(0, wagon.ValueRO.Health - wearAmount);

            // Проверка поломки
            if (wagon.ValueRO.Health <= 0 && !wagon.ValueRO.IsBroken)
            {
                wagon.ValueRW.IsBroken = true;
            }
        }
    }

    private void ProcessWagonRepairs(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        foreach (var (repairAction, entity) in
                 SystemAPI.Query<RefRO<WagonRepairAction>>().WithEntityAccess())
        {
            var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
            if (playerQuery.IsEmpty)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            var playerEntity = playerQuery.GetSingletonEntity();
            var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

            var wagonEntity = repairAction.ValueRO.WagonEntity;
            if (state.EntityManager.HasComponent<Wagon>(wagonEntity))
            {
                var wagon = state.EntityManager.GetComponentData<Wagon>(wagonEntity);
                var repairCost = CalculateRepairCost(wagon);

                if (resources.Gold >= repairCost)
                {
                    resources.Gold -= repairCost;
                    wagon.Health = wagon.MaxHealth;
                    wagon.IsBroken = false;

                    state.EntityManager.SetComponentData(wagonEntity, wagon);
                    state.EntityManager.SetComponentData(playerEntity, resources);
                }
            }

            ecb.DestroyEntity(entity);
        }
    }

    private float GetTerrainWearMultiplier(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Road => 0.7f,
            TerrainType.Plains => 1.0f,
            TerrainType.Forest => 1.3f,
            TerrainType.Mountains => 2.0f,
            TerrainType.Desert => 1.5f,
            TerrainType.River => 1.8f,
            _ => 1.0f
        };
    }

    private int CalculateRepairCost(Wagon wagon)
    {
        var damageRatio = 1.0f - ((float)wagon.Health / wagon.MaxHealth);
        return (int)(GetWagonCost(wagon.Type) * damageRatio * 0.5f);
    }

    private int GetWagonCost(WagonType type)
    {
        return type switch
        {
            WagonType.BasicCart => 100,
            WagonType.TradeWagon => 200,
            WagonType.HeavyWagon => 300,
            WagonType.LuxuryCoach => 500,
            _ => 100
        };
    }
}

// Компонент для запроса ремонта
public struct WagonRepairAction : IComponentData
{
    public Entity WagonEntity;
}