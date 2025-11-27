using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Map
{
    public class MapManager : MonoBehaviour
    {
        [Header("Настройки карты")]
        [SerializeField] private Texture2D mapTexture;
        [SerializeField] private float mapWidth = 100f;
        [SerializeField] private float mapHeight = 100f;
        [SerializeField] private float worldScale = 10f;

        [Header("Визуализация")]
        [SerializeField] private Material mapMaterial;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showTerrainTypes = true;

        [Header("Отладка")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private bool drawDebugGizmos = true;

        private EntityManager _entityManager;
        private World _ecsWorld;
        private bool _isMapInitialized = false;

        // Кэш данных карты
        private NativeArray<TerrainType> _terrainGrid;
        private int2 _gridSize;

        private void Awake()
        {
            Debug.Log("🗺️ MapManager: Инициализация...");

            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld != null)
            {
                _entityManager = _ecsWorld.EntityManager;
            }

            InitializeMap();
        }

        private void OnDestroy()
        {
            // Очищаем NativeArray при уничтожении
            if (_terrainGrid.IsCreated)
            {
                _terrainGrid.Dispose();
            }
        }

        private void InitializeMap()
        {
            if (_isMapInitialized)
            {
                Debug.LogWarning("⚠️ MapManager: Карта уже инициализирована");
                return;
            }

            try
            {
                // Устанавливаем размер сетки
                _gridSize = new int2((int)mapWidth, (int)mapHeight);

                // Создаем NativeArray для хранения данных карты
                _terrainGrid = new NativeArray<TerrainType>(_gridSize.x * _gridSize.y, Allocator.Persistent);

                // Генерируем или загружаем карту
                if (mapTexture != null)
                {
                    LoadMapFromTexture();
                }
                else
                {
                    GenerateProceduralMap();
                }

                // Создаем ECS сущности для карты
                CreateMapEntities();

                _isMapInitialized = true;
                Debug.Log($"✅ MapManager: Карта инициализирована {_gridSize.x}x{_gridSize.y}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ MapManager: Ошибка инициализации карты: {e.Message}");
            }
        }

        private void LoadMapFromTexture()
        {
            if (mapTexture == null) return;

            Debug.Log("🎨 MapManager: Загрузка карты из текстуры...");

            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int x = 0; x < _gridSize.x; x++)
                {
                    // Нормализуем координаты для текстуры
                    float u = (float)x / _gridSize.x;
                    float v = (float)y / _gridSize.y;

                    Color pixel = mapTexture.GetPixelBilinear(u, v);
                    TerrainType terrainType = ColorToTerrainType(pixel);

                    int index = y * _gridSize.x + x;
                    _terrainGrid[index] = terrainType;
                }
            }
        }

        private void GenerateProceduralMap()
        {
            Debug.Log("🎲 MapManager: Генерация процедурной карты...");

            var random = new Unity.Mathematics.Random(12345);

            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int x = 0; x < _gridSize.x; x++)
                {
                    TerrainType terrainType = GenerateTerrainAtPosition(x, y, random);
                    int index = y * _gridSize.x + x;
                    _terrainGrid[index] = terrainType;
                }
            }
        }

        private TerrainType GenerateTerrainAtPosition(int x, int y, Unity.Mathematics.Random random)
        {
            // Простая процедурная генерация на основе шума
            float noiseValue = noise.cnoise(new float2(x * 0.1f, y * 0.1f));

            // Добавляем некоторую случайность
            float randomValue = random.NextFloat();

            if (noiseValue > 0.6f)
                return TerrainType.Mountains;
            else if (noiseValue > 0.3f)
                return TerrainType.Forest;
            else if (noiseValue > 0.1f)
                return TerrainType.Plains;
            else if (noiseValue > -0.2f)
                return randomValue > 0.7f ? TerrainType.Road : TerrainType.Plains;
            else if (noiseValue > -0.4f)
                return TerrainType.Desert;
            else
                return TerrainType.River;
        }

        private TerrainType ColorToTerrainType(Color color)
        {
            // Конвертируем цвет пикселя в тип местности
            if (color.g > 0.7f) return TerrainType.Plains;        // Зеленый - равнины
            if (color.b > 0.7f) return TerrainType.River;         // Синий - реки
            if (color.r > 0.7f) return TerrainType.Mountains;     // Красный - горы
            if (color.g > 0.4f) return TerrainType.Forest;        // Темно-зеленый - лес
            if (color.r > 0.5f && color.g > 0.5f) return TerrainType.Desert; // Желтый - пустыня
            if (color.r == color.g && color.g == color.b && color.r > 0.7f) return TerrainType.Road; // Серый - дороги

            return TerrainType.Plains; // По умолчанию - равнины
        }

        private void CreateMapEntities()
        {
            Debug.Log("🏗️ MapManager: Создание ECS сущностей карты...");

            // Создаем сущности для каждой ячейки карты
            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int x = 0; x < _gridSize.x; x++)
                {
                    int index = y * _gridSize.x + x;
                    TerrainType terrainType = _terrainGrid[index];

                    CreateTerrainEntity(x, y, terrainType);
                }
            }

            // Создаем конфигурацию карты
            CreateMapConfigEntity();
        }

        private void CreateTerrainEntity(int x, int y, TerrainType terrainType)
        {
            var entity = _entityManager.CreateEntity();

            var worldPosition = new float3(x * worldScale, 0, y * worldScale);
            var movementCost = GetMovementCost(terrainType);
            var wearMultiplier = GetWearMultiplier(terrainType);
            var dangerLevel = GetDangerLevel(terrainType);
            var foodAvailability = GetFoodAvailability(terrainType);

            _entityManager.AddComponentData(entity, new TerrainData
            {
                GridPosition = new int2(x, y),
                Type = terrainType,
                MovementCost = movementCost,
                WearMultiplier = wearMultiplier,
                DangerLevel = dangerLevel,
                FoodAvailability = foodAvailability
            });
        }

        private void CreateMapConfigEntity()
        {
            var entity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(entity, new MapConfig
            {
                Width = _gridSize.x,
                Height = _gridSize.y,
                WorldScale = worldScale,
                Seed = 12345
            });
        }

        public TerrainType GetTerrainAtPosition(int2 gridPosition)
        {
            if (!_isMapInitialized || !_terrainGrid.IsCreated)
                return TerrainType.Plains;

            if (gridPosition.x < 0 || gridPosition.x >= _gridSize.x ||
                gridPosition.y < 0 || gridPosition.y >= _gridSize.y)
                return TerrainType.Plains;

            int index = gridPosition.y * _gridSize.x + gridPosition.x;
            return _terrainGrid[index];
        }


        public TerrainType GetTerrainAtWorldPosition(float3 worldPosition)
        {
            int2 gridPosition = WorldToGridPosition(worldPosition);
            return GetTerrainAtPosition(gridPosition);
        }


        public int2 WorldToGridPosition(float3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x / worldScale);
            int y = Mathf.RoundToInt(worldPosition.z / worldScale);
            return new int2(x, y);
        }

        public float3 GridToWorldPosition(int2 gridPosition)
        {
            return new float3(gridPosition.x * worldScale, 0, gridPosition.y * worldScale);
        }

        public bool IsPositionValid(int2 gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.x < _gridSize.x &&
                   gridPosition.y >= 0 && gridPosition.y < _gridSize.y;
        }

        private float GetMovementCost(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Road => 0.8f,
                TerrainType.Plains => 1.0f,
                TerrainType.Forest => 1.5f,
                TerrainType.Mountains => 2.0f,
                TerrainType.Desert => 1.3f,
                TerrainType.River => 1.8f,
                _ => 1.0f
            };
        }

        private float GetWearMultiplier(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Road => 0.7f,
                TerrainType.Plains => 1.0f,
                TerrainType.Forest => 1.3f,
                TerrainType.Mountains => 2.0f,
                TerrainType.Desert => 1.5f,
                TerrainType.River => 1.8f,
                _ => 1.0f
            };
        }

        private float GetDangerLevel(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Forest => 0.7f,
                TerrainType.Mountains => 0.8f,
                TerrainType.Desert => 0.6f,
                TerrainType.River => 0.5f,
                TerrainType.Road => 0.3f,
                TerrainType.Plains => 0.4f,
                _ => 0.5f
            };
        }

        private float GetFoodAvailability(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plains => 0.8f,
                TerrainType.Forest => 0.6f,
                TerrainType.River => 0.7f,
                TerrainType.Road => 0.3f,
                TerrainType.Mountains => 0.2f,
                TerrainType.Desert => 0.1f,
                _ => 0.5f
            };
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos || !_isMapInitialized || !_terrainGrid.IsCreated) return;

            // Рисуем отладочную информацию о карте
            DrawTerrainGizmos();
        }

        private void DrawTerrainGizmos()
        {
            // Рисуем только небольшую область вокруг камеры для производительности
            var camera = Camera.main;
            if (camera == null) return;

            var cameraPos = camera.transform.position;
            int2 cameraGrid = WorldToGridPosition(cameraPos);

            int drawRadius = 20;
            int startX = Mathf.Max(0, cameraGrid.x - drawRadius);
            int endX = Mathf.Min(_gridSize.x - 1, cameraGrid.x + drawRadius);
            int startY = Mathf.Max(0, cameraGrid.y - drawRadius);
            int endY = Mathf.Min(_gridSize.y - 1, cameraGrid.y + drawRadius);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = y * _gridSize.x + x;
                    TerrainType terrain = _terrainGrid[index];
                    Color gizmoColor = GetTerrainGizmoColor(terrain);

                    var worldPos = GridToWorldPosition(new int2(x, y));
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawCube(worldPos, new Vector3(worldScale * 0.9f, 0.1f, worldScale * 0.9f));
                }
            }
        }

        private Color GetTerrainGizmoColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plains => Color.green,
                TerrainType.Forest => new Color(0f, 0.5f, 0f), // Темно-зеленый
                TerrainType.Mountains => Color.gray,
                TerrainType.Desert => Color.yellow,
                TerrainType.River => Color.blue,
                TerrainType.Road => Color.white,
                _ => Color.magenta
            };
        }

        public string GetMapInfo()
        {
            var info = $"🗺️ Map Info:\n";
            info += $"Size: {_gridSize.x}x{_gridSize.y}\n";
            info += $"World Scale: {worldScale}\n";
            info += $"Initialized: {_isMapInitialized}\n";
            info += $"Terrain Data: {(_terrainGrid.IsCreated ? "Loaded" : "Not Loaded")}\n";

            return info;
        }
    }
}