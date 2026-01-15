using UnityEngine;
using TMPro;

/// <summary>
/// Панель улучшения для размещённого брейнрота.
/// При клике на панель увеличивает уровень брейнрота и списывает стоимость улучшения с баланса.
/// </summary>
public class GradePanel : MonoBehaviour
{
    [Header("Настройки панели")]
    [Tooltip("ID панели для связи с PlacementPanel (должен совпадать с panelID в PlacementPanel)")]
    [SerializeField] private int id = 0;
    
    [Tooltip("Радиус обнаружения игрока (панель скрывается, если игрок дальше этого расстояния)")]
    [SerializeField] private float range = 5f;
    
    [Tooltip("Использовать горизонтальное расстояние (игнорировать высоту Y)")]
    [SerializeField] private bool useHorizontalDistance = true;
    
    [Tooltip("Временно отключить автоматическое скрытие панели (для тестирования)")]
    [SerializeField] private bool disableAutoHide = false;
    
    [Header("UI")]
    [Tooltip("TextMeshPro компонент для отображения уровня (автоматически находится в дочернем объекте 'Level')")]
    [SerializeField] private TextMeshPro levelText;
    
    [Header("Настройки обнаружения игрока")]
    [Tooltip("Transform игрока (перетащите из иерархии, или будет найден автоматически по тегу 'Player')")]
    [SerializeField] private Transform playerTransform;
    
    [Header("Отладка")]
    [Tooltip("Показывать отладочные сообщения в консоли")]
    [SerializeField] private bool debug = false;
    
    // Ссылка на связанную PlacementPanel
    private PlacementPanel linkedPlacementPanel;
    
    // Ссылка на размещённый brainrot объект
    private BrainrotObject placedBrainrot;
    
    // Кэш для всех Renderer компонентов (для визуального скрытия)
    private Renderer[] renderers;
    
    // Кэш для всех Collider компонентов (для отключения взаимодействия)
    private Collider[] colliders;
    
    // Флаг для отслеживания предыдущего состояния видимости (для логирования)
    private bool wasVisible = true;
    
    private void Awake()
    {
        // Автоматически находим TextMeshPro компонент в дочернем объекте "Level"
        if (levelText == null)
        {
            Transform levelTransform = transform.Find("Level");
            if (levelTransform != null)
            {
                levelText = levelTransform.GetComponent<TextMeshPro>();
                if (levelText == null && debug)
                {
                    Debug.LogWarning($"[GradePanel] TextMeshPro компонент не найден на объекте 'Level' в {gameObject.name}");
                }
            }
            else if (debug)
            {
                Debug.LogWarning($"[GradePanel] Объект 'Level' не найден в дочерних объектах {gameObject.name}");
            }
        }
        
        // Пытаемся найти игрока автоматически, если не назначен
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else if (debug)
            {
                Debug.LogWarning($"[GradePanel] Игрок не найден по тегу 'Player'! Назначьте playerTransform в инспекторе.");
            }
        }
        
