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
        [SerializeField] private float baseDrainSpeed = 5f; // Saniyede kovadan depoya geçen su birimi
        [SerializeField] private float triggerRadius = 2f;

        public float StoredWater { get; private set; }
        public float MaxCapacity { get; private set; }
        public bool IsFull => StoredWater >= MaxCapacity;

        private BucketController _drainingBucket;

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
            if (type == UpgradeType.DepotCapacity) RefreshFromUpgrades();
        }

        private void RefreshFromUpgrades()
        {
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.DepotCapacity)
                : 0f;
            MaxCapacity = baseCapacity + bonus;
        }

        // ── Debug ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.green;

            GUILayout.BeginArea(new Rect(20, 80, 300, 40));
            GUILayout.Label($"Depo: {StoredWater:F1} / {MaxCapacity:F0}", style);
            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
