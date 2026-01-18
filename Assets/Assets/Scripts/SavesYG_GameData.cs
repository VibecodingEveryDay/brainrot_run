using System.Collections.Generic;
using YG;

/// <summary>
/// Расширение класса SavesYG для хранения игровых данных
/// </summary>
namespace YG
{
    public partial class SavesYG
    {
        /// <summary>
        /// Баланс игрока (основное значение)
        /// </summary>
        public int balanceCount = 0;
        
        /// <summary>
        /// Множитель баланса (K, M, B, T и т.д.)
        /// </summary>
        public string balanceScaler = "";
        
        /// <summary>
        /// Список всех Brainrot объектов игрока
        /// </summary>
        public List<BrainrotData> Brainrots = new List<BrainrotData>();
        
        /// <summary>
        /// Уровень скорости игрока (начальный уровень 10)
        /// </summary>
        public int PlayerSpeedLevel = 10;
        
        /// <summary>
        /// Список номеров купленных безопасных зон (1-4)
        /// </summary>
        public List<int> PurchasedSafeZones = new List<int>();
    }
}
