using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Объект, который можно взять и разместить.
/// Наследуется от InteractableObject для использования системы взаимодействия с UI подсказкой.
/// </summary>
public class BrainrotObject : InteractableObject
{
    [Header("Brainrot Object Settings")]
    [SerializeField] private string objectName = "Brainrot Object";
    
    [Tooltip("Редкость объекта")]
    [SerializeField] private int rarity = 1;
    
    [Tooltip("Радиус для взятия объекта")]
    [SerializeField] private float takerange = 3f;
    
    [Header("Настройки переноски")]
    [Tooltip("Смещение объекта при переноске по оси X (влево/вправо относительно игрока)")]
    [SerializeField] private float carryOffsetX = 0f;
    
    [Tooltip("Смещение объекта при переноске по оси Y (вверх/вниз относительно игрока)")]
    [SerializeField] private float carryOffsetY = 0f;
    
    [Tooltip("Смещение объекта при переноске по оси Z (вперед/назад относительно игрока)")]
    [SerializeField] private float carryOffsetZ = 0f;
    
    [Tooltip("Поворот объекта при переноске по оси Y (в градусах)")]
    [SerializeField] private float carryRotationY = 0f;
    
    [Header("Настройки размещения")]
    [Tooltip("Смещение объекта при размещении по оси X (влево/вправо относительно игрока)")]
    [SerializeField] private float putOffsetX = 0f;
    
    [Tooltip("Смещение объекта при размещении по оси Z (вперед/назад относительно игрока)")]
    [SerializeField] private float putOffsetZ = 1.5f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onTake;
    [SerializeField] private UnityEvent onPut;
    
    // Состояние объекта
    private bool isCarried = false;
    private bool isPlaced = false;
    private PlayerCarryController playerCarryController;
    
    // Кэшированные компоненты для оптимизации
    private Rigidbody cachedRigidbody;
    private Collider[] cachedColliders;
    private bool componentsCached = false;
    
    private void Awake()
    {
        // Находим PlayerCarryController
        FindPlayerCarryController();
        
        // Кэшируем компоненты один раз
        CacheComponents();
        
        // НЕ устанавливаем параметры автоматически - они должны быть установлены в инспекторе
        // Параметр takerange используется только как справочное значение
        // Пользователь должен вручную установить interactionRange и interactionTime в инспекторе
        // или синхронизировать их с takerange через OnValidate (только в редакторе)
    }
    
    /// <summary>
    /// Кэширует компоненты для оптимизации
    /// </summary>
    private void CacheComponents()
    {
        if (componentsCached) return;
        
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedColliders = GetComponentsInChildren<Collider>();
        componentsCached = true;
    }
    
    private void Start()
    {
        // Убеждаемся, что PlayerCarryController найден
        if (playerCarryController == null)
        {
            FindPlayerCarryController();
        }
    }
    
    protected override void Update()
    {
        // Вызываем базовый Update для обработки взаимодействия и UI
        base.Update();
        
        // Если объект взят, скрываем UI подсказку
        // Но только если UI существует и виден (оптимизация - проверяем только если объект взят)
        if (isCarried)
        {
            if (HasUI())
            {
                HideUI();
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
    /// Переопределяем CompleteInteraction для обработки Take/Put
    /// </summary>
    protected override void CompleteInteraction()
    {
        if (isCarried)
        {
            // Если объект уже взят, кладем его
            Put();
            // После Put НЕ вызываем base.CompleteInteraction() - это установит interactionCompleted = true
            // Вместо этого просто сбрасываем состояние, чтобы UI мог появиться снова
            // UI уже скрыт в методе Put() через ResetInteraction()
        }
        else
        {
            // Если объект не взят, берем его
            Take();
            
            // После Take НЕ вызываем base.CompleteInteraction() - UI должен остаться видимым
            // Вместо этого сбрасываем только состояние взаимодействия, чтобы можно было взаимодействовать снова
            ResetInteraction();
            
            // Вызываем событие базового класса вручную
            // Базовый класс вызывает onInteractionComplete.Invoke() в CompleteInteraction()
            // Но так как мы не вызываем base.CompleteInteraction(), нужно вызвать событие здесь
            // Однако OnInteractionComplete() не вызывает событие, поэтому вызываем его напрямую через рефлексию
            // Или просто не вызываем - событие onTake уже вызвано в методе Take()
        }
    }
    
    /// <summary>
    /// Переопределяем OnInteractionComplete для дополнительной логики
    /// </summary>
    protected override void OnInteractionComplete()
    {
        // Базовая логика уже обработана в CompleteInteraction()
    }
    
    /// <summary>
    /// Взять объект в руки
    /// </summary>
    public void Take()
    {
        if (playerCarryController == null)
        {
            FindPlayerCarryController();
            if (playerCarryController == null)
            {
                Debug.LogWarning($"[BrainrotObject] {objectName}: PlayerCarryController не найден!");
                return;
            }
        }
        
        // Проверяем, может ли игрок взять объект
        if (!playerCarryController.CanCarry())
        {
            Debug.Log($"[BrainrotObject] {objectName}: Игрок уже несет другой объект!");
            return;
        }
        
        // Убеждаемся, что компоненты кэшированы
        if (!componentsCached)
        {
            CacheComponents();
        }
        
        // Устанавливаем состояние
        isCarried = true;
        isPlaced = false;
        
        // Отключаем физику объекта (если есть) - используем кэшированный компонент
        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
        }
        
        // Отключаем коллайдеры (чтобы объект не мешал движению) - используем кэшированный массив
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = false;
                }
            }
        }
        
