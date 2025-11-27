using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EconomySystem : ISystem
{
    private float _priceUpdateTimer;

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        _priceUpdateTimer += deltaTime;

        // Обновляем цены каждые 30 секунд
        if (_priceUpdateTimer >= 30f)
        {
            UpdateMarketPrices(ref state);
            _priceUpdateTimer = 0f;
        }

        // Обрабатываем торговые транзакции
        ProcessTradeTransactions(ref state);
    }

    private void UpdateMarketPrices(ref SystemState state)
    {
        var marketQuery = SystemAPI.QueryBuilder().WithAll<CityMarket>().Build();
        var markets = marketQuery.ToEntityArray(Allocator.Temp);

        foreach (var marketEntity in markets)
        {
            UpdateCityPrices(marketEntity, ref state);
        }

        markets.Dispose();
    }

    private void UpdateCityPrices(Entity marketEntity, ref SystemState state)
    {
        var priceBuffer = state.EntityManager.GetBuffer<GoodPriceBuffer>(marketEntity);
        var random = Unity.Mathematics.Random.CreateFromIndex((uint)marketEntity.Index);

        for (int i = 0; i < priceBuffer.Length; i++)
        {
            var priceData = priceBuffer[i];

            // Имитация изменения спроса/предложения
            var demandChange = random.NextFloat(-0.1f, 0.1f);
            var supplyChange = random.NextFloat(-0.05f, 0.05f);

            priceData.Demand = math.clamp(priceData.Demand + demandChange, 0.5f, 2.0f);
            priceData.Supply = math.clamp(priceData.Supply + supplyChange, 0.5f, 2.0f);

            // Пересчет цены
            var basePrice = GetBasePrice(priceData.GoodEntity, ref state);
            var priceMultiplier = priceData.Demand / math.max(priceData.Supply, 0.1f);
            priceData.Price = (int)(basePrice * priceMultiplier);

            priceBuffer[i] = priceData;
        }
    }

    private void ProcessTradeTransactions(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transaction, entity) in
                 SystemAPI.Query<RefRO<TradeTransaction>>().WithEntityAccess())
        {
            if (transaction.ValueRO.Status == TradeStatus.Pending)
            {
                ProcessTransaction(transaction.ValueRO, ref state, ref ecb);
            }
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
                AddToInventory(playerEntity, transaction.GoodEntity, transaction.Quantity, ref state);
            }
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
        }
    }

    private int GetBasePrice(Entity goodEntity, ref SystemState state)
    {
        if (state.EntityManager.HasComponent<GoodData>(goodEntity))
        {
            var goodData = state.EntityManager.GetComponentData<GoodData>(goodEntity);
            return goodData.BaseValue;
        }
        return 10;
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