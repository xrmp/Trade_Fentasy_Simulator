using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class CityAuthoring : MonoBehaviour
{
    [Header("Настройки города")]
    [Tooltip("Название города")]
    public string cityName = "Новый Город";

    [Tooltip("Тип экономики города")]
    public EconomyType economyType = EconomyType.Agricultural;

    [Tooltip("Население города")]
    public int population = 2000;

    [Tooltip("Радиус торгового влияния")]
    public int tradeRadius = 20;

    [Header("Позиция на карте")]
    [Tooltip("Позиция города в сетке карты")]
    public Vector2 gridPosition = new Vector2(0, 0);

    [Tooltip("Множитель цен в городе")]
    [Range(0.5f, 2.0f)]
    public float priceMultiplier = 1.0f;

    class Baker : Baker<CityAuthoring>
    {
        public override void Bake(CityAuthoring authoring)
        {
            Debug.Log($"🏙️ CityAuthoring: Создаем город {authoring.cityName}...");

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Добавляем тэг города
            AddComponent<CityTag>(entity);

            // Создаем компонент города
            var worldPosition = new float3(authoring.gridPosition.x * 10f, 0, authoring.gridPosition.y * 10f);

            AddComponent(entity, new City
            {
                Name = authoring.cityName,
                GridPosition = new int2((int)authoring.gridPosition.x, (int)authoring.gridPosition.y),
                WorldPosition = worldPosition,
                Population = authoring.population,
                EconomyType = authoring.economyType,
                TradeRadius = authoring.tradeRadius
            });

            // Создаем рынок для города
            CreateCityMarket(entity, authoring.priceMultiplier);

            Debug.Log($"✅ Город создан: {authoring.cityName} ({authoring.economyType})");
        }

        private void CreateCityMarket(Entity cityEntity, float priceMultiplier)
        {
            var marketEntity = CreateAdditionalEntity(TransformUsageFlags.None);

            AddComponent(marketEntity, new CityMarket
            {
                CityEntity = cityEntity,
                PriceMultiplier = priceMultiplier,
                TradeVolume = 1.0f
            });

            // Добавляем буфер для цен (заполнится в GameInitializationSystem)
            AddBuffer<GoodPriceBuffer>(marketEntity);
        }
    }
}