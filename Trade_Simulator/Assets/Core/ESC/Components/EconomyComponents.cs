using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct City : IComponentData
{
    public FixedString64Bytes Name;
    public int2 GridPosition;
    public float3 WorldPosition;
    public int Population;
    public EconomyType EconomyType;
}

public struct CityMarket : IComponentData
{
    public Entity CityEntity;
    public float PriceMultiplier;
}

public struct GoodPriceBuffer : IBufferElementData
{
    public Entity GoodEntity;
    public int Price;
    public float Supply;
    public float Demand;
}

public struct TradeTransaction : IComponentData
{
    public Entity GoodEntity;
    public int Quantity;
    public int TotalPrice;
    public bool IsBuy;
}

public enum EconomyType
{
    Agricultural,
    Industrial,
    TradeHub,
    Mining
}