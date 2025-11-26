using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct MapConfig : IComponentData
{
    public int Width;
    public int Height;
    public float WorldScale;
    public int Seed;
}

public struct TerrainData : IComponentData
{
    public int2 GridPosition;
    public TerrainType Type;
    public float MovementCost;
    public float WearMultiplier;
    public float DangerLevel;
    public float FoodAvailability;
}

public struct CityData : IComponentData
{
    public Entity CityEntity;
    public int2 GridPosition;
    public FixedString64Bytes Name;
    public EconomyType EconomyType;
    public int TradeRadius;
}

public struct RoutePlan : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public float TotalDistance;
    public float EstimatedTime;
    public float FoodRequired;
    public float RiskLevel;
    public bool IsValid;
}

public struct PathNode : IBufferElementData
{
    public int2 Position;
    public float Cost;
    public TerrainType Terrain;
}