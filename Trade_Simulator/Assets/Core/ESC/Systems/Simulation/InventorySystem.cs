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

        // Получаем lookup для доступа к компонентам
        var goodDataLookup = SystemAPI.GetComponentLookup<GoodData>();

        // Сначала выполняем все джобы
        CheckOverload(ref state);

        // Порча товаров каждые 30 секунд
        if (_decayTimer >= 30f)
        {
            ProcessGoodsDecay(ref state, goodDataLookup);
            _decayTimer = 0f;
        }

        // Автоматическая сортировка
        SortInventory(ref state, goodDataLookup);

        // ЖДЕМ завершения всех джобов перед созданием ECB
        state.Dependency.Complete();

        // Теперь безопасно работаем с EntityManager
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Здесь можно добавить операции через ECB если нужно

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

    private void ProcessGoodsDecay(ref SystemState state, ComponentLookup<GoodData> goodDataLookup)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var parallelEcb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var decayJob = new GoodsDecayJob
        {
            ECB = parallelEcb,
            GoodDataLookup = goodDataLookup,
            RandomSeed = (uint)SystemAPI.Time.ElapsedTime + 1
        };

        state.Dependency = decayJob.Schedule(state.Dependency);
    }

    private void SortInventory(ref SystemState state, ComponentLookup<GoodData> goodDataLookup)
    {
        var sortJob = new SortInventoryJob
        {
            GoodDataLookup = goodDataLookup
        };

        state.Dependency = sortJob.Schedule(state.Dependency);
    }

    [BurstCompile]
    private partial struct GoodsDecayJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<GoodData> GoodDataLookup;
        public uint RandomSeed;

        public void Execute([EntityIndexInQuery] int index, Entity entity, DynamicBuffer<InventoryBuffer> inventory)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex(RandomSeed + (uint)index);

            for (int i = inventory.Length - 1; i >= 0; i--)
            {
                var item = inventory[i];

                // Получаем GoodData через Lookup вместо EntityManager
                if (GoodDataLookup.HasComponent(item.GoodEntity))
                {
                    var goodData = GoodDataLookup[item.GoodEntity];
                    var decayChance = GetDecayChance(goodData.Category);

                    if (random.NextFloat() < decayChance)
                    {
                        var decayAmount = math.max(1, item.Quantity / 10);
                        item.Quantity -= decayAmount;

                        if (item.Quantity <= 0)
                        {
                            // Удаляем элемент из буфера
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
        [ReadOnly] public ComponentLookup<GoodData> GoodDataLookup;

        public void Execute(DynamicBuffer<InventoryBuffer> inventory)
        {
            // Временный список для сортировки
            var items = new NativeList<InventoryItem>(Allocator.Temp);

            // Собираем товары для сортировки
            for (int i = 0; i < inventory.Length; i++)
            {
                var item = inventory[i];
                if (item.Quantity > 0)
                {
                    // Используем Lookup для доступа к данным
                    if (GoodDataLookup.HasComponent(item.GoodEntity))
                    {
                        var goodData = GoodDataLookup[item.GoodEntity];
                        var efficiency = (float)goodData.BaseValue / goodData.WeightPerUnit;

                        items.Add(new InventoryItem
                        {
                            GoodEntity = item.GoodEntity,
                            Quantity = item.Quantity,
                            Efficiency = efficiency
                        });
                    }
                }
            }

            // Сортировка по эффективности (по убыванию)
            if (items.Length > 1)
            {
                // Используем более эффективную сортировку
                SortItems(items);

                // Перезаписываем инвентарь в отсортированном порядке
                // Сначала очищаем, потом добавляем отсортированные
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

        private void SortItems(NativeList<InventoryItem> items)
        {
            // Сортировка выбором (более эффективная чем пузырьковая)
            for (int i = 0; i < items.Length - 1; i++)
            {
                int maxIndex = i;
                for (int j = i + 1; j < items.Length; j++)
                {
                    if (items[j].Efficiency > items[maxIndex].Efficiency)
                    {
                        maxIndex = j;
                    }
                }

                if (maxIndex != i)
                {
                    var temp = items[i];
                    items[i] = items[maxIndex];
                    items[maxIndex] = temp;
                }
            }
        }
    }

    private struct InventoryItem
    {
        public Entity GoodEntity;
        public int Quantity;
        public float Efficiency;
    }
}