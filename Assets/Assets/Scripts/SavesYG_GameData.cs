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
    }
}
