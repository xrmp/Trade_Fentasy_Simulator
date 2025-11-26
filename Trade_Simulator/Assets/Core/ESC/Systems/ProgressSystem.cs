using Unity.Entities;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ProgressSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Отслеживание прогресса игрока
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources, PlayerConvoy>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();

        // Проверка достижений и прогресса
        CheckForLevelUp(playerEntity, ref state);
        CheckForAchievements(playerEntity, ref state);
    }

    private void CheckForLevelUp(Entity playerEntity, ref SystemState state)
    {
        if (!state.EntityManager.HasComponent<PlayerProgress>(playerEntity))
        {
            // Инициализация прогресса
            state.EntityManager.AddComponentData(playerEntity, new PlayerProgress
            {
                Level = 1,
                Experience = 0,
                TotalDistanceTraveled = 0,
                TotalGoldEarned = 0,
                TotalTradesCompleted = 0
            });
            return;
        }

        var progress = state.EntityManager.GetComponentData<PlayerProgress>(playerEntity);
        var expForNextLevel = GetExpForLevel(progress.Level);

        if (progress.Experience >= expForNextLevel)
        {
            progress.Level++;
            progress.Experience = 0;
            state.EntityManager.SetComponentData(playerEntity, progress);

            Debug.Log($"🎉 Уровень повышен! Теперь уровень {progress.Level}");

            // Награда за уровень
            ApplyLevelUpRewards(progress.Level, playerEntity, ref state);
        }
    }

    private void CheckForAchievements(Entity playerEntity, ref SystemState state)
    {
        var progress = state.EntityManager.GetComponentData<PlayerProgress>(playerEntity);

        // Проверка различных достижений
        if (progress.TotalGoldEarned >= 1000 && !progress.Achievement_FirstThousand)
        {
            progress.Achievement_FirstThousand = true;
            Debug.Log("🏆 Достижение: Первая тысяча золота!");
        }

        if (progress.TotalDistanceTraveled >= 500 && !progress.Achievement_Explorer)
        {
            progress.Achievement_Explorer = true;
            Debug.Log("🏆 Достижение: Исследователь!");
        }

        state.EntityManager.SetComponentData(playerEntity, progress);
    }

    private void ApplyLevelUpRewards(int level, Entity playerEntity, ref SystemState state)
    {
        var resources = state.EntityManager.GetComponentData<ConvoyResources>(playerEntity);
        var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(playerEntity);

        // Награды за уровень
        resources.Gold += level * 100;
        resources.Morale += 0.1f;
        convoy.BaseSpeed += 0.5f;

        state.EntityManager.SetComponentData(playerEntity, resources);
        state.EntityManager.SetComponentData(playerEntity, convoy);

        Debug.Log($"🎁 Награда за уровень: {level * 100} золота, +0.5 к скорости");
    }

    private int GetExpForLevel(int level)
    {
        return level * 100; // 100 опыта за уровень
    }
}

// Система для отслеживания статистики
public partial struct StatisticsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, PlayerProgress>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var progress = SystemAPI.GetComponent<PlayerProgress>(playerEntity);

        // Обновление пройденной дистанции
        var travelState = SystemAPI.GetComponent<TravelState>(playerEntity);
        if (travelState.IsTraveling)
        {
            progress.TotalDistanceTraveled += SystemAPI.Time.DeltaTime * 5f; // Примерная скорость
        }

        SystemAPI.SetComponent(playerEntity, progress);
    }
}

public struct PlayerProgress : IComponentData
{
    public int Level;
    public int Experience;
    public float TotalDistanceTraveled;
    public int TotalGoldEarned;
    public int TotalTradesCompleted;

    // Достижения
    public bool Achievement_FirstThousand;
    public bool Achievement_Explorer;
    public bool Achievement_MasterTrader;
    public bool Achievement_CaravanKing;
}