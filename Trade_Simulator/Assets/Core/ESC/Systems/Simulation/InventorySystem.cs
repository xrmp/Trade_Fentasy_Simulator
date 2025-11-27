using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct InventorySystem : ISystem
{
    private float _decayTimer;

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        _decayTimer += deltaTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Проверка перегрузки
        CheckOverload(ref state);

        // Порча товаров каждые 30 секунд
        if (_decayTimer >= 30f)
        {
            ProcessGoodsDecay(ref state, ref ecb);
            _decayTimer = 0f;
        }

        // Автоматическая сортировка
        SortInventory(ref state, ref ecb);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void CheckOverload(ref SystemState state)
    {
        foreach (var (convoy, entity) in
                 SystemAPI.Query<RefRW<PlayerConvoy>>().WithAll<PlayerTag>().WithEntityAccess())
        {
            if (convoy.ValueRO.UsedCapacity > convoy.ValueRO.TotalCapacity)
            {
                var overloadRatio = (float)convoy.ValueRO.UsedCapacity / convoy.ValueRO.TotalCapacity;
                convoy.ValueRW.CurrentSpeedModifier = math.max(0.3f, 1.0f - (overloadRatio - 1.0f) * 0.5f);
            }
            else
            {
                convoy.ValueRW.CurrentSpeedModifier = 1.0f;
            }
        }
    }

    private void ProcessGoodsDecay(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var parallelEcb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var decayJob = new GoodsDecayJob
        {
            ECB = parallelEcb,
            EntityManager = state.EntityManager,
            RandomSeed = (uint)SystemAPI.Time.ElapsedTime + 1
        };

        state.Dependency = decayJob.Schedule(state.Dependency);
    }

    private void SortInventory(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var sortJob = new SortInventoryJob
        {
            EntityManager = state.EntityManager
        };

        state.Dependency = sortJob.Schedule(state.Dependency);
    }

    private float GetDecayChance(GoodCategory category)
    {
        return category switch
        {
            GoodCategory.Food => 0.3f,
            GoodCategory.RawMaterials => 0.1f,
            GoodCategory.Crafts => 0.05f,
            GoodCategory.Luxury => 0.02f,
            _ => 0.1f
        };
    }

    [BurstCompile]
    private partial struct GoodsDecayJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public EntityManager EntityManager;
        public uint RandomSeed;

        public void Execute([EntityIndexInQuery] int index, Entity entity, DynamicBuffer<InventoryBuffer> inventory)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex(RandomSeed + (uint)index);

            for (int i = inventory.Length - 1; i >= 0; i--)
            {
                var item = inventory[i];
                if (EntityManager.HasComponent<GoodData>(item.GoodEntity))
                {
                    var goodData = EntityManager.GetComponentData<GoodData>(item.GoodEntity);
                    var decayChance = GetDecayChance(goodData.Category);

                    if (random.NextFloat() < decayChance)
                    {
                        var decayAmount = math.max(1, item.Quantity / 10);
                        item.Quantity -= decayAmount;

                        if (item.Quantity <= 0)
                        {
                            inventory.RemoveAt(i);
                        }
                        else
                        {
                            inventory[i] = item;
                        }
                    }
                }
            }
        }

        private float GetDecayChance(GoodCategory category)
        {
            return category switch
            {
                GoodCategory.Food => 0.3f,
                GoodCategory.RawMaterials => 0.1f,
                GoodCategory.Crafts => 0.05f,
                GoodCategory.Luxury => 0.02f,
                _ => 0.1f
            };
        }
    }

    [BurstCompile]
    private partial struct SortInventoryJob : IJobEntity
    {
        public EntityManager EntityManager;

        public void Execute(DynamicBuffer<InventoryBuffer> inventory)
        {
            var items = new NativeList<InventoryItem>(Allocator.Temp);

            // Собираем товары для сортировки
            foreach (var item in inventory)
            {
                if (item.Quantity > 0 && EntityManager.HasComponent<GoodData>(item.GoodEntity))
                {
                    var goodData = EntityManager.GetComponentData<GoodData>(item.GoodEntity);
                    var efficiency = (float)goodData.BaseValue / goodData.WeightPerUnit;

                    items.Add(new InventoryItem
                    {
                        GoodEntity = item.GoodEntity,
                        Quantity = item.Quantity,
                        Efficiency = efficiency
                    });
                }
            }

            // Сортировка пузырьком по эффективности
            if (items.Length > 1)
            {
                for (int i = 0; i < items.Length - 1; i++)
                {
                    for (int j = i + 1; j < items.Length; j++)
                    {
                        if (items[i].Efficiency < items[j].Efficiency)
                        {
                            var temp = items[i];
                            items[i] = items[j];
                            items[j] = temp;
                        }
                    }
                }

                // Перезаписываем инвентарь
                inventory.Clear();
                foreach (var item in items)
                {
                    inventory.Add(new InventoryBuffer
                    {
                        GoodEntity = item.GoodEntity,
                        Quantity = item.Quantity
                    });
                }
            }

            items.Dispose();
        }
    }

    private struct InventoryItem
    {
        public Entity GoodEntity;
        public int Quantity;
        public float Efficiency;
    }
}