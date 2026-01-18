using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Спавнер брейнротов на Plane с учетом шансов редкостей, случайного baseIncome и отступов от краев
/// </summary>
public class ZoneSpawner : MonoBehaviour
{
    [Header("Шансы редкостей (в процентах, 0-100, можно оставить пустыми)")]
    [Tooltip("Шанс появления Common брейнрота (0-100)")]
    [SerializeField] private float commonChance = 0f;
    
    [Tooltip("Шанс появления Rare брейнрота (0-100)")]
    [SerializeField] private float rareChance = 0f;
    
    [Tooltip("Шанс появления Exclusive брейнрота (0-100)")]
    [SerializeField] private float exclusiveChance = 0f;
    
    [Tooltip("Шанс появления Epic брейнрота (0-100)")]
    [SerializeField] private float epicChance = 0f;
    
    [Tooltip("Шанс появления Mythic брейнрота (0-100)")]
    [SerializeField] private float mythicChance = 0f;
    
    [Tooltip("Шанс появления Legendary брейнрота (0-100)")]
    [SerializeField] private float legendaryChance = 0f;
    
    [Tooltip("Шанс появления Secret брейнрота (0-100)")]
    [SerializeField] private float secretChance = 0f;
    
    [Header("BaseIncome")]
    [Tooltip("Минимальный базовый доход")]
    [SerializeField] private long baseIncomeMin = 100;
    
    [Tooltip("Максимальный базовый доход")]
    [SerializeField] private long baseIncomeMax = 1000;
    
    [Header("Настройки спавна")]
    [Tooltip("Количество брейнротов для спавна")]
    [SerializeField] private int brainrotsCount = 10;
    
    [Tooltip("Отступ от краев Plane (в единицах)")]
    [SerializeField] private float spawnMargin = 1f;
    
    [Header("Отладка")]
    [Tooltip("Показывать отладочные сообщения")]
    [SerializeField] private bool debug = false;
    
    // Нормализованные шансы редкостей (сумма = 100%)
    private Dictionary<string, float> normalizedRarityChances = new Dictionary<string, float>();
    
    // Загруженные префабы брейнротов
    private GameObject[] brainrotPrefabs;
    
    // Границы области спавна
    private Bounds spawnBounds;
    
    // Список всех редкостей в порядке приоритета
    private readonly string[] allRarities = { "Common", "Rare", "Exclusive", "Epic", "Mythic", "Legendary", "Secret" };
    
    // Список спавненных брейнротов для возможности респавна
    private List<GameObject> spawnedBrainrots = new List<GameObject>();
    
    private void Awake()
    {
        // Загружаем префабы брейнротов
        LoadBrainrotPrefabs();
        
        // Определяем границы области спавна
        CalculateSpawnBounds();
    }
    
    private void Start()
    {
        // Нормализуем шансы редкостей
        NormalizeRarityChances();
        
        // Спавним брейнроты
        SpawnBrainrots();
    }
    
    /// <summary>
    /// Нормализует шансы редкостей до 100%
    /// Если сумма указанных шансов < 100, остаток распределяется поровну между неуказанными
    /// </summary>
    private void NormalizeRarityChances()
    {
        // Создаем словарь для исходных шансов
        Dictionary<string, float> originalChances = new Dictionary<string, float>
        {
            { "Common", commonChance },
            { "Rare", rareChance },
            { "Exclusive", exclusiveChance },
            { "Epic", epicChance },
            { "Mythic", mythicChance },
            { "Legendary", legendaryChance },
            { "Secret", secretChance }
        };
        
        // Вычисляем сумму указанных шансов (не равных 0)
        float sumOfSpecified = 0f;
        int countOfUnspecified = 0;
        
        foreach (string rarity in allRarities)
        {
            float chance = originalChances[rarity];
            if (chance > 0f)
            {
                sumOfSpecified += chance;
            }
            else
            {
                countOfUnspecified++;
            }
        }
        
        // Инициализируем нормализованные шансы
        normalizedRarityChances.Clear();
        
        if (sumOfSpecified > 100f)
        {
            // Если сумма > 100, нормализуем все пропорционально
            float scale = 100f / sumOfSpecified;
            foreach (string rarity in allRarities)
            {
                float chance = originalChances[rarity];
                normalizedRarityChances[rarity] = chance * scale;
            }
            
            if (debug)
            {
                Debug.Log($"[ZoneSpawner] Сумма шансов > 100 ({sumOfSpecified}), нормализовано пропорционально");
            }
        }
        else if (sumOfSpecified < 100f)
        {
            // Если сумма < 100, распределяем остаток поровну между неуказанными
            float remaining = 100f - sumOfSpecified;
            float perUnspecified = countOfUnspecified > 0 ? remaining / countOfUnspecified : 0f;
            
            foreach (string rarity in allRarities)
            {
                float chance = originalChances[rarity];
                if (chance > 0f)
                {
                    // Используем указанный шанс
                    normalizedRarityChances[rarity] = chance;
                }
                else
                {
                    // Распределяем остаток поровну
                    normalizedRarityChances[rarity] = perUnspecified;
                }
            }
            
            if (debug)
            {
                Debug.Log($"[ZoneSpawner] Сумма шансов < 100 ({sumOfSpecified}), остаток {remaining} распределен поровну между {countOfUnspecified} неуказанными редкостями (по {perUnspecified:F2}%)");
            }
        }
        else
        {
            // Если сумма = 100, используем как есть
            foreach (string rarity in allRarities)
            {
                normalizedRarityChances[rarity] = originalChances[rarity];
            }
            
            if (debug)
            {
                Debug.Log("[ZoneSpawner] Сумма шансов = 100, используется как есть");
            }
        }
        
        // Выводим итоговые шансы для отладки
        if (debug)
        {
            float totalCheck = 0f;
            foreach (string rarity in allRarities)
            {
                float chance = normalizedRarityChances[rarity];
                totalCheck += chance;
                Debug.Log($"[ZoneSpawner] {rarity}: {chance:F2}%");
            }
            Debug.Log($"[ZoneSpawner] Итоговая сумма: {totalCheck:F2}%");
        }
    }
    
