using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct TimeSystem : ISystem
{
    private float _gameTime;
    private float _salaryTimer;

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        _gameTime += deltaTime;
        _salaryTimer += deltaTime;

        // Выплата зарплаты каждые 60 секунд
        if (_salaryTimer >= 60f)
        {
            PaySalaries(ref state);
            _salaryTimer = 0f;
        }

        // Автосохранение каждые 120 секунд
        if (_gameTime >= 120f)
        {
            _gameTime = 0f;
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
            resources.Morale += 0.05f;
        }
        else
        {
            // Штраф за неуплату
            resources.Morale -= 0.1f;
        }

        resources.Morale = math.clamp(resources.Morale, 0.1f, 1.0f);
        SystemAPI.SetComponent(playerEntity, resources);
    }

    private int CalculateTotalSalary(int guards)
    {
        // Базовая зарплата: 2 золота за охранника
        return guards * 2;
    }
}