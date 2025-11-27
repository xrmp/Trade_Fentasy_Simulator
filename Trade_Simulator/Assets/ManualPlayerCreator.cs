using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ManualPlayerCreator : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🎮 ManualPlayerCreator запущен");
        Invoke("CreatePlayerManually", 1f);
    }

    void CreatePlayerManually()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            Debug.LogError("❌ ECS World не существует!");
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        // 1. Проверяем GameConfig
        var configQuery = em.CreateEntityQuery(typeof(GameConfig));
        if (configQuery.IsEmpty)
        {
            Debug.LogError("❌ GameConfig не найден! Создаем вручную...");
            CreateGameConfigManually(em);
        }
        else
        {
            var gameConfig = configQuery.GetSingleton<GameConfig>();
            Debug.Log($"✅ GameConfig найден: {gameConfig.StartGold} золота");
        }

        // 2. Проверяем игрока
        var playerQuery = em.CreateEntityQuery(typeof(PlayerTag));
        if (playerQuery.IsEmpty)
        {
            Debug.Log("❌ Игрок не найден! Создаем вручную...");
            CreatePlayerManually(em);
        }
        else
        {
            Debug.Log("✅ Игрок уже существует");
        }
    }

    void CreateGameConfigManually(EntityManager em)
    {
        var configEntity = em.CreateEntity();
        em.AddComponentData(configEntity, new GameConfig
        {
            StartGold = 1000,
            StartFood = 100,
            StartGuards = 5,
            FoodConsumptionRate = 2f,
            BaseMovementSpeed = 5f
        });
        Debug.Log("✅ GameConfig создан вручную");
    }

    void CreatePlayerManually(EntityManager em)
    {
        var playerEntity = em.CreateEntity();

        // Добавляем компоненты вручную
        em.AddComponent<PlayerTag>(playerEntity);
        em.AddComponentData(playerEntity, new PlayerConvoy
        {
            CurrentPosition = new float3(0, 0, 0),
            MoveSpeed = 5f,
            BaseSpeed = 5f,
            TotalCapacity = 1000,
            UsedCapacity = 0,
            CurrentSpeedModifier = 1f
        });
        em.AddComponentData(playerEntity, new ConvoyResources
        {
            Gold = 1000,
            Food = 100,
            Guards = 5,
            FoodConsumptionRate = 2,
            Morale = 1f
        });

        Debug.Log("✅ Игрок создан вручную!");
    }
}