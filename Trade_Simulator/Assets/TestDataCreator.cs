using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TestCommands : MonoBehaviour
{
    void Update()
    {
        // Тестовые команды для проверки механик
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddGold();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartMovement();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AddRandomEvent();
        }
    }

    void AddGold()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = em.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));

        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var resources = em.GetComponentData<ConvoyResources>(playerEntity);

        resources.Gold += 100;
        em.SetComponentData(playerEntity, resources);

        Debug.Log("💰 +100 gold added!");
    }

    void StartMovement()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = em.CreateEntityQuery(typeof(PlayerTag), typeof(TravelState), typeof(MapPosition));

        if (playerQuery.IsEmpty) return;

        var playerEntity = playerQuery.GetSingletonEntity();
        var travelState = em.GetComponentData<TravelState>(playerEntity);
        var position = em.GetComponentData<MapPosition>(playerEntity);

        travelState.Destination = position.WorldPosition + new float3(50, 0, 50);
        travelState.StartPosition = position.WorldPosition;
        travelState.IsTraveling = true;
        travelState.TotalTravelTime = 10f;
        travelState.DestinationReached = false;
        travelState.TravelProgress = 0f;

        em.SetComponentData(playerEntity, travelState);

        Debug.Log("🚀 Movement started to (50, 50)!");
    }

    void AddRandomEvent()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var eventEntity = em.CreateEntity();

        em.AddComponentData(eventEntity, new GameEvent
        {
            Type = EventType.TradeOpportunity,
            Severity = 0.7f,
            Duration = 15f,
            Description = "Test event: Merchant offers good prices!",
            Processed = false
        });

        Debug.Log("🎲 Test event created!");
    }
}