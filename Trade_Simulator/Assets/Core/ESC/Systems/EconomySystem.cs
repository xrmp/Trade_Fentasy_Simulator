using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EconomySystem : ISystem
{
    private float _priceUpdateTimer;

    public void OnUpdate(ref SystemState state)
    {
        _priceUpdateTimer += SystemAPI.Time.DeltaTime;

        // Обновляем цены каждые 30 секунд
        if (_priceUpdateTimer >= 30f)
        {
            UpdateMarketPrices(ref state);
            _priceUpdateTimer = 0f;
        }
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

        for (int i = 0; i < priceBuffer.Length; i++)
        {
            var priceData = priceBuffer[i];
            var random = Random.CreateFromIndex((uint)i);

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

    private int GetBasePrice(Entity goodEntity, ref SystemState state)
    {
        if (state.EntityManager.HasComponent<GoodData>(goodEntity))
        {
            var goodData = state.EntityManager.GetComponentData<GoodData>(goodEntity);
            return goodData.BaseValue;
        }
        return 10;
    }
}