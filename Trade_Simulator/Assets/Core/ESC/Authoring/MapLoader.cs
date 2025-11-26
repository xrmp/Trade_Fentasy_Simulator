using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

public class MapLoader : MonoBehaviour
{
    [Header("Настройки карты")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float worldScale = 10f;
    public int seed = 12345;

    [Header("Вероятности местности")]
    [Range(0, 1)] public float plainsProbability = 0.4f;
    [Range(0, 1)] public float forestProbability = 0.3f;
    [Range(0, 1)] public float mountainProbability = 0.2f;
    [Range(0, 1)] public float roadProbability = 0.1f;

    class Baker : Baker<MapLoader>
    {
        public override void Bake(MapLoader authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new MapConfig
            {
                Width = authoring.mapWidth,
                Height = authoring.mapHeight,
                WorldScale = authoring.worldScale,
                Seed = authoring.seed
            });

            // Карта будет генерироваться в MapInitializationSystem
            Debug.Log("✅ Конфигурация карты создана");
        }
    }
}

// Система инициализации карты
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct MapInitializationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false; // Одноразовая инициализация

        if (!SystemAPI.TryGetSingleton<MapConfig>(out var mapConfig)) return;

        GenerateMap(ref state, mapConfig);
        CreateCities(ref state, mapConfig);

        Debug.Log($"✅ Карта сгенерирована: {mapConfig.Width}x{mapConfig.Height}");
    }

    private void GenerateMap(ref SystemState state, MapConfig config)
    {
        var random = new Unity.Mathematics.Random((uint)config.Seed);

        for (int x = 0; x < config.Width; x++)
        {
            for (int y = 0; y < config.Height; y++)
            {
                var terrainEntity = state.EntityManager.CreateEntity();
                var terrainType = GenerateTerrainType(x, y, random);

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
    }

    private TerrainType GenerateTerrainType(int x, int y, Unity.Mathematics.Random random)
    {
        var value = random.NextFloat();

        if (value < 0.1f) return TerrainType.Road;
        if (value < 0.4f) return TerrainType.Forest;
        if (value < 0.6f) return TerrainType.Mountains;
        if (value < 0.8f) return TerrainType.Plains;
        return TerrainType.Desert;
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
            TerrainType.Road => 0.3f,
            TerrainType.Plains => 0.2f,
            TerrainType.Forest => 0.6f,
            TerrainType.Mountains => 0.7f,
            TerrainType.Desert => 0.5f,
            TerrainType.River => 0.4f,
            _ => 0.3f
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

    private void CreateCities(ref SystemState state, MapConfig config)
    {
        var cities = new (string, int2, EconomyType)[]
        {
            ("Стартовый Город", new int2(10, 10), EconomyType.Agricultural),
            ("Торговая Столица", new int2(50, 50), EconomyType.TradeHub),
            ("Горная Крепость", new int2(80, 20), EconomyType.Mining),
            ("Портовый Город", new int2(30, 70), EconomyType.Industrial)
        };

        foreach (var (name, pos, economy) in cities)
        {
            var cityEntity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(cityEntity, new City
            {
                Name = name,
                GridPosition = pos,
                WorldPosition = new float3(pos.x * config.WorldScale, 0, pos.y * config.WorldScale),
                Population = 1000,
                EconomyType = economy
            });

            // Создаем данные города для карты
            var cityDataEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(cityDataEntity, new CityData
            {
                CityEntity = cityEntity,
                GridPosition = pos,
                Name = name,
                EconomyType = economy,
                TradeRadius = 20
            });
        }
    }
}