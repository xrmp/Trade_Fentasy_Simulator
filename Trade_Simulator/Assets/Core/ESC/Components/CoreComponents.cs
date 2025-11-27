using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

// =============================================
// ОСНОВНЫЕ КОМПОНЕНТЫ ИГРОВОГО СОСТОЯНИЯ
// =============================================

// Тэги для идентификации сущностей
public struct PlayerTag : IComponentData { }
public struct WagonTag : IComponentData { }
public struct CityTag : IComponentData { }
public struct EnemyTag : IComponentData { }

// Основной обоз игрока
public struct PlayerConvoy : IComponentData
{
    public float3 CurrentPosition;      // Текущая позиция в мире
    public float MoveSpeed;            // Текущая скорость движения
    public float BaseSpeed;            // Базовая скорость
    public int TotalCapacity;          // Общая грузоподъемность
    public int UsedCapacity;           // Используемая грузоподъемность  
    public float CurrentSpeedModifier; // Модификатор скорости (штрафы/бонусы)
}

// Ресурсы обоза
public struct ConvoyResources : IComponentData
{
    public int Gold;                   // Золото
    public int Food;                   // Провиант
    public int Guards;                 // Количество охраны
    public int FoodConsumptionRate;    // Потребление пищи в день
    public float Morale;               // Мораль (0.0 - 1.0)
}

// Позиция на карте
public struct MapPosition : IComponentData
{
    public int2 GridPosition;          // Позиция в сетке карты
    public float3 WorldPosition;       // Позиция в мировых координатах
    public TerrainType CurrentTerrain; // Текущий тип местности
}

// Состояние путешествия
public struct TravelState : IComponentData
{
    public bool IsTraveling;           // В пути ли обоз
    public float TravelProgress;       // Прогресс пути (0.0 - 1.0)
    public float TotalTravelTime;      // Общее время пути
    public bool DestinationReached;    // Достигнут ли пункт назначения
    public float3 Destination;         // Целевая позиция
    public float3 StartPosition;       // Начальная позиция маршрута
}

// Инвентарь (буфер)
public struct InventoryBuffer : IBufferElementData
{
    public Entity GoodEntity;          // Ссылка на товар
    public int Quantity;               // Количество
}

// Повозка
public struct Wagon : IComponentData
{
    public Entity Owner;               // Владелец повозки
    public int Health;                 // Текущее здоровье
    public int MaxHealth;              // Максимальное здоровье
    public int LoadCapacity;           // Грузоподъемность
    public int CurrentLoad;            // Текущая загрузка
    public float SpeedModifier;        // Модификатор скорости
    public float WearRate;             // Скорость износа
    public WagonType Type;             // Тип повозки
    public bool IsBroken;              // Сломана ли
}

// Охранник
public struct Guard : IComponentData
{
    public Entity Owner;               // Владелец отряда
    public GuardType Type;             // Тип охраны
    public int Level;                  // Уровень
    public int CombatPower;            // Боевая сила
    public int Salary;                 // Зарплата в день
    public float Morale;               // Мораль отряда
    public bool IsInCombat;            // В бою ли
}

// Перечисления для Core компонентов
public enum WagonType
{
    BasicCart,     // Базовая повозка
    TradeWagon,    // Торговая повозка  
    HeavyWagon,    // Тяжелая повозка
    LuxuryCoach    // Роскошная карета
}

public enum GuardType
{
    Militia,       // Ополчение (дешево, слабо)
    Mercenary,     // Наемники (среднее)
    EliteGuard     // Элитная охрана (дорого, сильно)
}

public enum TerrainType
{
    Plains,        // Равнины
    Forest,        // Лес
    Mountains,     // Горы
    Desert,        // Пустыня
    River,         // Река
    Road           // Дорога
}