using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct CombatSystem : ISystem
{
    private Unity.Mathematics.Random _random;

    public void OnCreate(ref SystemState state)
    {
        _random = new Unity.Mathematics.Random(12345);
        Debug.Log("🔄 CombatSystem создана");
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка боевых столкновений
        foreach (var (combat, entity) in
                 SystemAPI.Query<RefRW<CombatEncounter>>().WithEntityAccess())
        {
            if (!combat.ValueRO.Resolved)
            {
                ResolveCombat(combat.ValueRO, ref state, ref ecb);
                combat.ValueRW.Resolved = true;
            }
            ecb.DestroyEntity(entity);
        }

        // Обработка задержек от блокировок дорог
        foreach (var (roadBlock, entity) in
                 SystemAPI.Query<RefRW<RoadBlockDelay>>().WithEntityAccess())
        {
            roadBlock.ValueRW.Duration -= SystemAPI.Time.DeltaTime;

            if (roadBlock.ValueRO.Duration <= 0f)
            {
                // Восстанавливаем скорость движения
                if (state.EntityManager.Exists(roadBlock.ValueRO.PlayerEntity) &&
                    state.EntityManager.HasComponent<PlayerConvoy>(roadBlock.ValueRO.PlayerEntity))
                {
                    var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(roadBlock.ValueRO.PlayerEntity);
                    convoy.CurrentSpeedModifier = roadBlock.ValueRO.OriginalSpeed;
                    state.EntityManager.SetComponentData(roadBlock.ValueRO.PlayerEntity, convoy);
                }
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ResolveCombat(CombatEncounter combat, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        if (!state.EntityManager.Exists(combat.PlayerEntity) ||
            !state.EntityManager.HasComponent<ConvoyResources>(combat.PlayerEntity))
        {
            return;
        }

        var resources = state.EntityManager.GetComponentData<ConvoyResources>(combat.PlayerEntity);
        var playerPower = CalculatePlayerPower(resources);
        var banditPower = combat.BanditPower;

        // Фактор неожиданности
        var surpriseMultiplier = 1.0f + combat.SurpriseFactor;
        banditPower = (int)(banditPower * surpriseMultiplier);

        // Случайный фактор
        var randomFactor = _random.NextFloat(0.8f, 1.2f);
        playerPower = (int)(playerPower * randomFactor);

        // Расчет результата
        var powerRatio = (float)playerPower / math.max(banditPower, 1);
        var outcome = CalculateCombatOutcome(powerRatio);

        // Применяем результаты боя
        ApplyCombatResults(outcome, playerPower, banditPower, ref resources, combat.PlayerEntity, ref state);

        // Создаем сущность с результатом для UI
        var resultEntity = ecb.CreateEntity();
        ecb.AddComponent(resultEntity, new CombatResult
        {
            Victory = outcome != CombatOutcome.Defeat && outcome != CombatOutcome.Rout,
            PlayerLosses = CalculatePlayerLosses(outcome, resources.Guards),
            EnemyLosses = CalculateEnemyLosses(outcome, combat.BanditCount),
            GoldLost = CalculateGoldLosses(outcome, resources.Gold),
            FoodLost = CalculateFoodLosses(outcome, resources.Food),
            MoraleChange = CalculateMoraleChange(outcome)
        });

        Debug.Log($"⚔️ Бой завершен: {GetOutcomeDescription(outcome)}");
    }

    private int CalculatePlayerPower(ConvoyResources resources)
    {
        // Базовая сила охраны + бонус от морали
        return resources.Guards * 10 + (int)(resources.Morale * 20);
    }

    private CombatOutcome CalculateCombatOutcome(float powerRatio)
    {
        if (powerRatio >= 2.0f) return CombatOutcome.DecisiveVictory;
        if (powerRatio >= 1.5f) return CombatOutcome.Victory;
        if (powerRatio >= 1.0f) return CombatOutcome.PyrrhicVictory;
        if (powerRatio >= 0.7f) return CombatOutcome.Stalemate;
        if (powerRatio >= 0.4f) return CombatOutcome.Defeat;
        return CombatOutcome.Rout;
    }

    private void ApplyCombatResults(CombatOutcome outcome, int playerPower, int banditPower,
                                  ref ConvoyResources resources, Entity playerEntity, ref SystemState state)
    {
        switch (outcome)
        {
            case CombatOutcome.DecisiveVictory:
                resources.Gold += banditPower / 2; // Трофеи
                resources.Morale += 0.2f;
                Debug.Log("🎉 Решительная победа! Получены трофеи");
                break;

            case CombatOutcome.Victory:
                resources.Gold += banditPower / 4;
                resources.Morale += 0.1f;
                resources.Guards = math.max(1, resources.Guards - 1);
                Debug.Log("✅ Победа! Небольшие потери, получены трофеи");
                break;

            case CombatOutcome.PyrrhicVictory:
                resources.Guards = math.max(1, resources.Guards - 2);
                resources.Morale -= 0.1f;
                Debug.Log("⚠️ Пиррова победа! Значительные потери");
                break;

            case CombatOutcome.Stalemate:
                resources.Guards = math.max(1, resources.Guards - 3);
                resources.Morale -= 0.2f;
                Debug.Log("🤝 Ничья! Обе стороны понесли потери");
                break;

            case CombatOutcome.Defeat:
                resources.Guards = math.max(1, resources.Guards - 5);
                resources.Gold = math.max(0, resources.Gold - banditPower);
                resources.Morale -= 0.3f;
                Debug.Log("💀 Поражение! Потеряны люди и золото");
                break;

            case CombatOutcome.Rout:
                resources.Guards = math.max(1, resources.Guards - 8);
                resources.Gold = math.max(0, resources.Gold - banditPower * 2);
                resources.Food = math.max(0, resources.Food - banditPower);
                resources.Morale -= 0.5f;
                Debug.Log("🏃‍♂️ Разгром! Катастрофические потери");
                break;
        }

        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);
        state.EntityManager.SetComponentData(playerEntity, resources);
    }

    private int CalculatePlayerLosses(CombatOutcome outcome, int currentGuards)
    {
        return outcome switch
        {
            CombatOutcome.DecisiveVictory => 0,
            CombatOutcome.Victory => 1,
            CombatOutcome.PyrrhicVictory => 2,
            CombatOutcome.Stalemate => 3,
            CombatOutcome.Defeat => 5,
            CombatOutcome.Rout => 8,
            _ => 0
        };
    }

    private int CalculateEnemyLosses(CombatOutcome outcome, int banditCount)
    {
        return outcome switch
        {
            CombatOutcome.DecisiveVictory => banditCount,
            CombatOutcome.Victory => banditCount - 1,
            CombatOutcome.PyrrhicVictory => banditCount - 2,
            CombatOutcome.Stalemate => banditCount / 2,
            CombatOutcome.Defeat => banditCount / 4,
            CombatOutcome.Rout => 0,
            _ => 0
        };
    }

    private int CalculateGoldLosses(CombatOutcome outcome, int currentGold)
    {
        return outcome switch
        {
            CombatOutcome.Defeat => currentGold / 4,
            CombatOutcome.Rout => currentGold / 2,
            _ => 0
        };
    }

    private int CalculateFoodLosses(CombatOutcome outcome, int currentFood)
    {
        return outcome switch
        {
            CombatOutcome.Rout => currentFood / 4,
            _ => 0
        };
    }

    private float CalculateMoraleChange(CombatOutcome outcome)
    {
        return outcome switch
        {
            CombatOutcome.DecisiveVictory => 0.2f,
            CombatOutcome.Victory => 0.1f,
            CombatOutcome.PyrrhicVictory => -0.1f,
            CombatOutcome.Stalemate => -0.2f,
            CombatOutcome.Defeat => -0.3f,
            CombatOutcome.Rout => -0.5f,
            _ => 0f
        };
    }

    private string GetOutcomeDescription(CombatOutcome outcome)
    {
        return outcome switch
        {
            CombatOutcome.DecisiveVictory => "Решительная победа",
            CombatOutcome.Victory => "Победа",
            CombatOutcome.PyrrhicVictory => "Пиррова победа",
            CombatOutcome.Stalemate => "Ничья",
            CombatOutcome.Defeat => "Поражение",
            CombatOutcome.Rout => "Разгром",
            _ => "Неизвестный результат"
        };
    }
}