        // Передаем объект в PlayerCarryController
        playerCarryController.CarryObject(this);
        
        // Скрываем UI подсказку когда объект взят
        HideUI();
        
        // Вызываем событие
        onTake.Invoke();
        
        Debug.Log($"[BrainrotObject] {objectName}: Объект взят игроком");
    }
    
    /// <summary>
    /// Положить объект на землю
    /// </summary>
    public void Put()
    {
        if (playerCarryController == null)
        {
            Debug.LogWarning($"[BrainrotObject] {objectName}: PlayerCarryController не найден!");
            return;
        }
        
        // Получаем позицию игрока и направление вперед
        Transform playerTransform = playerCarryController.GetPlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogWarning($"[BrainrotObject] {objectName}: Transform игрока не найден!");
            return;
        }
        
        // Вычисляем позицию перед игроком с учетом смещений
        Vector3 forwardDirection = playerTransform.forward;
        Vector3 rightDirection = playerTransform.right;
        Vector3 putPosition = playerTransform.position + 
                             forwardDirection * putOffsetZ + 
                             rightDirection * putOffsetX;
        
        // Используем Raycast для определения позиции на земле
        RaycastHit hit;
        if (Physics.Raycast(putPosition + Vector3.up * 2f, Vector3.down, out hit, 5f))
        {
            putPosition = hit.point;
        }
        else
        {
            // Если Raycast не нашел землю, используем позицию игрока с небольшим смещением вниз
            putPosition.y = playerTransform.position.y;
        }
        
        // Устанавливаем позицию объекта
        transform.position = putPosition;
        
        // Убеждаемся, что компоненты кэшированы
        if (!componentsCached)
        {
            CacheComponents();
        }
        
        // Включаем физику обратно - используем кэшированный компонент
        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = false;
        }
        
        // Включаем коллайдеры обратно - используем кэшированный массив
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = true;
                }
            }
        }
        
        // Устанавливаем состояние
        isCarried = false;
        isPlaced = true;
        
        // Освобождаем объект из PlayerCarryController
        playerCarryController.DropObject();
        
        // Сбрасываем состояние взаимодействия, чтобы UI мог появиться снова при следующем приближении
        ResetInteraction();
        
        // Вызываем событие
        onPut.Invoke();
        
        Debug.Log($"[BrainrotObject] {objectName}: Объект размещен на позиции {putPosition}");
    }
    
    
    /// <summary>
    /// Получить имя объекта
    /// </summary>
    public string GetObjectName()
    {
        return objectName;
    }
    
    /// <summary>
    /// Получить редкость объекта
    /// </summary>
    public int GetRarity()
    {
        return rarity;
    }
    
    /// <summary>
    /// Проверить, взят ли объект
    /// </summary>
    public bool IsCarried()
    {
        return isCarried;
    }
    
    /// <summary>
    /// Проверить, размещен ли объект
    /// </summary>
    public bool IsPlaced()
    {
        return isPlaced;
    }
    
    /// <summary>
    /// Сбросить состояние объекта (для повторного использования)
    /// </summary>
    public void ResetState()
    {
        isCarried = false;
        isPlaced = false;
        
        // Убеждаемся, что компоненты кэшированы
        if (!componentsCached)
        {
            CacheComponents();
        }
        
        // Включаем коллайдеры - используем кэшированный массив
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Получить смещение по X при переноске
    /// </summary>
    public float GetCarryOffsetX()
    {
        return carryOffsetX;
    }
    
    /// <summary>
    /// Получить смещение по Y при переноске
    /// </summary>
    public float GetCarryOffsetY()
    {
        return carryOffsetY;
    }
    
    /// <summary>
    /// Получить смещение по Z при переноске
    /// </summary>
    public float GetCarryOffsetZ()
    {
        return carryOffsetZ;
    }
    
    /// <summary>
    /// Получить поворот по Y при переноске
    /// </summary>
    public float GetCarryRotationY()
    {
        return carryRotationY;
    }
    
    /// <summary>
    /// Установить смещение по X при переноске
    /// </summary>
    public void SetCarryOffsetX(float offsetX)
    {
        carryOffsetX = offsetX;
    }
    
    /// <summary>
    /// Установить смещение по Y при переноске
    /// </summary>
    public void SetCarryOffsetY(float offsetY)
    {
        carryOffsetY = offsetY;
    }
    
    /// <summary>
    /// Установить смещение по Z при переноске
    /// </summary>
    public void SetCarryOffsetZ(float offsetZ)
    {
        carryOffsetZ = offsetZ;
    }
    
    /// <summary>
    /// Установить поворот по Y при переноске
    /// </summary>
    public void SetCarryRotationY(float rotationY)
    {
        carryRotationY = rotationY;
    }
    
    /// <summary>
    /// Получить смещение по X при размещении
    /// </summary>
    public float GetPutOffsetX()
    {
        return putOffsetX;
    }
    
    /// <summary>
    /// Получить смещение по Z при размещении
    /// </summary>
    public float GetPutOffsetZ()
    {
        return putOffsetZ;
    }
    
    /// <summary>
    /// Установить смещение по X при размещении
    /// </summary>
    public void SetPutOffsetX(float offsetX)
    {
        putOffsetX = offsetX;
    }
    
    /// <summary>
    /// Установить смещение по Z при размещении
    /// </summary>
    public void SetPutOffsetZ(float offsetZ)
    {
        putOffsetZ = offsetZ;
    }
}
