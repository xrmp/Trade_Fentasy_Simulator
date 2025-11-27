using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerCreationSystem : ISystem
{
    private bool _playerCreated;

    public void OnCreate(ref SystemState state)
    {
        _playerCreated = false;
        Debug.Log("🔄 PlayerCreationSystem создана, ожидаем GameConfig...");
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_playerCreated)
        {
            state.Enabled = false;
            return;
        }

        // Ждем GameConfig
        if (!SystemAPI.TryGetSingleton<GameConfig>(out var gameConfig))
        {
            Debug.Log("⏳ PlayerCreation: Ожидаем GameConfig...");
            return;
        }

        Debug.Log($"✅ PlayerCreation: GameConfig получен! {gameConfig.StartGold} золота");

        // Проверяем, не создан ли уже игрок
        var playerQuery = state.EntityManager.CreateEntityQuery(typeof(PlayerTag));
        if (!playerQuery.IsEmpty)
        {
            Debug.Log("✅ Игрок уже существует в ECS");
            _playerCreated = true;
            state.Enabled = false;
            return;
        }

        // Создаем игрока
        CreatePlayerEntity(ref state, gameConfig);

        _playerCreated = true;
        state.Enabled = false;
        Debug.Log("🎮 Игрок создан в ECS!");
    }

    private void CreatePlayerEntity(ref SystemState state, GameConfig gameConfig)
    {
        var playerEntity = state.EntityManager.CreateEntity();

        Debug.Log($"🔥 Создаем игрока с {gameConfig.StartGold} золота");

        // 1. Добавляем тэг игрока ПЕРВЫМ
        state.EntityManager.AddComponent<PlayerTag>(playerEntity);

        // 2. Основные компоненты игрока
        state.EntityManager.AddComponentData(playerEntity, new PlayerConvoy
        {
            CurrentPosition = float3.zero,
            MoveSpeed = gameConfig.BaseMovementSpeed,
            BaseSpeed = gameConfig.BaseMovementSpeed,
            TotalCapacity = 1000,
            UsedCapacity = 0,
            CurrentSpeedModifier = 1.0f
        });

        state.EntityManager.AddComponentData(playerEntity, new ConvoyResources
        {
            Gold = gameConfig.StartGold,
            Food = gameConfig.StartFood,
            Guards = gameConfig.StartGuards,
            FoodConsumptionRate = (int)gameConfig.FoodConsumptionRate,
            Morale = 1.0f
        });

        state.EntityManager.AddComponentData(playerEntity, new MapPosition
        {
            GridPosition = new int2(10, 10),
            WorldPosition = new float3(100, 0, 100),
            CurrentTerrain = TerrainType.Plains
        });

        state.EntityManager.AddComponentData(playerEntity, new TravelState
        {
            IsTraveling = false,
            TravelProgress = 0f,
            TotalTravelTime = 0f,
            DestinationReached = true,
            Destination = float3.zero,
            StartPosition = float3.zero
        });

        // 3. Буфер инвентаря
        state.EntityManager.AddBuffer<InventoryBuffer>(playerEntity);

        // 4. Прогресс игрока
        state.EntityManager.AddComponentData(playerEntity, new PlayerProgress
        {
            Level = 1,
            Experience = 0,
            TotalDistanceTraveled = 0,
            TotalGoldEarned = 0,
            TotalTradesCompleted = 0,
            Achievement_FirstThousand = false,
            Achievement_Explorer = false,
            Achievement_MasterTrader = false,
            Achievement_CaravanKing = false
        });

        // 5. Создаем стартовую повозку
        CreateStarterWagon(playerEntity, ref state);

        Debug.Log($"🎯 Игрок создан! Entity: {playerEntity.Index}");
    }

    private void CreateStarterWagon(Entity playerEntity, ref SystemState state)
    {
        var wagonEntity = state.EntityManager.CreateEntity();

        state.EntityManager.AddComponent<WagonTag>(wagonEntity);
        state.EntityManager.AddComponentData(wagonEntity, new Wagon
        {
            Owner = playerEntity,
            Health = 100,
            MaxHealth = 100,
            LoadCapacity = 500,
            CurrentLoad = 0,
            SpeedModifier = 1.0f,
            WearRate = 0.1f,
            Type = WagonType.BasicCart,
            IsBroken = false
        });

        // Обновляем общую грузоподъемность игрока
        var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(playerEntity);
        convoy.TotalCapacity += 500;
        state.EntityManager.SetComponentData(playerEntity, convoy);

        Debug.Log("🚛 Стартовая повозка создана");
    }
}