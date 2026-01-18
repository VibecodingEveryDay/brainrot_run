using UnityEngine;

/// <summary>
/// Скрипт для 3D кнопки открытия магазина безопасных зон
/// При наступлении игрока на кнопку показывает SafeZonesModalContainer
/// При уходе игрока скрывает модальное окно
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopSafeZonesButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("SafeZonesModalContainer GameObject, который будет показан при наступлении на кнопку")]
    [SerializeField] private GameObject safeZonesModalContainer;
    
    [Header("Settings")]
    [Tooltip("Скрывать SafeZonesModalContainer при старте (если true, контейнер будет скрыт в Start)")]
    [SerializeField] private bool hideOnStart = true;
    
    [Header("Player Detection")]
    [Tooltip("Тег игрока (по умолчанию 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    // Флаг для отслеживания, находится ли игрок на кнопке
    private bool isPlayerOnButton = false;
    
    private void Awake()
    {
        // Проверяем наличие триггера
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // Убеждаемся, что коллайдер настроен как триггер
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
        }
    }
    
    private void Start()
    {
        // Автоматически находим SafeZonesModalContainer, если не назначен
        if (safeZonesModalContainer == null)
        {
            // Ищем в сцене объект с именем "SafeZonesModalContainer"
            GameObject foundContainer = GameObject.Find("SafeZonesModalContainer");
            if (foundContainer != null)
            {
                safeZonesModalContainer = foundContainer;
            }
        }
        
        // Скрываем контейнер при старте, если требуется
        if (safeZonesModalContainer != null && hideOnStart)
        {
            safeZonesModalContainer.SetActive(false);
        }
    }
    
    /// <summary>
    /// Вызывается когда объект входит в триггер
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это игрок
        if (other.CompareTag(playerTag))
        {
            isPlayerOnButton = true;
            OpenShop();
        }
    }
    
    /// <summary>
    /// Вызывается когда объект выходит из триггера
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Проверяем, что это игрок
        if (other.CompareTag(playerTag))
        {
            isPlayerOnButton = false;
            
            // Скрываем модальное окно при уходе игрока
            CloseShop();
        }
    }
    
    /// <summary>
    /// Публичный метод для открытия магазина (можно вызвать извне)
    /// </summary>
    public void OpenShop()
    {
        if (safeZonesModalContainer != null)
        {
            safeZonesModalContainer.SetActive(true);
        }
    }
    
    /// <summary>
    /// Публичный метод для закрытия магазина
    /// </summary>
    public void CloseShop()
    {
        if (safeZonesModalContainer != null)
        {
            safeZonesModalContainer.SetActive(false);
        }
    }
}
