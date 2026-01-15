using UnityEngine;
using System.Reflection;

/// <summary>
/// Панель для размещения brainrot объектов.
/// Когда игрок находится в зоне панели и зажимает E с brainrot в руках,
/// объект размещается на панели вместо земли.
/// </summary>
public class PlacementPanel : InteractableObject
{
    [Header("Настройки размещения на панели")]
    [Tooltip("ID панели для связи с EarnPanel")]
    [SerializeField] private int panelID = 0;
    
    [Tooltip("Точка размещения на панели (если не назначена, используется центр панели)")]
    [SerializeField] private Transform placementPoint;
    
    [Tooltip("Высота размещения над панелью")]
    [SerializeField] private float placementHeight = 0.1f;
    
    // Статическая ссылка на активную панель размещения
    private static PlacementPanel activePanel = null;
    
    // Статический список всех панелей для определения ближайшей
    private static System.Collections.Generic.List<PlacementPanel> allPanels = new System.Collections.Generic.List<PlacementPanel>();
    
    // Статическая переменная для кэширования ближайшей панели (обновляется один раз на кадр)
    private static PlacementPanel cachedClosestPanel = null;
    private static int lastClosestPanelUpdateFrame = -1;
    
    private PlayerCarryController playerCarryController;
    
    // Размещённый brainrot объект на этой панели
    private BrainrotObject placedBrainrot = null;
    
    private Collider panelCollider;
    
    // Флаг, указывающий, является ли эта панель ближайшей к игроку
    private bool isClosestPanel = false;
    
    private void Awake()
    {
        // Регистрируем панель в статическом списке
        if (!allPanels.Contains(this))
        {
            allPanels.Add(this);
        }
        
        // Находим PlayerCarryController
        FindPlayerCarryController();
        
        // Находим коллайдер панели для правильного позиционирования UI
        panelCollider = GetComponent<Collider>();
        if (panelCollider == null)
        {
            panelCollider = GetComponentInChildren<Collider>();
        }
        
        // Создаем interactionPoint в центре коллайдера для правильного позиционирования UI
        // Это гарантирует, что UI будет позиционироваться относительно правильной мировой позиции
        if (panelCollider != null)
        {
            CreateInteractionPoint();
        }
    }
    
    private void OnEnable()
    {
        // Регистрируем панель при включении
        if (!allPanels.Contains(this))
        {
            allPanels.Add(this);
            // Сбрасываем кэш при добавлении новой панели
            ResetClosestPanelCache();
        }
    }
    
    private void OnDisable()
    {
        // Отменяем регистрацию при выключении
        allPanels.Remove(this);
        
        // Сбрасываем кэш при удалении панели
        ResetClosestPanelCache();
        
        // Если эта панель была активной, снимаем регистрацию
        if (activePanel == this)
        {
            activePanel = null;
        }
        
        isClosestPanel = false;
    }
    
    /// <summary>
    /// Сбрасывает кэш ближайшей панели (вызывается при изменении списка панелей)
    /// </summary>
    private static void ResetClosestPanelCache()
    {
        cachedClosestPanel = null;
        lastClosestPanelUpdateFrame = -1;
    }
    
