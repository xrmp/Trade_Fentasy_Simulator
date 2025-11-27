using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using System.Collections.Generic;

namespace Core.Managers
{

    public class ECSBootstrap : MonoBehaviour
    {
        [Header("Настройки ECS")]
        [Tooltip("Автоматически создавать Default World при старте")]
        public bool createDefaultWorld = true;

        [Header("Отладка")]
        [Tooltip("Включить детальное логирование инициализации")]
        public bool verboseLogging = true;

        private void Awake()
        {
            if (verboseLogging)
                Debug.Log("🚀 ECSBootstrap: Начало инициализации ECS мира...");

            // Проверяем, не создан ли уже мир
            if (World.All.Count > 0)
            {
                if (verboseLogging)
                    Debug.Log("ℹ️ ECSBootstrap: ECS мир уже инициализирован");
                return;
            }

            if (createDefaultWorld)
            {
                InitializeDefaultWorld();
            }

            if (verboseLogging)
                Debug.Log("✅ ECSBootstrap: Инициализация завершена успешно");
        }

        private void InitializeDefaultWorld()
        {
            try
            {
                // Создаем стандартный мир Unity
                var world = World.DefaultGameObjectInjectionWorld;

                if (world == null)
                {
                    Debug.LogError("❌ ECSBootstrap: Не удалось создать Default World");
                    return;
                }

                if (verboseLogging)
                    Debug.Log($"✅ ECSBootstrap: Default World создан - {world.Name}");

                // В Unity 6 системные группы создаются автоматически
                // Мы только проверяем их наличие
                EnsureSystemGroupsExist(world);

            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ ECSBootstrap: Ошибка инициализации Default World: {e.Message}");
            }
        }

        private void EnsureSystemGroupsExist(World world)
        {
            // Получаем стандартные системные группы
            // В Unity 6 они обычно создаются автоматически

            var initializationGroup = world.GetExistingSystemManaged<InitializationSystemGroup>();
            var simulationGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            var presentationGroup = world.GetExistingSystemManaged<PresentationSystemGroup>();

            if (verboseLogging)
            {
                Debug.Log($"📊 ECSBootstrap: System Groups Status:");
                Debug.Log($"  - Initialization: {initializationGroup != null}");
                Debug.Log($"  - Simulation: {simulationGroup != null}");
                Debug.Log($"  - Presentation: {presentationGroup != null}");
            }

            // Если какие-то группы отсутствуют, логируем предупреждение
            if (initializationGroup == null || simulationGroup == null || presentationGroup == null)
            {
                Debug.LogWarning("⚠️ ECSBootstrap: Некоторые системные группы отсутствуют. " +
                               "В Unity 6 рекомендуется использовать автоматическую инициализацию.");
            }
        }

        private void OnDestroy()
        {
            if (verboseLogging)
                Debug.Log("🧹 ECSBootstrap: Очистка ECS мира...");
        }


        public string GetWorldStats()
        {
            var stats = "📊 ECS World Statistics:\n";

            if (World.All.Count == 0)
            {
                stats += "No ECS worlds initialized\n";
                return stats;
            }

            foreach (var world in World.All)
            {
                // Получаем все системы мира
                var systemCount = GetSystemCount(world);
                stats += $"- {world.Name}: {systemCount} систем\n";

                // Логируем только первые 10 систем для читаемости
                var systemNames = GetSystemNames(world, 10);
                foreach (var systemName in systemNames)
                {
                    stats += $"  └ {systemName}\n";
                }

                if (systemCount > 10)
                {
                    stats += $"  └ ... и еще {systemCount - 10} систем\n";
                }
            }

            return stats;
        }


        private int GetSystemCount(World world)
        {
            int count = 0;
            foreach (var system in world.Systems)
            {
                count++;
            }
            return count;
        }


        private List<string> GetSystemNames(World world, int maxCount)
        {
            var names = new List<string>();
            int count = 0;

            foreach (var system in world.Systems)
            {
                if (count++ >= maxCount) break;
                names.Add(system.GetType().Name);
            }

            return names;
        }


        public bool AreKeySystemsInitialized()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return false;

            // Проверяем наличие ключевых системных групп
            var hasInitialization = world.GetExistingSystemManaged<InitializationSystemGroup>() != null;
            var hasSimulation = world.GetExistingSystemManaged<SimulationSystemGroup>() != null;
            var hasPresentation = world.GetExistingSystemManaged<PresentationSystemGroup>() != null;

            return hasInitialization && hasSimulation && hasPresentation;
        }

        /// <summary>
        /// Получить информацию о конкретной системной группе
        /// </summary>
        public string GetSystemGroupInfo<T>() where T : ComponentSystemGroup
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return "World not initialized";

            var group = world.GetExistingSystemManaged<T>();
            if (group == null) return $"System group {typeof(T).Name} not found";

            var info = $"🔧 {typeof(T).Name}:\n";

            // В новых версиях Unity ECS нужно использовать другие методы для получения систем
            var systemCount = GetSystemCountInGroup(group);
            info += $"  Systems Count: {systemCount}\n";

            var systemNames = GetSystemNamesInGroup(group, 5);
            foreach (var systemName in systemNames)
            {
                info += $"  └ {systemName}\n";
            }

            if (systemCount > 5)
            {
                info += $"  └ ... и еще {systemCount - 5} систем\n";
            }

            return info;
        }


        private int GetSystemCountInGroup(ComponentSystemGroup group)
        {
            int count = 0;
            // Используем рефлексию или другие методы для получения систем в группе
            // В новых версиях Unity это может быть сложнее
            return count;
        }


        private List<string> GetSystemNamesInGroup(ComponentSystemGroup group, int maxCount)
        {
            var names = new List<string>();
            // В новых версиях Unity API для получения систем в группе изменилось
            // Возвращаем пустой список, так как прямой доступ к Systems больше не работает
            return names;
        }


        public string GetStandardSystemGroupsInfo()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return "World not initialized";

            var info = "🔧 Standard System Groups:\n";

            var initializationGroup = world.GetExistingSystemManaged<InitializationSystemGroup>();
            if (initializationGroup != null)
            {
                info += $"  - Initialization: присутствует\n";
            }

            var simulationGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulationGroup != null)
            {
                info += $"  - Simulation: присутствует\n";
            }

            var presentationGroup = world.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentationGroup != null)
            {
                info += $"  - Presentation: присутствует\n";
            }

            return info;
        }

        public string GetSimpleSystemGroupsInfo()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return "World not initialized";

            var info = "🔧 System Groups Status:\n";

            info += $"  - Default World: {world.Name}\n";
            info += $"  - Total Systems: {GetSystemCount(world)}\n";
            info += $"  - Initialization: {(world.GetExistingSystemManaged<InitializationSystemGroup>() != null ? "✓" : "✗")}\n";
            info += $"  - Simulation: {(world.GetExistingSystemManaged<SimulationSystemGroup>() != null ? "✓" : "✗")}\n";
            info += $"  - Presentation: {(world.GetExistingSystemManaged<PresentationSystemGroup>() != null ? "✓" : "✗")}\n";

            return info;
        }
    }
}