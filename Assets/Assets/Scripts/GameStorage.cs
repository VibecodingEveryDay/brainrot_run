using System.Collections.Generic;
using UnityEngine;
using YG;

/// <summary>
/// Синглтон для управления игровыми данными через YG2 Storage
/// Все скрипты, которым требуются сохранённые данные игрока, должны брать данные из этого синглтона
/// </summary>
public class GameStorage : MonoBehaviour
{
    private static GameStorage _instance;
    
    public static GameStorage Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject storageObject = GameObject.Find("Storage");
                if (storageObject == null)
                {
                    storageObject = new GameObject("Storage");
                    _instance = storageObject.AddComponent<GameStorage>();
                    DontDestroyOnLoad(storageObject);
                }
                else
                {
                    _instance = storageObject.GetComponent<GameStorage>();
                    if (_instance == null)
                    {
                        _instance = storageObject.AddComponent<GameStorage>();
                    }
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Подписываемся на событие загрузки данных YG2
            YG2.onGetSDKData += LoadData;
            
            // Загружаем данные при старте
            LoadData();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Отписываемся от события
        YG2.onGetSDKData -= LoadData;
    }
    
    /// <summary>
    /// Загружает данные из YG2.saves
    /// </summary>
    private void LoadData()
    {
        // Данные автоматически загружаются из YG2.saves
        // Этот метод вызывается при загрузке сохранений
    }
    
    #region Balance Methods
    
    /// <summary>
    /// Получить текущий баланс (int) - для обратной совместимости
    /// </summary>
    public int GetBalance()
    {
        return YG2.saves.balanceCount;
    }
    
    /// <summary>
    /// Получить текущий баланс как double (с учётом множителя)
    /// </summary>
    public double GetBalanceDouble()
    {
        return ConvertBalanceToDouble(YG2.saves.balanceCount, YG2.saves.balanceScaler);
    }
    
    /// <summary>
    /// Получить значение баланса и множитель
    /// </summary>
    public void GetBalance(out int value, out string scaler)
    {
        value = YG2.saves.balanceCount;
        scaler = YG2.saves.balanceScaler ?? "";
    }
    
    /// <summary>
    /// Установить баланс
    /// </summary>
    public void SetBalance(int balance)
    {
        YG2.saves.balanceCount = balance;
        YG2.saves.balanceScaler = "";
    }
    
    /// <summary>
    /// Установить баланс с множителем
    /// </summary>
    public void SetBalance(int value, string scaler)
    {
        YG2.saves.balanceCount = value;
        YG2.saves.balanceScaler = scaler ?? "";
    }
    
    /// <summary>
    /// Добавить к балансу
    /// </summary>
    public void AddBalance(int amount)
    {
        AddBalanceWithScaler(amount, "");
    }
    
    /// <summary>
    /// Добавить к балансу (с поддержкой long для больших значений)
    /// </summary>
    public void AddBalanceLong(long amount)
    {
        // Конвертируем long в значение с множителем
        (int value, string scaler) = ConvertDoubleToBalance(amount);
        AddBalanceWithScaler(value, scaler);
    }
    
    /// <summary>
    /// Добавить к балансу с множителем
    /// </summary>
    public void AddBalanceWithScaler(int value, string scaler)
    {
        // Получаем текущий баланс как double
        double currentBalance = GetBalanceDouble();
        
        // Конвертируем добавляемое значение в double
        double amountToAdd = ConvertBalanceToDouble(value, scaler ?? "");
        
        // Складываем
        double newBalance = currentBalance + amountToAdd;
        
        // Конвертируем обратно в value + scaler
        (int newValue, string newScaler) = ConvertDoubleToBalance(newBalance);
        
        // Устанавливаем новый баланс
        YG2.saves.balanceCount = newValue;
        YG2.saves.balanceScaler = newScaler;
    }
    
    /// <summary>
    /// Вычесть из баланса
    /// </summary>
    public bool SubtractBalance(int amount)
    {
        return SubtractBalanceWithScaler(amount, "");
    }
    
