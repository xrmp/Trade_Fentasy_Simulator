using Unity.Entities;
using Unity.Collections;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct TradeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка торговых транзакций
        foreach (var (transaction, entity) in
                 SystemAPI.Query<RefRO<TradeTransaction>>().WithEntityAccess())
        {
            ProcessTransaction(transaction.ValueRO, ref state, ref ecb);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ProcessTransaction(TradeTransaction transaction, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources, PlayerConvoy>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = state.EntityManager.GetComponentData<ConvoyResources>(playerEntity);
        var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(playerEntity);

        if (transaction.IsBuy)
        {
            ProcessBuyTransaction(transaction, ref resources, ref convoy, playerEntity, ref state);
        }
        else
        {
            ProcessSellTransaction(transaction, ref resources, ref convoy, playerEntity, ref state);
        }

        state.EntityManager.SetComponentData(playerEntity, resources);
        state.EntityManager.SetComponentData(playerEntity, convoy);
    }

    private void ProcessBuyTransaction(TradeTransaction transaction, ref ConvoyResources resources,
                                     ref PlayerConvoy convoy, Entity playerEntity, ref SystemState state)
    {
        if (resources.Gold >= transaction.TotalPrice)
        {
            var goodWeight = GetGoodWeight(transaction.GoodEntity, ref state);
            var totalWeight = goodWeight * transaction.Quantity;

            if (convoy.UsedCapacity + totalWeight <= convoy.TotalCapacity)
            {
                resources.Gold -= transaction.TotalPrice;
                convoy.UsedCapacity += totalWeight;

                // Добавляем товар в инвентарь
                AddToInventory(playerEntity, transaction.GoodEntity, transaction.Quantity, ref state);

                Debug.Log($"🛒 Куплено {transaction.Quantity} ед. товара за {transaction.TotalPrice} золота");
            }
            else
            {
                Debug.Log("❌ Недостаточно места в обозе!");
            }
        }
        else
        {
            Debug.Log("❌ Недостаточно золота для покупки!");
        }
    }

    private void ProcessSellTransaction(TradeTransaction transaction, ref ConvoyResources resources,
                                      ref PlayerConvoy convoy, Entity playerEntity, ref SystemState state)
    {
        var goodWeight = GetGoodWeight(transaction.GoodEntity, ref state);
        var totalWeight = goodWeight * transaction.Quantity;

        if (RemoveFromInventory(playerEntity, transaction.GoodEntity, transaction.Quantity, ref state))
        {
            resources.Gold += transaction.TotalPrice;
            convoy.UsedCapacity -= totalWeight;
            Debug.Log($"💰 Продано {transaction.Quantity} ед. товара за {transaction.TotalPrice} золота");
        }
        else
        {
            Debug.Log("❌ Недостаточно товара для продажи!");
        }
    }

    private int GetGoodWeight(Entity goodEntity, ref SystemState state)
    {
        if (state.EntityManager.HasComponent<GoodData>(goodEntity))
        {
            var goodData = state.EntityManager.GetComponentData<GoodData>(goodEntity);
            return goodData.WeightPerUnit;
        }
        return 1;
    }

    private void AddToInventory(Entity playerEntity, Entity goodEntity, int quantity, ref SystemState state)
    {
        var inventory = state.EntityManager.GetBuffer<InventoryBuffer>(playerEntity);

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].GoodEntity == goodEntity)
            {
                var item = inventory[i];
                item.Quantity += quantity;
                inventory[i] = item;
                return;
            }
        }

        // Новый товар в инвентаре
        inventory.Add(new InventoryBuffer { GoodEntity = goodEntity, Quantity = quantity });
    }

    private bool RemoveFromInventory(Entity playerEntity, Entity goodEntity, int quantity, ref SystemState state)
    {
        var inventory = state.EntityManager.GetBuffer<InventoryBuffer>(playerEntity);

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].GoodEntity == goodEntity && inventory[i].Quantity >= quantity)
            {
                var item = inventory[i];
                item.Quantity -= quantity;
                inventory[i] = item;

                // Удаляем если количество 0
                if (item.Quantity <= 0)
                {
                    inventory.RemoveAt(i);
                }

                return true;
            }
        }

        return false;
    }
}