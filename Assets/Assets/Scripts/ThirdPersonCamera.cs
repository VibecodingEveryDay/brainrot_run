using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if EnvirData_yg
using YG;
#endif

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // Объект Player
    [SerializeField] public Transform cameraTarget; // Дочерний объект CameraTarget для точки обзора
    
    [Header("Camera Distance")]
    [SerializeField] private float distance = 5f;
    
    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;
    
    [Header("Touch Settings")]
    [SerializeField] private float touchSensitivity = 2f; // Чувствительность для touch-ввода
    
    [Header("Debug")]
    [Tooltip("Показывать отладочные сообщения в консоли")]
    [SerializeField] private bool debugCameraInput = false;
    
    [Header("Vertical Angle Limits")]
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    
    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private Camera cam;
    
    // Для touch-ввода на мобильных устройствах
    private Vector2 lastTouchPosition;
    private bool isTouching = false;
    private bool isMobileDevice = false;
    
    // Векторы направления камеры для использования в ThirdPersonController
    private Vector3 cameraForward;
    private Vector3 cameraRight;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        // Автоматически найти Player, если не назначен
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Автоматически находим CameraTarget, если не назначен
        if (cameraTarget == null && target != null)
        {
            // Ищем дочерний объект с именем "CameraTarget"
            Transform foundTarget = target.Find("CameraTarget");
            if (foundTarget != null)
            {
                cameraTarget = foundTarget;
            }
        }
        
        // Инициализация углов камеры на основе текущей позиции камеры
        if (target != null)
        {
            Vector3 targetCenter = GetTargetCenter();
            Vector3 directionToCamera = transform.position - targetCenter;
            float initialDistance = directionToCamera.magnitude;
            
            if (initialDistance > 0.001f)
            {
                // Нормализуем направление
                Vector3 normalizedDirection = directionToCamera / initialDistance;
                
                // Вычисляем горизонтальный угол
                Vector3 horizontalDir = new Vector3(normalizedDirection.x, 0f, normalizedDirection.z);
                if (horizontalDir.magnitude > 0.001f)
                {
                    horizontalDir.Normalize();
                    currentHorizontalAngle = Mathf.Atan2(horizontalDir.x, horizontalDir.z) * Mathf.Rad2Deg;
                }
                
                // Вычисляем вертикальный угол
                currentVerticalAngle = Mathf.Asin(normalizedDirection.y) * Mathf.Rad2Deg;
                currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
            }
            else
            {
                // Если камера слишком близко, используем значения по умолчанию
                currentHorizontalAngle = 0f;
                currentVerticalAngle = 0f;
            }
        }
    }
    
    private void Start()
    {
        // Курсор не блокируется автоматически - только при зажатой ПКМ
        
        // Определяем, является ли устройство мобильным/планшетом
        UpdateMobileDeviceStatus();
    }
    
    /// <summary>
    /// Обновляет статус мобильного устройства
    /// </summary>
    private void UpdateMobileDeviceStatus()
    {
#if EnvirData_yg
        // Используем YG2 envirdata для определения устройства
        isMobileDevice = YG2.envir.isMobile || YG2.envir.isTablet;
        
        // В редакторе также проверяем симулятор
#if UNITY_EDITOR
        if (!isMobileDevice)
        {
            // Проверяем настройки симуляции в YG2
            if (YG2.envir.device == YG2.Device.Mobile || YG2.envir.device == YG2.Device.Tablet)
            {
                isMobileDevice = true;
            }
        }
        // В редакторе/симуляторе всегда разрешаем touch-управление, если устройство определено как мобильное
        // даже если Input.touchSupported = false (это нормально для симулятора)
#else
        // На реальном устройстве проверяем touch support
        if (isMobileDevice && !Input.touchSupported)
        {
            // Если устройство мобильное, но touch не поддерживается, используем эмуляцию мыши
        }
#endif
#else
        // Если модуль EnvirData не подключен, используем стандартную проверку
        isMobileDevice = Application.isMobilePlatform || Input.touchSupported;
        
#if UNITY_EDITOR
        // В редакторе также проверяем симулятор через настройки проекта
        if (!isMobileDevice)
        {
            // Проверяем, запущен ли симулятор мобильного устройства
            // В Unity Editor можно симулировать мобильные устройства через Device Simulator
            isMobileDevice = Application.isMobilePlatform;
        }
#endif
#endif
    }
    
    // Получает позицию точки обзора камеры (CameraTarget или позиция target)
    // Этот метод вызывается каждый кадр, чтобы камера всегда следовала за движущимся персонажем
    private Vector3 GetTargetCenter()
    {
        // Если назначен cameraTarget, используем его позицию
        // Это обеспечивает точное позиционирование и отсутствие дрожания
        if (cameraTarget != null)
        {
            return cameraTarget.position;
        }
        
        // Если cameraTarget не назначен, пытаемся найти его автоматически
        if (target != null)
        {
            Transform foundTarget = target.Find("CameraTarget");
            if (foundTarget != null)
            {
                cameraTarget = foundTarget;
                return cameraTarget.position;
            }
        }
        
        // Если cameraTarget не найден, используем позицию target как fallback
        if (target != null)
        {
            return target.position;
        }
        
        return Vector3.zero;
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // ВАЖНО: LateUpdate вызывается после всех Update()
        // Это гарантирует, что позиция персонажа уже обновлена
        
        // Сначала обрабатываем ввод мыши (обновляем углы)
        HandleMouseInput();
        
        // Затем обновляем позицию камеры относительно текущей позиции CameraTarget
        // Это должно быть последним, чтобы использовать самую актуальную позицию персонажа
        UpdateCameraPosition();
        
        // И наконец обновляем векторы направления для контроллера
        UpdateCameraDirection();
    }
    
    private void HandleMouseInput()
    {
        // Обновляем статус мобильного устройства (на случай если данные YG2 пришли позже)
#if EnvirData_yg
        // Проверяем YG2 данные каждый кадр в редакторе, так как они могут прийти позже
#if UNITY_EDITOR
        bool yg2IsMobile = YG2.envir.isMobile || YG2.envir.isTablet;
        if (yg2IsMobile != isMobileDevice)
        {
            UpdateMobileDeviceStatus();
        }
#else
        if (!isMobileDevice && (YG2.envir.isMobile || YG2.envir.isTablet))
        {
            UpdateMobileDeviceStatus();
        }
#endif
#endif
        
        // На мобильных устройствах всегда используем touch-ввод (или эмуляцию мыши в редакторе)
        // ВАЖНО: Эта проверка должна быть ПЕРВОЙ, так как isMobileDevice уже установлен в true
        if (isMobileDevice)
        {
            HandleTouchInput();
            return;
        }
        
        // В редакторе: проверяем наличие JoystickManager для определения симулятора (fallback)
        // НО только если устройство уже определено как мобильное через YG2
#if UNITY_EDITOR
        // Проверяем, действительно ли мы в симуляторе мобильного устройства через YG2
        bool isSimulatorMobile = false;
#if EnvirData_yg
        isSimulatorMobile = YG2.envir.isMobile || YG2.envir.isTablet || 
                           YG2.envir.device == YG2.Device.Mobile || 
                           YG2.envir.device == YG2.Device.Tablet;
#endif
        
        // Если симулятор мобильного устройства активен И JoystickManager активен, используем touch-ввод
        if (isSimulatorMobile)
        {
            JoystickManager joystickManager = JoystickManager.Instance;
            bool joystickActive = joystickManager != null && joystickManager.gameObject.activeInHierarchy;
            
            if (!joystickActive)
            {
                joystickManager = FindFirstObjectByType<JoystickManager>();
                joystickActive = joystickManager != null && joystickManager.gameObject.activeInHierarchy;
            }
            
            if (joystickActive)
            {
                // В симуляторе мобильного устройства используем touch-ввод (ЛКМ)
                if (!isMobileDevice)
                {
                    isMobileDevice = true;
                }
                HandleTouchInput();
                return;
            }
        }
#endif
        
        // Проверяем наличие touch-ввода (на случай если устройство не определено как мобильное, но есть touch)
        bool hasTouchInput = Input.touchCount > 0;
        if (hasTouchInput)
        {
            HandleTouchInput();
            return;
        }
        
        // На desktop используем ПКМ
        HandleDesktopInput();
    }
    
    /// <summary>
    /// Обработка ввода на desktop (ПКМ)
    /// </summary>
    private void HandleDesktopInput()
    {
        // Проверяем, зажата ли правая кнопка мыши
        bool rightMouseButtonPressed = false;
        
#if ENABLE_INPUT_SYSTEM
        // Новый Input System
        if (Mouse.current != null)
        {
            rightMouseButtonPressed = Mouse.current.rightButton.isPressed;
        }
#else
        // Старый Input System
        rightMouseButtonPressed = Input.GetMouseButton(1); // 1 = правая кнопка мыши
#endif
        
        // Управление камерой только при зажатой ПКМ
        if (!rightMouseButtonPressed)
        {
            // Разблокируем курсор, если ПКМ не зажата
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }
        
        // Блокируем курсор при зажатой ПКМ
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        float mouseX = 0f;
        float mouseY = 0f;
        
#if ENABLE_INPUT_SYSTEM
        // Новый Input System
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * mouseSensitivity;
            mouseY = mouseDelta.y * mouseSensitivity;
        }
