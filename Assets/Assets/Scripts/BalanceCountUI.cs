using UnityEngine;
using TMPro;

/// <summary>
/// Компонент для отображения баланса игрока из GameStorage в TextMeshProUGUI
/// </summary>
public class BalanceCountUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TextMeshProUGUI компонент для отображения баланса (если не назначен, будет найден автоматически)")]
    [SerializeField] private TextMeshProUGUI balanceText;
    
    [Header("Settings")]
    [Tooltip("Обновлять баланс каждый кадр (если false, обновляется только при изменении)")]
    [SerializeField] private bool updateEveryFrame = false;
    
    [Tooltip("Интервал обновления в секундах (если updateEveryFrame = false)")]
    [SerializeField] private float updateInterval = 0.05f; // Уменьшено для более частого обновления
    
    [Header("Debug")]
    [SerializeField] private bool debug = false;
    
    private GameStorage gameStorage;
    private double lastBalance = -1;
    private float updateTimer = 0f;
    private string lastFormattedBalance = "";
    
    private void Awake()
    {
        // Автоматически находим TextMeshProUGUI компонент, если не назначен
        if (balanceText == null)
        {
            balanceText = GetComponent<TextMeshProUGUI>();
            if (balanceText == null)
            {
                balanceText = GetComponentInChildren<TextMeshProUGUI>();
            }
            
            if (balanceText == null)
            {
                Debug.LogError($"[BalanceCountUI] TextMeshProUGUI компонент не найден на {gameObject.name}!");
            }
            else if (debug)
            {
                Debug.Log($"[BalanceCountUI] TextMeshProUGUI компонент найден на {gameObject.name}");
            }
        }
    }
    
    private void Start()
    {
        // Получаем ссылку на GameStorage
        gameStorage = GameStorage.Instance;
        
        if (gameStorage == null)
        {
            Debug.LogError("[BalanceCountUI] GameStorage.Instance не найден!");
            return;
        }
        
        // Обновляем баланс при старте
        UpdateBalance();
    }
    
    private void Update()
    {
        if (gameStorage == null || balanceText == null)
        {
            return;
        }
        
        if (updateEveryFrame)
        {
            // Обновляем каждый кадр
            UpdateBalance();
        }
        else
        {
            // Обновляем с интервалом (но проверка изменений происходит всегда)
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateBalance();
            }
        }
    }
    
    /// <summary>
    /// Обновляет текст баланса из GameStorage
    /// </summary>
    private void UpdateBalance()
    {
        if (gameStorage == null || balanceText == null)
        {
            return;
        }
        
        // Получаем текущий баланс
        double currentBalance = gameStorage.GetBalanceDouble();
        
        // Форматируем баланс через GameStorage
        string formattedBalance = gameStorage.FormatBalance(currentBalance);
        
        // Обновляем текст, если баланс или форматированная строка изменились
        // Используем сравнение форматированной строки для более надежной проверки
        if (formattedBalance != lastFormattedBalance || Mathf.Abs((float)(currentBalance - lastBalance)) > 0.0001f)
        {
            // Устанавливаем текст
            balanceText.text = formattedBalance;
            
            if (debug)
            {
                Debug.Log($"[BalanceCountUI] Баланс обновлен: {formattedBalance} (raw: {currentBalance}, предыдущий: {lastBalance})");
            }
            
            lastBalance = currentBalance;
            lastFormattedBalance = formattedBalance;
        }
    }
    
    /// <summary>
    /// Принудительно обновить баланс (можно вызвать извне)
    /// </summary>
    public void RefreshBalance()
    {
        lastBalance = -1; // Сбрасываем, чтобы принудительно обновить
        lastFormattedBalance = ""; // Сбрасываем форматированную строку
        UpdateBalance();
    }
    
    private void OnEnable()
    {
        // Обновляем баланс при включении объекта
        if (gameStorage != null)
        {
            RefreshBalance();
        }
    }
}
