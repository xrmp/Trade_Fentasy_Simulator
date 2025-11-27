using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Map
{

    public class Pathfinder : MonoBehaviour
    {
        [Header("Настройки поиска пути")]
        [SerializeField] private bool allowDiagonalMovement = false;
        [SerializeField] private int maxPathLength = 1000;
        [SerializeField] private float heuristicWeight = 1.0f;

        [Header("Отладка")]
        [SerializeField] private bool drawDebugPaths = true;
        [SerializeField] private Color pathColor = Color.red;
        [SerializeField] private Color exploredColor = Color.yellow;

        private MapManager _mapManager;
        private NativeArray<TerrainType> _terrainGrid;
        private int2 _gridSize;

        private void Awake()
        {
            Debug.Log("🧭 Pathfinder: Инициализация...");
            _mapManager = UnityEngine.Object.FindAnyObjectByType<MapManager>();
        }


        public NativeList<int2> FindPath(int2 start, int2 target)
        {
            var path = new NativeList<int2>(Allocator.Temp);

            if (_mapManager == null)
            {
                Debug.LogError("❌ Pathfinder: MapManager не найден");
                return path;
            }

            if (!_mapManager.IsPositionValid(start) || !_mapManager.IsPositionValid(target))
            {
                Debug.LogError($"❌ Pathfinder: Невалидные позиции start:{start} target:{target}");
                return path;
            }

            if (start.Equals(target))
            {
                path.Add(start);
                return path;
            }

            try
            {
                path = AStarSearch(start, target);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Pathfinder: Ошибка поиска пути: {e.Message}");
            }

            return path;
        }


        public NativeList<float3> FindWorldPath(float3 start, float3 target)
        {
            var gridPath = FindPath(
                _mapManager.WorldToGridPosition(start),
                _mapManager.WorldToGridPosition(target)
            );

            var worldPath = new NativeList<float3>(gridPath.Length, Allocator.Temp);

            foreach (var gridPos in gridPath)
            {
                worldPath.Add(_mapManager.GridToWorldPosition(gridPos));
            }

            gridPath.Dispose();
            return worldPath;
        }

        private NativeList<int2> AStarSearch(int2 start, int2 target)
        {
            var openSet = new NativeHashSet<int2>(maxPathLength, Allocator.Temp);
            var closedSet = new NativeHashSet<int2>(maxPathLength, Allocator.Temp);

            var gScore = new NativeHashMap<int2, float>(maxPathLength, Allocator.Temp);
            var fScore = new NativeHashMap<int2, float>(maxPathLength, Allocator.Temp);
            var cameFrom = new NativeHashMap<int2, int2>(maxPathLength, Allocator.Temp);

            // Инициализация начальных значений
            openSet.Add(start);
            gScore.TryAdd(start, 0);
            fScore.TryAdd(start, HeuristicCost(start, target));

            while (openSet.Count > 0)
            {
                // Находим узел с наименьшим fScore
                int2 current = GetLowestFScoreNode(openSet, fScore);

                if (current.Equals(target))
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                // Проверяем соседей
                var neighbors = GetNeighbors(current);
                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor)) continue;

                    float tentativeGScore = gScore[current] + GetMovementCost(current, neighbor);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (tentativeGScore >= gScore[neighbor])
                    {
                        continue;
                    }

                    // Этот путь лучше, запоминаем его
                    cameFrom.Remove(neighbor);
                    cameFrom.TryAdd(neighbor, current);
                    gScore.Remove(neighbor);
                    gScore.TryAdd(neighbor, tentativeGScore);

                    float fScoreValue = tentativeGScore + HeuristicCost(neighbor, target);
                    fScore.Remove(neighbor);
                    fScore.TryAdd(neighbor, fScoreValue);
                }

                neighbors.Dispose();

                // Защита от бесконечного цикла
                if (closedSet.Count > maxPathLength)
                {
                    Debug.LogWarning("⚠️ Pathfinder: Достигнут лимит длины пути");
                    break;
                }
            }

            // Путь не найден
            Debug.LogWarning($"⚠️ Pathfinder: Путь от {start} до {target} не найден");
            return new NativeList<int2>(Allocator.Temp);
        }

        private NativeList<int2> GetNeighbors(int2 position)
        {
            var neighbors = new NativeList<int2>(8, Allocator.Temp);

            // Базовые направления (вверх, вниз, влево, вправо)
            int2[] directions = {
                new int2(0, 1),   // Вверх
                new int2(1, 0),   // Вправо
                new int2(0, -1),  // Вниз
                new int2(-1, 0)   // Влево
            };

            // Диагональные направления (если разрешены)
            if (allowDiagonalMovement)
            {
                int2[] diagonalDirections = {
                    new int2(1, 1),    // Вверх-вправо
                    new int2(1, -1),   // Вниз-вправо
                    new int2(-1, -1),  // Вниз-влево
                    new int2(-1, 1)    // Вверх-влево
                };

                foreach (var dir in diagonalDirections)
                {
                    int2 neighborPos = position + dir;
                    if (_mapManager.IsPositionValid(neighborPos))
                    {
                        neighbors.Add(neighborPos);
                    }
                }
            }

            // Добавляем базовые направления
            foreach (var dir in directions)
            {
                int2 neighborPos = position + dir;
                if (_mapManager.IsPositionValid(neighborPos))
                {
                    neighbors.Add(neighborPos);
                }
            }

            return neighbors;
        }

        private float GetMovementCost(int2 from, int2 to)
        {
            TerrainType fromTerrain = _mapManager.GetTerrainAtPosition(from);
            TerrainType toTerrain = _mapManager.GetTerrainAtPosition(to);

            float baseCost = GetTerrainMovementCost(toTerrain);

            // Учитываем разницу в высоте (если есть)
            float heightCost = 0f;

            // Диагональное движение дороже
            bool isDiagonal = math.abs(from.x - to.x) == 1 && math.abs(from.y - to.y) == 1;
            if (isDiagonal)
            {
                baseCost *= 1.414f; // √2
            }

            return baseCost + heightCost;
        }

        private float GetTerrainMovementCost(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Road => 1.0f,
                TerrainType.Plains => 1.5f,
                TerrainType.Forest => 2.0f,
                TerrainType.Mountains => 4.0f,
                TerrainType.Desert => 2.5f,
                TerrainType.River => 3.0f,
                _ => 2.0f
            };
        }

        private float HeuristicCost(int2 a, int2 b)
        {
            // Манхэттенское расстояние
            int dx = math.abs(a.x - b.x);
            int dy = math.abs(a.y - b.y);

            if (allowDiagonalMovement)
            {
                // Евклидово расстояние для диагонального движения
                return math.sqrt(dx * dx + dy * dy) * heuristicWeight;
            }
            else
            {
                // Манхэттенское расстояние для ортогонального движения
                return (dx + dy) * heuristicWeight;
            }
        }

        private int2 GetLowestFScoreNode(NativeHashSet<int2> openSet, NativeHashMap<int2, float> fScore)
        {
            int2 lowestNode = openSet.ToNativeArray(Allocator.Temp)[0];
            float minScore = float.MaxValue;

            foreach (var node in openSet)
            {
                if (fScore.TryGetValue(node, out float score) && score < minScore)
                {
                    lowestNode = node;
                    minScore = score;
                }
            }

            return lowestNode;
        }

        private NativeList<int2> ReconstructPath(NativeHashMap<int2, int2> cameFrom, int2 current)
        {
            var path = new NativeList<int2>(Allocator.Temp);
            path.Add(current);

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            // Разворачиваем путь (от начала до конца)
            for (int i = 0; i < path.Length / 2; i++)
            {
                var temp = path[i];
                path[i] = path[path.Length - 1 - i];
                path[path.Length - 1 - i] = temp;
            }

            // Оптимизируем путь (убираем лишние точки)
            var optimizedPath = OptimizePath(path);
            path.Dispose();

            return optimizedPath;
        }

        private NativeList<int2> OptimizePath(NativeList<int2> path)
        {
            if (path.Length <= 2) return path;

            var optimized = new NativeList<int2>(path.Length, Allocator.Temp);
            optimized.Add(path[0]);

            for (int i = 1; i < path.Length - 1; i++)
            {
                // Проверяем, можно ли пропустить точку
                int2 prev = optimized[optimized.Length - 1];
                int2 next = path[i + 1];

                // Если движение от prev к next прямое (без препятствий), пропускаем текущую точку
                if (!HasObstacleBetween(prev, next))
                {
                    continue;
                }

                optimized.Add(path[i]);
            }

            optimized.Add(path[path.Length - 1]);
            return optimized;
        }

        private bool HasObstacleBetween(int2 from, int2 to)
        {
            // Проверяем, есть ли препятствия на прямой линии между двумя точками
            // Упрощенная версия - проверяем только конечные точки
            TerrainType fromTerrain = _mapManager.GetTerrainAtPosition(from);
            TerrainType toTerrain = _mapManager.GetTerrainAtPosition(to);

            return GetTerrainMovementCost(fromTerrain) > 3.0f ||
                   GetTerrainMovementCost(toTerrain) > 3.0f;
        }

        public float CalculatePathCost(NativeList<int2> path)
        {
            if (path.Length <= 1) return 0f;

            float totalCost = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                totalCost += GetMovementCost(path[i], path[i + 1]);
            }

            return totalCost;
        }

        public float CalculatePathTime(NativeList<int2> path, float speed)
        {
            float pathCost = CalculatePathCost(path);
            return pathCost / math.max(speed, 0.1f);
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugPaths) return;


        }
    }
}