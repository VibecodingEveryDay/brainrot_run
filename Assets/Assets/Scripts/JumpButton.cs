using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if EnvirData_yg
using YG;
#endif

/// <summary>
/// Кнопка прыжка для мобильных устройств
/// Отображается только на mobile/tablet устройствах
/// </summary>
public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Button jumpButton;
    [SerializeField] private Image buttonImage;
    
    [Header("Settings")]
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    private ThirdPersonController playerController;
    private bool isPressed = false;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isMobileDevice = false;
    
    private void Awake()
    {
        // Автоматически найти компоненты, если не назначены
        if (jumpButton == null)
        {
            jumpButton = GetComponent<Button>();
        }
        
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
        
        // Сохраняем оригинальные значения
        if (buttonImage != null)
        {
            originalScale = buttonImage.transform.localScale;
            originalColor = buttonImage.color;
        }
        
        // Настраиваем кнопку
        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(OnJumpButtonClick);
        }
    }
    
    private void Start()
    {
        // Найти ThirdPersonController
        playerController = FindFirstObjectByType<ThirdPersonController>();
        
        // Определить, является ли устройство мобильным
        UpdateMobileDeviceStatus();
        
        // Показать/скрыть кнопку в зависимости от устройства
        UpdateButtonVisibility();
    }
    
    private void Update()
    {
        // Обновляем статус мобильного устройства каждый кадр (на случай если данные YG2 пришли позже)
#if EnvirData_yg
        bool wasMobile = isMobileDevice;
        UpdateMobileDeviceStatus();
        if (wasMobile != isMobileDevice)
        {
            UpdateButtonVisibility();
        }
#endif
    }
    
    private void UpdateMobileDeviceStatus()
    {
#if EnvirData_yg
        isMobileDevice = YG2.envir.isMobile || YG2.envir.isTablet;
        
#if UNITY_EDITOR
        // В редакторе проверяем симулятор
        if (!isMobileDevice)
        {
            if (YG2.envir.device == YG2.Device.Mobile || YG2.envir.device == YG2.Device.Tablet)
            {
                isMobileDevice = true;
            }
        }
#endif
#else
        isMobileDevice = Application.isMobilePlatform || Input.touchSupported;
#endif
    }
    
    private void UpdateButtonVisibility()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(isMobileDevice);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        
        // Вызываем прыжок при нажатии
        if (playerController != null)
        {
            playerController.Jump();
        }
        
        // Визуальная обратная связь
        if (buttonImage != null)
        {
            buttonImage.transform.localScale = originalScale * pressedScale;
            buttonImage.color = pressedColor;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        
        // Возвращаем визуальное состояние
        if (buttonImage != null)
        {
            buttonImage.transform.localScale = originalScale;
            buttonImage.color = originalColor;
        }
    }
    
    private void OnJumpButtonClick()
    {
        // Вызываем прыжок при клике
        if (playerController != null)
        {
            playerController.Jump();
        }
    }
    
    private void OnDestroy()
    {
        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveListener(OnJumpButtonClick);
        }
    }
}
