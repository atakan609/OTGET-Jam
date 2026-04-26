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
        [Tooltip("Ağaç yapısını okuyarak kilit (parent) kontrolü yapmak için gereklidir.")]
        [SerializeField] private UpgradeTreeDataSO treeData;

        // UpgradeType → mevcut seviye
        private Dictionary<UpgradeType, int> _levels = new Dictionary<UpgradeType, int>();

        // UpgradeType → ScriptableObject hızlı erişim için lookup
        private Dictionary<UpgradeType, UpgradeDataSO> _dataLookup = new Dictionary<UpgradeType, UpgradeDataSO>();

        // UpgradeType → Parent UpgradeType lookup (Kilit kontrolü için)
        private Dictionary<UpgradeType, UpgradeType> _parentLookup = new Dictionary<UpgradeType, UpgradeType>();

        public static event Action<UpgradeType, int> OnUpgradePurchased;

        protected override void Awake()
        {
            base.Awake();
            EnsureInitialized();
        }

        private void BuildTreeLookup()
        {
            if (treeData == null || treeData.rootNode == null) return;
            TraverseNode(treeData.rootNode);
        }

        private void TraverseNode(UpgradeNodeDataSO node)
        {
            if (node == null || node.upgradeData == null) return;

            // Daima ağaçtaki veriyi baz alması için eziyoruz
            _dataLookup[node.upgradeData.upgradeType] = node.upgradeData;

            // _levels'ta yoksa 0 ile başlat (KeyNotFoundException önlemi)
            if (!_levels.ContainsKey(node.upgradeData.upgradeType))
                _levels[node.upgradeData.upgradeType] = 0;

            if (node.children == null) return;

            foreach (var child in node.children)
            {
                if (child != null && child.upgradeData != null)
                {
                    _parentLookup[child.upgradeData.upgradeType] = node.upgradeData.upgradeType;
                    TraverseNode(child);
                }
            }
        }

        private void BuildLookup()
        {
            if (upgrades == null) return;
            foreach (var data in upgrades)
            {
                if (data == null) continue;
                // Sadece daha önceden (ağaçtan) eklenmemişse ekle. Ağaç verisi daha önceliklidir.
                if (!_dataLookup.ContainsKey(data.upgradeType))
                {
                    _dataLookup[data.upgradeType] = data;
                }
                if (!_levels.ContainsKey(data.upgradeType))
                    _levels[data.upgradeType] = 0;
            }
        }

        // ─── PUBLIC API ────────────────────────────────────────────────────────

        /// <summary>Upgrade satın almayı dener. Para yeterliyse, kilidi açıksa ve max seviye değilse başarılı döner.</summary>
        private bool _isInitialized = false;

        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            // Önce ağacı tara (ağaçtaki datalar öncelikli), sonra listeyi tara
            BuildTreeLookup();
            BuildLookup();
            _isInitialized = true;
        }

        /// <summary>Eğer data lookup içinde yoksa dinamik olarak ekler.</summary>
        public void RegisterUpgradeData(UpgradeDataSO data)
        {
            EnsureInitialized();
            if (data == null) return;
            // Daima güncel olanı (UI'dan geleni) kullan
            _dataLookup[data.upgradeType] = data;
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            EnsureInitialized();
            if (!_dataLookup.TryGetValue(type, out var data)) return false;

            // PARENT KİLİDİ KONTROLÜ
            if (!IsUnlocked(type)) return false;

            int currentLevel = GetLevel(type); // GetLevel zaten 0 döner, ama _levels'ı da garantile
            if (!_levels.ContainsKey(type)) _levels[type] = 0;

            if (!data.isInfinite && currentLevel >= data.maxLevel) return false;

            int cost = data.GetCost(currentLevel);
            if (!CurrencyManager.Instance.SpendCurrency(cost)) return false;

            _levels[type] = currentLevel + 1;
            OnUpgradePurchased?.Invoke(type, _levels[type]);
            return true;
        }

        /// <summary>Mevcut upgrade seviyesini döndürür.</summary>
        public int GetLevel(UpgradeType type)
        {
            EnsureInitialized();
            return _levels.TryGetValue(type, out int lvl) ? lvl : 0;
        }

        /// <summary>Parent upgrade satın alınmış mı kontrol eder (kilidi açık mı?).</summary>
        public bool IsUnlocked(UpgradeType type)
        {
            EnsureInitialized();
            // Eğer treeData hiç verilmemişse kilit sistemi devre dışı sayılır.
            if (treeData == null || treeData.rootNode == null) return true;

            // _parentLookup'ta bu type yoksa → ya root'tur, ya da ağaçta tanımsızdır. Her iki durumda da açık.
            if (!_parentLookup.TryGetValue(type, out UpgradeType parentType)) return true;

            // Parent'ı varsa → parent en az 1 kere satın alınmış olmalı.
            return GetLevel(parentType) > 0;
        }

        /// <summary>Mevcut seviyeye göre upgrade değerini döndürür.</summary>
        public float GetCurrentValue(UpgradeType type)
        {
            EnsureInitialized();
            if (!_dataLookup.TryGetValue(type, out var data)) return 0f;
            return data.GetValue(GetLevel(type));
        }

        /// <summary>Bir sonraki seviyenin maliyetini döndürür. Max seviyedeyse -1 döner.</summary>
        public int GetNextCost(UpgradeType type)
        {
            EnsureInitialized();
            if (!_dataLookup.TryGetValue(type, out var data)) return -1;
            int currentLevel = GetLevel(type);
            if (!data.isInfinite && currentLevel >= data.maxLevel) return -1;
            return data.GetCost(currentLevel);
        }

        /// <summary>Upgrade maksimum seviyede mi?</summary>
        public bool IsMaxLevel(UpgradeType type)
        {
            EnsureInitialized();
            if (!_dataLookup.TryGetValue(type, out var data)) return false;
            if (data.isInfinite) return false;
            return GetLevel(type) >= data.maxLevel;
        }

        /// <summary>Upgrade için veri tanımı var mı?</summary>
        public bool HasData(UpgradeType type) => _dataLookup.ContainsKey(type);

        /// <summary>
        /// GameWin hariç tüm kayıtlı upgrade'lerin max seviyede olup olmadığını döndürür.
        /// Bu koşul GameWin upgrade'inin kilidini açmak için kullanılır.
        /// </summary>
        public bool IsAllUpgradesMaxed()
        {
            EnsureInitialized();
            foreach (var kvp in _dataLookup)
            {
                if (kvp.Key == UpgradeType.GameWin) continue; // Kendisi sayılmaz
                if (kvp.Value.isInfinite) continue;            // Sonsuz olanlar sayılmaz
                if (GetLevel(kvp.Key) < kvp.Value.maxLevel) return false;
            }
            return true;
        }

        /// <summary>Debug için: GameWin hariç tüm upgradeleri max seviyeye çeker.</summary>
        public void Debug_MaxAllUpgrades()
        {
            EnsureInitialized();
            
            var targetTree = treeData;
            // Eğer Inspector'da unutulduysa UI'dan çekmeyi dene
            if (targetTree == null)
            {
                var ui = FindObjectOfType<UI.UpgradeTreeUI>();
                if (ui != null) targetTree = ui.TreeData;
            }

            if (targetTree != null && targetTree.rootNode != null)
            {
                // Ağacı BFS ile tarayarak baştan (depo) sona doğru ağaç hiyerarşisinde upgrade yap
                var queue = new Queue<UpgradeNodeDataSO>();
                var visited = new HashSet<UpgradeNodeDataSO>();
                
                queue.Enqueue(targetTree.rootNode);
                visited.Add(targetTree.rootNode);
                
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    if (node != null && node.upgradeData != null)
                    {
                        var type = node.upgradeData.upgradeType;
                        if (type != UpgradeType.GameWin && !node.upgradeData.isInfinite)
                        {
                            _levels[type] = node.upgradeData.maxLevel;
                            OnUpgradePurchased?.Invoke(type, node.upgradeData.maxLevel);
                        }
                        
                        if (node.children != null)
                        {
                            foreach (var child in node.children)
                            {
                                if (child != null && !visited.Contains(child))
                                {
                                    visited.Add(child);
                                    queue.Enqueue(child);
                                }
                            }
                        }
                    }
                }
                Debug.Log("⬆️ Tüm upgradelar ağaç bağımlılıklarına dikkat edilerek tek tek maxlandı.");
            }
            else
            {
                Debug.LogWarning("[UpgradeManager] UpgradeTreeDataSO referansı BULUNAMADI! Upgradeler rastgele sırada maxlanıyor...");
                var safeList = new List<KeyValuePair<UpgradeType, UpgradeDataSO>>(_dataLookup);
                foreach (var kvp in safeList)
                {
                    if (kvp.Key == UpgradeType.GameWin) continue;
                    if (kvp.Value.isInfinite) continue;
                    _levels[kvp.Key] = kvp.Value.maxLevel;
                    OnUpgradePurchased?.Invoke(kvp.Key, kvp.Value.maxLevel);
                }
            }
        }

        // ─── DEBUG ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (GameManager.Instance != null && !GameManager.Instance.ShowDebugUI) return;
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
                string maxTag = lvl >= data.maxLevel ? " [MAX]" : $" (next: {GetNextCost(data.upgradeType)} mL)";
                GUILayout.Label($"{data.upgradeName}: Lv{lvl}/{data.maxLevel}{maxTag}", style);
            }
            GUILayout.EndArea();
        }
    }
}
