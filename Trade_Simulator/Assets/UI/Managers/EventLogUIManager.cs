using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;
using System.Text;

namespace UI.Managers
{
    public class EventLogUIManager : MonoBehaviour
    {
        [Header("Основные элементы")]
        [SerializeField] private GameObject eventLogPanel;
        [SerializeField] private Transform eventsContainer;
        [SerializeField] private GameObject eventEntryPrefab;
        [SerializeField] private ScrollRect eventsScrollRect;

        [Header("Уведомления")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private Image notificationIcon;
        [SerializeField] private float notificationDuration = 5f;

        [Header("Фильтры событий")]
        [SerializeField] private Toggle filterCombatToggle;
        [SerializeField] private Toggle filterTradeToggle;
        [SerializeField] private Toggle filterTravelToggle;
        [SerializeField] private Toggle filterSystemToggle;
        [SerializeField] private Button clearLogButton;

        [Header("Статистика")]
        [SerializeField] private TextMeshProUGUI eventsCountText;
        [SerializeField] private TextMeshProUGUI unreadCountText;

        private EntityManager _entityManager;
        private World _ecsWorld;
        private Queue<GameEvent> _pendingEvents = new Queue<GameEvent>();
        private List<GameObject> _eventEntries = new List<GameObject>();
        private float _notificationTimer = 0f;
        private bool _showingNotification = false;

        // Статистика
        private int _totalEvents = 0;
        private int _unreadEvents = 0;
        private EventType _activeFilters = EventType.BanditAttack | EventType.TradeOpportunity |
                                         EventType.WeatherStorm | EventType.WagonBreakdown |
                                         EventType.GoodWeather | EventType.LuckyFind;

        private void Awake()
        {
            Debug.Log("📋 EventLogUIManager: Инициализация...");

            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld != null)
            {
                _entityManager = _ecsWorld.EntityManager;
            }

            SetupUIEvents();
        }

        private void Update()
        {
            ProcessPendingEvents();
            UpdateNotificationTimer();
            UpdateEventsFromECS();
        }

        private void SetupUIEvents()
        {
            if (filterCombatToggle != null)
                filterCombatToggle.onValueChanged.AddListener((value) => UpdateFilter(EventType.BanditAttack, value));

            if (filterTradeToggle != null)
                filterTradeToggle.onValueChanged.AddListener((value) => UpdateFilter(EventType.TradeOpportunity, value));

            if (filterTravelToggle != null)
                filterTravelToggle.onValueChanged.AddListener((value) => UpdateFilter(EventType.WeatherStorm, value));

            if (filterSystemToggle != null)
                filterSystemToggle.onValueChanged.AddListener((value) => UpdateFilter(EventType.WagonBreakdown, value));

            if (clearLogButton != null)
                clearLogButton.onClick.AddListener(ClearEventLog);
        }

        private void UpdateFilter(EventType eventType, bool include)
        {
            if (include)
                _activeFilters |= eventType;
            else
                _activeFilters &= ~eventType;

            RefreshEventLog();
        }

        private void ProcessPendingEvents()
        {
            while (_pendingEvents.Count > 0)
            {
                var gameEvent = _pendingEvents.Dequeue();
                AddEventToLog(gameEvent);
                ShowNotification(gameEvent);
            }
        }

        private void UpdateEventsFromECS()
        {
            // Получаем новые события из ECS
            var eventQuery = _entityManager.CreateEntityQuery(typeof(GameEvent));
            var events = eventQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var eventEntity in events)
            {
                var gameEvent = _entityManager.GetComponentData<GameEvent>(eventEntity);

                if (!gameEvent.Processed)
                {
                    _pendingEvents.Enqueue(gameEvent);

                    // Помечаем событие как обработанное
                    var eventData = gameEvent;
                    eventData.Processed = true;
                    _entityManager.SetComponentData(eventEntity, eventData);
                }
            }

            events.Dispose();
        }

        private void AddEventToLog(GameEvent gameEvent)
        {
            if (eventEntryPrefab == null || eventsContainer == null) return;

            // Проверяем фильтр
            if ((_activeFilters & gameEvent.Type) == 0) return;

            var eventEntry = Instantiate(eventEntryPrefab, eventsContainer);
            SetupEventEntry(eventEntry, gameEvent);
            _eventEntries.Add(eventEntry);

            // Обновляем статистику
            _totalEvents++;
            _unreadEvents++;
            UpdateStatistics();

            // Прокручиваем к новому событию
            if (eventsScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                eventsScrollRect.verticalNormalizedPosition = 0f;
            }

            // Ограничиваем количество записей (опционально)
            if (_eventEntries.Count > 100)
            {
                RemoveOldestEvent();
            }
        }

