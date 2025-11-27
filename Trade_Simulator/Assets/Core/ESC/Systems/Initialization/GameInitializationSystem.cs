using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct GameInitializationSystem : ISystem
{
    private bool _initialized;

    public void OnCreate(ref SystemState state)
    {
        _initialized = false;
        Debug.Log("🔄 GameInitializationSystem создана");
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_initialized)
        {
            state.Enabled = false;
            return;
        }

        // Ждем GameConfig
        if (!SystemAPI.TryGetSingleton<GameConfig>(out var gameConfig))
        {
            Debug.Log("⏳ GameInit: Ожидаем GameConfig...");
            return;
        }

        Debug.Log("🎯 GameInit: Начинаем инициализацию игровых данных...");

        // Создаем все базовые данные игры
        CreateGoodsData(ref state);
        CreateCitiesData(ref state, gameConfig);
        CreateMapData(ref state, gameConfig);

        _initialized = true;
        state.Enabled = false;
        Debug.Log("✅ Игровые данные инициализированы!");
    }

    private void CreateGoodsData(ref SystemState state)
    {
        var goods = new (string, int, int, GoodCategory, float)[]
        {
            // Название, Вес, Базовая цена, Категория, Скорость порчи
            ("Зерно", 1, 10, GoodCategory.RawMaterials, 0.3f),
            ("Древесина", 2, 15, GoodCategory.RawMaterials, 0.1f),
            ("Железная Руда", 5, 20, GoodCategory.RawMaterials, 0.05f),
            ("Уголь", 3, 12, GoodCategory.RawMaterials, 0.02f),

            ("Ткань", 3, 25, GoodCategory.Crafts, 0.1f),
            ("Кожа", 4, 35, GoodCategory.Crafts, 0.15f),
            ("Инструменты", 2, 50, GoodCategory.Crafts, 0.02f),
            ("Гончарные Изделия", 2, 30, GoodCategory.Crafts, 0.2f),

            ("Вино", 2, 50, GoodCategory.Luxury, 0.1f),
            ("Украшения", 1, 100, GoodCategory.Luxury, 0.01f),
            ("Шелк", 1, 80, GoodCategory.Luxury, 0.05f),
            ("Пряности", 1, 60, GoodCategory.Luxury, 0.08f),

            ("Фрукты", 1, 8, GoodCategory.Food, 0.4f),
            ("Овощи", 1, 6, GoodCategory.Food, 0.35f),
            ("Мясо", 2, 25, GoodCategory.Food, 0.5f),
            ("Хлеб", 1, 5, GoodCategory.Food, 0.3f)
        };

        foreach (var (name, weight, value, category, decayRate) in goods)
        {
            var goodEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(goodEntity, new GoodData
            {
                Name = name,
                WeightPerUnit = weight,
                BaseValue = value,
                Category = category,
                ProfitPerKm = value * 0.01f,
                DecayRate = decayRate
            });
        }

        Debug.Log($"✅ Создано {goods.Length} видов товаров");
    }

    private void CreateCitiesData(ref SystemState state, GameConfig gameConfig)
    {
        var cities = new (string, int2, EconomyType, int)[]
        {
            ("Стартовый Город", new int2(10, 10), EconomyType.Agricultural, 2000),
            ("Торговая Столица", new int2(50, 50), EconomyType.TradeHub, 5000),
            ("Горная Крепость", new int2(80, 20), EconomyType.Mining, 1500),
            ("Портовый Город", new int2(30, 70), EconomyType.Industrial, 3000),
            ("Северный Форпост", new int2(20, 80), EconomyType.Agricultural, 1200),
            ("Южная Деревня", new int2(70, 30), EconomyType.Agricultural, 800)
        };

        foreach (var (name, gridPos, economy, population) in cities)
        {
            var cityEntity = state.EntityManager.CreateEntity();
            var worldPos = new float3(gridPos.x * gameConfig.WorldScale, 0, gridPos.y * gameConfig.WorldScale);

            // Создаем город
            state.EntityManager.AddComponent<CityTag>(cityEntity);
            state.EntityManager.AddComponentData(cityEntity, new City
            {
                Name = name,
                GridPosition = gridPos,
                WorldPosition = worldPos,
                Population = population,
                EconomyType = economy,
                TradeRadius = 20
            });

            // Создаем рынок для города
            CreateCityMarket(cityEntity, economy, ref state);
        }

        Debug.Log($"✅ Создано {cities.Length} городов");
    }

    private void CreateCityMarket(Entity cityEntity, EconomyType economy, ref SystemState state)
    {
        var marketEntity = state.EntityManager.CreateEntity();

        state.EntityManager.AddComponentData(marketEntity, new CityMarket
        {
            CityEntity = cityEntity,
            PriceMultiplier = GetEconomyMultiplier(economy),
            TradeVolume = 1.0f
        });

        // Добавляем цены на все товары
        var priceBuffer = state.EntityManager.AddBuffer<GoodPriceBuffer>(marketEntity);
        var goodsQuery = state.EntityManager.CreateEntityQuery(typeof(GoodData));
        var goods = goodsQuery.ToEntityArray(Allocator.Temp);

        foreach (var goodEntity in goods)
        {
            var goodData = state.EntityManager.GetComponentData<GoodData>(goodEntity);
            var basePrice = goodData.BaseValue;
            var economyModifier = GetEconomyPriceModifier(economy, goodData.Category);
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(cityEntity.Index + goodEntity.Index));
            priceBuffer.Add(new GoodPriceBuffer
            {
                GoodEntity = goodEntity,
                Price = (int)(basePrice * economyModifier * random.NextFloat(0.8f, 1.2f)),
                Supply = random.NextFloat(0.5f, 1.5f),
                Demand = random.NextFloat(0.5f, 1.5f)
            });
        }

        goods.Dispose();
    }

    private void CreateMapData(ref SystemState state, GameConfig gameConfig)
    {
        // Создаем конфигурацию карты
        var mapConfigEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(mapConfigEntity, new MapConfig
        {
            Width = gameConfig.MapWidth,
            Height = gameConfig.MapHeight,
            WorldScale = gameConfig.WorldScale,
            Seed = 12345
        });

        // Создаем тестовые данные местности (упрощенно)
        // В реальной системе здесь будет генерация карты
        for (int x = 0; x < math.min(10, gameConfig.MapWidth); x++)
        {
            for (int y = 0; y < math.min(10, gameConfig.MapHeight); y++)
            {
                var terrainEntity = state.EntityManager.CreateEntity();
                var terrainType = GetTerrainTypeForPosition(new int2(x, y));

                state.EntityManager.AddComponentData(terrainEntity, new TerrainData
                {
                    GridPosition = new int2(x, y),
                    Type = terrainType,
                    MovementCost = GetMovementCost(terrainType),
                    WearMultiplier = GetWearMultiplier(terrainType),
                    DangerLevel = GetDangerLevel(terrainType),
                    FoodAvailability = GetFoodAvailability(terrainType)
                });
            }
        }

        Debug.Log("🗺️ Базовая карта создана");
    }

    private float GetEconomyMultiplier(EconomyType economy)
    {
        return economy switch
        {
            EconomyType.Agricultural => 0.8f,
            EconomyType.Industrial => 1.2f,
            EconomyType.TradeHub => 1.0f,
            EconomyType.Mining => 1.1f,
            _ => 1.0f
        };
    }

    private float GetEconomyPriceModifier(EconomyType economy, GoodCategory category)
    {
        return (economy, category) switch
        {
            (EconomyType.Agricultural, GoodCategory.RawMaterials) => 0.7f,
            (EconomyType.Agricultural, GoodCategory.Food) => 0.6f,
            (EconomyType.Agricultural, GoodCategory.Luxury) => 1.3f,

            (EconomyType.Industrial, GoodCategory.Crafts) => 0.8f,
            (EconomyType.Industrial, GoodCategory.RawMaterials) => 1.1f,

            (EconomyType.Mining, GoodCategory.RawMaterials) => 0.9f,
            (EconomyType.Mining, GoodCategory.Luxury) => 1.2f,

            (EconomyType.TradeHub, _) => 1.0f,
            _ => 1.0f
        };
    }

    private TerrainType GetTerrainTypeForPosition(int2 position)
    {
        var hash = math.hash(position);
        var value = hash % 6;

        return value switch
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

    private float GetMovementCost(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Road => 0.8f,
            TerrainType.Plains => 1.0f,
            TerrainType.Forest => 1.5f,
            TerrainType.Mountains => 2.0f,
            TerrainType.Desert => 1.3f,
            TerrainType.River => 1.8f,
            _ => 1.0f
        };
    }

    private float GetWearMultiplier(TerrainType terrain)
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

    private float GetDangerLevel(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Forest => 0.7f,
            TerrainType.Mountains => 0.8f,
            TerrainType.Desert => 0.6f,
            TerrainType.River => 0.5f,
            TerrainType.Road => 0.3f,
            TerrainType.Plains => 0.4f,
            _ => 0.5f
        };
    }

    private float GetFoodAvailability(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Plains => 0.8f,
            TerrainType.Forest => 0.6f,
            TerrainType.River => 0.7f,
            TerrainType.Road => 0.3f,
            TerrainType.Mountains => 0.2f,
            TerrainType.Desert => 0.1f,
            _ => 0.5f
        };
    }
}