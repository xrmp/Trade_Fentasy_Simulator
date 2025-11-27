using UnityEngine;
using System;

namespace Map.Data
{
    [CreateAssetMenu(fileName = "TerrainTypes", menuName = "Game/Terrain Types")]
    public class TerrainTypes : ScriptableObject
    {
        [Header("Настройки типов местности")]
        public TerrainTypeSettings[] terrainSettings;


        public TerrainTypeSettings GetSettings(TerrainType terrainType)
        {
            foreach (var settings in terrainSettings)
            {
                if (settings.terrainType == terrainType)
                    return settings;
            }

            // Возвращаем настройки по умолчанию если не найдено
            Debug.LogWarning($"⚠️ TerrainTypes: Настройки для {terrainType} не найдены, используются значения по умолчанию");
            return new TerrainTypeSettings { terrainType = terrainType };
        }


        public float GetMovementCost(TerrainType terrainType)
        {
            return GetSettings(terrainType).movementCost;
        }


        public float GetWearMultiplier(TerrainType terrainType)
        {
            return GetSettings(terrainType).wearMultiplier;
        }


        public float GetDangerLevel(TerrainType terrainType)
        {
            return GetSettings(terrainType).dangerLevel;
        }


        public float GetFoodAvailability(TerrainType terrainType)
        {
            return GetSettings(terrainType).foodAvailability;
        }


        public Color GetTerrainColor(TerrainType terrainType)
        {
            return GetSettings(terrainType).visualColor;
        }


        public string GetTerrainName(TerrainType terrainType)
        {
            return GetSettings(terrainType).displayName;
        }

        public bool IsTraversable(TerrainType terrainType)
        {
            return GetSettings(terrainType).isTraversable;
        }


        public void ResetToDefaults()
        {
            terrainSettings = new TerrainTypeSettings[]
            {
                new TerrainTypeSettings
                {
                    terrainType = TerrainType.Plains,
                    displayName = "Равнины",
                    movementCost = 1.0f,
                    wearMultiplier = 1.0f,
                    dangerLevel = 0.4f,
                    foodAvailability = 0.8f,
                    visualColor = Color.green,
                    isTraversable = true
                },
                new TerrainTypeSettings
                {
                    terrainType = TerrainType.Forest,
                    displayName = "Лес",
                    movementCost = 1.5f,
                    wearMultiplier = 1.3f,
                    dangerLevel = 0.7f,
                    foodAvailability = 0.6f,
                    visualColor = new Color(0f, 0.5f, 0f),
                    isTraversable = true
                },
                new TerrainTypeSettings
                {
                    terrainType = TerrainType.Mountains,
                    displayName = "Горы",
                    movementCost = 2.0f,
                    wearMultiplier = 2.0f,
                    dangerLevel = 0.8f,
                    foodAvailability = 0.2f,
                    visualColor = Color.gray,
                    isTraversable = true
                },
                new TerrainTypeSettings
                {
                    terrainType = TerrainType.Desert,
                    displayName = "Пустыня",
                    movementCost = 1.3f,
                    wearMultiplier = 1.5f,
                    dangerLevel = 0.6f,
                    foodAvailability = 0.1f,
                    visualColor = Color.yellow,
                    isTraversable = true
                },
                new TerrainTypeSettings
                {
                    terrainType = TerrainType.River,
                    displayName = "Река",
                    movementCost = 1.8f,
                    wearMultiplier = 1.8f,
                    dangerLevel = 0.5f,
                    foodAvailability = 0.7f,
                    visualColor = Color.blue,
                    isTraversable = true
                },
                new TerrainTypeSettings
                {
                    terrainType = TerrainType.Road,
                    displayName = "Дорога",
                    movementCost = 0.8f,
                    wearMultiplier = 0.7f,
                    dangerLevel = 0.3f,
                    foodAvailability = 0.3f,
                    visualColor = Color.white,
                    isTraversable = true
                }
            };
        }
    }

    [Serializable]
    public class TerrainTypeSettings
    {
        [Tooltip("Тип местности")]
        public TerrainType terrainType;

        [Tooltip("Отображаемое имя")]
        public string displayName = "Новая местность";

        [Header("Характеристики")]
        [Tooltip("Стоимость движения (1.0 = нормальная)")]
        [Range(0.1f, 5.0f)]
        public float movementCost = 1.0f;

        [Tooltip("Множитель износа повозок")]
        [Range(0.1f, 3.0f)]
        public float wearMultiplier = 1.0f;

        [Tooltip("Уровень опасности (0-1)")]
        [Range(0.0f, 1.0f)]
        public float dangerLevel = 0.5f;

        [Tooltip("Доступность пищи (0-1)")]
        [Range(0.0f, 1.0f)]
        public float foodAvailability = 0.5f;

        [Header("Визуальные настройки")]
        [Tooltip("Цвет для отображения на карте")]
        public Color visualColor = Color.white;

        [Tooltip("Можно ли перемещаться по этой местности")]
        public bool isTraversable = true;

        [Tooltip("Описание местности")]
        [TextArea(2, 4)]
        public string description = "Описание местности";
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
}