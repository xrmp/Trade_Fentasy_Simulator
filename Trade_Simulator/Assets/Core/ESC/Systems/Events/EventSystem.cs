using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EventSystem : ISystem
{
    private Unity.Mathematics.Random _random;
    private float _eventCheckTimer;
    private float _travelEventTimer;

    public void OnCreate(ref SystemState state)
    {
        _random = new Unity.Mathematics.Random(12345);
        _eventCheckTimer = 0f;
        _travelEventTimer = 0f;
        Debug.Log("🔄 EventSystem создана");
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        _eventCheckTimer += deltaTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Проверка случайных событий каждые 15 секунд
        if (_eventCheckTimer >= 15f)
        {
            CheckForRandomEvents(ref state, ref ecb);
            _eventCheckTimer = 0f;
        }

        // Проверка событий в пути (чаще)
        CheckForTravelEvents(ref state, ref ecb, deltaTime);

        // Обработка активных событий
        ProcessActiveEvents(ref state, ref ecb, deltaTime);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void CheckForRandomEvents(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var baseEventChance = 0.25f; // 25% шанс события

        // Увеличиваем шанс событий в зависимости от пройденной дистанции
        if (state.EntityManager.HasComponent<PlayerProgress>(playerEntity))
        {
            var progress = state.EntityManager.GetComponentData<PlayerProgress>(playerEntity);
            var distanceBonus = math.min(progress.TotalDistanceTraveled / 1000f, 0.3f);
            baseEventChance += distanceBonus;
        }

        if (_random.NextFloat() < baseEventChance)
        {
            var eventType = GetRandomEventType();
            var severity = _random.NextFloat(0.3f, 1.0f);
            var description = GetEventDescription(eventType);

            CreateEvent(eventType, severity, description, playerEntity, ref ecb);
            ApplyImmediateEventEffects(eventType, severity, playerEntity, ref state, ref ecb);

            Debug.Log($"🎲 Случайное событие: {description}");
        }
    }

    private void CheckForTravelEvents(ref SystemState state, ref EntityCommandBuffer ecb, float deltaTime)
    {
        _travelEventTimer += deltaTime;

        // Проверяем события в пути каждые 5 секунд
        if (_travelEventTimer < 5f) return;
        _travelEventTimer = 0f;

        foreach (var (travelState, position, resources, entity) in
                 SystemAPI.Query<RefRO<TravelState>, RefRO<MapPosition>,
                                RefRO<ConvoyResources>>().WithAll<PlayerTag>().WithEntityAccess())
        {
            if (!travelState.ValueRO.IsTraveling) continue;

            var travelEventChance = GetTravelEventChance(position.ValueRO.CurrentTerrain);

            if (_random.NextFloat() < travelEventChance)
            {
                var eventType = GetTravelEventType(position.ValueRO.CurrentTerrain);
                var severity = _random.NextFloat(0.4f, 0.8f);
                var description = GetTravelEventDescription(eventType, position.ValueRO.CurrentTerrain);

                CreateEvent(eventType, severity, description, entity, ref ecb);
                ApplyTravelEventEffects(eventType, severity, entity, ref state, ref ecb);

                Debug.Log($"🛣️ Событие в пути: {description}");
            }
        }
    }

    private void ProcessActiveEvents(ref SystemState state, ref EntityCommandBuffer ecb, float deltaTime)
    {
        foreach (var (gameEvent, entity) in
                 SystemAPI.Query<RefRW<GameEvent>>().WithEntityAccess())
        {
            gameEvent.ValueRW.Duration -= deltaTime;

            // Применяем постоянные эффекты события
            ApplyOngoingEventEffects(gameEvent.ValueRO, ref state, deltaTime);

            if (gameEvent.ValueRO.Duration <= 0)
            {
                // Завершаем событие
                OnEventEnd(gameEvent.ValueRO, ref state, ref ecb);
                ecb.DestroyEntity(entity);
            }
        }
    }

    private EventType GetRandomEventType()
    {
        var value = _random.NextFloat();

        if (value < 0.3f) return EventType.TradeOpportunity;
        if (value < 0.5f) return EventType.LuckyFind;
        if (value < 0.6f) return EventType.GoodWeather;
        if (value < 0.7f) return EventType.NewRecruits;
        if (value < 0.8f) return EventType.CityEvent;
        return EventType.TravelEncounter;
    }

    private EventType GetTravelEventType(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Forest => _random.NextFloat() < 0.6f ? EventType.BanditAttack : EventType.WagonBreakdown,
            TerrainType.Mountains => _random.NextFloat() < 0.7f ? EventType.WeatherStorm : EventType.WagonBreakdown,
            TerrainType.Desert => EventType.WeatherStorm,
            TerrainType.River => EventType.WagonBreakdown,
            _ => _random.NextFloat() < 0.5f ? EventType.BanditAttack : EventType.RoadBlock
        };
    }

    private float GetTravelEventChance(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Forest => 0.4f,
            TerrainType.Mountains => 0.5f,
            TerrainType.Desert => 0.3f,
            TerrainType.River => 0.35f,
            TerrainType.Road => 0.1f,
            TerrainType.Plains => 0.2f,
            _ => 0.2f
        };
    }

    private void CreateEvent(EventType type, float severity, string description, Entity targetEntity, ref EntityCommandBuffer ecb)
    {
        var eventEntity = ecb.CreateEntity();
        ecb.AddComponent(eventEntity, new GameEvent
        {
            Type = type,
            Severity = severity,
            Duration = GetEventDuration(type),
            Description = description,
            Processed = false,
            TargetEntity = targetEntity
        });
    }

    private void ApplyImmediateEventEffects(EventType eventType, float severity, Entity playerEntity, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        if (!state.EntityManager.HasComponent<ConvoyResources>(playerEntity)) return;

        var resources = state.EntityManager.GetComponentData<ConvoyResources>(playerEntity);

        switch (eventType)
        {
            case EventType.TradeOpportunity:
                var bonusGold = (int)(100 * severity);
                resources.Gold += bonusGold;
                Debug.Log($"💰 Торговая возможность! +{bonusGold} золота");
                break;

            case EventType.LuckyFind:
                var foundGold = (int)(50 * severity);
                resources.Gold += foundGold;
                Debug.Log($"🍀 Удачная находка! +{foundGold} золота");
                break;

            case EventType.GoodWeather:
                resources.Morale += 0.1f * severity;
                Debug.Log($"☀️ Благоприятная погода! +Мораль");
                break;

            case EventType.NewRecruits:
                var newGuards = (int)(2 * severity);
                resources.Guards += newGuards;
                Debug.Log($"🛡️ Новые рекруты! +{newGuards} охраны");
                break;
        }

        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);
        state.EntityManager.SetComponentData(playerEntity, resources);
    }

    private void ApplyTravelEventEffects(EventType eventType, float severity, Entity playerEntity, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        if (!state.EntityManager.HasComponent<ConvoyResources>(playerEntity) ||
            !state.EntityManager.HasComponent<PlayerConvoy>(playerEntity)) return;

        var resources = state.EntityManager.GetComponentData<ConvoyResources>(playerEntity);
        var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(playerEntity);

        switch (eventType)
        {
            case EventType.BanditAttack:
                // Создаем сущность боя
                var combatEntity = ecb.CreateEntity();
                ecb.AddComponent(combatEntity, new CombatEncounter
                {
                    PlayerEntity = playerEntity,
                    BanditCount = (int)(5 * severity),
                    BanditPower = (int)(30 * severity),
                    SurpriseFactor = _random.NextFloat(0.1f, 0.3f)
                });
                break;

            case EventType.WeatherStorm:
                convoy.CurrentSpeedModifier *= (1f - severity * 0.5f);
                resources.Morale -= 0.1f * severity;
                Debug.Log($"⛈️ Шторм! Скорость снижена");
                break;

            case EventType.WagonBreakdown:
                // Находим случайную повозку для поломки
                var wagonQuery = state.EntityManager.CreateEntityQuery(typeof(Wagon));
                var wagons = wagonQuery.ToEntityArray(Allocator.Temp);
                if (wagons.Length > 0)
                {
                    var randomWagon = wagons[_random.NextInt(0, wagons.Length)];
                    var wagon = state.EntityManager.GetComponentData<Wagon>(randomWagon);
                    wagon.Health = (int)(wagon.Health * (1f - severity));
                    if (wagon.Health <= 0) wagon.IsBroken = true;
                    state.EntityManager.SetComponentData(randomWagon, wagon);
                    Debug.Log($"🔧 Поломка повозки! Здоровье: {wagon.Health}");
                }
                wagons.Dispose();
                break;

            case EventType.RoadBlock:
                convoy.CurrentSpeedModifier = 0f; // Полная остановка
                // Автоматическое устранение через время
                var delayEntity = ecb.CreateEntity();
                ecb.AddComponent(delayEntity, new RoadBlockDelay
                {
                    Duration = 10f,
                    PlayerEntity = playerEntity,
                    OriginalSpeed = convoy.CurrentSpeedModifier
                });
                Debug.Log($"🚧 Дорога заблокирована! Движение остановлено");
                break;
        }

        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);
        convoy.CurrentSpeedModifier = math.max(0.1f, convoy.CurrentSpeedModifier);

        state.EntityManager.SetComponentData(playerEntity, resources);
        state.EntityManager.SetComponentData(playerEntity, convoy);
    }

    private void ApplyOngoingEventEffects(GameEvent gameEvent, ref SystemState state, float deltaTime)
    {
        if (!state.EntityManager.Exists(gameEvent.TargetEntity) ||
            !state.EntityManager.HasComponent<ConvoyResources>(gameEvent.TargetEntity)) return;

        var resources = state.EntityManager.GetComponentData<ConvoyResources>(gameEvent.TargetEntity);

        switch (gameEvent.Type)
        {
            case EventType.WeatherStorm:
                // Постоянный штраф к морали во время шторма
                resources.Morale -= 0.02f * gameEvent.Severity * deltaTime;
                break;

            case EventType.Sickness:
                // Постепенная потеря здоровья/морали
                resources.Morale -= 0.05f * gameEvent.Severity * deltaTime;
                break;
        }

        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);
        state.EntityManager.SetComponentData(gameEvent.TargetEntity, resources);
    }

    private void OnEventEnd(GameEvent gameEvent, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        if (!state.EntityManager.Exists(gameEvent.TargetEntity) ||
            !state.EntityManager.HasComponent<PlayerConvoy>(gameEvent.TargetEntity)) return;

        var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(gameEvent.TargetEntity);

        switch (gameEvent.Type)
        {
            case EventType.WeatherStorm:
                // Восстанавливаем скорость после шторма
                convoy.CurrentSpeedModifier = 1.0f;
                state.EntityManager.SetComponentData(gameEvent.TargetEntity, convoy);
                Debug.Log($"🌈 Шторм закончился! Скорость восстановлена");
                break;

            case EventType.GoodWeather:
                Debug.Log($"🌤️ Благоприятная погода закончилась");
                break;
        }
    }

    private float GetEventDuration(EventType eventType)
    {
        return eventType switch
        {
            EventType.WeatherStorm => 30f,
            EventType.GoodWeather => 45f,
            EventType.Sickness => 60f,
            EventType.RoadBlock => 15f,
            _ => 20f
        };
    }

    private string GetEventDescription(EventType eventType)
    {
        return eventType switch
        {
            EventType.WagonBreakdown => "Одна из повозок сломалась и требует ремонта",
            EventType.BanditAttack => "Бандиты напали на ваш караван!",
            EventType.WeatherStorm => "Начался сильный шторм, движение замедлилось",
            EventType.TradeOpportunity => "Встретился купец с выгодным предложением",
            EventType.RoadBlock => "Дорога заблокирована упавшим деревом",
            EventType.Sickness => "В отряде распространилась болезнь",
            EventType.LuckyFind => "Вы нашли брошенный сундук с сокровищами",
            EventType.GoodWeather => "Установилась прекрасная погода для путешествия",
            EventType.NewRecruits => "К вашему отряду присоединились новые бойцы",
            EventType.CityEvent => "В городе проходит праздник - цены снижены",
            EventType.TravelEncounter => "Вы встретили странствующего торговца",
            _ => "Произошло неожиданное событие"
        };
    }

    private string GetTravelEventDescription(EventType eventType, TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Forest => eventType == EventType.BanditAttack ?
                "В лесу на вас напали разбойники!" :
                "Лесная тропа повредила повозку",

            TerrainType.Mountains => eventType == EventType.WeatherStorm ?
                "Горный шторм замедлил ваше продвижение" :
                "Крутой подъем повредил повозки",

            TerrainType.Desert => "Песчаная буря обрушилась на ваш караван",
            TerrainType.River => "Переправа через реку повредила повозку",
            _ => GetEventDescription(eventType)
        };
    }
}

// Компоненты для системы событий
public struct CombatEncounter : IComponentData
{
    public Entity PlayerEntity;
    public int BanditCount;
    public int BanditPower;
    public float SurpriseFactor;
    public bool Resolved;
}

public struct RoadBlockDelay : IComponentData
{
    public float Duration;
    public Entity PlayerEntity;
    public float OriginalSpeed;
}