    /// <summary>
    /// Вычесть из баланса с множителем
    /// </summary>
    public bool SubtractBalanceWithScaler(int value, string scaler)
    {
        double currentBalance = GetBalanceDouble();
        double amountToSubtract = ConvertBalanceToDouble(value, scaler ?? "");
        
        if (currentBalance >= amountToSubtract)
        {
            double newBalance = currentBalance - amountToSubtract;
            (int newValue, string newScaler) = ConvertDoubleToBalance(newBalance);
            YG2.saves.balanceCount = newValue;
            YG2.saves.balanceScaler = newScaler;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Конвертирует баланс (value + scaler) в double
    /// </summary>
    private double ConvertBalanceToDouble(int value, string scaler)
    {
        double result = value;
        
        if (!string.IsNullOrEmpty(scaler))
        {
            scaler = scaler.ToUpper();
            
            switch (scaler)
            {
                case "K":
                    result *= 1000.0;
                    break;
                case "M":
                    result *= 1000000.0;
                    break;
                case "B":
                    result *= 1000000000.0;
                    break;
                case "T":
                    result *= 1000000000000.0;
                    break;
                case "QA": // Квадриллионы (10^15)
                    result *= 1000000000000000.0;
                    break;
                case "QI": // Квинтиллионы (10^18)
                    result *= 1000000000000000000.0;
                    break;
                case "SX": // Секстиллионы (10^21)
                    result *= 1000000000000000000000.0;
                    break;
                case "SP": // Септиллионы (10^24)
                    result *= 1000000000000000000000000.0;
                    break;
                case "OC": // Октиллионы (10^27)
                    result *= 1000000000000000000000000000.0;
                    break;
                case "NO": // Нониллионы (10^30)
                    result *= 1000000000000000000000000000000.0;
                    break;
                default:
                    // Пытаемся распарсить как число (например, "1.5M")
                    if (scaler.Length > 1)
                    {
                        char lastChar = scaler[scaler.Length - 1];
                        string numberPart = scaler.Substring(0, scaler.Length - 1);
                        
                        if (double.TryParse(numberPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double multiplier))
                        {
                            switch (lastChar)
                            {
                                case 'K':
                                    result *= multiplier * 1000.0;
                                    break;
                                case 'M':
                                    result *= multiplier * 1000000.0;
                                    break;
                                case 'B':
                                    result *= multiplier * 1000000000.0;
                                    break;
                                case 'T':
                                    result *= multiplier * 1000000000000.0;
                                    break;
                            }
                        }
                        // Проверяем двухсимвольные множители
                        if (scaler.Length >= 2)
                        {
                            string lastTwo = scaler.Substring(scaler.Length - 2);
                            string numberPart2 = scaler.Length > 2 ? scaler.Substring(0, scaler.Length - 2) : "";
                            
                            double mult = 1.0;
                            if (!string.IsNullOrEmpty(numberPart2))
                            {
                                if (double.TryParse(numberPart2, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double multiplier2))
                                {
                                    mult = multiplier2;
                                }
                                else
                                {
                                    // Если не удалось распарсить, пропускаем
                                    break;
                                }
                            }
                            
                            switch (lastTwo)
                            {
                                case "QA":
                                    result *= mult * 1000000000000000.0;
                                    break;
                                case "QI":
                                    result *= mult * 1000000000000000000.0;
                                    break;
                                case "SX":
                                    result *= mult * 1000000000000000000000.0;
                                    break;
                                case "SP":
                                    result *= mult * 1000000000000000000000000.0;
                                    break;
                                case "OC":
                                    result *= mult * 1000000000000000000000000000.0;
                                    break;
                                case "NO":
                                    result *= mult * 1000000000000000000000000000000.0;
                                    break;
                            }
                        }
                    }
                    break;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Конвертирует double в баланс (value + scaler)
    /// </summary>
    private (int value, string scaler) ConvertDoubleToBalance(double balance)
    {
        if (balance <= 0)
        {
            return (0, "");
        }
        
        // Нониллионы (10^30)
        if (balance >= 1000000000000000000000000000000.0)
        {
            double nonillions = balance / 1000000000000000000000000000000.0;
            return ((int)nonillions, "NO");
        }
        // Октиллионы (10^27)
        else if (balance >= 1000000000000000000000000000.0)
        {
            double octillions = balance / 1000000000000000000000000000.0;
            return ((int)octillions, "OC");
        }
        // Септиллионы (10^24)
        else if (balance >= 1000000000000000000000000.0)
        {
            double septillions = balance / 1000000000000000000000000.0;
            return ((int)septillions, "SP");
        }
        // Секстиллионы (10^21)
        else if (balance >= 1000000000000000000000.0)
        {
            double sextillions = balance / 1000000000000000000000.0;
            return ((int)sextillions, "SX");
        }
        // Квинтиллионы (10^18)
        else if (balance >= 1000000000000000000.0)
        {
            double quintillions = balance / 1000000000000000000.0;
            return ((int)quintillions, "QI");
        }
        // Квадриллионы (10^15)
        else if (balance >= 1000000000000000.0)
        {
            double quadrillions = balance / 1000000000000000.0;
            return ((int)quadrillions, "QA");
        }
        // Триллионы (10^12)
        else if (balance >= 1000000000000.0)
        {
            double trillions = balance / 1000000000000.0;
            return ((int)trillions, "T");
        }
        // Миллиарды (10^9)
        else if (balance >= 1000000000.0)
        {
            double billions = balance / 1000000000.0;
            return ((int)billions, "B");
        }
        // Миллионы (10^6)
        else if (balance >= 1000000.0)
        {
            double millions = balance / 1000000.0;
            return ((int)millions, "M");
        }
        // Тысячи (10^3)
        else if (balance >= 1000.0)
        {
            double thousands = balance / 1000.0;
            return ((int)thousands, "K");
        }
        else
        {
            // Меньше тысячи - просто число
            return ((int)balance, "");
        }
    }
    
    /// <summary>
    /// Форматирует баланс в читаемый формат (600B, 1.5T и т.д.)
    /// </summary>
    public string FormatBalance()
    {
        return FormatBalance(GetBalanceDouble());
    }
    
    /// <summary>
    /// Форматирует баланс из double в читаемый формат (600B, 1.5T и т.д.)
    /// Целые числа отображаются без десятичных знаков
    /// </summary>
    public string FormatBalance(double balance)
    {
        if (balance <= 0)
        {
            return "0";
        }
        
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
            return ((long)balance).ToString();
        }
    }
    
    /// <summary>
    /// Вспомогательный метод для форматирования значения баланса
    /// Целые числа отображаются без десятичных знаков
    /// </summary>
    private string FormatBalanceValue(double value, string suffix)
    {
        // Проверяем, является ли число целым
        if (value == Mathf.Floor((float)value))
        {
            // Целое число - без десятичных знаков
            return $"{(long)value}{suffix}";
        }
        else
        {
            // Дробное число - с десятичными знаками (убираем лишние нули)
            string formatted = $"{value:F2}{suffix}".TrimEnd('0').TrimEnd('.');
            return formatted;
        }
    }
    
    #endregion
    
    #region Brainrots Methods
    
    /// <summary>
    /// Получить все Brainrot объекты
    /// </summary>
    public List<BrainrotData> GetAllBrainrots()
    {
        return YG2.saves.Brainrots;
    }
    
    /// <summary>
    /// Получить Brainrot по slotID
    /// </summary>
    public BrainrotData GetBrainrotBySlotID(int slotID)
    {
        return YG2.saves.Brainrots.Find(b => b.slotID == slotID);
    }
    
    /// <summary>
    /// Получить Brainrot по имени
    /// </summary>
    public BrainrotData GetBrainrotByName(string name)
    {
        return YG2.saves.Brainrots.Find(b => b.name == name);
    }
    
    /// <summary>
    /// Добавить новый Brainrot
    /// </summary>
    public void AddBrainrot(BrainrotData brainrot)
    {
        if (brainrot != null)
        {
            YG2.saves.Brainrots.Add(brainrot);
        }
    }
    
    /// <summary>
    /// Добавить новый Brainrot с параметрами
    /// </summary>
    public void AddBrainrot(string name, int rarity, int income, int level, int slotID)
    {
        BrainrotData brainrot = new BrainrotData(name, rarity, income, level, slotID);
        YG2.saves.Brainrots.Add(brainrot);
    }
    
    /// <summary>
    /// Удалить Brainrot по slotID
    /// </summary>
    public bool RemoveBrainrotBySlotID(int slotID)
    {
        BrainrotData brainrot = YG2.saves.Brainrots.Find(b => b.slotID == slotID);
        if (brainrot != null)
        {
            return YG2.saves.Brainrots.Remove(brainrot);
        }
        return false;
    }
    
    /// <summary>
    /// Удалить Brainrot по имени
    /// </summary>
    public bool RemoveBrainrotByName(string name)
    {
        BrainrotData brainrot = YG2.saves.Brainrots.Find(b => b.name == name);
        if (brainrot != null)
        {
            return YG2.saves.Brainrots.Remove(brainrot);
        }
        return false;
    }
    
    /// <summary>
    /// Обновить данные Brainrot по slotID
    /// </summary>
    public bool UpdateBrainrot(int slotID, string name = null, int? rarity = null, int? income = null, int? level = null)
    {
        BrainrotData brainrot = YG2.saves.Brainrots.Find(b => b.slotID == slotID);
        if (brainrot != null)
        {
            if (name != null) brainrot.name = name;
            if (rarity.HasValue) brainrot.rarity = rarity.Value;
            if (income.HasValue) brainrot.income = income.Value;
            if (level.HasValue) brainrot.level = level.Value;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Получить количество Brainrot объектов
    /// </summary>
    public int GetBrainrotsCount()
    {
        return YG2.saves.Brainrots.Count;
    }
    
    /// <summary>
    /// Очистить все Brainrot объекты
    /// </summary>
    public void ClearAllBrainrots()
    {
        YG2.saves.Brainrots.Clear();
    }
    
    #endregion
    
    #region Save Methods
    
    /// <summary>
    /// Сохранить прогресс в YG2
    /// </summary>
    public void Save()
    {
        YG2.SaveProgress();
    }
    
    /// <summary>
    /// Очистить все данные storage (баланс и все Brainrot объекты)
    /// Можно вызвать из инспектора
    /// </summary>
    [ContextMenu("Clear Storage")]
    public void ClearStorage()
    {
        // Очищаем баланс
        YG2.saves.balanceCount = 0;
        YG2.saves.balanceScaler = "";
        
        // Очищаем все Brainrot объекты
        YG2.saves.Brainrots.Clear();
        
        // Сохраняем изменения
        YG2.SaveProgress();
        
        Debug.Log("[GameStorage] Storage очищен: баланс и все Brainrot объекты удалены");
    }
    
    /// <summary>
    /// Полностью сбросить storage к значениям по умолчанию (использует YG2.SetDefaultSaves)
    /// </summary>
    public void ResetStorage()
    {
        // Сохраняем idSave перед сбросом
        int savedIdSave = YG2.saves.idSave;
        
        // Сбрасываем все сохранения к значениям по умолчанию
        YG2.SetDefaultSaves();
        
        // Восстанавливаем idSave
        YG2.saves.idSave = savedIdSave;
        
        // Сохраняем изменения
        YG2.SaveProgress();
        
        Debug.Log("[GameStorage] Storage полностью сброшен к значениям по умолчанию");
    }
    
    #endregion
}
