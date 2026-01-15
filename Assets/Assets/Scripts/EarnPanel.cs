using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Панель для накопления дохода от размещённого brainrot объекта.
/// Каждую секунду прибавляет доход от brainrot на PlacementPanel с тем же ID.
/// При наступлении игрока собирает накопленный баланс и добавляет его в GameStorage.
/// </summary>
public class EarnPanel : MonoBehaviour
{
    [Header("Настройки панели")]
    [Tooltip("ID панели для связи с PlacementPanel (должен совпадать с panelID в PlacementPanel)")]
    [SerializeField] private int panelID = 0;
    
    [Header("UI")]
    [Tooltip("TextMeshPro компонент для отображения накопленного баланса")]
    [SerializeField] private TextMeshPro moneyText;
    
    [Header("Настройки обнаружения игрока")]
    [Tooltip("Transform игрока (перетащите из иерархии)")]
    [SerializeField] private Transform playerTransform;
    
    [Tooltip("Радиус обнаружения игрока (игрок считается на панели, если находится в этом радиусе)")]
    [SerializeField] private float detectionRadius = 2f;
    
    [Header("Отладка")]
    [Tooltip("Показывать отладочные сообщения в консоли")]
    [SerializeField] private bool debug = false;
    
    // Накопленный баланс панели
    private double accumulatedBalance = 0.0;
    
    // Ссылка на связанную PlacementPanel
    private PlacementPanel linkedPlacementPanel;
    
    // Ссылка на размещённый brainrot объект
    private BrainrotObject placedBrainrot;
    
    // Корутина для обновления дохода
    private Coroutine incomeCoroutine;
    
    // Флаг, находится ли игрок на панели
    private bool isPlayerOnPanel = false;
    
    // Кэш для оптимизации - обновляем текст только при изменении
    private string lastFormattedBalance = "";
    private double lastAccumulatedBalance = -1;
    
