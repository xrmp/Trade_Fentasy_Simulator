using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct WagonManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка запросов на ремонт повозок
        foreach (var (repair, entity) in
                 SystemAPI.Query<RefRO<WagonRepair>>().WithEntityAccess())
        {
            ProcessRepair(repair.ValueRO, ref state);
            ecb.DestroyEntity(entity);
        }

        // Обработка запросов на покупку повозок
        foreach (var (purchase, entity) in
                 SystemAPI.Query<RefRO<WagonPurchase>>().WithEntityAccess())
        {
            ProcessPurchase(purchase.ValueRO, ref state, ref ecb);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ProcessRepair(WagonRepair repair, ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

        // Поиск сломанной повозки
        var wagonQuery = SystemAPI.QueryBuilder().WithAll<Wagon>().Build();
        var wagons = wagonQuery.ToEntityArray(Allocator.Temp);

        foreach (var wagonEntity in wagons)
        {
            var wagon = SystemAPI.GetComponent<Wagon>(wagonEntity);

            if (wagon.IsBroken)
            {
                var repairCost = CalculateRepairCost(wagon);

                if (resources.Gold >= repairCost)
                {
                    resources.Gold -= repairCost;

                    // Ремонт повозки
                    wagon.Health = wagon.MaxHealth;
                    wagon.IsBroken = false;
                    SystemAPI.SetComponent(wagonEntity, wagon);

                    Debug.Log($"🔧 Повозка отремонтирована за {repairCost} золота");
                    break;
                }
                else
                {
                    Debug.Log("❌ Недостаточно золота для ремонта");
                }
            }
        }

        wagons.Dispose();
        SystemAPI.SetComponent(playerEntity, resources);
    }

    private void ProcessPurchase(WagonPurchase purchase, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources, PlayerConvoy>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);
        var convoy = SystemAPI.GetComponent<PlayerConvoy>(playerEntity);

        var cost = GetWagonCost(purchase.WagonType);

        if (resources.Gold >= cost)
        {
            resources.Gold -= cost;

            // Создание новой повозки
            var wagonEntity = ecb.CreateEntity();
            ecb.AddComponent(wagonEntity, new Wagon
            {
                Owner = playerEntity,
                Health = GetWagonHealth(purchase.WagonType),
                MaxHealth = GetWagonHealth(purchase.WagonType),
                LoadCapacity = GetWagonCapacity(purchase.WagonType),
                CurrentLoad = 0,
                SpeedModifier = GetWagonSpeed(purchase.WagonType),
                WearRate = GetWagonWearRate(purchase.WagonType),
                WagonType = purchase.WagonType,
                IsBroken = false
            });

            // Обновление общей грузоподъемности
            convoy.TotalCapacity += GetWagonCapacity(purchase.WagonType);

            SystemAPI.SetComponent(playerEntity, resources);
            SystemAPI.SetComponent(playerEntity, convoy);

            Debug.Log($"🛒 Куплена новая повозка ({purchase.WagonType}) за {cost} золота");
        }
        else
        {
            Debug.Log("❌ Недостаточно золота для покупки повозки");
        }
    }

    private int CalculateRepairCost(Wagon wagon)
    {
        var damageRatio = 1.0f - ((float)wagon.Health / wagon.MaxHealth);
        return (int)(GetWagonCost(wagon.WagonType) * damageRatio * 0.5f);
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

    private int GetWagonHealth(WagonType type)
    {
        return type switch
        {
            WagonType.BasicCart => 100,
            WagonType.TradeWagon => 120,
            WagonType.HeavyWagon => 150,
            WagonType.LuxuryCoach => 80,
            _ => 100
        };
    }

    private int GetWagonCapacity(WagonType type)
    {
        return type switch
        {
            WagonType.BasicCart => 500,
            WagonType.TradeWagon => 800,
            WagonType.HeavyWagon => 1200,
            WagonType.LuxuryCoach => 300,
            _ => 500
        };
    }

    private float GetWagonSpeed(WagonType type)
    {
        return type switch
        {
            WagonType.BasicCart => 1.0f,
            WagonType.TradeWagon => 0.9f,
            WagonType.HeavyWagon => 0.7f,
            WagonType.LuxuryCoach => 1.2f,
            _ => 1.0f
        };
    }

    private float GetWagonWearRate(WagonType type)
    {
        return type switch
        {
            WagonType.BasicCart => 0.1f,
            WagonType.TradeWagon => 0.08f,
            WagonType.HeavyWagon => 0.12f,
            WagonType.LuxuryCoach => 0.15f,
            _ => 0.1f
        };
    }
}

public struct WagonRepair : IComponentData
{
    public Entity WagonEntity; // Если Entity.Null - ремонт любой сломанной повозки
}

public struct WagonPurchase : IComponentData
{
    public WagonType WagonType;
}