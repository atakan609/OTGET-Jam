using System;
using System.Collections.Generic;
using UnityEngine;
using Core;
using Gameplay;

namespace Managers
{
    public class UpgradeManager : Singleton<UpgradeManager>
    {
        [Header("Upgrade Definitions")]
        [SerializeField] private UpgradeDataSO[] upgrades;

        // UpgradeType → mevcut seviye
        private Dictionary<UpgradeType, int> _levels = new Dictionary<UpgradeType, int>();

        // UpgradeType → ScriptableObject hızlı erişim için lookup
        private Dictionary<UpgradeType, UpgradeDataSO> _dataLookup = new Dictionary<UpgradeType, UpgradeDataSO>();

        public static event Action<UpgradeType, int> OnUpgradePurchased;

        protected override void Awake()
        {
            base.Awake();
            BuildLookup();
        }

        private void BuildLookup()
        {
            if (upgrades == null) return;
            foreach (var data in upgrades)
            {
                if (data == null) continue;
                _dataLookup[data.upgradeType] = data;
                if (!_levels.ContainsKey(data.upgradeType))
                    _levels[data.upgradeType] = 0;
            }
        }

        // ─── PUBLIC API ────────────────────────────────────────────────────────

        /// <summary>Upgrade satın almayı dener. Para yeterliyse ve max seviye değilse başarılı döner.</summary>
        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            if (!_dataLookup.TryGetValue(type, out var data)) return false;

            int currentLevel = GetLevel(type);
            if (currentLevel >= data.maxLevel) return false;

            int cost = data.GetCost(currentLevel);
            if (!CurrencyManager.Instance.SpendCurrency(cost)) return false;

            _levels[type] = currentLevel + 1;
            OnUpgradePurchased?.Invoke(type, _levels[type]);
            return true;
        }

        /// <summary>Mevcut upgrade seviyesini döndürür.</summary>
        public int GetLevel(UpgradeType type)
        {
            return _levels.TryGetValue(type, out int lvl) ? lvl : 0;
        }

        /// <summary>Mevcut seviyeye göre upgrade değerini döndürür.</summary>
        public float GetCurrentValue(UpgradeType type)
        {
            if (!_dataLookup.TryGetValue(type, out var data)) return 0f;
            return data.GetValue(GetLevel(type));
        }

        /// <summary>Bir sonraki seviyenin maliyetini döndürür. Max seviyedeyse -1 döner.</summary>
        public int GetNextCost(UpgradeType type)
        {
            if (!_dataLookup.TryGetValue(type, out var data)) return -1;
            int currentLevel = GetLevel(type);
            if (currentLevel >= data.maxLevel) return -1;
            return data.GetCost(currentLevel);
        }

        /// <summary>Upgrade maksimum seviyede mi?</summary>
        public bool IsMaxLevel(UpgradeType type)
        {
            if (!_dataLookup.TryGetValue(type, out var data)) return false;
            return GetLevel(type) >= data.maxLevel;
        }

        /// <summary>Upgrade için veri tanımı var mı?</summary>
        public bool HasData(UpgradeType type) => _dataLookup.ContainsKey(type);

        // ─── DEBUG ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (upgrades == null || upgrades.Length == 0) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = Color.yellow;

            float startY = 20f;
            float lineHeight = 18f;
            GUILayout.BeginArea(new Rect(Screen.width - 260f, startY, 250f, upgrades.Length * lineHeight + 10f));
            GUILayout.Label("─── Upgrades ───", style);
            foreach (var data in upgrades)
            {
                if (data == null) continue;
                int lvl = GetLevel(data.upgradeType);
                string maxTag = lvl >= data.maxLevel ? " [MAX]" : $" (next: {GetNextCost(data.upgradeType)} 💧)";
                GUILayout.Label($"{data.upgradeName}: Lv{lvl}/{data.maxLevel}{maxTag}", style);
            }
            GUILayout.EndArea();
        }
    }
}
