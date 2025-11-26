using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EventResolutionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка событий, требующих реакции игрока
        foreach (var (gameEvent, entity) in
                 SystemAPI.Query<RefRO<GameEvent>>().WithEntityAccess())
        {
            if (!gameEvent.ValueRO.Processed)
            {
                ResolveEvent(gameEvent.ValueRO, ref state, ref ecb);

                // Помечаем как обработанное
                var updatedEvent = gameEvent.ValueRO;
                updatedEvent.Processed = true;
                state.EntityManager.SetComponentData(entity, updatedEvent);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ResolveEvent(GameEvent gameEvent, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        switch (gameEvent.Type)
        {
            case EventType.BanditAttack:
                ResolveBanditAttack(gameEvent.Severity, ref state);
                break;
            case EventType.RoadBlock:
                ResolveRoadBlock(gameEvent.Severity, ref state);
                break;
        }
    }

    private void ResolveBanditAttack(float severity, ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

        // Простой расчет боя
        var combatPower = resources.Guards * 10;
        var banditPower = (int)(50 * severity);

        if (combatPower >= banditPower)
        {
            // Победа
            var losses = (int)(resources.Guards * severity * 0.1f);
            resources.Guards = math.max(1, resources.Guards - losses);
            Debug.Log($"⚔️ Отразили нападение! Потеряно {losses} охраны");
        }
        else
        {
            // Поражение
            var losses = (int)(resources.Guards * severity * 0.3f);
            var goldLost = (int)(resources.Gold * severity * 0.2f);

            resources.Guards = math.max(1, resources.Guards - losses);
            resources.Gold = math.max(0, resources.Gold - goldLost);
            resources.Morale -= severity * 0.2f;

            Debug.Log($"💀 Проиграли бой! Потеряно {losses} охраны и {goldLost} золота");
        }

        SystemAPI.SetComponent(playerEntity, resources);
    }

    private void ResolveRoadBlock(float severity, ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, PlayerConvoy>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var convoy = SystemAPI.GetComponent<PlayerConvoy>(playerEntity);

        // Задержка из-за блокировки дороги
        convoy.CurrentSpeedModifier = 0f; // Полная остановка

        // Автоматическое устранение через 5 секунд
        var delayEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(delayEntity, new RoadBlockDelay
        {
            Duration = 5f,
            PlayerEntity = playerEntity,
            OriginalSpeed = convoy.CurrentSpeedModifier
        });

        SystemAPI.SetComponent(playerEntity, convoy);
        Debug.Log("🚧 Дорога заблокирована! Движение остановлено");
    }
}

public struct RoadBlockDelay : IComponentData
{
    public float Duration;
    public Entity PlayerEntity;
    public float OriginalSpeed;
}

// Система для обработки задержек
public partial struct RoadBlockSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (delay, entity) in
                 SystemAPI.Query<RefRW<RoadBlockDelay>>().WithEntityAccess())
        {
            delay.ValueRW.Duration -= deltaTime;

            if (delay.ValueRO.Duration <= 0f)
            {
                // Восстанавливаем скорость движения
                if (state.EntityManager.Exists(delay.ValueRO.PlayerEntity))
                {
                    var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(delay.ValueRO.PlayerEntity);
                    convoy.CurrentSpeedModifier = delay.ValueRO.OriginalSpeed;
                    state.EntityManager.SetComponentData(delay.ValueRO.PlayerEntity, convoy);
                }

                ecb.DestroyEntity(entity);
                Debug.Log("🛣️ Дорога расчищена! Движение восстановлено");
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}