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
        [Tooltip("Ağaç yapılarını okuyarak kilit (parent ve prerequisite) kontrolü yapmak için gereklidir.")]
        [SerializeField] private List<UpgradeTreeDataSO> treeDatas = new List<UpgradeTreeDataSO>();
        public List<UpgradeTreeDataSO> TreeDatas => treeDatas;

        // UpgradeType → mevcut seviye
        private Dictionary<UpgradeType, int> _levels = new Dictionary<UpgradeType, int>();

        // UpgradeType → ScriptableObject hızlı erişim için lookup
        private Dictionary<UpgradeType, UpgradeDataSO> _dataLookup = new Dictionary<UpgradeType, UpgradeDataSO>();

        // UpgradeType → Hangi ağaca ait (Önkoşul kontrolü için)
        private Dictionary<UpgradeType, UpgradeTreeDataSO> _nodeTreeLookup = new Dictionary<UpgradeType, UpgradeTreeDataSO>();

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
            if (treeDatas == null) return;
            HashSet<UpgradeNodeDataSO> visitedNodes = new HashSet<UpgradeNodeDataSO>();
            foreach (var tree in treeDatas)
            {
                if (tree == null || tree.rootNode == null) continue;
                TraverseNode(tree.rootNode, tree, visitedNodes);
            }
        }

        private void TraverseNode(UpgradeNodeDataSO node, UpgradeTreeDataSO currentTree, HashSet<UpgradeNodeDataSO> visited)
        {
            if (node == null || node.upgradeData == null) return;

            // Döngü (StackOverflow) koruması
            if (visited.Contains(node)) return;
            visited.Add(node);

            // Daima ağaçtaki veriyi baz alması için eziyoruz
            _dataLookup[node.upgradeData.upgradeType] = node.upgradeData;
            _nodeTreeLookup[node.upgradeData.upgradeType] = currentTree;

            // _levels'ta yoksa 0 ile başlat (KeyNotFoundException önlemi)
            if (!_levels.ContainsKey(node.upgradeData.upgradeType))
                _levels[node.upgradeData.upgradeType] = 0;

            if (node.children == null) return;

            foreach (var child in node.children)
            {
                if (child != null && child.upgradeData != null)
                {
                    _parentLookup[child.upgradeData.upgradeType] = node.upgradeData.upgradeType;
                    TraverseNode(child, currentTree, visited);
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

        /// <summary>Oyunu sıfırlamak için tüm upgradeleri ve level verilerini başa döndürür (0 seviyesi).</summary>
        public void ResetAllUpgrades()
        {
            EnsureInitialized();
            var keys = new List<UpgradeType>(_levels.Keys);
            foreach (var key in keys)
            {
                _levels[key] = 0;
                OnUpgradePurchased?.Invoke(key, 0); // Diğer sistemlerin (UI vs) 0'landığını anlaması için event fırlat
            }
            Debug.Log("🔄 Tüm upgradeler sıfırlandı.");
        }

        /// <summary>Bir ağacın içindeki "tüm sonsuz olmayan upgradelerden en az 1 tane alınmış mı" durumunu kontrol eder.</summary>
        public bool IsTreeCompleted(UpgradeTreeDataSO tree)
        {
            if (tree == null || tree.rootNode == null) return true;

            Queue<UpgradeNodeDataSO> queue = new Queue<UpgradeNodeDataSO>();
            HashSet<UpgradeNodeDataSO> visited = new HashSet<UpgradeNodeDataSO>();

            queue.Enqueue(tree.rootNode);
            visited.Add(tree.rootNode);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node != null && node.upgradeData != null)
                {
                    if (!node.upgradeData.isInfinite && node.upgradeData.upgradeType != UpgradeType.GameWin)
                    {
                        if (GetLevel(node.upgradeData.upgradeType) < 1)
                            return false;
                    }

                    if (node.children != null)
                    {
                        foreach (var c in node.children)
                        {
                            if (c != null && !visited.Contains(c))
                            {
                                visited.Add(c);
                                queue.Enqueue(c);
                            }
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>Parent upgrade satın alınmış mı kontrol eder (kilidi açık mı?).</summary>
        public bool IsUnlocked(UpgradeType type)
        {
            EnsureInitialized();
            
            // 1. Ağaç kilitli mi kontrolü (Önkoşul Ağaç)
            if (_nodeTreeLookup.TryGetValue(type, out var tree))
            {
                if (tree != null && tree.prerequisiteTree != null)
                {
                    if (!IsTreeCompleted(tree.prerequisiteTree))
                        return false; 
                }
            }

            // 2. Parent kilidi kontrolü
            if (treeDatas == null || treeDatas.Count == 0) return true;

            // _parentLookup'ta bu type yoksa → ya root'tur, ya da ağaçta tanımsızdır.
            if (!_parentLookup.TryGetValue(type, out UpgradeType parentType)) return true;

            // Parent'ı varsa → parent en az 1 kere satın alınmış olmalı.
            return GetLevel(parentType) > 0;
        }

        /// <summary>Ağaç önkoşulu yüzünden kilitliyse Inspector'dan girilen uyarı mesajını döndürür. Aksi halde null döner.</summary>
        public string GetLockedPrerequisiteMessage(UpgradeType type)
        {
            EnsureInitialized();
            if (_nodeTreeLookup.TryGetValue(type, out var tree))
            {
                if (tree != null && tree.prerequisiteTree != null && !IsTreeCompleted(tree.prerequisiteTree))
                {
                    return tree.lockedTooltipMessage;
                }
            }
            return null;
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

        /// <summary>Debug için: Belirtilen ağaçtaki GameWin hariç tüm upgradeleri max seviyeye çeker.</summary>
        public void Debug_MaxTreeUpgrades(int treeIndex)
        {
            EnsureInitialized();
            
            var targetTrees = treeDatas != null && treeDatas.Count > 0 ? treeDatas : new List<UpgradeTreeDataSO>();
            // Eğer Inspector'da unutulduysa UI'dan çekmeyi dene
            if (targetTrees.Count == 0)
            {
                var ui = FindObjectOfType<UI.UpgradeTreeUI>();
                if (ui != null && ui.TreeDatas != null) targetTrees = ui.TreeDatas;
            }

            if (targetTrees != null && targetTrees.Count > treeIndex)
            {
                var tTree = targetTrees[treeIndex];
                if (tTree == null || tTree.rootNode == null) return;

                // Ağacı BFS ile tarayarak baştan sona doğru upgrade yap
                var queue = new Queue<UpgradeNodeDataSO>();
                var visited = new HashSet<UpgradeNodeDataSO>();
                
                queue.Enqueue(tTree.rootNode);
                visited.Add(tTree.rootNode);
                
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
                Debug.Log($"⬆️ {treeIndex}. ağacın tüm upgradeları maxlandı.");
            }
            else
            {
                Debug.LogWarning("UI.UpgradeTreeUI içinde ağaç verisi bulunamadı!");
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
                string maxTag = lvl >= data.maxLevel ? " [MAX]" : $" (next: {CurrencyManager.FormatWater(GetNextCost(data.upgradeType))})";
                GUILayout.Label($"{data.upgradeName}: Lv{lvl}/{data.maxLevel}{maxTag}", style);
            }
            GUILayout.EndArea();
        }
    }
}