#else
        // Старый Input System
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
#endif
        
        ApplyCameraRotation(mouseX, mouseY);
    }
    
    /// <summary>
    /// Обработка touch-ввода на мобильных устройствах
    /// </summary>
    private void HandleTouchInput()
    {
        // В редакторе или если touch не поддерживается, эмулируем touch через мышь
#if UNITY_EDITOR
        // В редакторе всегда используем эмуляцию мыши для touch-ввода
        HandleMouseAsTouch();
        return;
#else
        // На реальном устройстве проверяем поддержку touch
        if (!Input.touchSupported)
        {
            // Если touch не поддерживается, но устройство мобильное, используем эмуляцию мыши
            HandleMouseAsTouch();
            return;
        }
        
        // Проверяем наличие touch-ввода
        if (Input.touchCount == 0)
        {
            // Если нет touch-ввода, но мы были в режиме тапа, сбрасываем состояние
            if (isTouching)
            {
                isTouching = false;
            }
            return;
        }
#endif
        
        Touch touch = Input.GetTouch(0);
        
        // Обрабатываем начало тапа
        // Явно указываем UnityEngine.TouchPhase для избежания конфликта с InputSystem.TouchPhase
        if (touch.phase == UnityEngine.TouchPhase.Began)
        {
            // Проверяем, не на Canvas UI элементе ли тап (не на 3D объектах)
            if (IsPointerOverCanvasUI(touch.position))
            {
                // Тап на UI элементе - игнорируем для управления камерой
                isTouching = false;
                return;
            }
            
            // Проверяем, не на джойстике ли тап
            if (IsTouchOnJoystick(touch.position))
            {
                // Тап на джойстике - игнорируем для управления камерой
                isTouching = false;
                return;
            }
            else
            {
                // Тап не на джойстике и не на UI - начинаем управление камерой
                isTouching = true;
                lastTouchPosition = touch.position;
                return;
            }
        }
        
        // Если мы уже в режиме тапа, но тап был на джойстике, сбрасываем состояние
        // Не проверяем UI во время движения - если тап начался не на UI, продолжаем управление
        if (isTouching && IsTouchOnJoystick(touch.position))
        {
            isTouching = false;
            return;
        }
        
        // Обрабатываем движение тапа (только если не на джойстике)
        if (isTouching && (touch.phase == UnityEngine.TouchPhase.Moved || touch.phase == UnityEngine.TouchPhase.Stationary))
        {
            if (!IsTouchOnJoystick(touch.position))
            {
                Vector2 touchDelta = touch.position - lastTouchPosition;
                
                // Применяем вращение только если есть движение
                if (touchDelta.magnitude > 0.01f)
                {
                    // Нормализуем чувствительность (не умножаем на Time.deltaTime, так как это уже дельта позиции)
                    float touchX = touchDelta.x * touchSensitivity * 0.1f; // Увеличили с 0.01f до 0.1f для более отзывчивого управления
                    float touchY = touchDelta.y * touchSensitivity * 0.1f;
                    
                    ApplyCameraRotation(touchX, touchY);
                }
                
                lastTouchPosition = touch.position;
            }
        }
        
        // Обрабатываем окончание тапа
        if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
        {
            isTouching = false;
        }
    }
    
    /// <summary>
    /// Эмуляция touch-ввода через мышь в редакторе/симуляторе
    /// </summary>
    private void HandleMouseAsTouch()
    {
        bool mouseButtonDown = false;
        bool mouseButtonJustPressed = false;
        Vector2 mousePosition = Vector2.zero;
        
#if ENABLE_INPUT_SYSTEM
        // Новый Input System
        // В симуляторе Unity Device Simulator лучше использовать Pointer.current вместо Mouse.current
        UnityEngine.InputSystem.Pointer pointer = UnityEngine.InputSystem.Pointer.current;
        
        if (pointer != null)
        {
            // Используем Pointer для получения позиции (работает для touch и mouse)
            try
            {
                mousePosition = pointer.position.ReadValue();
                mouseButtonDown = pointer.press.isPressed;
                mouseButtonJustPressed = pointer.press.wasPressedThisFrame;
            }
            catch
            {
                // Fallback на Mouse
                if (Mouse.current != null)
                {
                    mouseButtonDown = Mouse.current.leftButton.isPressed;
                    mouseButtonJustPressed = Mouse.current.leftButton.wasPressedThisFrame;
                    try
                    {
                        mousePosition = Mouse.current.position.ReadValue();
                    }
                    catch
                    {
                        mousePosition = Vector2.zero;
                    }
                }
            }
        }
        else if (Mouse.current != null)
        {
            // Fallback на Mouse, если Pointer недоступен
            mouseButtonDown = Mouse.current.leftButton.isPressed;
            mouseButtonJustPressed = Mouse.current.leftButton.wasPressedThisFrame;
            try
            {
                mousePosition = Mouse.current.position.ReadValue();
            }
            catch
            {
                mousePosition = Vector2.zero;
            }
        }
        else
        {
            // Ни Pointer, ни Mouse не доступны
            if (isTouching)
            {
                isTouching = false;
            }
            return;
        }
        
        // Если позиция нулевая, но кнопка нажата, используем центр экрана как fallback
        if (mousePosition == Vector2.zero && (mouseButtonDown || mouseButtonJustPressed))
        {
            mousePosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        }
#else
        // Старый Input System (если не используется новый)
        mouseButtonDown = Input.GetMouseButton(0);
        mouseButtonJustPressed = Input.GetMouseButtonDown(0);
        mousePosition = Input.mousePosition;
#endif
        
        // При начале клика проверяем, не на UI элементе ли (проверяем только в момент начала тапа)
        if (mouseButtonJustPressed)
        {
            // Проверяем только Canvas UI элементы, а не все GameObjects
            bool isOverUI = IsPointerOverCanvasUI(mousePosition);
            
            if (debugCameraInput)
            {
                Debug.Log($"[ThirdPersonCamera] mouseButtonJustPressed: mousePosition={mousePosition}, isOverUI={isOverUI}");
            }
            
            if (isOverUI)
            {
                // Клик на UI элементе - игнорируем для управления камерой
                if (debugCameraInput)
                {
                    Debug.Log("[ThirdPersonCamera] Клик на UI элементе - игнорируем для камеры");
                }
                isTouching = false;
                return;
            }
            
            bool onJoystick = IsTouchOnJoystick(mousePosition);
            if (onJoystick)
            {
                // Клик на джойстике - игнорируем для управления камерой
                isTouching = false;
                return;
            }
            else
            {
                // Клик не на джойстике и не на UI - начинаем управление камерой
                isTouching = true;
                lastTouchPosition = mousePosition;
                return;
            }
        }
        
        // Если ЛКМ нажата, но isTouching еще не установлен (пропустили mouseButtonJustPressed)
        // НЕ проверяем UI здесь - проверка UI была только при начале тапа, иначе можно заблокировать управление
        if (mouseButtonDown && !isTouching)
        {
            bool onJoystick = IsTouchOnJoystick(mousePosition);
            if (!onJoystick)
            {
                // Начинаем управление камерой
                isTouching = true;
                lastTouchPosition = mousePosition;
            }
        }
        
        // Если мы уже в режиме тапа, проверяем, не переместилась ли мышь на UI или джойстик
        // Если тап начался на UI/джойстике, сразу сбрасываем состояние
        if (isTouching && mouseButtonDown)
        {
            // Проверяем UI (если тап переместился на UI элемент)
            bool isOverUI = IsPointerOverCanvasUI(mousePosition);
            bool onJoystick = IsTouchOnJoystick(mousePosition);
            
            if (isOverUI || onJoystick)
            {
                if (debugCameraInput)
                {
                    Debug.Log($"[ThirdPersonCamera] Тап переместился на UI/джойстик, сбрасываем управление камерой. isOverUI={isOverUI}, onJoystick={onJoystick}");
                }
                isTouching = false;
                return;
            }
        }
        
        // Обрабатываем движение мыши при зажатой кнопке (только если не на джойстике и не на UI)
        if (isTouching && mouseButtonDown)
        {
            // Проверяем, не на джойстике или UI ли текущая позиция
            bool onJoystick = IsTouchOnJoystick(mousePosition);
            bool isOverUI = IsPointerOverCanvasUI(mousePosition);
            
            if (!onJoystick && !isOverUI)
            {
                Vector2 mouseDelta = mousePosition - lastTouchPosition;
                
                // Применяем вращение даже при небольшом движении (уменьшили порог)
                if (mouseDelta.magnitude > 0.001f)
                {
                    // Увеличиваем чувствительность для более отзывчивого управления
                    float touchX = mouseDelta.x * touchSensitivity * 0.1f; // Увеличили с 0.01f до 0.1f
                    float touchY = mouseDelta.y * touchSensitivity * 0.1f;
                    
                    ApplyCameraRotation(touchX, touchY);
                }
                
                // Всегда обновляем позицию для следующего кадра
                lastTouchPosition = mousePosition;
            }
            else
            {
                // Если мышь переместилась на джойстик, сбрасываем состояние
                isTouching = false;
            }
        }
        else if (!mouseButtonDown && isTouching)
        {
            // Отпускание кнопки
            isTouching = false;
        }
        
        // Обрабатываем отпускание кнопки
        if (!mouseButtonDown && isTouching)
        {
            isTouching = false;
        }
    }
    
    /// <summary>
    /// Проверяет, находится ли тап в области джойстика
    /// </summary>
    private bool IsTouchOnJoystick(Vector2 screenPosition)
    {
        JoystickManager joystickManager = JoystickManager.Instance;
        if (joystickManager == null) return false;
        
        return joystickManager.IsPointOnJoystick(screenPosition);
    }
    
    /// <summary>
    /// Проверяет, находится ли указатель над Canvas UI элементом (не 3D объектами)
    /// Фильтрует только интерактивные UI элементы (Button, Selectable и т.д.), игнорируя фоновые Canvas элементы
    /// </summary>
    private bool IsPointerOverCanvasUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        
        // Используем EventSystem.RaycastAll для проверки UI элементов
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = screenPosition;
        
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);
        
        // Фильтруем результаты - проверяем только интерактивные UI элементы из GraphicRaycaster
        // Игнорируем контейнеры MobileUIContainer и OverflowUiContainer, но учитываем их дочерние элементы
        foreach (var result in results)
        {
            // Проверяем, что это UI элемент Canvas (GraphicRaycaster)
            if (result.module is GraphicRaycaster)
            {
                GameObject hitObject = result.gameObject;
                
                // Пропускаем контейнеры MobileUIContainer и OverflowUiContainer
                if (hitObject.name == "MobileUIContainer" || hitObject.name == "OverflowUiContainer")
                {
                    if (debugCameraInput)
                    {
                        Debug.Log($"[ThirdPersonCamera] IsPointerOverCanvasUI: пропускаем контейнер: {hitObject.name}");
                    }
                    continue; // Пропускаем контейнер и проверяем следующие элементы
                }
                
                // Проверяем, является ли элемент интерактивным (Button, Selectable, IPointerDownHandler и т.д.)
                // Игнорируем фоновые Canvas элементы, которые не должны блокировать клики
                bool isInteractive = 
                    hitObject.GetComponent<Selectable>() != null ||
                    hitObject.GetComponent<Button>() != null ||
                    hitObject.GetComponent<IPointerClickHandler>() != null ||
                    hitObject.GetComponent<IPointerDownHandler>() != null ||
                    hitObject.GetComponent<Toggle>() != null ||
                    hitObject.GetComponent<Slider>() != null ||
                    hitObject.GetComponent<Scrollbar>() != null ||
                    hitObject.GetComponent<Dropdown>() != null ||
                    hitObject.GetComponent<InputField>() != null ||
                    hitObject.GetComponent<TMP_InputField>() != null;
                
                // Также проверяем, имеет ли Image raycastTarget (интерактивный элемент)
                Image image = hitObject.GetComponent<Image>();
                if (image != null && image.raycastTarget)
                {
                    isInteractive = true;
                }
                
                if (isInteractive)
                {
                    if (debugCameraInput)
                    {
                        Debug.Log($"[ThirdPersonCamera] IsPointerOverCanvasUI: найдено интерактивное UI: {hitObject.name}");
                    }
                    return true;
                }
                else if (debugCameraInput)
                {
                    Debug.Log($"[ThirdPersonCamera] IsPointerOverCanvasUI: UI элемент найден, но не интерактивный: {hitObject.name} (игнорируем)");
                }
            }
        }
        
        if (debugCameraInput)
        {
            Debug.Log($"[ThirdPersonCamera] IsPointerOverCanvasUI: интерактивные UI элементы не найдены, results.Count={results.Count}");
        }
        
        return false;
    }
    
    /// <summary>
    /// Применяет вращение камеры на основе ввода
    /// </summary>
    private void ApplyCameraRotation(float inputX, float inputY)
    {
        // Инверсия по Y, если нужно
        if (invertY)
        {
            inputY = -inputY;
        }
        
        // Обновляем горизонтальный угол (вращение вокруг персонажа)
        currentHorizontalAngle += inputX;
        
        // Обновляем вертикальный угол с ограничениями
        currentVerticalAngle -= inputY;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
    }
    
    private void UpdateCameraPosition()
    {
        // ВАЖНО: Получаем текущую позицию CameraTarget каждый кадр
        // Это гарантирует, что камера всегда следует за движущимся персонажем
        // Автоматически находим CameraTarget, если не назначен
        if (cameraTarget == null && target != null)
        {
            Transform foundTarget = target.Find("CameraTarget");
            if (foundTarget != null)
            {
                cameraTarget = foundTarget;
            }
        }
        
        // Получаем актуальную позицию точки обзора (обновляется каждый кадр)
        Vector3 targetCenter = GetTargetCenter();
        
        // Преобразуем углы в радианы для вычислений
        // Используем точные вычисления без накопления ошибок
        float horizontalRad = currentHorizontalAngle * Mathf.Deg2Rad;
        float verticalRad = currentVerticalAngle * Mathf.Deg2Rad;
        
        // Вычисляем единичный вектор направления в сферических координатах
        // Эта формула гарантирует, что вектор всегда единичной длины
        float cosVertical = Mathf.Cos(verticalRad);
        float sinVertical = Mathf.Sin(verticalRad);
        float cosHorizontal = Mathf.Cos(horizontalRad);
        float sinHorizontal = Mathf.Sin(horizontalRad);
        
        Vector3 direction = new Vector3(
            sinHorizontal * cosVertical,  // X: горизонтальное смещение
            sinVertical,                  // Y: вертикальное смещение  
            cosHorizontal * cosVertical   // Z: глубина
        );
        
        // ВАЖНО: Вычисляем позицию камеры относительно ТЕКУЩЕЙ позиции CameraTarget
        // Каждый кадр мы берем актуальную позицию и вычисляем позицию заново
        // Это гарантирует, что камера всегда точно следует за персонажем
        
        // Вычисляем желаемую позицию камеры на точном расстоянии от CameraTarget
        Vector3 desiredCameraPosition = targetCenter + direction * distance;
        
        // Устанавливаем позицию камеры
        transform.position = desiredCameraPosition;
        
        // КРИТИЧНО: Проверяем и корректируем расстояние для предотвращения накопления ошибок
        // Вычисляем фактическое расстояние от камеры до CameraTarget
        Vector3 actualDirection = transform.position - targetCenter;
        float actualDistance = actualDirection.magnitude;
        
        // Если расстояние отличается от желаемого, принудительно корректируем
        // Это предотвращает постепенное смещение камеры
        if (Mathf.Abs(actualDistance - distance) > 0.001f)
        {
            if (actualDistance > 0.001f)
            {
                // Нормализуем направление и устанавливаем точное расстояние
                Vector3 correctedDirection = actualDirection / actualDistance;
                transform.position = targetCenter + correctedDirection * distance;
            }
            else
            {
                // Если камера слишком близко, используем вычисленное направление
                transform.position = targetCenter + direction * distance;
            }
        }
        
        // Камера всегда смотрит точно на текущую позицию CameraTarget
        // Вычисляем направление от камеры к точке обзора
        Vector3 lookDirection = targetCenter - transform.position;
        float lookDistance = lookDirection.magnitude;
        
        // Применяем поворот только если направление валидно
        if (lookDistance > 0.001f)
        {
            // Нормализуем направление и применяем поворот
            // Это гарантирует, что камера всегда смотрит точно на CameraTarget
            transform.rotation = Quaternion.LookRotation(lookDirection / lookDistance);
        }
    }
    
    private void UpdateCameraDirection()
    {
        // Обновляем векторы направления камеры для использования в ThirdPersonController
        cameraForward = transform.forward;
        cameraRight = transform.right;
        
        // Убираем вертикальную составляющую для движения по горизонтали
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
    }
    
    // Публичные методы для получения направления камеры (используются в ThirdPersonController)
    public Vector3 GetCameraForward()
    {
        return cameraForward;
    }
    
    public Vector3 GetCameraRight()
    {
        return cameraRight;
    }
    
    // Метод для получения дистанции камеры (используется в CameraCollisionHandler)
    public float GetDistance()
    {
        return distance;
    }
    
    // Метод для разблокировки курсора (может быть полезен для меню)
    public void SetCursorLocked(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Визуализация в редакторе для отладки
        Vector3 targetCenter = GetTargetCenter();
        
        if (targetCenter != Vector3.zero)
        {
            // Рисуем сферу в точке обзора CameraTarget (желтый)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetCenter, 0.3f);
            
            // Рисуем линию от точки обзора к камере (зеленый)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(targetCenter, transform.position);
            
            // Рисуем позицию объекта Player (красный)
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(target.position, 0.2f);
            }
        }
    }
}
