using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "OTGET/Upgrade Data")]
    public class UpgradeDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string upgradeName;
        [TextArea] public string description;
        public UpgradeType upgradeType;

        [Header("Leveling")]
        public int maxLevel = 5;

        [Header("Cost")]
        public int baseCost = 10;
        [Tooltip("Her seviyede maliyet bu çarpanla artar. Örnek: 1.5 → 10, 15, 22, 33...")]
        public float costMultiplier = 1.5f;

        [Header("Value")]
        [Tooltip("Upgrade satın alınmadan önceki başlangıç değeri. UpgradeManager bu değere level*valuePerLevel ekler.")]
        public float baseValue = 0f;
        [Tooltip("Her seviyede eklenen değer miktarı.")]
        public float valuePerLevel = 1f;

        /// <summary>Verilen seviye için upgrade değerini döndürür.</summary>
        public float GetValue(int level)
        {
            return baseValue + level * valuePerLevel;
        }

        /// <summary>Verilen seviye için satın alma maliyetini döndürür.</summary>
        public int GetCost(int level)
        {
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, level));
        }
    }
}