    private void Awake()
    {
        // Автоматически находим TextMeshPro компонент, если не назначен
        if (moneyText == null)
        {
            moneyText = GetComponentInChildren<TextMeshPro>();
            if (debug)
            {
                Debug.Log($"[EarnPanel] TextMeshPro компонент {(moneyText != null ? "найден" : "НЕ найден")} на {gameObject.name}");
            }
        }
        
        // Пытаемся найти игрока автоматически, если не назначен
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                if (debug)
                {
                    Debug.Log($"[EarnPanel] Игрок найден автоматически: {player.name}");
                }
            }
            else
            {
                if (debug)
                {
                    Debug.LogWarning($"[EarnPanel] Игрок не найден! Назначьте playerTransform в инспекторе.");
                }
            }
        }
    }
    
    private void Start()
    {
        // Находим связанную PlacementPanel по ID
        FindLinkedPlacementPanel();
        
        // Запускаем корутину для обновления дохода
        if (incomeCoroutine == null)
        {
            incomeCoroutine = StartCoroutine(UpdateIncomeCoroutine());
        }
    }
    
    private void OnEnable()
    {
        // Перезапускаем корутину при включении
        if (incomeCoroutine == null)
        {
            incomeCoroutine = StartCoroutine(UpdateIncomeCoroutine());
        }
    }
    
    private void OnDisable()
    {
        // Останавливаем корутину при выключении
        if (incomeCoroutine != null)
        {
            StopCoroutine(incomeCoroutine);
            incomeCoroutine = null;
        }
    }
    
    private void Update()
    {
        // Обновляем размещённый brainrot объект
        UpdatePlacedBrainrot();
        
        // Проверяем, находится ли игрок на панели
        CheckPlayerOnPanel();
        
        // Обновляем текст баланса только если игрок не на панели и баланс изменился
        if (!isPlayerOnPanel)
        {
            // Обновляем текст только при изменении баланса (оптимизация)
            if (Mathf.Abs((float)(accumulatedBalance - lastAccumulatedBalance)) > 0.0001f)
            {
                UpdateMoneyText();
            }
        }
    }
    
    /// <summary>
    /// Проверяет, находится ли игрок на панели (в радиусе обнаружения)
    /// </summary>
    private void CheckPlayerOnPanel()
    {
        if (playerTransform == null)
        {
            isPlayerOnPanel = false;
            return;
        }
        
        // Вычисляем расстояние от центра панели до игрока
        Vector3 panelPosition = transform.position;
        Vector3 playerPosition = playerTransform.position;
        
        // Используем только горизонтальное расстояние (игнорируем высоту)
        Vector2 panelPos2D = new Vector2(panelPosition.x, panelPosition.z);
        Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.z);
        float distance = Vector2.Distance(panelPos2D, playerPos2D);
        
        bool wasOnPanel = isPlayerOnPanel;
        isPlayerOnPanel = distance <= detectionRadius;
        
        if (debug && wasOnPanel != isPlayerOnPanel)
        {
            Debug.Log($"[EarnPanel] Игрок {(isPlayerOnPanel ? "на" : "не на")} панели. Расстояние: {distance:F2}, Радиус: {detectionRadius}");
        }
        
        // Если игрок только что наступил на панель, обнуляем баланс (один раз)
        if (!wasOnPanel && isPlayerOnPanel)
        {
            if (debug)
            {
                Debug.Log($"[EarnPanel] Игрок наступил на панель! Расстояние: {distance:F2}");
            }
            CollectBalance();
        }
    }
    
    /// <summary>
    /// Находит связанную PlacementPanel по ID
    /// </summary>
    private void FindLinkedPlacementPanel()
    {
        linkedPlacementPanel = PlacementPanel.GetPanelByID(panelID);
        if (linkedPlacementPanel == null)
        {
            Debug.LogWarning($"[EarnPanel] PlacementPanel с ID {panelID} не найдена!");
        }
    }
    
    /// <summary>
    /// Обновляет ссылку на размещённый brainrot объект
    /// </summary>
    private void UpdatePlacedBrainrot()
    {
        if (linkedPlacementPanel == null)
        {
            // Пытаемся найти панель снова
            FindLinkedPlacementPanel();
            if (linkedPlacementPanel == null)
            {
                placedBrainrot = null;
                return;
            }
        }
        
        // Получаем размещённый brainrot из связанной панели
        placedBrainrot = linkedPlacementPanel.GetPlacedBrainrot();
    }
    
    /// <summary>
    /// Корутина для обновления дохода каждую секунду
    /// </summary>
    private IEnumerator UpdateIncomeCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            // Не добавляем доход, если игрок на панели (баланс должен быть обнулён)
            if (isPlayerOnPanel) continue;
            
            // Если есть размещённый brainrot, добавляем доход
            if (placedBrainrot != null && placedBrainrot.IsPlaced() && !placedBrainrot.IsCarried())
            {
                // Получаем финальный доход (уже включает редкость и уровень)
                double finalIncome = placedBrainrot.GetFinalIncome();
                accumulatedBalance += finalIncome;
            }
        }
    }
    
    /// <summary>
    /// Обновляет текст баланса в формате 1.89B (без скобок)
    /// </summary>
    private void UpdateMoneyText()
    {
        if (moneyText == null) return;
        
        // Кэшируем значение для оптимизации
        lastAccumulatedBalance = accumulatedBalance;
        
        if (accumulatedBalance <= 0)
        {
            if (lastFormattedBalance != "0")
            {
                moneyText.text = "0";
                lastFormattedBalance = "0";
            }
            return;
        }
        
        // Форматируем баланс в нужный формат
        string formattedBalance = FormatBalance(accumulatedBalance);
        
        // Формат: число + буква, например 1.89B
        // Ограничиваем длину текста
        // Максимум 8 символов (например, "999.99T" = 7 символов)
        if (formattedBalance.Length > 8)
        {
            formattedBalance = formattedBalance.Substring(0, 8);
        }
        
        // Обновляем текст только если он изменился (оптимизация)
        if (formattedBalance != lastFormattedBalance)
        {
            moneyText.text = formattedBalance;
            lastFormattedBalance = formattedBalance;
        }
    }
    
    /// <summary>
    /// Форматирует баланс в читаемый формат (1.89B, 5.2M и т.д.)
    /// Возвращает строку без скобок, максимум 8 символов
    /// Целые числа отображаются без десятичных знаков
    /// </summary>
    private string FormatBalance(double balance)
    {
        // Нониллионы (10^30)
        if (balance >= 1000000000000000000000000000000.0)
        {
            double nonillions = balance / 1000000000000000000000000000000.0;
            return FormatBalanceValue(nonillions, "NO");
        }
        // Октиллионы (10^27)
        else if (balance >= 1000000000000000000000000000.0)
        {
            double octillions = balance / 1000000000000000000000000000.0;
            return FormatBalanceValue(octillions, "OC");
        }
        // Септиллионы (10^24)
        else if (balance >= 1000000000000000000000000.0)
        {
            double septillions = balance / 1000000000000000000000000.0;
            return FormatBalanceValue(septillions, "SP");
        }
        // Секстиллионы (10^21)
        else if (balance >= 1000000000000000000000.0)
        {
            double sextillions = balance / 1000000000000000000000.0;
            return FormatBalanceValue(sextillions, "SX");
        }
        // Квинтиллионы (10^18)
        else if (balance >= 1000000000000000000.0)
        {
            double quintillions = balance / 1000000000000000000.0;
            return FormatBalanceValue(quintillions, "QI");
        }
        // Квадриллионы (10^15)
        else if (balance >= 1000000000000000.0)
        {
            double quadrillions = balance / 1000000000000000.0;
            return FormatBalanceValue(quadrillions, "QA");
        }
        // Триллионы (10^12)
        else if (balance >= 1000000000000.0)
        {
            double trillions = balance / 1000000000000.0;
            return FormatBalanceValue(trillions, "T");
        }
        // Миллиарды (10^9)
        else if (balance >= 1000000000.0)
        {
            double billions = balance / 1000000000.0;
            return FormatBalanceValue(billions, "B");
        }
        // Миллионы (10^6)
        else if (balance >= 1000000.0)
        {
            double millions = balance / 1000000.0;
            return FormatBalanceValue(millions, "M");
        }
        // Тысячи (10^3)
        else if (balance >= 1000.0)
        {
            double thousands = balance / 1000.0;
            return FormatBalanceValue(thousands, "K");
        }
        else
        {
            // Меньше тысячи - показываем как целое число
            string formatted = ((long)balance).ToString();
            // Ограничиваем до 8 символов
            if (formatted.Length > 8) formatted = formatted.Substring(0, 8);
            return formatted;
        }
    }
    
    /// <summary>
    /// Вспомогательный метод для форматирования значения баланса
    /// </summary>
    private string FormatBalanceValue(double value, string suffix)
    {
        // Проверяем, является ли число целым
        if (value == Mathf.Floor((float)value))
        {
            string formatted = $"{(long)value}{suffix}";
            if (formatted.Length > 8) formatted = formatted.Substring(0, 8);
            return formatted;
        }
        else
        {
            string formatted = $"{value:F2}{suffix}".TrimEnd('0').TrimEnd('.');
            if (formatted.Length > 8) formatted = formatted.Substring(0, 8);
            return formatted;
        }
    }
    
    
    /// <summary>
    /// Собирает накопленный баланс и добавляет его в GameStorage
    /// Всегда обнуляет счётчик при вызове
    /// </summary>
    public void CollectBalance()
    {
        // Конвертируем double в int (теряем дробную часть, но это нормально для баланса)
        long balanceToAdd = (long)accumulatedBalance;
        
        // Всегда обнуляем баланс панели при наступлении игрока
        accumulatedBalance = 0.0;
        lastAccumulatedBalance = 0.0;
        
        // Немедленно обновляем текст, чтобы показать 0
        if (moneyText != null)
        {
            moneyText.text = "0";
            lastFormattedBalance = "0";
        }
        
        // Добавляем баланс в GameStorage только если он больше 0
        if (balanceToAdd > 0)
        {
            // Проверяем, что GameStorage доступен
            if (GameStorage.Instance != null)
            {
                string balanceBeforeFormatted = null;
                if (debug)
                {
                    balanceBeforeFormatted = GameStorage.Instance.FormatBalance();
                }
                
                // Используем AddBalanceLong для корректной обработки больших значений с множителями
                GameStorage.Instance.AddBalanceLong(balanceToAdd);
                
                if (debug)
                {
                    string balanceAfterFormatted = GameStorage.Instance.FormatBalance();
                    string balanceToAddFormatted = FormatBalance(balanceToAdd);
                    Debug.Log($"[EarnPanel] Собран баланс: {balanceToAddFormatted}. Баланс игрока: {balanceBeforeFormatted} -> {balanceAfterFormatted}");
                }
            }
        }
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
        FindLinkedPlacementPanel();
    }
    
    /// <summary>
    /// Получить текущий накопленный баланс
    /// </summary>
    public double GetAccumulatedBalance()
    {
        return accumulatedBalance;
    }
    
    /// <summary>
    /// Установить накопленный баланс (для тестирования или загрузки сохранений)
    /// </summary>
    public void SetAccumulatedBalance(double balance)
    {
        accumulatedBalance = balance;
    }
}
