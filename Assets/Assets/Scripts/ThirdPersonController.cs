using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = -9.81f;
    
    [Header("References")]
    [SerializeField] private Transform modelTransform; // Дочерний объект с моделью
    [SerializeField] private Animator animator;
    [SerializeField] private ThirdPersonCamera cameraController;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [Header("Jump Rotation")]
    [SerializeField] private float jumpRotationAngle = 10f; // Угол поворота модели при прыжке
    
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;
    private bool jumpRequested = false; // Запрос на прыжок от кнопки
    private bool isJumping = false; // Флаг прыжка для поворота модели
    private Quaternion savedModelRotation; // Сохраненный поворот модели перед прыжком
    
    // Ввод от джойстика (для мобильных устройств)
    private Vector2 joystickInput = Vector2.zero;
    
    // Параметры аниматора
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        // Автоматически найти дочерний объект с моделью, если не назначен
        if (modelTransform == null)
        {
            // Ищем дочерний объект с Animator
            Animator childAnimator = GetComponentInChildren<Animator>();
            if (childAnimator != null)
            {
                modelTransform = childAnimator.transform;
            }
        }
        
        // Автоматически найти Animator, если не назначен
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // ВАЖНО: Отключаем Apply Root Motion в Animator, чтобы анимации не влияли на позицию модели
        // Это предотвращает смещение дочерней модели из-за анимаций
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Автоматически найти камеру, если не назначена
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<ThirdPersonCamera>();
        }
    }
    
    private void Update()
    {
        HandleGroundCheck();
        HandleJump();
        ApplyGravity();
        HandleMovement();
        UpdateAnimator();
    }
    
    private void LateUpdate()
    {
        // Применяем компенсацию поворота после обновления анимации
        HandleJumpRotation();
    }
    
    private void HandleGroundCheck()
    {
        // Проверка земли через CharacterController (основной метод)
        isGrounded = characterController.isGrounded;
        
        // Дополнительная проверка через Raycast только если CharacterController говорит что не на земле
        // Это помогает определить, действительно ли персонаж в воздухе или просто небольшой зазор
        if (!isGrounded)
        {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            // Если Raycast находит землю близко, считаем что персонаж на земле
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance + 0.2f))
            {
                // Проверяем расстояние до земли
                float distanceToGround = hit.distance;
                if (distanceToGround <= groundCheckDistance + 0.1f)
                {
                    isGrounded = true;
                }
            }
        }
        
        // Сброс вертикальной скорости при приземлении
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Небольшая отрицательная скорость для удержания на земле
            // Сбрасываем флаг прыжка при приземлении
            isJumping = false;
        }
    }
    
    private void HandleMovement()
    {
        // Получаем ввод с клавиатуры или джойстика
        float horizontal = 0f; // A/D
        float vertical = 0f; // W/S
        
        // Приоритет джойстику на мобильных устройствах
        if (joystickInput.magnitude > 0.1f)
        {
            horizontal = joystickInput.x;
            vertical = joystickInput.y;
        }
        else
        {
            // Используем клавиатуру, если джойстик не активен
#if ENABLE_INPUT_SYSTEM
            // Новый Input System
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    horizontal += 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    vertical += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    vertical -= 1f;
            }
#else
            // Старый Input System
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
#endif
        }
        
        // Вычисляем направление движения относительно камеры
        Vector3 moveDirection = Vector3.zero;
        
        if (cameraController != null)
        {
            // Получаем направление камеры (только горизонтальное вращение)
            Vector3 cameraForward = cameraController.GetCameraForward();
            Vector3 cameraRight = cameraController.GetCameraRight();
            
            // Нормализуем векторы камеры и убираем вертикальную составляющую
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // Вычисляем направление движения относительно камеры
            moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        }
        else
        {
            // Если камера не найдена, используем мировые оси
            moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        }
        
        // Вычисляем скорость движения
        currentSpeed = moveDirection.magnitude * moveSpeed;
        
        // Применяем движение через CharacterController
        if (moveDirection.magnitude > 0.1f)
        {
            // Движение
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            
            // Плавный поворот корневого объекта в сторону движения
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Плавный поворот модели для визуального эффекта (только если не в прыжке)
            if (modelTransform != null && !isJumping)
            {
                modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    private void HandleJump()
    {
        // Прыжок на Space или от кнопки, только если персонаж на земле
        bool jumpPressed = false;
        
#if ENABLE_INPUT_SYSTEM
        // Новый Input System
        if (Keyboard.current != null)
        {
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
#else
        // Старый Input System
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
#endif
        
        // Проверяем запрос от кнопки прыжка (для мобильных устройств)
        if (jumpRequested)
        {
            jumpPressed = true;
            jumpRequested = false; // Сбрасываем запрос после обработки
        }
        
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            isJumping = true; // Устанавливаем флаг прыжка для поворота модели
            // Сохраняем текущий поворот модели перед прыжком для компенсации
            if (modelTransform != null)
            {
                savedModelRotation = modelTransform.rotation;
            }
        }
        
        // Сбрасываем флаг прыжка при приземлении
        if (isGrounded && isJumping && velocity.y <= 0)
        {
            isJumping = false;
        }
    }
    
    /// <summary>
    /// Обработка поворота модели во время прыжка
    /// Компенсирует возможный поворот анимации прыжка на -10 градусов по Y
    /// </summary>
    private void HandleJumpRotation()
    {
        if (modelTransform == null || !isJumping) return;
        
        // Анимация прыжка поворачивает модель на -10 градусов по Y каждый кадр
        // Компенсируем это, устанавливая поворот модели равным базовому повороту + компенсация
        // Это перезаписывает поворот анимации и предотвращает накопление ошибки
        Quaternion baseRotation = transform.rotation;
        Quaternion compensationRotation = Quaternion.Euler(0f, jumpRotationAngle, 0f);
        
        // Устанавливаем поворот модели напрямую, игнорируя поворот анимации
        // LateUpdate гарантирует, что это применяется после обновления анимации
        modelTransform.rotation = baseRotation * compensationRotation;
    }
    
    /// <summary>
    /// Публичный метод для прыжка (вызывается из UI кнопки)
    /// </summary>
    public void Jump()
    {
        // Устанавливаем запрос на прыжок, который будет обработан в HandleJump()
        if (isGrounded)
        {
            jumpRequested = true;
        }
    }
    
    private void ApplyGravity()
    {
        // Применяем гравитацию
        velocity.y += gravity * Time.deltaTime;
        
        // Применяем вертикальное движение
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            // Обновляем параметр Speed
            animator.SetFloat(SpeedHash, currentSpeed);
            
            // Обновляем параметр isGrounded
            animator.SetBool(IsGroundedHash, isGrounded);
        }
    }
    
    // Публичные методы для получения состояния (могут быть полезны для других скриптов)
    public bool IsGrounded()
    {
        return isGrounded;
    }
    
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    public Vector3 GetVelocity()
    {
        return characterController.velocity;
    }
    
    /// <summary>
    /// Установить ввод от джойстика (вызывается из JoystickManager)
    /// </summary>
    public void SetJoystickInput(Vector2 input)
    {
        joystickInput = input;
    }
}
