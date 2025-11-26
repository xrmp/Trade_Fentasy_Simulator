using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Random = Unity.Mathematics.Random;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct CombatSystem : ISystem
{
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
        _random = new Random(12345);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка встреч с бандитами
        foreach (var (encounter, entity) in
                 SystemAPI.Query<RefRW<BanditEncounter>>().WithEntityAccess())
        {
            if (!encounter.ValueRO.Resolved)
            {
                ResolveCombat(encounter.ValueRO, ref state, ref ecb);
                encounter.ValueRW.Resolved = true;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ResolveCombat(BanditEncounter encounter, ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

        // Расчет сил
        var playerPower = CalculatePlayerPower(resources);
        var banditPower = encounter.BanditPower;

        // Фактор неожиданности
        var surpriseMultiplier = 1.0f + encounter.SurpriseFactor;
        banditPower = (int)(banditPower * surpriseMultiplier);

        // Случайный фактор
        var randomFactor = _random.NextFloat(0.8f, 1.2f);
        playerPower = (int)(playerPower * randomFactor);

        // Расчет результата
        var powerRatio = (float)playerPower / math.max(banditPower, 1);
        var result = CalculateCombatResult(powerRatio);

        ApplyCombatResults(result, playerPower, banditPower, ref resources, ref ecb);

        SystemAPI.SetComponent(playerEntity, resources);

        // Создаем entity с результатом боя
        var resultEntity = ecb.CreateEntity();
        ecb.AddComponent(resultEntity, new CombatResult
        {
            Victory = result != CombatOutcome.Defeat && result != CombatOutcome.Rout,
            PlayerLosses = CalculatePlayerLosses(result, resources.Guards),
            BanditLosses = CalculateBanditLosses(result, encounter.BanditCount),
            GoldLost = CalculateGoldLosses(result, resources.Gold),
            FoodLost = CalculateFoodLosses(result, resources.Food),
            MoraleChange = CalculateMoraleChange(result)
        });

        Debug.Log($"⚔️ Бой завершен: {result}");
    }

    private int CalculatePlayerPower(ConvoyResources resources)
    {
        return resources.Guards * 10 + (int)(resources.Morale * 20);
    }

    private CombatOutcome CalculateCombatResult(float powerRatio)
    {
        if (powerRatio >= 2.0f) return CombatOutcome.DecisiveVictory;
        if (powerRatio >= 1.5f) return CombatOutcome.Victory;
        if (powerRatio >= 1.0f) return CombatOutcome.PyrrhicVictory;
        if (powerRatio >= 0.7f) return CombatOutcome.Stalemate;
        if (powerRatio >= 0.4f) return CombatOutcome.Defeat;
        return CombatOutcome.Rout;
    }

    private void ApplyCombatResults(CombatOutcome outcome, int playerPower, int banditPower,
                                  ref ConvoyResources resources, ref EntityCommandBuffer ecb)
    {
        switch (outcome)
        {
            case CombatOutcome.DecisiveVictory:
                resources.Gold += banditPower / 2; // Трофеи
                resources.Morale += 0.2f;
                break;

            case CombatOutcome.Victory:
                resources.Gold += banditPower / 4;
                resources.Morale += 0.1f;
                resources.Guards = math.max(1, resources.Guards - 1);
                break;

            case CombatOutcome.PyrrhicVictory:
                resources.Guards = math.max(1, resources.Guards - 2);
                resources.Morale -= 0.1f;
                break;

            case CombatOutcome.Stalemate:
                resources.Guards = math.max(1, resources.Guards - 3);
                resources.Morale -= 0.2f;
                break;

            case CombatOutcome.Defeat:
                resources.Guards = math.max(1, resources.Guards - 5);
                resources.Gold = math.max(0, resources.Gold - banditPower);
                resources.Morale -= 0.3f;
                break;

            case CombatOutcome.Rout:
                resources.Guards = math.max(1, resources.Guards - 8);
                resources.Gold = math.max(0, resources.Gold - banditPower * 2);
                resources.Food = math.max(0, resources.Food - banditPower);
                resources.Morale -= 0.5f;
                break;
        }

        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);
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

    private int CalculateBanditLosses(CombatOutcome outcome, int banditCount)
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
}

public enum CombatOutcome
{
    DecisiveVictory,
    Victory,
    PyrrhicVictory,
    Stalemate,
    Defeat,
    Rout
}