        private void SetupEventEntry(GameObject eventEntry, GameEvent gameEvent)
        {
            var timeText = eventEntry.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
            var eventText = eventEntry.transform.Find("EventText")?.GetComponent<TextMeshProUGUI>();
            var typeIcon = eventEntry.transform.Find("TypeIcon")?.GetComponent<Image>();
            var severityIndicator = eventEntry.transform.Find("SeverityIndicator")?.GetComponent<Image>();

            // Время события
            if (timeText != null)
                timeText.text = $"{Time.time:F0}s";

            // Текст события
            if (eventText != null)
                eventText.text = gameEvent.Description.ToString();

            // Иконка типа события
            if (typeIcon != null)
            {
                typeIcon.color = GetEventTypeColor(gameEvent.Type);
                typeIcon.sprite = GetEventTypeIcon(gameEvent.Type);
            }

            // Индикатор серьезности
            if (severityIndicator != null)
            {
                severityIndicator.color = GetSeverityColor(gameEvent.Severity);

                // Размер индикатора в зависимости от серьезности
                var scale = 0.5f + gameEvent.Severity * 0.5f;
                severityIndicator.transform.localScale = new Vector3(scale, scale, 1f);
            }

            // Цвет фона в зависимости от типа события
            var background = eventEntry.GetComponent<Image>();
            if (background != null)
            {
                background.color = GetEventBackgroundColor(gameEvent.Type);
            }
        }

        private void ShowNotification(GameEvent gameEvent)
        {
            if (notificationPanel == null || notificationText == null) return;

            notificationText.text = gameEvent.Description.ToString();

            if (notificationIcon != null)
            {
                notificationIcon.color = GetEventTypeColor(gameEvent.Type);
            }

            notificationPanel.SetActive(true);
            _showingNotification = true;
            _notificationTimer = notificationDuration;

            // Автоматическое скрытие через заданное время
            Invoke(nameof(HideNotification), notificationDuration);
        }

        private void HideNotification()
        {
            if (notificationPanel != null)
                notificationPanel.SetActive(false);

            _showingNotification = false;
        }

        private void UpdateNotificationTimer()
        {
            if (!_showingNotification) return;

            _notificationTimer -= Time.deltaTime;
            if (_notificationTimer <= 0f)
            {
                HideNotification();
            }
        }

        private void RemoveOldestEvent()
        {
            if (_eventEntries.Count == 0) return;

            var oldestEvent = _eventEntries[0];
            _eventEntries.RemoveAt(0);

            if (oldestEvent != null)
                Destroy(oldestEvent);
        }

        private void RefreshEventLog()
        {
            // Очищаем и пересоздаем записи согласно текущим фильтрам
            ClearEventEntries();

            // В реальной реализации здесь будет перезапись всех событий из истории
            // Для демонстрации просто очищаем лог
            _unreadEvents = 0;
            UpdateStatistics();
        }

        private void ClearEventLog()
        {
            ClearEventEntries();
            _totalEvents = 0;
            _unreadEvents = 0;
            UpdateStatistics();

            Debug.Log("🧹 EventLog: Журнал событий очищен");
        }

        private void ClearEventEntries()
        {
            foreach (var entry in _eventEntries)
            {
                if (entry != null)
                    Destroy(entry);
            }
            _eventEntries.Clear();
        }

        private void UpdateStatistics()
        {
            if (eventsCountText != null)
                eventsCountText.text = $"Всего: {_totalEvents}";

            if (unreadCountText != null)
            {
                unreadCountText.text = $"Новых: {_unreadEvents}";
                unreadCountText.color = _unreadEvents > 0 ? Color.yellow : Color.gray;
            }
        }

        private Color GetEventTypeColor(EventType eventType)
        {
            return eventType switch
            {
                EventType.BanditAttack => Color.red,
                EventType.WagonBreakdown => new Color(1f, 0.5f, 0f), // Оранжевый
                EventType.WeatherStorm => Color.blue,
                EventType.TradeOpportunity => Color.green,
                EventType.LuckyFind => Color.yellow,
                EventType.GoodWeather => Color.cyan,
                EventType.NewRecruits => Color.magenta,
                _ => Color.gray
            };
        }

        private Sprite GetEventTypeIcon(EventType eventType)
        {
            // В реальном проекте здесь будут загружаться иконки из Resources
            return null;
        }

        private Color GetSeverityColor(float severity)
        {
            return severity switch
            {
                < 0.3f => Color.green,
                < 0.6f => Color.yellow,
                _ => Color.red
            };
        }

        private Color GetEventBackgroundColor(EventType eventType)
        {
            var baseColor = GetEventTypeColor(eventType);
            return new Color(baseColor.r, baseColor.g, baseColor.b, 0.1f);
        }

        public void SetEventLogVisible(bool visible)
        {
            if (eventLogPanel != null)
                eventLogPanel.SetActive(visible);

            if (visible)
            {
                _unreadEvents = 0;
                UpdateStatistics();
            }
        }

        public void AddTestEvent(string message, EventType eventType = EventType.TravelEncounter)
        {
            var testEvent = new GameEvent
            {
                Type = eventType,
                Description = message,
                Severity = 0.5f,
                Duration = 10f,
                Processed = true
            };

            AddEventToLog(testEvent);
        }

        public bool IsEventLogOpen()
        {
            return eventLogPanel != null && eventLogPanel.activeInHierarchy;
        }
    }
}