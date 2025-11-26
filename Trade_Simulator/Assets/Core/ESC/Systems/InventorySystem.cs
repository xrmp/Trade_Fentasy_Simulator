using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct InventorySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Проверка перегрузки обоза
        CheckOverload(ref state);

        // Автоматическая сортировка инвентаря
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
                // Штраф за перегрузку - снижение скорости
                var overloadRatio = (float)convoy.ValueRO.UsedCapacity / convoy.ValueRO.TotalCapacity;
                convoy.ValueRW.CurrentSpeedModifier = math.max(0.3f, 1.0f - (overloadRatio - 1.0f) * 0.5f);
            }
            else
            {
                // Восстанавливаем нормальную скорость
                convoy.ValueRW.CurrentSpeedModifier = 1.0f;
            }
        }
    }

    private void SortInventory(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        foreach (var (inventory, entity) in
                 SystemAPI.Query<DynamicBuffer<InventoryBuffer>>().WithAll<PlayerTag>().WithEntityAccess())
        {
            // Создаем временный список для сортировки
            var items = new NativeList<InventoryItem>(Allocator.Temp);

            // Собираем все товары
            for (int i = 0; i < inventory.Length; i++)
            {
                var item = inventory[i];
                if (item.Quantity > 0 && state.EntityManager.HasComponent<GoodData>(item.GoodEntity))
                {
                    var goodData = state.EntityManager.GetComponentData<GoodData>(item.GoodEntity);
                    var efficiency = (float)goodData.BaseValue / goodData.WeightPerUnit;

                    items.Add(new InventoryItem
                    {
                        GoodEntity = item.GoodEntity,
                        Quantity = item.Quantity,
                        Efficiency = efficiency
                    });
                }
            }

            // Сортировка по эффективности (убывание)
            if (items.Length > 1)
            {
                // Простая пузырьковая сортировка
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

                // Очищаем и перезаписываем инвентарь
                inventory.Clear();
                for (int i = 0; i < items.Length; i++)
                {
                    inventory.Add(new InventoryBuffer
                    {
                        GoodEntity = items[i].GoodEntity,
                        Quantity = items[i].Quantity
                    });
                }
            }

            items.Dispose();
        }
    }

    // Вспомогательные структуры для сортировки
    private struct InventoryItem
    {
        public Entity GoodEntity;
        public int Quantity;
        public float Efficiency;
    }
}

// Упрощенная система статистики инвентаря
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct InventoryStatsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (inventory, convoy, entity) in
                 SystemAPI.Query<DynamicBuffer<InventoryBuffer>, RefRO<PlayerConvoy>>()
                 .WithAll<PlayerTag>().WithEntityAccess())
        {
            var stats = new InventoryStats();

            foreach (var item in inventory)
            {
                if (item.Quantity > 0 && state.EntityManager.HasComponent<GoodData>(item.GoodEntity))
                {
                    var goodData = state.EntityManager.GetComponentData<GoodData>(item.GoodEntity);

                    stats.TotalItems += item.Quantity;
                    stats.TotalValue += goodData.BaseValue * item.Quantity;
                    stats.TotalWeight += goodData.WeightPerUnit * item.Quantity;
                }
            }

            // Обновляем статистику
            if (state.EntityManager.HasComponent<InventoryStats>(entity))
            {
                state.EntityManager.SetComponentData(entity, stats);
            }
            else
            {
                // Используем EntityManager для добавления компонента
                state.EntityManager.AddComponentData(entity, stats);
            }
        }
    }
}

// Компоненты для системы инвентаря
public struct InventoryStats : IComponentData
{
    public int TotalItems;
    public int TotalValue;
    public int TotalWeight;
}