        // Проверяем наличие коллайдера для обработки кликов
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
            if (col == null && debug)
            {
                Debug.LogWarning($"[GradePanel] На объекте {gameObject.name} нет коллайдера! Клики не будут работать. Добавьте Collider для обработки кликов.");
            }
        }
        
        // Кэшируем все Renderer компоненты для визуального скрытия
        renderers = GetComponentsInChildren<Renderer>(true);
        
        // Кэшируем все Collider компоненты для отключения взаимодействия
        colliders = GetComponentsInChildren<Collider>(true);
    }
    
    private void Start()
    {
        // Находим связанную PlacementPanel по ID
        FindLinkedPlacementPanel();
        
        if (linkedPlacementPanel == null && debug)
        {
            Debug.LogWarning($"[GradePanel] PlacementPanel с ID {id} НЕ найдена! Проверьте, что ID совпадает с panelID в PlacementPanel.");
        }
        
        // ВАЖНО: Не скрываем панель сразу в Start, даём Update() возможность проверить условия
        // Панель должна быть видима по умолчанию, если она активна в иерархии
    }
    
    private void Update()
    {
        // Обновляем ссылку на размещённый brainrot объект
        UpdatePlacedBrainrot();
        
        // Проверяем видимость панели (расстояние до игрока и наличие брейнрота)
        UpdatePanelVisibility();
        
        // Обновляем текст уровня (всегда, так как объект активен)
        UpdateLevelText();
        
        // Обрабатываем клик мыши через Raycast (всегда, так как объект активен)
        HandleMouseClick();
    }
    
    /// <summary>
    /// Находит связанную PlacementPanel по ID
    /// </summary>
    private void FindLinkedPlacementPanel()
    {
        linkedPlacementPanel = PlacementPanel.GetPanelByID(id);
        if (linkedPlacementPanel == null)
        {
            if (debug)
            {
                Debug.LogWarning($"[GradePanel] PlacementPanel с ID {id} не найдена!");
            }
        }
    }
    
    /// <summary>
    /// Обновляет ссылку на размещённый brainrot объект
    /// </summary>
    private void UpdatePlacedBrainrot()
    {
        if (linkedPlacementPanel == null)
        {
            // Пытаемся найти панель снова
            FindLinkedPlacementPanel();
            if (linkedPlacementPanel == null)
            {
                placedBrainrot = null;
                if (debug && Time.frameCount % 120 == 0) // Логируем каждые 2 секунды
                {
                    Debug.LogWarning($"[GradePanel] PlacementPanel с ID {id} не найдена! Проверьте, что ID совпадает с panelID в PlacementPanel.");
                }
                return;
            }
        }
        
        // Получаем размещённый brainrot из связанной панели
        placedBrainrot = linkedPlacementPanel.GetPlacedBrainrot();
    }
    
    /// <summary>
    /// Получает мировую позицию для проверки расстояния (использует позицию размещения из PlacementPanel)
    /// </summary>
    private Vector3 GetDetectionPosition()
    {
        // Приоритет: используем позицию размещения из связанной PlacementPanel
        if (linkedPlacementPanel != null)
        {
            return linkedPlacementPanel.GetPlacementPosition();
        }
        
        // Если PlacementPanel не найдена, используем позицию самой панели
        // Пытаемся использовать центр коллайдера (всегда возвращает мировые координаты)
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
        }
        
        if (col != null)
        {
            // bounds.center всегда возвращает мировую позицию
            return col.bounds.center;
        }
        
        // Если коллайдера нет, используем transform.position (тоже мировые координаты)
        return transform.position;
    }
    
    /// <summary>
    /// Обновляет видимость панели в зависимости от расстояния до игрока и наличия брейнрота
    /// </summary>
    private void UpdatePanelVisibility()
    {
        // Проверяем наличие размещённого брейнрота
        bool hasBrainrot = placedBrainrot != null;
        
        // Получаем позицию для обнаружения (из PlacementPanel или самой панели)
        Vector3 detectionPosition = GetDetectionPosition();
        
        // Проверяем расстояние до игрока (используя мировые координаты)
        bool playerInRange = false;
        float distance = float.MaxValue;
        
        if (playerTransform != null)
        {
            // playerTransform.position всегда возвращает мировые координаты
            Vector3 playerWorldPosition = playerTransform.position;
            
            if (useHorizontalDistance)
            {
                // Используем только горизонтальное расстояние (игнорируем высоту Y)
                Vector2 detectionPos2D = new Vector2(detectionPosition.x, detectionPosition.z);
                Vector2 playerPos2D = new Vector2(playerWorldPosition.x, playerWorldPosition.z);
                distance = Vector2.Distance(detectionPos2D, playerPos2D);
            }
            else
            {
                // Используем полное 3D расстояние
                distance = Vector3.Distance(detectionPosition, playerWorldPosition);
            }
            
            playerInRange = distance <= range;
        }
        else
        {
            // Если игрок не найден, пытаемся найти его снова
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Vector3 playerWorldPosition = playerTransform.position;
                
                if (useHorizontalDistance)
                {
                    Vector2 detectionPos2D = new Vector2(detectionPosition.x, detectionPosition.z);
                    Vector2 playerPos2D = new Vector2(playerWorldPosition.x, playerWorldPosition.z);
                    distance = Vector2.Distance(detectionPos2D, playerPos2D);
                }
                else
                {
                    distance = Vector3.Distance(detectionPosition, playerWorldPosition);
                }
                
                playerInRange = distance <= range;
            }
        }
        
        // Панель должна быть видима только если:
        // 1. Есть размещённый брейнрот
        // 2. Игрок в радиусе
        // ИЛИ если отключено автоматическое скрытие (для тестирования)
        bool shouldBeVisible = disableAutoHide || (hasBrainrot && playerInRange);
        
        // Обновляем видимость панели (визуально, но объект остаётся активным)
        // ВАЖНО: Объект должен оставаться активным, чтобы скрипт продолжал работать
        SetPanelVisibility(shouldBeVisible);
        
        // Логируем изменение видимости только при изменении состояния и если debug включен
        if (wasVisible != shouldBeVisible && Time.frameCount > 1 && debug)
        {
            wasVisible = shouldBeVisible;
            
            if (shouldBeVisible)
            {
                string reason = disableAutoHide ? "автоскрытие отключено" : $"брейнрот размещён, игрок в радиусе {distance:F2}/{range}";
                Debug.Log($"[GradePanel] Панель ПОКАЗАНА (ID: {id}, причина: {reason})");
            }
            else
            {
                string reason = !hasBrainrot ? "нет размещённого брейнрота" : $"игрок вне радиуса (расстояние: {distance:F2}, range: {range})";
                Debug.Log($"[GradePanel] Панель СКРЫТА (ID: {id}, причина: {reason})");
            }
        }
        else if (wasVisible != shouldBeVisible)
        {
            wasVisible = shouldBeVisible;
        }
    }
    
    /// <summary>
    /// Устанавливает визуальную видимость панели (объект остаётся активным)
    /// </summary>
    private void SetPanelVisibility(bool visible)
    {
        // Скрываем/показываем все Renderer компоненты
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
        
        // Отключаем/включаем все Collider компоненты (чтобы нельзя было кликнуть когда скрыто)
        if (colliders != null)
        {
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    col.enabled = visible;
                }
            }
        }
        
        // Также скрываем/показываем TextMeshPro компонент
        if (levelText != null)
        {
            levelText.enabled = visible;
        }
    }
    
    /// <summary>
    /// Обновляет текст уровня в формате "(Lv.1 -> Lv.2)" или "Max Level" если уровень 20
    /// </summary>
    private void UpdateLevelText()
    {
        if (levelText == null) return;
        
        if (placedBrainrot == null)
        {
            // Если нет размещённого брейнрота, показываем пустой текст или дефолтное значение
            levelText.text = "(Lv.0 -> Lv.0)";
            return;
        }
        
        int currentLevel = placedBrainrot.GetLevel();
        
        // Если уровень достиг 20, показываем "Max Level"
        if (currentLevel >= 20)
        {
            levelText.text = "Max Level";
            return;
        }
        
        int nextLevel = currentLevel + 1;
        
        // Форматируем текст в формате "(Lv.1 -> Lv.2)"
        levelText.text = $"(Lv.{currentLevel} -> Lv.{nextLevel})";
    }
    
    /// <summary>
    /// Обрабатывает клик мыши через Raycast
    /// </summary>
    private void HandleMouseClick()
    {
        // Проверяем, была ли нажата левая кнопка мыши
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }
        
        // Получаем камеру (главную камеру или камеру из сцены)
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera == null)
        {
            if (debug)
            {
                Debug.LogWarning("[GradePanel] Камера не найдена для обработки клика!");
            }
            return;
        }
        
        // Создаём луч из позиции мыши
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Проверяем, попал ли луч в коллайдер этого объекта или его дочерних объектов
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
        }
        
        if (col == null)
        {
            return;
        }
        
        // Проверяем пересечение луча с коллайдером
        if (col.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Клик попал в панель - обрабатываем улучшение
            ProcessUpgrade();
        }
    }
    
    /// <summary>
    /// Обрабатывает улучшение брейнрота
    /// </summary>
    private void ProcessUpgrade()
    {
        // Проверяем, что есть размещённый брейнрот
        if (placedBrainrot == null)
        {
            if (debug)
            {
                Debug.Log("[GradePanel] Нет размещённого брейнрота для улучшения!");
            }
            return;
        }
        
        // Получаем текущий уровень брейнрота
        int currentLevel = placedBrainrot.GetLevel();
        
        // Проверяем, не достиг ли брейнрот максимального уровня
        if (currentLevel >= 20)
        {
            if (debug)
            {
                Debug.Log("[GradePanel] Брейнрот уже достиг максимального уровня (20)!");
            }
            return;
        }
        
        // Получаем финальный доход (уже включает редкость и уровень)
        double finalIncome = placedBrainrot.GetFinalIncome();
        
        // Вычисляем стоимость улучшения: финальный доход * 20
        double upgradeCost = finalIncome * 20.0;
        
        // Конвертируем стоимость в value + scaler для списания
        (int costValue, string costScaler) = ConvertDoubleToBalance(upgradeCost);
        
        // Проверяем баланс игрока
        if (GameStorage.Instance == null)
        {
            Debug.LogError("[GradePanel] GameStorage.Instance не найден!");
            return;
        }
        
        // Пытаемся списать баланс
        bool success = GameStorage.Instance.SubtractBalanceWithScaler(costValue, costScaler);
        
        if (success)
        {
            // Увеличиваем уровень брейнрота
            placedBrainrot.SetLevel(currentLevel + 1);
            
            if (debug)
            {
                string costFormatted = FormatBalance(costValue, costScaler);
                Debug.Log($"[GradePanel] Брейнрот улучшен с уровня {currentLevel} до {currentLevel + 1}. Стоимость: {costFormatted}");
            }
        }
        else
        {
            if (debug)
            {
                string costFormatted = FormatBalance(costValue, costScaler);
                double currentBalance = GameStorage.Instance.GetBalanceDouble();
                string balanceFormatted = GameStorage.Instance.FormatBalance();
                Debug.LogWarning($"[GradePanel] Недостаточно средств для улучшения! Требуется: {costFormatted}, Баланс: {balanceFormatted}");
            }
        }
    }
    
    
    /// <summary>
    /// Конвертирует double в баланс (value + scaler)
    /// Использует ту же логику, что и GameStorage
    /// </summary>
    private (int value, string scaler) ConvertDoubleToBalance(double balance)
    {
        if (balance <= 0)
        {
            return (0, "");
        }
        
        // Нониллионы (10^30)
        if (balance >= 1000000000000000000000000000000.0)
        {
            double nonillions = balance / 1000000000000000000000000000000.0;
            return ((int)nonillions, "NO");
        }
        // Октиллионы (10^27)
        else if (balance >= 1000000000000000000000000000.0)
        {
            double octillions = balance / 1000000000000000000000000000.0;
            return ((int)octillions, "OC");
        }
        // Септиллионы (10^24)
        else if (balance >= 1000000000000000000000000.0)
        {
            double septillions = balance / 1000000000000000000000000.0;
            return ((int)septillions, "SP");
        }
        // Секстиллионы (10^21)
        else if (balance >= 1000000000000000000000.0)
        {
            double sextillions = balance / 1000000000000000000000.0;
            return ((int)sextillions, "SX");
        }
        // Квинтиллионы (10^18)
        else if (balance >= 1000000000000000000.0)
        {
            double quintillions = balance / 1000000000000000000.0;
            return ((int)quintillions, "QI");
        }
        // Квадриллионы (10^15)
        else if (balance >= 1000000000000000.0)
        {
            double quadrillions = balance / 1000000000000000.0;
            return ((int)quadrillions, "QA");
        }
        // Триллионы (10^12)
        else if (balance >= 1000000000000.0)
        {
            double trillions = balance / 1000000000000.0;
            return ((int)trillions, "T");
        }
        // Миллиарды (10^9)
        else if (balance >= 1000000000.0)
        {
            double billions = balance / 1000000000.0;
            return ((int)billions, "B");
        }
        // Миллионы (10^6)
        else if (balance >= 1000000.0)
        {
            double millions = balance / 1000000.0;
            return ((int)millions, "M");
        }
        // Тысячи (10^3)
        else if (balance >= 1000.0)
        {
            double thousands = balance / 1000.0;
            return ((int)thousands, "K");
        }
        else
        {
            // Меньше тысячи - возвращаем как int
            return ((int)balance, "");
        }
    }
    
    /// <summary>
    /// Форматирует баланс для отображения (вспомогательный метод для логов)
    /// </summary>
    private string FormatBalance(int value, string scaler)
    {
        if (string.IsNullOrEmpty(scaler))
        {
            return value.ToString();
        }
        return $"{value}{scaler}";
    }
    
    /// <summary>
    /// Получить ID панели
    /// </summary>
    public int GetID()
    {
        return id;
    }
    
    /// <summary>
    /// Установить ID панели
    /// </summary>
    public void SetID(int newID)
    {
        id = newID;
        FindLinkedPlacementPanel();
    }
    
    /// <summary>
    /// Получить информацию о том, должна ли панель быть видима
    /// Используется другими скриптами (например, TrashPlacement) для синхронизации видимости
    /// </summary>
    public bool ShouldBeVisible()
    {
        // Проверяем наличие размещённого брейнрота
        bool hasBrainrot = placedBrainrot != null;
        
        // Если автоскрытие отключено, панель всегда видима
        if (disableAutoHide)
        {
            return hasBrainrot; // Но всё равно проверяем наличие брейнрота
        }
        
        // Если нет брейнрота, панель не видима
        if (!hasBrainrot)
        {
            return false;
        }
        
        // Проверяем расстояние до игрока
        Vector3 detectionPosition = GetDetectionPosition();
        bool playerInRange = false;
        
        if (playerTransform != null)
        {
            Vector3 playerWorldPosition = playerTransform.position;
            float distance;
            
            if (useHorizontalDistance)
            {
                Vector2 detectionPos2D = new Vector2(detectionPosition.x, detectionPosition.z);
                Vector2 playerPos2D = new Vector2(playerWorldPosition.x, playerWorldPosition.z);
                distance = Vector2.Distance(detectionPos2D, playerPos2D);
            }
            else
            {
                distance = Vector3.Distance(detectionPosition, playerWorldPosition);
            }
            
            playerInRange = distance <= range;
        }
        else
        {
            // Если игрок не найден, пытаемся найти его
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Vector3 playerWorldPosition = playerTransform.position;
                float distance;
                
                if (useHorizontalDistance)
                {
                    Vector2 detectionPos2D = new Vector2(detectionPosition.x, detectionPosition.z);
                    Vector2 playerPos2D = new Vector2(playerWorldPosition.x, playerWorldPosition.z);
                    distance = Vector2.Distance(detectionPos2D, playerPos2D);
                }
                else
                {
                    distance = Vector3.Distance(detectionPosition, playerWorldPosition);
                }
                
                playerInRange = distance <= range;
            }
        }
        
        return hasBrainrot && playerInRange;
    }
    
    /// <summary>
    /// Проверяет, видима ли панель визуально (через Renderer компоненты)
    /// </summary>
    public bool IsVisuallyVisible()
    {
        if (renderers == null || renderers.Length == 0)
        {
            return gameObject.activeSelf; // Если нет Renderer, используем активность объекта
        }
        
        // Проверяем, включен ли хотя бы один Renderer
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                return true;
            }
        }
        
        return false;
    }
}
