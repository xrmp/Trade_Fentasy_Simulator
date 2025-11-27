using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

// =============================================
// КОМПОНЕНТЫ КАРТЫ И НАВИГАЦИИ
// =============================================

// Город
public struct City : IComponentData
{
    public FixedString64Bytes Name;    // Название города
    public int2 GridPosition;          // Позиция на сетке карты
    public float3 WorldPosition;       // Позиция в мире
    public int Population;             // Население
    public EconomyType EconomyType;    // Тип экономики
    public int TradeRadius;            // Радиус торгового влияния
}

// Данные местности
public struct TerrainData : IComponentData
{
    public int2 GridPosition;          // Позиция в сетке
    public TerrainType Type;           // Тип местности
    public float MovementCost;         // Стоимость движения (1.0 = норма)
    public float WearMultiplier;       // Множитель износа повозок
    public float DangerLevel;          // Уровень опасности (0.0 - 1.0)
    public float FoodAvailability;     // Доступность пищи (0.0 - 1.0)
}

// План маршрута
public struct RoutePlan : IComponentData
{
    public float3 StartPosition;       // Начальная точка
    public float3 EndPosition;         // Конечная точка
    public float TotalDistance;        // Общая дистанция
    public float EstimatedTime;        // Расчетное время
    public float FoodRequired;         // Необходимый провиант
    public float RiskLevel;            // Уровень риска (0.0 - 1.0)
    public bool IsValid;               // Корректен ли маршрут
}

// Узел пути (буфер для сложных маршрутов)
public struct PathNode : IBufferElementData
{
    public int2 Position;              // Позиция узла
    public float Cost;                 // Стоимость прохождения
    public TerrainType Terrain;        // Тип местности в узле
}

// Конфигурация карты (синглтон)
public struct MapConfig : IComponentData
{
    public int Width;                  // Ширина карты в тайлах
    public int Height;                 // Высота карты в тайлах
    public float WorldScale;           // Масштаб мира
    public int Seed;                   // Сид генерации
}

// Текущая локация игрока
public struct CurrentLocation : IComponentData
{
    public Entity CityEntity;          // Текущий город (Entity.Null если в пути)
    public float3 WorldPosition;       // Текущая позиция в мире
    public TerrainType CurrentTerrain; // Текущая местность
}