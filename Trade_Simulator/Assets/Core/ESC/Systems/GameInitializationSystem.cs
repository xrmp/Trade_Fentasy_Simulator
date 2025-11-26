using Unity.Entities;
using Unity.Collections;
using UnityEngine;


[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct GameInitializationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        // Создаем тестовые товары
        CreateTestGoods(ref state);

        // Создаем стартовую повозку
        CreateStarterWagon(ref state);

        Debug.Log("✅ Игровые данные инициализированы в ECS");
    }

    private void CreateTestGoods(ref SystemState state)
    {
        var goods = new (string, int, int, GoodCategory)[]
        {
            ("Зерно", 1, 10, GoodCategory.RawMaterials),
            ("Древесина", 2, 15, GoodCategory.RawMaterials),
            ("Железная Руда", 5, 20, GoodCategory.RawMaterials),
            ("Ткань", 3, 25, GoodCategory.Crafts),
            ("Кожа", 4, 35, GoodCategory.Crafts),
            ("Вино", 2, 50, GoodCategory.Luxury),
            ("Украшения", 1, 100, GoodCategory.Luxury)
        };

        foreach (var (name, weight, value, category) in goods)
        {
            var goodEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(goodEntity, new GoodData
            {
                Name = name,
                WeightPerUnit = weight,
                BaseValue = value,
                Category = category,
                ProfitPerKm = value * 0.01f
            });
        }
    }

    private void CreateStarterWagon(ref SystemState state)
    {
        var playerQuery = state.EntityManager.CreateEntityQuery(typeof(PlayerTag));
        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var wagonEntity = state.EntityManager.CreateEntity();

        state.EntityManager.AddComponentData(wagonEntity, new Wagon
        {
            Owner = playerEntity,
            Health = 100,
            MaxHealth = 100,
            LoadCapacity = 500,
            CurrentLoad = 0,
            SpeedModifier = 1.0f,
            WearRate = 0.1f,
            WagonType = WagonType.BasicCart,
            IsBroken = false
        });

        // Обновляем грузоподъемность игрока
        var convoy = state.EntityManager.GetComponentData<PlayerConvoy>(playerEntity);
        convoy.TotalCapacity += 500;
        state.EntityManager.SetComponentData(playerEntity, convoy);
    }
}