    /// <summary>
    /// Создает точку взаимодействия в центре коллайдера панели
    /// </summary>
    private void CreateInteractionPoint()
    {
        // Используем рефлексию для установки interactionPoint
        FieldInfo interactionPointField = typeof(InteractableObject).GetField("interactionPoint", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (interactionPointField != null)
        {
            Transform existingPoint = interactionPointField.GetValue(this) as Transform;
            
            // Если точка взаимодействия еще не создана, создаем её
            if (existingPoint == null)
            {
                GameObject interactionPointObj = new GameObject("InteractionPoint_" + gameObject.name);
                // ВАЖНО: НЕ устанавливаем родителя, чтобы избежать проблем с локальными координатами
                // interactionPointObj.transform.SetParent(null);
                
                // Устанавливаем мировую позицию в центр коллайдера
                // Используем bounds.center, который всегда возвращает мировую позицию
                Vector3 worldCenter = panelCollider.bounds.center;
                interactionPointObj.transform.position = worldCenter;
                
                // Устанавливаем точку взаимодействия через рефлексию
                interactionPointField.SetValue(this, interactionPointObj.transform);
                
                Debug.Log($"[PlacementPanel] Создана точка взаимодействия в позиции {worldCenter} (центр коллайдера панели)");
            }
            else
            {
                // Если точка уже существует, обновляем её позицию
                Vector3 worldCenter = panelCollider.bounds.center;
                existingPoint.position = worldCenter;
            }
        }
    }
    
    protected override void Update()
    {
        // Определяем ближайшую панель к игроку
        DetermineClosestPanel();
        
        // Проверяем, есть ли размещённый brainrot на панели
        CheckPlacedBrainrot();
        
        // Проверяем, есть ли brainrot в руках у игрока
        bool hasBrainrotInHands = false;
        if (playerCarryController == null)
        {
            FindPlayerCarryController();
        }
        if (playerCarryController != null)
        {
            hasBrainrotInHands = playerCarryController.GetCurrentCarriedObject() != null;
        }
        
        // Всегда вызываем base.Update() для проверки расстояния до игрока
        // Это нужно, чтобы isPlayerInRange обновлялся корректно
        base.Update();
        
        // Показываем UI если:
        // 1. Эта панель является ближайшей к игроку
        // 2. Игрок в радиусе взаимодействия
        // 3. Есть размещённый brainrot на панели (чтобы можно было взять обратно) ИЛИ у игрока в руках есть brainrot (чтобы можно было разместить)
        bool shouldShowUI = isClosestPanel && isPlayerInRange && (placedBrainrot != null || hasBrainrotInHands);
        
        // Если эта панель не ближайшая, принудительно отключаем взаимодействие
        if (!isClosestPanel)
        {
            // Используем рефлексию для установки isPlayerInRange = false
            System.Reflection.FieldInfo isPlayerInRangeField = typeof(InteractableObject).GetField("isPlayerInRange", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isPlayerInRangeField != null)
            {
                isPlayerInRangeField.SetValue(this, false);
            }
            
            // Скрываем UI если он показан
            if (HasUI())
            {
                HideUI();
            }
            
            // Если эта панель была активной, снимаем регистрацию (игрок не в зоне этой панели)
            if (activePanel == this)
            {
                activePanel = null;
            }
            return;
        }
        
        // Управляем видимостью UI в зависимости от условий
        if (!shouldShowUI)
        {
            // Нет ни размещённого brainrot, ни brainrot в руках - скрываем UI
            if (HasUI())
            {
                HideUI();
            }
        }
        else
        {
            // Условия выполнены - убеждаемся, что UI показан (base.Update() уже вызван, он создаст UI если нужно)
            // Дополнительно проверяем, что UI создан и показан
            if (!HasUI() && isPlayerInRange)
            {
                // Если UI не создан, но игрок в радиусе, создаём его
                // base.Update() должен был создать UI, но на всякий случай проверяем
            }
        }
        
        // Регистрируем/снимаем регистрацию панели в зависимости от того, находится ли игрок в зоне
        // (только для ближайшей панели)
        if (isPlayerInRange)
        {
            // Игрок в зоне - регистрируем эту панель как активную
            if (activePanel != this)
            {
                activePanel = this;
            }
        }
        else
        {
            // Игрок не в зоне - если эта панель была активной, снимаем регистрацию
            if (activePanel == this)
            {
                activePanel = null;
            }
        }
    }
    
    /// <summary>
    /// Определяет ближайшую панель к игроку из всех панелей в радиусе
    /// Оптимизировано: вычисляется один раз на кадр для всех панелей
    /// </summary>
    private void DetermineClosestPanel()
    {
        // Если ближайшая панель уже определена в этом кадре, используем кэш
        if (lastClosestPanelUpdateFrame == Time.frameCount && cachedClosestPanel != null)
        {
            isClosestPanel = (cachedClosestPanel == this);
            return;
        }
        
        // Получаем Transform игрока
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            isClosestPanel = false;
            cachedClosestPanel = null;
            lastClosestPanelUpdateFrame = Time.frameCount;
            return;
        }
        
        // Находим все панели в радиусе взаимодействия
        PlacementPanel closestPanel = null;
        float closestDistance = float.MaxValue;
        
        // Получаем радиус взаимодействия через рефлексию (используем значение из первой панели)
        System.Reflection.FieldInfo interactionRangeField = typeof(InteractableObject).GetField("interactionRange", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float interactionRange = 3f; // Значение по умолчанию
        if (interactionRangeField != null)
        {
            interactionRange = (float)interactionRangeField.GetValue(this);
        }
        
        // Проходим по всем панелям и находим ближайшую
        foreach (PlacementPanel panel in allPanels)
        {
            if (panel == null || !panel.gameObject.activeInHierarchy) continue;
            
            // Получаем позицию панели
            Vector3 panelPosition = panel.GetPlacementPosition();
            float distance = Vector3.Distance(playerTransform.position, panelPosition);
            
            // Если панель в радиусе взаимодействия и ближе текущей ближайшей
            if (distance <= interactionRange && distance < closestDistance)
            {
                closestPanel = panel;
                closestDistance = distance;
            }
        }
        
        // Кэшируем результат
        cachedClosestPanel = closestPanel;
        lastClosestPanelUpdateFrame = Time.frameCount;
        
        // Устанавливаем флаг для этой панели
        isClosestPanel = (closestPanel == this);
    }
    
    /// <summary>
    /// Получает Transform игрока
    /// </summary>
    private Transform GetPlayerTransform()
    {
        // Пытаемся получить через PlayerCarryController
        if (playerCarryController == null)
        {
            FindPlayerCarryController();
        }
        
        if (playerCarryController != null)
        {
            Transform playerTransform = playerCarryController.GetPlayerTransform();
            if (playerTransform != null)
            {
                return playerTransform;
            }
        }
        
        // Если не получилось, используем рефлексию для получения из базового класса
        System.Reflection.FieldInfo playerTransformField = typeof(InteractableObject).GetField("playerTransform", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerTransformField != null)
        {
            return playerTransformField.GetValue(this) as Transform;
        }
        
        return null;
    }
    
    /// <summary>
    /// Проверяет, есть ли размещённый brainrot на панели
    /// </summary>
    private void CheckPlacedBrainrot()
    {
        // Если уже есть размещённый brainrot, проверяем, что он всё ещё размещён
        if (placedBrainrot != null)
        {
            // Проверяем, что объект существует, всё ещё размещён и не взят
            if (placedBrainrot == null || !placedBrainrot.IsPlaced() || placedBrainrot.IsCarried())
            {
                // Объект больше не размещён или взят - очищаем ссылку
                placedBrainrot = null;
            }
            else
            {
                // Проверяем расстояние до объекта - если он слишком далеко от панели, он больше не на панели
                // ВАЖНО: Увеличиваем радиус проверки до 3 единиц для более надёжного определения
                float distance = Vector3.Distance(placedBrainrot.transform.position, GetPlacementPosition());
                if (distance > 3f) // Если объект дальше 3 единиц от центра панели
                {
                    placedBrainrot = null;
                }
            }
        }
        else
        {
            // Ищем размещённые brainrot объекты рядом с панелью
            // ВАЖНО: Ищем только среди объектов, которые НЕ размещены на других панелях
            // Это предотвращает конфликты, когда один объект может быть найден несколькими панелями
            BrainrotObject[] allBrainrots = FindObjectsByType<BrainrotObject>(FindObjectsSortMode.None);
            Vector3 panelCenter = GetPlacementPosition();
            
            foreach (BrainrotObject brainrot in allBrainrots)
            {
                if (brainrot != null && brainrot.IsPlaced() && !brainrot.IsCarried())
                {
                    // ВАЖНО: Проверяем, не размещён ли этот объект уже на другой панели
                    // Если он уже размещён на другой панели, пропускаем его
                    bool alreadyOnAnotherPanel = false;
                    foreach (PlacementPanel otherPanel in allPanels)
                    {
                        if (otherPanel != null && otherPanel != this && otherPanel.placedBrainrot == brainrot)
                        {
                            alreadyOnAnotherPanel = true;
                            break;
                        }
                    }
                    
                    if (alreadyOnAnotherPanel)
                    {
                        continue; // Пропускаем объекты, которые уже размещены на других панелях
                    }
                    
                    // Проверяем расстояние до панели
                    // ВАЖНО: Увеличиваем радиус поиска до 3 единиц для более надёжного определения
                    float distance = Vector3.Distance(brainrot.transform.position, panelCenter);
                    if (distance < 3f) // Если объект близко к панели (в радиусе 3 единиц)
                    {
                        // Нашли размещённый brainrot на этой панели
                        placedBrainrot = brainrot;
                        break;
                    }
                }
            }
        }
    }
    
    private void LateUpdate()
    {
        // Принудительно обновляем кэшированную позицию взаимодействия ПОСЛЕ всех обновлений базового класса
        // Это важно, так как CheckPlayerDistance() в базовом Update() может перезаписать позицию
        // Вызываем в LateUpdate, чтобы наше обновление было последним
        UpdateInteractionPosition();
    }
    
    /// <summary>
    /// Принудительно обновляет кэшированную позицию взаимодействия
    /// Использует центр коллайдера панели для правильного позиционирования UI
    /// </summary>
    private void UpdateInteractionPosition()
    {
        if (panelCollider == null) return;
        
        // ВАЖНО: Всегда используем bounds.center как источник истины
        // bounds.center всегда возвращает правильную мировую позицию независимо от масштаба родителя
        Vector3 correctPosition = panelCollider.bounds.center;
        
        // Обновляем interactionPoint, если он существует
        FieldInfo interactionPointField = typeof(InteractableObject).GetField("interactionPoint", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (interactionPointField != null)
        {
            Transform interactionPointTransform = interactionPointField.GetValue(this) as Transform;
            if (interactionPointTransform != null)
            {
                // Устанавливаем правильную мировую позицию
                interactionPointTransform.position = correctPosition;
            }
        }
        
        // Обновляем cachedInteractionPosition через рефлексию
        FieldInfo cachedPositionField = typeof(InteractableObject).GetField("cachedInteractionPosition", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (cachedPositionField != null)
        {
            cachedPositionField.SetValue(this, correctPosition);
        }
        
        // Также обновляем позицию UI напрямую, если он уже создан
        // Это исправляет проблему, если UI был создан с неправильной позицией
        FieldInfo currentUIInstanceField = typeof(InteractableObject).GetField("currentUIInstance", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (currentUIInstanceField != null)
        {
            GameObject currentUI = currentUIInstanceField.GetValue(this) as GameObject;
            if (currentUI != null && currentUI.activeSelf)
            {
                // Получаем uiOffset через рефлексию
                FieldInfo uiOffsetField = typeof(InteractableObject).GetField("uiOffset", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (uiOffsetField != null)
                {
                    Vector3 uiOffset = (Vector3)uiOffsetField.GetValue(this);
                    // Вычисляем правильную позицию UI с учетом offset
                    Vector3 uiPosition = correctPosition + uiOffset;
                    currentUI.transform.position = uiPosition;
                }
            }
        }
    }
    
    /// <summary>
    /// Находит PlayerCarryController в сцене
    /// </summary>
    private void FindPlayerCarryController()
    {
        if (playerCarryController == null)
        {
            playerCarryController = FindFirstObjectByType<PlayerCarryController>();
        }
    }
    
    /// <summary>
    /// Переопределяем CompleteInteraction для обработки размещения на панели
    /// </summary>
    protected override void CompleteInteraction()
    {
        // Проверяем, есть ли brainrot в руках
        if (playerCarryController == null)
        {
            FindPlayerCarryController();
            if (playerCarryController == null)
            {
                Debug.LogWarning("[PlacementPanel] PlayerCarryController не найден!");
                base.CompleteInteraction();
                return;
            }
        }
        
        BrainrotObject carriedObject = playerCarryController.GetCurrentCarriedObject();
        
        if (carriedObject != null)
        {
            // Размещаем объект на панели
            PlaceOnPanel(carriedObject);
            
            // Сбрасываем состояние взаимодействия
            ResetInteraction();
        }
        else if (placedBrainrot != null)
        {
            // Если нет объекта в руках, но есть размещённый brainrot - берём его обратно
            TakePlacedBrainrot();
            
            // Сбрасываем состояние взаимодействия
            ResetInteraction();
        }
        else
        {
            // Если нет объекта в руках и нет размещённого brainrot, вызываем базовую логику
            base.CompleteInteraction();
        }
    }
    
    /// <summary>
    /// Берёт размещённый brainrot обратно в руки
    /// </summary>
    private void TakePlacedBrainrot()
    {
        if (placedBrainrot == null)
        {
            return;
        }
        
        if (playerCarryController == null)
        {
            FindPlayerCarryController();
            if (playerCarryController == null)
            {
                Debug.LogWarning("[PlacementPanel] PlayerCarryController не найден!");
                return;
            }
        }
        
        // Проверяем, может ли игрок взять объект
        if (!playerCarryController.CanCarry())
        {
            Debug.Log("[PlacementPanel] Игрок уже несет другой объект!");
            return;
        }
        
        // Берём объект обратно в руки
        placedBrainrot.Take();
        
        // Очищаем ссылку на размещённый объект
        placedBrainrot = null;
        
        Debug.Log("[PlacementPanel] Размещённый brainrot взят обратно в руки");
    }
    
    /// <summary>
    /// Размещает brainrot объект на панели с учетом всех настроек размещения
    /// </summary>
    public void PlaceOnPanel(BrainrotObject brainrotObject)
    {
        if (brainrotObject == null)
        {
            Debug.LogWarning("[PlacementPanel] Попытка разместить null объект!");
            return;
        }
        
        if (playerCarryController == null)
        {
            FindPlayerCarryController();
            if (playerCarryController == null)
            {
                Debug.LogWarning("[PlacementPanel] PlayerCarryController не найден!");
                return;
            }
        }
        
        // Получаем позицию панели для размещения
        Vector3 panelPosition = GetPlacementPosition();
        
        // Получаем ориентацию панели
        Vector3 panelForward = transform.forward;
        Vector3 panelRight = transform.right;
        Vector3 panelUp = transform.up;
        
        // Вычисляем позицию размещения с учетом смещений из BrainrotObject
        float putOffsetX = brainrotObject.GetPutOffsetX();
        float putOffsetZ = brainrotObject.GetPutOffsetZ();
        float placementOffsetX = brainrotObject.GetPlacementOffsetX();
        float placementOffsetZ = brainrotObject.GetPlacementOffsetZ();
        
        // Вычисляем финальную позицию относительно панели
        Vector3 placementPosition = panelPosition + 
                                   panelForward * (putOffsetZ + placementOffsetZ) + 
                                   panelRight * (putOffsetX + placementOffsetX) +
                                   panelUp * placementHeight;
        
        // Используем Raycast для определения точной позиции на поверхности панели
        RaycastHit hit;
        if (Physics.Raycast(placementPosition + panelUp * 0.5f, -panelUp, out hit, 1f))
        {
            // Если нашли поверхность, используем точку попадания
            placementPosition = hit.point + panelUp * placementHeight;
        }
        
        // Вычисляем поворот объекта при размещении
        float placementRotationY = brainrotObject.GetPlacementRotationY();
        Quaternion placementRotation;
        
        if (Mathf.Abs(placementRotationY) > 0.01f)
        {
            // Поворачиваем относительно панели
            Quaternion panelRotation = transform.rotation;
            Quaternion additionalRotation = Quaternion.Euler(0f, placementRotationY, 0f);
            placementRotation = panelRotation * additionalRotation;
        }
        else
        {
            // Если поворот не задан, используем поворот панели
            placementRotation = transform.rotation;
        }
        
        // ВАЖНО: Сохраняем ссылку на размещённый объект ПЕРЕД размещением
        // Это критично, чтобы IsBrainrotPlacedOnPanel() могла правильно определить, что объект размещён на панели
        // НЕ очищаем старую ссылку - если на панели уже был объект, он должен быть заменён
        placedBrainrot = brainrotObject;
        
        // Используем метод PutAtPosition из BrainrotObject для размещения
        // Этот метод установит позицию, поворот, масштаб, включит физику и коллайдеры,
        // освободит объект из рук и установит состояние
        // ВАЖНО: PutAtPosition() внутри проверяет IsBrainrotPlacedOnPanel(), которая должна найти этот объект
        brainrotObject.PutAtPosition(placementPosition, placementRotation);
        
        // ВАЖНО: После размещения принудительно устанавливаем ссылку ещё раз
        // Это гарантирует, что даже если CheckPlacedBrainrot() очистит ссылку в том же кадре,
        // она будет восстановлена
        placedBrainrot = brainrotObject;
        
        Debug.Log($"[PlacementPanel] Объект {brainrotObject.GetObjectName()} размещен на панели в позиции {placementPosition}, placedBrainrot установлен: {placedBrainrot != null}, ID панели: {panelID}");
    }
    
    /// <summary>
    /// Получает позицию размещения на панели
    /// </summary>
    public Vector3 GetPlacementPosition()
    {
        if (placementPoint != null)
        {
            return placementPoint.position;
        }
        
        // Используем центр коллайдера панели, если есть
        Collider panelCollider = GetComponent<Collider>();
        if (panelCollider != null)
        {
            return panelCollider.bounds.center;
        }
        
        // Иначе используем позицию панели
        return transform.position;
    }
    
    /// <summary>
    /// Получить активную панель размещения
    /// </summary>
    public static PlacementPanel GetActivePanel()
    {
        return activePanel;
    }
    
    /// <summary>
    /// Получить панель по ID
    /// </summary>
    public static PlacementPanel GetPanelByID(int id)
    {
        foreach (PlacementPanel panel in allPanels)
        {
            if (panel != null && panel.GetPanelID() == id)
            {
                return panel;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Проверить, является ли эта панель активной
    /// </summary>
    public bool IsActive()
    {
        return activePanel == this;
    }
    
    /// <summary>
    /// Получить ID панели
    /// </summary>
    public int GetPanelID()
    {
        return panelID;
    }
    
    /// <summary>
    /// Установить ID панели
    /// </summary>
    public void SetPanelID(int id)
    {
        panelID = id;
    }
    
    /// <summary>
    /// Проверяет, размещён ли указанный brainrot на какой-либо панели
    /// </summary>
    public static bool IsBrainrotPlacedOnPanel(BrainrotObject brainrot)
    {
        if (brainrot == null || !brainrot.IsPlaced() || brainrot.IsCarried())
        {
            return false;
        }
        
        // Проходим по всем панелям и проверяем, размещён ли brainrot на какой-либо из них
        foreach (PlacementPanel panel in allPanels)
        {
            if (panel != null && panel.placedBrainrot == brainrot)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Получить размещённый brainrot объект на этой панели
    /// </summary>
    public BrainrotObject GetPlacedBrainrot()
    {
        return placedBrainrot;
    }
    
    private void OnDestroy()
    {
        // Отменяем регистрацию панели при уничтожении
        allPanels.Remove(this);
        
        // Сбрасываем кэш при удалении панели
        ResetClosestPanelCache();
        
        // При уничтожении панели снимаем регистрацию, если она была активной
        if (activePanel == this)
        {
            activePanel = null;
        }
    }
}
