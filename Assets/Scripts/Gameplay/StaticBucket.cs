using UnityEngine;
using Managers;

namespace Gameplay
{
    /// <summary>
    /// Depo upgrade'i olarak açılan, yere sabit konumlandırılmış otomatik toplayıcı kova.
    /// Kapasitesi çok sınırlıdır; biriken suyu belirli aralıklarla depoya gönderir.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StaticBucket : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float baseCapacity = 5f;
        [SerializeField] private float baseSendSpeed = 1f; // Saniyede depoya gönderilen su birimi

        public float CurrentWater { get; private set; }
        public float MaxCapacity { get; private set; }
        public bool IsFull => CurrentWater >= MaxCapacity;

        private DepotController _depot;
        private float _sendTimer;

        private void Awake()
        {
            MaxCapacity = baseCapacity;
            _depot = FindObjectOfType<DepotController>();

            // Collider'ı trigger yap — damlaların içinden geçip OnTriggerEnter2D'yi tetiklemesi için
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            // OnTriggerEnter2D'nin tetiklenebilmesi için Kinematic Rigidbody2D gerekli
            if (GetComponent<Rigidbody2D>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var drop = other.GetComponent<Raindrop>();
            if (drop == null) return;

            TryAddWater(drop.dropValue);
            Destroy(other.gameObject);
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
            if (CurrentWater <= 0f || _depot == null) return;

            float sendSpeed = UpgradeManager.Instance != null
                ? baseSendSpeed + UpgradeManager.Instance.GetCurrentValue(UpgradeType.AutoCollectorSendSpeed)
                : baseSendSpeed;

            float toSend = Mathf.Min(sendSpeed * Time.deltaTime, CurrentWater);
            float accepted = _depot.AddWater(toSend);
            CurrentWater -= accepted;
            if (accepted > 0f) CurrencyManager.Instance.NotifyWaterChanged();
        }

        /// <summary>Damladan su eklemeye çalışır. Kova doluysa false döner.</summary>
        public bool TryAddWater(float amount)
        {
            if (IsFull) return false;
            CurrentWater = Mathf.Min(CurrentWater + amount, MaxCapacity);
            CurrencyManager.Instance.NotifyWaterChanged();
            return true;
        }

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            if (type == UpgradeType.AutoCollectorCapacity) RefreshFromUpgrades();
        }

        private void RefreshFromUpgrades()
        {
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.AutoCollectorCapacity)
                : 0f;
            MaxCapacity = baseCapacity + bonus;
            CurrentWater = Mathf.Min(CurrentWater, MaxCapacity);
        }
    }
}

