using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спавнер стен, которые движутся по оси Z и при столкновении с игроком телепортируют его в начало координат
/// </summary>
public class WallSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Интервал между спавнами стен (в секундах)")]
    [SerializeField] private float spawnTime = 3f;
    
    [Header("Wall Speeds")]
    [Tooltip("Скорость движения стены 1")]
    [SerializeField] private float wall1Speed = 5f;
    [Tooltip("Скорость движения стены 2")]
    [SerializeField] private float wall2Speed = 5f;
    [Tooltip("Скорость движения стены 3")]
    [SerializeField] private float wall3Speed = 5f;
    [Tooltip("Скорость движения стены 4")]
    [SerializeField] private float wall4Speed = 5f;
    
    [Header("Spawn Positions")]
    [Tooltip("Начальная позиция по Z для спавна стен")]
    [SerializeField] private float startPosZ = 50f;
    [Tooltip("Конечная позиция по Z (стена уничтожается при достижении)")]
    [SerializeField] private float endPosZ = -50f;
    
    [Header("Wall Prefabs")]
    [Tooltip("Префаб стены 1")]
    [SerializeField] private GameObject wall1Prefab;
    [Tooltip("Префаб стены 2")]
    [SerializeField] private GameObject wall2Prefab;
    [Tooltip("Префаб стены 3")]
    [SerializeField] private GameObject wall3Prefab;
    [Tooltip("Префаб стены 4")]
    [SerializeField] private GameObject wall4Prefab;
    
    [Header("Player Reference")]
    [Tooltip("Ссылка на игрока (если не назначена, будет искаться по тегу 'Player')")]
    [SerializeField] private Transform playerTransform;
    
    [Header("Collision Settings")]
    [Tooltip("Допустимая разница по Z для обнаружения коллизии (чем больше, тем проще обнаружить). Рекомендуется 5-10 для быстрых стен")]
    [SerializeField] private float zTolerance = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool debug = false;
    
    private PlayerCarryController playerCarryController;
    private List<GameObject> activeWalls = new List<GameObject>();
    private Coroutine spawnCoroutine;
    
    private void Awake()
    {
        // Ищем игрока, если ссылка не назначена
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                if (debug)
                {
                    Debug.Log($"[WallSpawner] Игрок найден по тегу: {player.name}");
                }
            }
            else
            {
                Debug.LogWarning("[WallSpawner] Игрок не найден по тегу 'Player'! Назначьте playerTransform в инспекторе.");
            }
        }
        
        // Ищем PlayerCarryController
        if (playerTransform != null)
        {
            playerCarryController = playerTransform.GetComponent<PlayerCarryController>();
            if (playerCarryController == null)
            {
                Debug.LogWarning("[WallSpawner] PlayerCarryController не найден на игроке!");
            }
        }
    }
    
    private void Start()
    {
        // Запускаем корутину спавна
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnWallsCoroutine());
        }
    }
    
    private void OnDestroy()
    {
        // Останавливаем корутину при уничтожении
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
    
    /// <summary>
    /// Корутина для периодического спавна стен
    /// </summary>
    private IEnumerator SpawnWallsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnTime);
            SpawnRandomWall();
        }
    }
    
    /// <summary>
    /// Спавнит случайную стену
    /// </summary>
    private void SpawnRandomWall()
    {
        // Выбираем случайный префаб
        GameObject prefabToSpawn = null;
        float speed = 0f;
        int randomIndex = Random.Range(0, 4);
        
        switch (randomIndex)
        {
            case 0:
                prefabToSpawn = wall1Prefab;
                speed = wall1Speed;
                break;
            case 1:
                prefabToSpawn = wall2Prefab;
                speed = wall2Speed;
                break;
            case 2:
                prefabToSpawn = wall3Prefab;
                speed = wall3Speed;
                break;
            case 3:
                prefabToSpawn = wall4Prefab;
                speed = wall4Speed;
                break;
        }
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[WallSpawner] Префаб стены {randomIndex + 1} не назначен!");
            return;
        }
        
        // Создаем стену
        Vector3 spawnPosition = new Vector3(-108f, 0f, startPosZ);
        GameObject wall = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        
        if (wall == null)
        {
            Debug.LogError($"[WallSpawner] Не удалось создать стену из префаба {randomIndex + 1}!");
            return;
        }
        
        // Настраиваем стену
        SetupWall(wall, speed);
        
        // Добавляем в список активных стен
        activeWalls.Add(wall);
        
        if (debug)
        {
            Debug.Log($"[WallSpawner] Создана стена {randomIndex + 1} на позиции {spawnPosition}, скорость: {speed}");
        }
    }
    
    /// <summary>
    /// Настраивает стену: добавляет компонент движения и коллайдер
    /// </summary>
    private void SetupWall(GameObject wall, float speed)
    {
        if (wall == null)
        {
            Debug.LogError("[WallSpawner] SetupWall: wall == null!");
            return;
        }
        
        // Убеждаемся, что объект активен (для добавления компонентов)
        bool wasActive = wall.activeSelf;
        if (!wasActive)
        {
            wall.SetActive(true);
        }
        
        // Добавляем компонент движения стены
        WallMovement wallMovement = wall.GetComponent<WallMovement>();
        if (wallMovement == null)
        {
            try
            {
                wallMovement = wall.AddComponent<WallMovement>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WallSpawner] Ошибка при добавлении компонента WallMovement: {e.Message}");
                if (!wasActive)
                {
                    wall.SetActive(false);
                }
                return;
            }
        }
        
        if (wallMovement == null)
        {
            Debug.LogError($"[WallSpawner] Не удалось добавить компонент WallMovement к стене! Активен: {wall.activeSelf}, Имя: {wall.name}");
            if (!wasActive)
            {
                wall.SetActive(false);
            }
            return;
        }
        
        // Инициализируем компонент движения
        wallMovement.Initialize(speed, endPosZ, this, zTolerance);
        
        // Возвращаем исходное состояние активности, если нужно
        if (!wasActive)
        {
            wall.SetActive(false);
        }
    }
    
    /// <summary>
    /// Вызывается компонентом WallMovement при коллизии с игроком
    /// </summary>
    /// <param name="collidingWall">Стена, которая вызвала коллизию</param>
    public void OnWallCollisionWithPlayer(GameObject collidingWall)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[WallSpawner] playerTransform == null, не могу телепортировать игрока!");
            return;
        }
        
        // ФИНАЛЬНАЯ ПРОВЕРКА: проверяем конкретную стену, которая вызвала коллизию
        // Если игрок за этой стеной (Z игрока > Z стены), не телепортируем
        if (collidingWall != null)
        {
            float playerZ = playerTransform.position.z;
            float wallZ = collidingWall.transform.position.z;
            
            // Если игрок за стеной (Z игрока > Z стены), не телепортируем
            if (playerZ > wallZ)
            {
                if (debug)
                {
                    Debug.LogWarning($"[WallSpawner] ФИНАЛЬНАЯ ЗАЩИТА: Игрок за стеной {collidingWall.name}! Z игрока: {playerZ:F2} > Z стены: {wallZ:F2}, телепорт ОТМЕНЁН");
                }
                return;
            }
        }
        
        // Получаем CharacterController для правильной телепортации
        CharacterController characterController = playerTransform.GetComponent<CharacterController>();
        bool wasEnabled = false;
        
        if (characterController != null)
        {
            // Временно отключаем CharacterController, чтобы можно было изменить позицию
            wasEnabled = characterController.enabled;
            characterController.enabled = false;
        }
        
        // Телепортируем игрока на (0, 0, 0)
        playerTransform.position = Vector3.zero;
        
        // Включаем CharacterController обратно, если он был включен
        if (characterController != null && wasEnabled)
        {
            characterController.enabled = true;
        }
        
        if (debug)
        {
            Debug.Log($"[WallSpawner] Игрок телепортирован на позицию (0, 0, 0). CharacterController был {(wasEnabled ? "включен" : "выключен")}");
        }
        
        // Уничтожаем брейнрот в руках игрока
        if (playerCarryController != null)
        {
            BrainrotObject carriedObject = playerCarryController.GetCurrentCarriedObject();
            if (carriedObject != null)
            {
                // Уничтожаем объект
                Destroy(carriedObject.gameObject);
                
                // Освобождаем руки
                playerCarryController.DropObject();
                
                if (debug)
                {
                    Debug.Log("[WallSpawner] Брейнрот в руках игрока уничтожен");
                }
            }
        }
        
        // Очищаем все активные стены
        ClearAllWalls();
    }
    
    /// <summary>
    /// Уничтожает все активные стены
    /// </summary>
    private void ClearAllWalls()
    {
        // Создаем копию списка, чтобы избежать проблем при итерации и уничтожении
        List<GameObject> wallsToDestroy = new List<GameObject>(activeWalls);
        
        // Уничтожаем все стены
        foreach (GameObject wall in wallsToDestroy)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }
        
        // Очищаем список
        activeWalls.Clear();
        
        if (debug)
        {
            Debug.Log($"[WallSpawner] Все активные стены уничтожены ({wallsToDestroy.Count} шт.)");
        }
    }
    
    /// <summary>
    /// Удаляет стену из списка активных (вызывается при уничтожении)
    /// </summary>
    public void RemoveWall(GameObject wall)
    {
        if (activeWalls.Contains(wall))
        {
            activeWalls.Remove(wall);
        }
    }
}
