using UnityEngine;

/// <summary>
/// Компонент для управления движением стены и обработки коллизий
/// </summary>
public class WallMovement : MonoBehaviour
{
    private float speed;
    private float endPosZ;
    private WallSpawner spawner;
    private bool isInitialized = false;
    private bool hasCollided = false; // Флаг для предотвращения множественных срабатываний
    
    // Для проверки коллизии с игроком
    private Transform playerTransform;
    
    // Параметры проверки коллизии
    private const float minX = -42.2f;
    private const float maxX = 47.2f;
    private const float minY = -1f;
    [SerializeField] private float zTolerance = 3f; // Допустимая разница по Z для обнаружения коллизии (настраивается в Inspector)
    
    [Header("Debug")]
    [SerializeField] private bool debugCollision = false; // Включить отладку коллизий
    
    /// <summary>
    /// Инициализирует компонент движения стены
    /// </summary>
    public void Initialize(float wallSpeed, float endPositionZ, WallSpawner wallSpawner, float collisionZTolerance = 3f)
    {
        speed = wallSpeed;
        endPosZ = endPositionZ;
        spawner = wallSpawner;
        zTolerance = collisionZTolerance; // Устанавливаем допуск из параметра
        isInitialized = true;
        
        // Находим игрока
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Удаляем все коллайдеры у стены
        RemoveColliders();
    }
    
    /// <summary>
    /// Удаляет все коллайдеры у стены и её дочерних объектов
    /// </summary>
    private void RemoveColliders()
    {
        // Удаляем коллайдеры на корневом объекте
        Collider[] rootColliders = GetComponents<Collider>();
        foreach (Collider col in rootColliders)
        {
            Destroy(col);
        }
        
        // Удаляем коллайдеры в дочерних объектах
        Collider[] childColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in childColliders)
        {
            Destroy(col);
        }
    }
    
    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }
        
        // Движемся по оси Z
        transform.position += Vector3.back * speed * Time.deltaTime;
        
        // Проверяем коллизию с игроком через проверку координат
        if (!hasCollided && playerTransform != null)
        {
            CheckPlayerCollision();
        }
        
        // Проверяем, достигли ли конечной позиции
        if (transform.position.z <= endPosZ)
        {
            // Уничтожаем стену
            if (spawner != null)
            {
                spawner.RemoveWall(gameObject);
            }
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Проверяет коллизию с игроком по заданным условиям:
    /// X: от -42.2 до 47.2
    /// Y: > -1
    /// Z: позиция игрока и стены совпадают (с учетом движения стены)
    /// </summary>
    private void CheckPlayerCollision()
    {
        if (playerTransform == null)
        {
            return;
        }
        
        Vector3 playerPos = playerTransform.position;
        Vector3 wallPos = transform.position;
        
        // Проверка X: игрок должен быть в диапазоне от -42.2 до 47.2
        bool checkX = playerPos.x >= minX && playerPos.x <= maxX;
        
        // Проверка Y: игрок должен быть выше -1
        bool checkY = playerPos.y > minY;
        
        if (!checkX || !checkY)
        {
            return; // Если X или Y не подходят, дальше не проверяем
        }
        
        // Проверка Z: учитываем движение стены
        // Стена движется назад (по отрицательному Z)
        // Столкновение происходит только если Z игрока <= Z стены (игрок на одной линии или сзади стены)
        // Если Z игрока > Z стены (игрок впереди стены), столкновения быть не должно
        
        float zDifference = playerPos.z - wallPos.z; // Положительное = игрок впереди стены
        
        // Если игрок впереди стены, столкновения не происходит
        if (zDifference > 0)
        {
            if (debugCollision)
            {
                Debug.Log($"[WallMovement] Игрок впереди стены (Z разница: {zDifference:F2}), столкновение не происходит");
            }
            return;
        }
        
        // Игрок на одной линии или сзади стены - проверяем, что разница в пределах допуска
        bool checkZ = Mathf.Abs(zDifference) <= zTolerance;
        
        if (debugCollision)
        {
            Debug.Log($"[WallMovement] Проверка: X={checkX} ({playerPos.x:F2}), Y={checkY} ({playerPos.y:F2}), " +
                     $"Z разница={zDifference:F2} (игрок сзади/на линии), допуск={zTolerance}, результат={checkZ}");
        }
        
        // Если все условия выполнены, коллизия обнаружена
        if (checkZ)
        {
            if (debugCollision)
            {
                Debug.Log($"[WallMovement] КОЛЛИЗИЯ ОБНАРУЖЕНА! Игрок: {playerPos}, Стена: {wallPos}, Z разница: {zDifference:F2}");
            }
            OnPlayerCollision();
        }
    }
    
    /// <summary>
    /// Обрабатывает коллизию с игроком
    /// </summary>
    private void OnPlayerCollision()
    {
        if (hasCollided)
        {
            return; // Уже обработали коллизию
        }
        
        hasCollided = true;
        
        // Уведомляем спавнер о коллизии
        if (spawner != null)
        {
            spawner.OnWallCollisionWithPlayer();
        }
    }
}
