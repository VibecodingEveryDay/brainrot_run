using UnityEngine;

/// <summary>
/// Панель для удаления размещённого брейнрота.
/// При клике на панель удаляет брейнрот из PlacementPanel и из GameStorage.
/// </summary>
public class TrashPlacement : MonoBehaviour
{
    [Header("Настройки панели")]
    [Tooltip("Ссылка на GradePanel (для получения PlacementPanel и брейнрота)")]
    [SerializeField] private GradePanel gradePanel;
    
    [Tooltip("Временно отключить автоматическое скрытие панели (для тестирования). Видимость берётся из GradePanel.")]
    [SerializeField] private bool disableAutoHide = false;
    
    // Transform игрока (находится автоматически, не показывается в Inspector)
    private Transform playerTransform;
    
    [Header("Отладка")]
    [Tooltip("Показывать отладочные сообщения в консоли")]
    [SerializeField] private bool debug = false;
    
    // Ссылка на связанную PlacementPanel (получается через GradePanel)
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
        Debug.Log($"[TrashPlacement] Awake вызван на объекте: {gameObject.name}, Активен: {gameObject.activeSelf}");
        
        // Пытаемся найти игрока автоматически, если не назначен
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log($"[TrashPlacement] Игрок найден автоматически: {player.name}, позиция: {playerTransform.position}");
            }
            else
            {
                Debug.LogWarning($"[TrashPlacement] Игрок не найден по тегу 'Player'! Назначьте playerTransform в инспекторе.");
            }
        }
        else
        {
            Debug.Log($"[TrashPlacement] Игрок назначен вручную: {playerTransform.name}, позиция: {playerTransform.position}");
        }
        
        // Проверяем наличие коллайдера для обработки кликов
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"[TrashPlacement] На объекте {gameObject.name} нет коллайдера! Клики не будут работать. Добавьте Collider для обработки кликов.");
            }
            else
            {
                Debug.Log($"[TrashPlacement] Коллайдер найден в дочернем объекте: {col.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"[TrashPlacement] Коллайдер найден на объекте: {col.gameObject.name}");
        }
        
        // Кэшируем все Renderer компоненты для визуального скрытия
        renderers = GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[TrashPlacement] Найдено Renderer компонентов: {renderers.Length}");
        
        // Кэшируем все Collider компоненты для отключения взаимодействия
        colliders = GetComponentsInChildren<Collider>(true);
        Debug.Log($"[TrashPlacement] Найдено Collider компонентов: {colliders.Length}");
    }
    
    private void Start()
    {
        Debug.Log($"[TrashPlacement] Start вызван на объекте: {gameObject.name}, Активен: {gameObject.activeSelf}");
        
        // Получаем PlacementPanel через GradePanel
        if (gradePanel != null)
        {
            // Используем рефлексию для получения linkedPlacementPanel из GradePanel
            var gradePanelType = typeof(GradePanel);
            var placementPanelField = gradePanelType.GetField("linkedPlacementPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (placementPanelField != null)
            {
                linkedPlacementPanel = placementPanelField.GetValue(gradePanel) as PlacementPanel;
                if (linkedPlacementPanel != null)
                {
                    Debug.Log($"[TrashPlacement] PlacementPanel получена через GradePanel: {linkedPlacementPanel.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[TrashPlacement] PlacementPanel не найдена в GradePanel. Убедитесь, что GradePanel правильно настроен.");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[TrashPlacement] GradePanel не назначена! Назначьте GradePanel в инспекторе.");
        }
    }
    
    private void Update()
    {
        // Логируем первый кадр для диагностики
        if (Time.frameCount == 1)
        {
            Debug.Log($"[TrashPlacement] Update вызван первый раз на объекте: {gameObject.name}, Активен: {gameObject.activeSelf}");
        }
        
        // Обновляем ссылку на размещённый brainrot объект
        UpdatePlacedBrainrot();
        
        // Проверяем видимость панели (расстояние до игрока и наличие брейнрота)
        UpdatePanelVisibility();
        
        // Обрабатываем клик мыши через Raycast (всегда, так как объект активен)
        HandleMouseClick();
    }
    
    /// <summary>
    /// Обновляет ссылку на размещённый brainrot объект
    /// </summary>
    private void UpdatePlacedBrainrot()
    {
        if (linkedPlacementPanel == null)
        {
            // Пытаемся получить PlacementPanel через GradePanel снова
            if (gradePanel != null)
            {
                var gradePanelType = typeof(GradePanel);
                var placementPanelField = gradePanelType.GetField("linkedPlacementPanel", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (placementPanelField != null)
                {
                    linkedPlacementPanel = placementPanelField.GetValue(gradePanel) as PlacementPanel;
                }
            }
            
            if (linkedPlacementPanel == null)
            {
                placedBrainrot = null;
                if (Time.frameCount % 120 == 0)
                {
                    Debug.LogWarning($"[TrashPlacement] PlacementPanel не найдена через GradePanel!");
                }
                return;
            }
        }
        
        // Получаем размещённый brainrot из связанной панели
        BrainrotObject previousBrainrot = placedBrainrot;
        placedBrainrot = linkedPlacementPanel.GetPlacedBrainrot();
        
        // Отладочная информация при изменении состояния брейнрота
        if (previousBrainrot != placedBrainrot)
        {
            if (placedBrainrot != null)
            {
                Debug.Log($"[TrashPlacement] Брейнрот размещён: {placedBrainrot.GetObjectName()}");
            }
            else if (previousBrainrot != null)
            {
                Debug.Log($"[TrashPlacement] Брейнрот удалён с панели");
            }
        }
    }
    
    /// <summary>
    /// Обновляет видимость панели, используя данные из GradePanel (чтобы не дублировать проверки)
    /// </summary>
    private void UpdatePanelVisibility()
    {
        // Получаем информацию о видимости из GradePanel
        bool shouldBeVisible = false;
        
        if (gradePanel != null)
        {
            // Проверяем, видима ли GradePanel визуально
            bool gradePanelVisible = gradePanel.IsVisuallyVisible();
            
            // Если GradePanel скрыта, то и TrashPlacement должен быть скрыт
            if (!gradePanelVisible)
            {
                shouldBeVisible = false;
            }
            else
            {
                // Если GradePanel видима, используем метод ShouldBeVisible() из GradePanel
                shouldBeVisible = gradePanel.ShouldBeVisible();
                
                // Если автоскрытие отключено в TrashPlacement, переопределяем
                if (disableAutoHide)
                {
                    shouldBeVisible = placedBrainrot != null;
                }
            }
        }
        else
        {
            // Если GradePanel не назначена, используем старую логику как запасной вариант
            bool hasBrainrot = placedBrainrot != null;
            shouldBeVisible = disableAutoHide || hasBrainrot;
            
            if (Time.frameCount % 120 == 0)
            {
                Debug.LogWarning("[TrashPlacement] GradePanel не назначена! Используется запасная логика видимости.");
            }
        }
        
        // Обновляем видимость панели (визуально, но объект остаётся активным)
        SetPanelVisibility(shouldBeVisible);
        
        // Логируем изменение видимости только при изменении состояния
        if (wasVisible != shouldBeVisible && Time.frameCount > 1)
        {
            wasVisible = shouldBeVisible;
            
            if (shouldBeVisible)
            {
                string reason = gradePanel != null ? "синхронизировано с GradePanel" : "запасная логика";
                Debug.Log($"[TrashPlacement] Панель ПОКАЗАНА (причина: {reason})");
            }
            else
            {
                string reason = gradePanel != null ? "GradePanel скрыта или синхронизировано" : "нет брейнрота";
                Debug.Log($"[TrashPlacement] Панель СКРЫТА (причина: {reason})");
            }
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
        
        // Получаем камеру
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera == null)
        {
            if (debug)
            {
                Debug.LogWarning("[TrashPlacement] Камера не найдена для обработки клика!");
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
            // Клик попал в панель - обрабатываем удаление
            ProcessDeletion();
        }
    }
    
    /// <summary>
    /// Обрабатывает удаление брейнрота
    /// </summary>
    private void ProcessDeletion()
    {
        // Проверяем, что есть размещённый брейнрот
        if (placedBrainrot == null)
        {
            if (debug)
            {
                Debug.Log("[TrashPlacement] Нет размещённого брейнрота для удаления!");
            }
            return;
        }
        
        string brainrotName = placedBrainrot.GetObjectName();
        
        // Удаляем брейнрот из GameStorage
        if (GameStorage.Instance != null)
        {
            bool removed = GameStorage.Instance.RemoveBrainrotByName(brainrotName);
            if (removed)
            {
                Debug.Log($"[TrashPlacement] Брейнрот '{brainrotName}' удалён из GameStorage");
            }
            else
            {
                if (debug)
                {
                    Debug.LogWarning($"[TrashPlacement] Брейнрот '{brainrotName}' не найден в GameStorage (возможно, уже удалён)");
                }
            }
        }
        else
        {
            Debug.LogError("[TrashPlacement] GameStorage.Instance не найден!");
        }
        
        // Удаляем объект из сцены
        Destroy(placedBrainrot.gameObject);
        
        // Очищаем ссылку
        placedBrainrot = null;
        
        Debug.Log($"[TrashPlacement] Брейнрот '{brainrotName}' удалён из сцены и GameStorage");
    }
}
