using Core.Managers;
using Unity.Entities;
using UnityEngine;

public class SceneValidator : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔍 Проверка сцены...");

        // Проверяем наличие обязательных объектов
        CheckForGameDataAuthoring();
        CheckForECSBootstrap();

        Invoke("CheckECSAfterDelay", 2f); // Проверяем ECS через 2 секунды
    }

    void CheckForGameDataAuthoring()
    {
        var gameData = FindFirstObjectByType<GameDataAuthoring>();
        if (gameData != null)
        {
            Debug.Log($"✅ GameDataAuthoring найден: {gameData.startGold} золота");
        }
        else
        {
            Debug.LogError("❌ GameDataAuthoring НЕ НАЙДЕН на сцене!");
        }
    }

    void CheckForECSBootstrap()
    {
        var bootstrap = FindFirstObjectByType<ECSBootstrap>();
        if (bootstrap != null)
        {
            Debug.Log("✅ ECSBootstrap найден");
        }
        else
        {
            Debug.LogError("❌ ECSBootstrap НЕ НАЙДЕН на сцене!");
        }
    }

    void CheckECSAfterDelay()
    {
        Debug.Log("🔍 Проверяем ECS через 2 секунды...");

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            Debug.LogError("❌ ECS World не создан!");
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Проверяем GameConfig
        var configQuery = em.CreateEntityQuery(typeof(GameConfig));
        if (!configQuery.IsEmpty)
        {
            var gameConfig = configQuery.GetSingleton<GameConfig>();
            Debug.Log($"✅ GameConfig найден в ECS: {gameConfig.StartGold} золота");
        }
        else
        {
            Debug.LogError("❌ GameConfig НЕ найден в ECS!");
        }

        // Проверяем игрока
        var playerQuery = em.CreateEntityQuery(typeof(PlayerTag));
        if (!playerQuery.IsEmpty)
        {
            Debug.Log("✅ Игрок найден в ECS!");
        }
        else
        {
            Debug.LogError("❌ Игрок НЕ найден в ECS!");
        }
    }
}