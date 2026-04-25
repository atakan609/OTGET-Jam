using System;
using UnityEngine;
using Managers;

namespace Gameplay
{
    /// <summary>
    /// Sahnede sabit duran depo binası. Oyuncu yaklaşınca kova otomatik boşalmaya başlar.
    /// Sabit kovalar da (StaticBucket) biriken suyu buraya gönderir.
    /// </summary>
    public class DepotController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float baseCapacity = 100f;
        [SerializeField] private float baseDrainSpeed = 5f;
        [SerializeField] private float triggerRadius = 2f;

        [Header("Depot Prefabs per Tier")]
        [Tooltip("index 0 = Başlangıç (level 0-2), index 1 = Orta (level 3-5), index 2 = Gelişmiş (level 6+)")]
        [SerializeField] private GameObject[] depotPrefabs;
        [Tooltip("Depo prefabının doğacagı local offset (ana objeye göre).")]
        [SerializeField] private Vector3 visualOffset = Vector3.zero;

        public float StoredWater  { get; private set; }
        public float MaxCapacity   { get; private set; }
        public bool  IsFull        => StoredWater >= MaxCapacity;

        private BucketController _drainingBucket;
        private GameObject _currentVisual;
        private int _currentPrefabIndex = -1;

        // Kapasite upgrade eşiği → prefab index eşleme
        // level 0-2 → index 0, level 3-5 → index 1, level 6+ → index 2
        private static readonly int[] _prefabThresholds = { 0, 3, 6 };

        private void Awake()
        {
            MaxCapacity = baseCapacity;
        }

        private void Start()
        {
            UpgradeManager.OnUpgradePurchased += HandleUpgrade;
            RefreshFromUpgrades();
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= HandleUpgrade;
        }

        private void Update()
        {
            if (_drainingBucket == null || IsFull) return;

            float drainSpeed = UpgradeManager.Instance != null
                ? baseDrainSpeed + UpgradeManager.Instance.GetCurrentValue(UpgradeType.DepotDrainSpeed)
                : baseDrainSpeed;

            float toMove = Mathf.Min(drainSpeed * Time.deltaTime, _drainingBucket.CurrentWater, MaxCapacity - StoredWater);
            if (toMove <= 0f) return;

            float drained = _drainingBucket.DrainWater(toMove);
            StoredWater += drained;
            CurrencyManager.Instance.NotifyWaterChanged();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _drainingBucket = other.GetComponentInChildren<BucketController>();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _drainingBucket = null;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>StaticBucket gibi dış kaynakların su eklemesi için. Kabul edilen miktarı döndürür.</summary>
        public float AddWater(float amount)
        {
            float canAccept = Mathf.Max(0f, MaxCapacity - StoredWater);
            float accepted = Mathf.Min(amount, canAccept);
            StoredWater += accepted;
            if (accepted > 0f) CurrencyManager.Instance.NotifyWaterChanged();
            return accepted;
        }

        /// <summary>SpendCurrency tarafından çağrılır.</summary>
        public void RemoveWater(float amount)
        {
            StoredWater = Mathf.Max(0f, StoredWater - amount);
            CurrencyManager.Instance.NotifyWaterChanged();
        }

        // ── Upgrade ──────────────────────────────────────────────────────────────

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            if (type == UpgradeType.DepotCapacity || type == UpgradeType.BuyWaterDepot)
                RefreshFromUpgrades();
        }

        private void RefreshFromUpgrades()
        {
            if (UpgradeManager.Instance == null) return;

            float bonus = UpgradeManager.Instance.GetCurrentValue(UpgradeType.DepotCapacity);
            MaxCapacity = baseCapacity + bonus;

            int level = UpgradeManager.Instance.GetLevel(UpgradeType.DepotCapacity);
            ApplyDepotVisual(level);
        }

        /// <summary>
        /// level 0-2 → prefab[0], level 3-5 → prefab[1], level 6+ → prefab[2]
        /// </summary>
        private void ApplyDepotVisual(int level)
        {
            if (depotPrefabs == null || depotPrefabs.Length == 0) return;

            // Hangi prefab index'e düşeðini bul (eşiğleri geriden tara)
            int targetIndex = 0;
            for (int i = _prefabThresholds.Length - 1; i >= 0; i--)
            {
                if (level >= _prefabThresholds[i])
                {
                    targetIndex = i;
                    break;
                }
            }
            targetIndex = Mathf.Clamp(targetIndex, 0, depotPrefabs.Length - 1);

            // Prefab değişmiyorsa dokunma
            if (targetIndex == _currentPrefabIndex) return;

            GameObject prefab = depotPrefabs[targetIndex];
            if (prefab == null) return;

            // Eski görseli yok et
            if (_currentVisual != null) Destroy(_currentVisual);

            // Yeni görseli child olarak ekle
            _currentVisual = Instantiate(prefab, transform);
            _currentVisual.transform.localPosition = visualOffset;
            _currentVisual.transform.localRotation = Quaternion.identity;
            _currentPrefabIndex = targetIndex;
        }

        // ── Debug ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.green;

            GUILayout.BeginArea(new Rect(20, 80, 300, 40));
            GUILayout.Label($"Depo: {StoredWater:F1} / {MaxCapacity:F0} mL", style);
            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
