using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Random = Unity.Mathematics.Random;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct WearAndEventSystem : ISystem
{
    private Random _random;
    private float _eventTimer;

    public void OnCreate(ref SystemState state)
    {
        _random = new Random(12345);
        _eventTimer = 0f;
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        _eventTimer += deltaTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Износ повозок
        ProcessWagonWear(deltaTime, ref state, ref ecb);

        // Случайные события каждые 10 секунд
        if (_eventTimer >= 10f)
        {
            CheckForRandomEvents(ref state, ref ecb);
            _eventTimer = 0f;
        }

        // Обработка активных событий
        ProcessActiveEvents(deltaTime, ref state, ref ecb);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ProcessWagonWear(float deltaTime, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        foreach (var (wagon, position, entity) in
                 SystemAPI.Query<RefRW<Wagon>, RefRO<MapPosition>>().WithEntityAccess())
        {
            if (wagon.ValueRO.IsBroken) continue;

            // Базовый износ + износ от местности
            var terrainModifier = GetTerrainWearMultiplier(position.ValueRO.CurrentTerrainType);
            var wearAmount = wagon.ValueRO.WearRate * terrainModifier * deltaTime;

            // Дополнительный износ от перегрузки
            if (wagon.ValueRO.CurrentLoad > wagon.ValueRO.LoadCapacity)
            {
                var overload = (float)wagon.ValueRO.CurrentLoad / wagon.ValueRO.LoadCapacity;
                wearAmount *= overload;
            }

            wagon.ValueRW.Health = (int)math.max(0, wagon.ValueRO.Health - wearAmount);

            // Проверка поломки
            if (wagon.ValueRO.Health <= 0 && !wagon.ValueRO.IsBroken)
            {
                wagon.ValueRW.IsBroken = true;
                CreateEvent(EventType.WagonBreakdown, 0.7f, "Повозка сломалась!", ref ecb);
                Debug.Log("🔧 Повозка сломалась!");
            }
        }
    }

    private void CheckForRandomEvents(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var eventChance = 0.3f; // 30% шанс события

        if (_random.NextFloat() < eventChance)
        {
            var eventType = GetRandomEventType();
            var severity = _random.NextFloat(0.3f, 1.0f);
            var description = GetEventDescription(eventType);

            CreateEvent(eventType, severity, description, ref ecb);
            ApplyImmediateEventEffects(eventType, severity, ref state, ref ecb);

            Debug.Log($"🎲 Случайное событие: {description}");
        }
    }

    private void ProcessActiveEvents(float deltaTime, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        foreach (var (gameEvent, entity) in
                 SystemAPI.Query<RefRW<GameEvent>>().WithEntityAccess())
        {
            gameEvent.ValueRW.Duration -= deltaTime;

            if (gameEvent.ValueRO.Duration <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }

    private EventType GetRandomEventType()
    {
        var value = _random.NextFloat();
        if (value < 0.3f) return EventType.BanditAttack;
        if (value < 0.5f) return EventType.WeatherStorm;
        if (value < 0.7f) return EventType.WagonBreakdown;
        if (value < 0.9f) return EventType.RoadBlock;
        return EventType.TradeOpportunity;
    }

    private void CreateEvent(EventType type, float severity, string description, ref EntityCommandBuffer ecb)
    {
        var eventEntity = ecb.CreateEntity();
        ecb.AddComponent(eventEntity, new GameEvent
        {
            Type = type,
            Severity = severity,
            Duration = 30f, // 30 секунд
            Description = description,
            Processed = false
        });
    }

    private void ApplyImmediateEventEffects(EventType eventType, float severity,
                                          ref SystemState state, ref EntityCommandBuffer ecb)
    {
        switch (eventType)
        {
            case EventType.WeatherStorm:
                ApplyWeatherEffects(severity, ref state);
                break;
            case EventType.TradeOpportunity:
                ApplyTradeOpportunity(severity, ref state);
                break;
        }
    }

    private void ApplyWeatherEffects(float severity, ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, PlayerConvoy>().Build();
        if (!playerQuery.IsEmpty)
        {
            var playerEntity = playerQuery.GetSingletonEntity();
            var convoy = SystemAPI.GetComponent<PlayerConvoy>(playerEntity);

            convoy.CurrentSpeedModifier = math.max(0.3f, 1.0f - severity * 0.5f);
            SystemAPI.SetComponent(playerEntity, convoy);
        }
    }

    private void ApplyTradeOpportunity(float severity, ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (!playerQuery.IsEmpty)
        {
            var playerEntity = playerQuery.GetSingletonEntity();
            var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

            var bonusGold = (int)(100 * severity);
            resources.Gold += bonusGold;
            SystemAPI.SetComponent(playerEntity, resources);
        }
    }

    private float GetTerrainWearMultiplier(TerrainType terrain)
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

    private string GetEventDescription(EventType eventType)
    {
        return eventType switch
        {
            EventType.WagonBreakdown => "Одна из повозок сломалась",
            EventType.BanditAttack => "Бандиты напали на караван!",
            EventType.WeatherStorm => "Начался сильный шторм",
            EventType.TradeOpportunity => "Встретился купец с выгодным предложением",
            EventType.RoadBlock => "Дорога заблокирована",
            _ => "Неожиданное событие"
        };
    }
}