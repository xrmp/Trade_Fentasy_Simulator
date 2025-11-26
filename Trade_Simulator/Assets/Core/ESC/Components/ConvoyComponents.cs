using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

// === Œ—ÕŒ¬Õ€≈  ŒÃœŒÕ≈Õ“€ ===
public struct PlayerConvoy : IComponentData
{
    public float3 CurrentPosition;
    public float MoveSpeed;
    public float BaseSpeed;
    public int TotalCapacity;
    public int UsedCapacity;
    public float CurrentSpeedModifier;
}

public struct ConvoyResources : IComponentData
{
    public int Gold;
    public int Food;
    public int Guards;
    public int FoodConsumptionRate;
    public float Morale;
}

public struct MapPosition : IComponentData
{
    public int2 GridPosition;
    public float3 WorldPosition;
    public TerrainType CurrentTerrainType;
}

public struct TravelState : IComponentData
{
    public bool IsTraveling;
    public float TravelProgress;
    public float TotalTravelTime;
    public bool DestinationReached;
    public float3 Destination;
    public float3 StartPosition;
}

// === “Œ¬¿–€ » »Õ¬≈Õ“¿–‹ ===
public struct GoodData : IComponentData
{
    public FixedString64Bytes Name;
    public int WeightPerUnit;
    public int BaseValue;
    public GoodCategory Category;
    public float ProfitPerKm;
}

public struct InventoryBuffer : IBufferElementData
{
    public Entity GoodEntity;
    public int Quantity;
}

// === œŒ¬Œ« » ===
public struct Wagon : IComponentData
{
    public Entity Owner;
    public int Health;
    public int MaxHealth;
    public int LoadCapacity;
    public int CurrentLoad;
    public float SpeedModifier;
    public float WearRate;
    public WagonType WagonType;
    public bool IsBroken;
}

// === —Œ¡€“»ﬂ ===
public struct GameEvent : IComponentData
{
    public EventType Type;
    public float Severity;
    public float Duration;
    public FixedString128Bytes Description;
    public bool Processed;
}


// === œ≈–≈◊»—À≈Õ»ﬂ ===
public enum TerrainType
{
    Plains,
    Forest,
    Mountains,
    Desert,
    River,
    Road
}

public enum GoodCategory
{
    RawMaterials,
    Crafts,
    Luxury,
    Food
}

public enum WagonType
{
    BasicCart,
    TradeWagon,
    HeavyWagon,
    LuxuryCoach
}

public enum EventType
{
    WagonBreakdown,
    BanditAttack,
    WeatherStorm,
    TradeOpportunity,
    RoadBlock
}