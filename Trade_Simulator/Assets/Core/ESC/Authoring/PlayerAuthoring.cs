using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class PlayerAuthoring : MonoBehaviour
{
    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerConvoy
            {
                CurrentPosition = float3.zero,
                MoveSpeed = 5.0f,
                TotalCapacity = 1000,
                UsedCapacity = 0,
                CurrentSpeedModifier = 1.0f
            });

            AddComponent(entity, new ConvoyResources
            {
                Gold = 1000,
                Food = 100,
                Guards = 5,
                FoodConsumptionRate = 2,
                Morale = 1.0f
            });

            AddComponent(entity, new MapPosition
            {
                GridPosition = new int2(0, 0),
                WorldPosition = float3.zero,
                CurrentTerrainType = TerrainType.Plains
            });

            AddComponent(entity, new TravelState
            {
                IsTraveling = false,
                TravelProgress = 0f,
                TotalTravelTime = 0f,
                DestinationReached = true
            });

            AddBuffer<InventoryBuffer>(entity);
            AddComponent<PlayerTag>(entity);
        }
    }
}

public struct PlayerTag : IComponentData { }