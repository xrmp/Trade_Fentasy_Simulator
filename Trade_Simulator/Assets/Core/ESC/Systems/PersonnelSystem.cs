using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PersonnelSystem : ISystem
{
    private float _salaryTimer;

    public void OnUpdate(ref SystemState state)
    {
        _salaryTimer += SystemAPI.Time.DeltaTime;

        // Выплата зарплаты каждые 30 секунд
        if (_salaryTimer >= 30f)
        {
            PaySalaries(ref state);
            UpdateMorale(ref state);
            _salaryTimer = 0f;
        }
    }

    private void PaySalaries(ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

        var totalSalary = CalculateTotalSalary(resources.Guards);

        if (resources.Gold >= totalSalary)
        {
            resources.Gold -= totalSalary;
            resources.Morale += 0.05f; // Бонус за своевременную выплату
            Debug.Log($"💰 Выплачена зарплата охране: {totalSalary} золота");
        }
        else
        {
            // Штраф за неуплату
            resources.Morale -= 0.1f;
            Debug.Log("⚠️ Не хватает золота для выплаты зарплаты! Мораль падает");
        }

        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);
        SystemAPI.SetComponent(playerEntity, resources);
    }

    private void UpdateMorale(ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

        // Факторы влияющие на мораль
        var foodModifier = resources.Food > 0 ? 0.01f : -0.05f;
        var goldModifier = resources.Gold > 100 ? 0.01f : -0.02f;

        resources.Morale += foodModifier + goldModifier;
        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);

        SystemAPI.SetComponent(playerEntity, resources);
    }

    private int CalculateTotalSalary(int guards)
    {
        // Базовая зарплата: 2 золота за охранника
        return guards * 2;
    }
}

// Система найма/увольнения охраны
public partial struct RecruitmentSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Обработка запросов на найм/увольнение
        foreach (var (recruitment, entity) in
                 SystemAPI.Query<RefRO<RecruitmentAction>>().WithEntityAccess())
        {
            ProcessRecruitment(recruitment.ValueRO, ref state);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ProcessRecruitment(RecruitmentAction action, ref SystemState state)
    {
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

        if (action.Hire)
        {
            HireGuards(action.Count, ref resources);
        }
        else
        {
            FireGuards(action.Count, ref resources);
        }

        SystemAPI.SetComponent(playerEntity, resources);
    }

    private void HireGuards(int count, ref ConvoyResources resources)
    {
        var hireCost = count * 25; // 25 золота за охранника

        if (resources.Gold >= hireCost)
        {
            resources.Gold -= hireCost;
            resources.Guards += count;
            Debug.Log($"🛡️ Нанято {count} охранников за {hireCost} золота");
        }
        else
        {
            Debug.Log("❌ Недостаточно золота для найма охраны");
        }
    }

    private void FireGuards(int count, ref ConvoyResources resources)
    {
        var fireCount = math.min(count, resources.Guards - 1); // Оставляем минимум 1 охранника

        if (fireCount > 0)
        {
            resources.Guards -= fireCount;
            resources.Morale -= 0.05f * fireCount; // Штраф к морали за увольнение
            Debug.Log($"👋 Уволено {fireCount} охранников");
        }
    }
}

public struct RecruitmentAction : IComponentData
{
    public bool Hire; // true = наем, false = увольнение
    public int Count;
}