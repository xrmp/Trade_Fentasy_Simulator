using Unity.Entities;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct TimeSystem : ISystem
{
    private float _gameTime;

    public void OnUpdate(ref SystemState state)
    {
        _gameTime += SystemAPI.Time.DeltaTime;

        // Каждые 5 секунд сохраняем или выполняем периодические действия
        if (_gameTime >= 5f)
        {
            _gameTime = 0f;
            PerformPeriodicActions(ref state);
        }
    }

    private void PerformPeriodicActions(ref SystemState state)
    {
        // Выплата зарплаты охране
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, ConvoyResources>().Build();
        if (!playerQuery.IsEmpty)
        {
            var playerEntity = playerQuery.GetSingletonEntity();
            var resources = SystemAPI.GetComponent<ConvoyResources>(playerEntity);

            var salary = resources.Guards * 2; // 2 золота за охранника
            if (resources.Gold >= salary)
            {
                resources.Gold -= salary;
                SystemAPI.SetComponent(playerEntity, resources);
                Debug.Log($"💰 Выплачена зарплата охране: {salary} золота");
            }
            else
            {
                // Штраф за неуплату
                resources.Morale -= 0.1f;
                SystemAPI.SetComponent(playerEntity, resources);
                Debug.Log("⚠️ Не хватает денег на зарплату! Мораль падает");
            }
        }
    }
}