    /// <summary>
    /// Загружает префабы брейнротов из Resources
    /// </summary>
    private void LoadBrainrotPrefabs()
    {
#if UNITY_EDITOR
        // В редакторе используем AssetDatabase для загрузки из папки
        string folderPath = "Assets/Assets/Prefabs/Brainrots/game";
        List<GameObject> prefabsList = new List<GameObject>();
        
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<BrainrotObject>() != null)
            {
                prefabsList.Add(prefab);
            }
        }
        
        brainrotPrefabs = prefabsList.ToArray();
#else
        // В билде используем Resources
        brainrotPrefabs = Resources.LoadAll<GameObject>("Brainrots/game");
        
        // Фильтруем только те, у которых есть компонент BrainrotObject
        List<GameObject> filteredPrefabs = new List<GameObject>();
        foreach (GameObject prefab in brainrotPrefabs)
        {
            if (prefab != null && prefab.GetComponent<BrainrotObject>() != null)
            {
                filteredPrefabs.Add(prefab);
            }
        }
        brainrotPrefabs = filteredPrefabs.ToArray();
#endif
        
        if (brainrotPrefabs == null || brainrotPrefabs.Length == 0)
        {
            Debug.LogError("[ZoneSpawner] Не удалось загрузить префабы брейнротов! Проверьте путь к папке.");
        }
        else if (debug)
        {
            Debug.Log($"[ZoneSpawner] Загружено {brainrotPrefabs.Length} префабов брейнротов");
        }
    }
    
    /// <summary>
    /// Вычисляет границы области спавна на основе размеров Plane
    /// </summary>
    private void CalculateSpawnBounds()
    {
        // Пытаемся получить MeshRenderer
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            spawnBounds = meshRenderer.bounds;
        }
        else
        {
            // Если нет MeshRenderer, пытаемся получить Collider
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                spawnBounds = collider.bounds;
            }
            else
            {
                // Если нет ни того, ни другого, используем размеры по умолчанию
                Debug.LogWarning("[ZoneSpawner] Не найден MeshRenderer или Collider! Используются размеры по умолчанию.");
                spawnBounds = new Bounds(transform.position, new Vector3(10f, 0.1f, 10f));
            }
        }
        
        // Уменьшаем границы на spawnMargin
        Vector3 reducedSize = spawnBounds.size;
        reducedSize.x = Mathf.Max(0.1f, reducedSize.x - spawnMargin * 2f);
        reducedSize.z = Mathf.Max(0.1f, reducedSize.z - spawnMargin * 2f);
        
        spawnBounds = new Bounds(spawnBounds.center, reducedSize);
        
        if (debug)
        {
            Debug.Log($"[ZoneSpawner] Границы спавна: центр={spawnBounds.center}, размер={spawnBounds.size}, margin={spawnMargin}");
        }
    }
    
    /// <summary>
    /// Выбирает случайную редкость на основе нормализованных шансов
    /// </summary>
    private string GetRandomRarity()
    {
        // Генерируем случайное число от 0 до 100
        float randomValue = Random.Range(0f, 100f);
        
        // Проходим по редкостям и находим диапазон, в который попало число
        float currentSum = 0f;
        foreach (string rarity in allRarities)
        {
            float chance = normalizedRarityChances[rarity];
            if (randomValue >= currentSum && randomValue < currentSum + chance)
            {
                return rarity;
            }
            currentSum += chance;
        }
        
        // Если не попали ни в один диапазон (из-за ошибок округления), возвращаем последнюю
        return allRarities[allRarities.Length - 1];
    }
    
    /// <summary>
    /// Генерирует случайную позицию спавна в пределах области с учетом отступов
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        // Генерируем случайные координаты X и Z в пределах уменьшенных границ
        float randomX = Random.Range(
            spawnBounds.center.x - spawnBounds.size.x / 2f,
            spawnBounds.center.x + spawnBounds.size.x / 2f
        );
        
        float randomZ = Random.Range(
            spawnBounds.center.z - spawnBounds.size.z / 2f,
            spawnBounds.center.z + spawnBounds.size.z / 2f
        );
        
        // Y координата - используем центр bounds или делаем Raycast для определения высоты поверхности
        float yPosition = spawnBounds.center.y;
        
        // Пытаемся определить высоту через Raycast
        RaycastHit hit;
        Vector3 rayStart = new Vector3(randomX, spawnBounds.center.y + 5f, randomZ);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f))
        {
            yPosition = hit.point.y;
        }
        
        return new Vector3(randomX, yPosition, randomZ);
    }
    
    /// <summary>
    /// Основной метод спавна брейнротов
    /// </summary>
    private void SpawnBrainrots()
    {
        if (brainrotPrefabs == null || brainrotPrefabs.Length == 0)
        {
            Debug.LogError("[ZoneSpawner] Нет префабов для спавна!");
            return;
        }
        
        if (normalizedRarityChances.Count == 0)
        {
            Debug.LogError("[ZoneSpawner] Шансы редкостей не нормализованы!");
            return;
        }
        
        int spawnedCount = 0;
        
        for (int i = 0; i < brainrotsCount; i++)
        {
            // Выбираем случайную редкость
            string selectedRarity = GetRandomRarity();
            
            // Выбираем случайный префаб из загруженных
            GameObject prefabToSpawn = brainrotPrefabs[Random.Range(0, brainrotPrefabs.Length)];
            
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[ZoneSpawner] Префаб null при попытке спавна {i + 1}-го брейнрота");
                continue;
            }
            
            // Генерируем случайную позицию
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // Получаем spawnRotationY из префаба (если есть компонент BrainrotObject на префабе)
            float rotationY = 0f;
            BrainrotObject prefabBrainrot = prefabToSpawn.GetComponent<BrainrotObject>();
            if (prefabBrainrot != null)
            {
                // Используем рефлексию для получения spawnRotationY (приватное поле)
                System.Reflection.FieldInfo spawnRotationYField = typeof(BrainrotObject).GetField("spawnRotationY",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnRotationYField != null)
                {
                    rotationY = (float)spawnRotationYField.GetValue(prefabBrainrot);
                }
            }
            
            // Если spawnRotationY не задан (0), используем случайный поворот
            if (Mathf.Abs(rotationY) < 0.01f)
            {
                rotationY = Random.Range(0f, 360f);
            }
            
            Quaternion spawnRotation = Quaternion.Euler(0f, rotationY, 0f);
            
            // Создаем экземпляр
            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
            
            if (spawnedObject == null)
            {
                Debug.LogWarning($"[ZoneSpawner] Не удалось создать экземпляр префаба {prefabToSpawn.name}");
                continue;
            }
            
            // Получаем компонент BrainrotObject
            BrainrotObject brainrotObject = spawnedObject.GetComponent<BrainrotObject>();
            if (brainrotObject == null)
            {
                Debug.LogWarning($"[ZoneSpawner] У спавненного объекта {spawnedObject.name} нет компонента BrainrotObject!");
                Destroy(spawnedObject);
                continue;
            }
            
            // Устанавливаем редкость
            brainrotObject.SetRarity(selectedRarity);
            
            // Генерируем случайный baseIncome
            long randomBaseIncome = Random.Range((int)baseIncomeMin, (int)baseIncomeMax + 1);
            brainrotObject.SetBaseIncome(randomBaseIncome);
            
            // Добавляем в список спавненных объектов
            spawnedBrainrots.Add(spawnedObject);
            
            spawnedCount++;
            
            if (debug)
            {
                Debug.Log($"[ZoneSpawner] Спавнен брейнрот {i + 1}/{brainrotsCount}: {brainrotObject.GetObjectName()}, " +
                         $"редкость={selectedRarity}, baseIncome={randomBaseIncome}, позиция={spawnPosition}");
            }
        }
        
        if (debug)
        {
            Debug.Log($"[ZoneSpawner] Спавнено {spawnedCount} из {brainrotsCount} брейнротов");
        }
    }
    
    /// <summary>
    /// Удаляет все спавненные брейнроты
    /// </summary>
    private void ClearSpawnedBrainrots()
    {
        foreach (GameObject brainrot in spawnedBrainrots)
        {
            if (brainrot != null)
            {
                Destroy(brainrot);
            }
        }
        spawnedBrainrots.Clear();
        
        if (debug)
        {
            Debug.Log("[ZoneSpawner] Все спавненные брейнроты удалены");
        }
    }
    
    /// <summary>
    /// Респавнит брейнроты (удаляет старые и создает новые)
    /// Публичный метод для вызова из других скриптов
    /// </summary>
    public void RespawnBrainrots()
    {
        // Удаляем старые брейнроты
        ClearSpawnedBrainrots();
        
        // Спавним новые
        SpawnBrainrots();
        
        if (debug)
        {
            Debug.Log("[ZoneSpawner] Брейнроты респавнены");
        }
    }
}
