using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Map.Data
{

    [CreateAssetMenu(fileName = "CityData", menuName = "Game/City Data")]
    public class CityData : ScriptableObject
    {
        [Header("Список городов")]
        public CitySettings[] cities;


        public CitySettings GetCitySettings(string cityName)
        {
            foreach (var city in cities)
            {
                if (city.cityName == cityName)
                    return city;
            }

            Debug.LogWarning($"⚠️ CityData: Город {cityName} не найден");
            return null;
        }


        public CitySettings GetCitySettings(int index)
        {
            if (index >= 0 && index < cities.Length)
                return cities[index];

            Debug.LogWarning($"⚠️ CityData: Город с индексом {index} не найден");
            return null;
        }


        public List<CitySettings> GetCitiesByEconomy(EconomyType economyType)
        {
            var result = new List<CitySettings>();
            foreach (var city in cities)
            {
                if (city.economyType == economyType)
                    result.Add(city);
            }
            return result;
        }


        public CitySettings GetNearestCity(float2 position)
        {
            if (cities.Length == 0) return null;

            CitySettings nearest = cities[0];
            float nearestDistance = math.distance(position, nearest.gridPosition);

            for (int i = 1; i < cities.Length; i++)
            {
                float distance = math.distance(position, cities[i].gridPosition);
                if (distance < nearestDistance)
                {
                    nearest = cities[i];
                    nearestDistance = distance;
                }
            }

            return nearest;
        }


        public List<CitySettings> GetCitiesInRadius(float2 position, float radius)
        {
            var result = new List<CitySettings>();
            foreach (var city in cities)
            {
                float distance = math.distance(position, city.gridPosition);
                if (distance <= radius)
                    result.Add(city);
            }
            return result;
        }


        public void ResetToDefaults()
        {
            cities = new CitySettings[]
            {
                new CitySettings
                {
                    cityName = "Стартовый Город",
                    gridPosition = new float2(10, 10),
                    economyType = EconomyType.Agricultural,
                    population = 2000,
                    tradeRadius = 20,
                    priceMultiplier = 0.8f,
                    description = "Мирный сельскохозяйственный городок, идеальное место для начала торгового пути."
                },
                new CitySettings
                {
                    cityName = "Торговая Столица",
                    gridPosition = new float2(50, 50),
                    economyType = EconomyType.TradeHub,
                    population = 5000,
                    tradeRadius = 30,
                    priceMultiplier = 1.0f,
                    description = "Крупный торговый центр, здесь можно найти самые выгодные сделки."
                },
                new CitySettings
                {
                    cityName = "Горная Крепость",
                    gridPosition = new float2(80, 20),
                    economyType = EconomyType.Mining,
                    population = 1500,
                    tradeRadius = 15,
                    priceMultiplier = 1.1f,
                    description = "Укрепленный город в горах, известный своими рудниками и кузницами."
                },
                new CitySettings
                {
                    cityName = "Портовый Город",
                    gridPosition = new float2(30, 70),
                    economyType = EconomyType.Industrial,
                    population = 3000,
                    tradeRadius = 25,
                    priceMultiplier = 1.2f,
                    description = "Процветающий портовый город с развитой промышленностью."
                },
                new CitySettings
                {
                    cityName = "Северный Форпост",
                    gridPosition = new float2(20, 80),
                    economyType = EconomyType.Agricultural,
                    population = 1200,
                    tradeRadius = 15,
                    priceMultiplier = 0.9f,
                    description = "Небольшой форпост на севере, специализирующийся на животноводстве."
                },
                new CitySettings
                {
                    cityName = "Южная Деревня",
                    gridPosition = new float2(70, 30),
                    economyType = EconomyType.Agricultural,
                    population = 800,
                    tradeRadius = 10,
                    priceMultiplier = 0.7f,
                    description = "Тихая деревушка на юге, известная своими фруктовыми садами."
                }
            };
        }


        public string GetCitiesInfo()
        {
            var info = $"🏙️ Cities Info ({cities.Length} cities):\n";
            foreach (var city in cities)
            {
                info += $"- {city.cityName}: {city.economyType}, Pop: {city.population}, Pos: ({city.gridPosition.x}, {city.gridPosition.y})\n";
            }
            return info;
        }
    }


    [Serializable]
    public class CitySettings
    {
        [Header("Основная информация")]
        [Tooltip("Название города")]
        public string cityName = "Новый Город";

        [Tooltip("Позиция на карте в сеточных координатах")]
        public float2 gridPosition;

        [Tooltip("Тип экономики города")]
        public EconomyType economyType = EconomyType.Agricultural;

        [Tooltip("Население города")]
        [Range(100, 10000)]
        public int population = 2000;

        [Header("Торговые настройки")]
        [Tooltip("Радиус торгового влияния")]
        [Range(5, 50)]
        public int tradeRadius = 20;

        [Tooltip("Множитель цен в городе")]
        [Range(0.5f, 2.0f)]
        public float priceMultiplier = 1.0f;

        [Header("Визуальные настройки")]
        [Tooltip("Размер значка города на карте")]
        [Range(0.5f, 3.0f)]
        public float mapIconSize = 1.0f;

        [Tooltip("Цвет города на карте")]
        public Color mapColor = Color.white;

        [Header("Описание")]
        [Tooltip("Описание города")]
        [TextArea(2, 4)]
        public string description = "Описание города";

        [Header("Специализация")]
        [Tooltip("Основные производимые товары")]
        public GoodCategory[] producedGoods;

        [Tooltip("Основные импортируемые товары")]
        public GoodCategory[] importedGoods;

        [Tooltip("Уникальные товары доступные только в этом городе")]
        public string[] uniqueGoods;
    }


    public enum EconomyType
    {
        Agricultural,  // Аграрная экономика
        Industrial,    // Индустриальная экономика  
        TradeHub,      // Торговый узел
        Mining         // Горнодобывающая
    }

    public enum GoodCategory
    {
        RawMaterials,  // Сырье (зерно, руда, древесина)
        Crafts,        // Ремесла (ткань, кожа, инструменты)
        Luxury,        // Роскошь (вино, украшения, специи)
        Food           // Еда (фрукты, мясо, хлеб)
    }
}