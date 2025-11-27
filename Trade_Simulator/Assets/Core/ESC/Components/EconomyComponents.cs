using Unity.Entities;
using Unity.Collections;

// =============================================
// КОМПОНЕНТЫ ЭКОНОМИКИ И ТОРГОВЛИ
// =============================================

// Данные товара
public struct GoodData : IComponentData
{
    public FixedString64Bytes Name;    // Название товара
    public int WeightPerUnit;          // Вес за единицу
    public int BaseValue;              // Базовая стоимость
    public GoodCategory Category;      // Категория товара
    public float ProfitPerKm;          // Прибыль за км перевозки
    public float DecayRate;            // Скорость порчи (0.0 - 1.0)
}

// Рынок города
public struct CityMarket : IComponentData
{
    public Entity CityEntity;          // Ссылка на город
    public float PriceMultiplier;      // Множитель цен
    public float TradeVolume;          // Объем торговли
}

// Цены на товары в рынке (буфер)
public struct GoodPriceBuffer : IBufferElementData
{
    public Entity GoodEntity;          // Ссылка на товар
    public int Price;                  // Текущая цена
    public float Supply;               // Уровень предложения (0.0 - 2.0)
    public float Demand;               // Уровень спроса (0.0 - 2.0)
}

// Торговая транзакция
public struct TradeTransaction : IComponentData
{
    public Entity GoodEntity;          // Товар
    public Entity MarketEntity;        // Рынок
    public int Quantity;               // Количество
    public int TotalPrice;             // Общая стоимость
    public bool IsBuy;                 // Покупка (true) или продажа (false)
    public TradeStatus Status;         // Статус транзакции
}

// Статистика инвентаря
public struct InventoryStats : IComponentData
{
    public int TotalItems;             // Всего предметов
    public int TotalValue;             // Общая стоимость
    public int TotalWeight;            // Общий вес
    public float AverageCondition;     // Среднее качество товаров
}

// Перечисления для экономики
public enum GoodCategory
{
    RawMaterials,  // Сырье (зерно, руда, древесина)
    Crafts,        // Ремесла (ткань, кожа, инструменты)
    Luxury,        // Роскошь (вино, украшения, специи)
    Food           // Еда (фрукты, мясо, хлеб)
}

public enum TradeStatus
{
    Pending,       // Ожидает обработки
    Success,       // Успешно завершена
    Failed,        // Не удалась
    Cancelled      // Отменена
}

public enum EconomyType
{
    Agricultural,  // Аграрная экономика
    Industrial,    // Индустриальная экономика  
    TradeHub,      // Торговый узел
    Mining         // Горнодобывающая
}