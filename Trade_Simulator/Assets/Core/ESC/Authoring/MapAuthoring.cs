using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class MapAuthoring : MonoBehaviour
{
    [Header("Настройки карты")]
    [Tooltip("Ширина карты в тайлах")]
    public int mapWidth = 100;

    [Tooltip("Высота карты в тайлах")]
    public int mapHeight = 100;

    [Tooltip("Масштаб мира")]
    public float worldScale = 10f;

    [Tooltip("Сид для генерации карты")]
    public int seed = 12345;

    [Header("Генерация местности")]
    [Tooltip("Вероятность равнин")]
    [Range(0f, 1f)]
    public float plainsProbability = 0.4f;

    [Tooltip("Вероятность лесов")]
    [Range(0f, 1f)]
    public float forestProbability = 0.3f;

    [Tooltip("Вероятность гор")]
    [Range(0f, 1f)]
    public float mountainProbability = 0.2f;

    [Tooltip("Вероятность дорог")]
    [Range(0f, 1f)]
    public float roadProbability = 0.1f;

    class Baker : Baker<MapAuthoring>
    {
        public override void Bake(MapAuthoring authoring)
        {
            Debug.Log("🗺️ MapAuthoring: Создаем конфигурацию карты...");

            var entity = GetEntity(TransformUsageFlags.None);

            // Создаем конфигурацию карты
            AddComponent(entity, new MapConfig
            {
                Width = authoring.mapWidth,
                Height = authoring.mapHeight,
                WorldScale = authoring.worldScale,
                Seed = authoring.seed
            });

            // Добавляем настройки генерации
            AddComponent(entity, new MapGenerationSettings
            {
                PlainsProbability = authoring.plainsProbability,
                ForestProbability = authoring.forestProbability,
                MountainProbability = authoring.mountainProbability,
                RoadProbability = authoring.roadProbability
            });

            Debug.Log($"✅ Конфигурация карты создана: {authoring.mapWidth}x{authoring.mapHeight}, seed: {authoring.seed}");
        }
    }
}

// Дополнительный компонент для настроек генерации
public struct MapGenerationSettings : IComponentData
{
    public float PlainsProbability;
    public float ForestProbability;
    public float MountainProbability;
    public float RoadProbability;
    public float DesertProbability => 1f - (PlainsProbability + ForestProbability + MountainProbability + RoadProbability);
}