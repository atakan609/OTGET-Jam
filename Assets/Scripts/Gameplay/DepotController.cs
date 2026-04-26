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
        [SerializeField] private float baseCapacity  = 100f;
        [SerializeField] private float baseDrainSpeed = 5f;
        [SerializeField] private float triggerRadius  = 2f;

        [Header("Depot Prefabs per Tier")]
        [Tooltip("index 0 = Başlangıç (level 0-2), index 1 = Orta (level 3-5), index 2 = Gelişmiş (level 6+)")]
        [SerializeField] private GameObject[] depotPrefabs;
        [Tooltip("Depo prefabının doğacağı local offset (ana objeye göre).")]
        [SerializeField] private Vector3 visualOffset = Vector3.zero;

        [Header("Floating Text (Deposit)")]
        [Tooltip("FloatingWaterText scriptine sahip prefab.")]
        [SerializeField] private GameObject floatingTextPrefab;
        [Tooltip("Floating textin deponun üstünde doğacağı offset.")]
        [SerializeField] private Vector3 floatingTextOffset = new Vector3(0f, 1.5f, 0f);

        public float StoredWater { get; private set; }
        public float MaxCapacity  { get; private set; }
        public bool  IsFull       => StoredWater >= MaxCapacity;

        private BucketController _drainingBucket;
        private GameObject       _currentVisual;
        private int              _currentPrefabIndex = -1;

        // Kazara nested gelirse kendi scriptini kapat
        private bool _disabled = false;

        // Birikimli deposit floating text
        private FloatingWaterText _activeFloatingText;
        private bool _wasDepositing = false; // önceki frame'de deposit yapılıyor muydu

        // Kapasite upgrade eşiği → prefab index eşleme
        private static readonly int[] _prefabThresholds = { 0, 3, 6 };

        // ── Unity ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Görsel prefab içinde nested DepotController varsa kapat
            if (transform.parent != null && transform.parent.GetComponentInParent<DepotController>() != null)
            {
                _disabled = true;
                enabled = false;
                return;
            }
            MaxCapacity = baseCapacity;
        }

        private void Start()
        {
            if (_disabled) return;
            UpgradeManager.OnUpgradePurchased += HandleUpgrade;
            RefreshFromUpgrades();
        }

        private void OnDestroy()
        {
            if (!_disabled)
                UpgradeManager.OnUpgradePurchased -= HandleUpgrade;
        }

        private void Update()
        {
            if (_disabled) return;
            if (_drainingBucket == null || IsFull)
            {
                // Deposit bitti → floating text'i uçur
                if (_wasDepositing)
                    FinalizeDepositText();
                _wasDepositing = false;
                return;
            }

            float drainSpeed = UpgradeManager.Instance != null
                ? baseDrainSpeed + UpgradeManager.Instance.GetCurrentValue(UpgradeType.DepotDrainSpeed)
                : baseDrainSpeed;

            float toMove = Mathf.Min(drainSpeed * Time.deltaTime, _drainingBucket.CurrentWater, MaxCapacity - StoredWater);
            if (toMove <= 0f)
            {
                // Su tükendi → bitir
                if (_wasDepositing)
                    FinalizeDepositText();
                _wasDepositing = false;
                return;
            }

            float drained = _drainingBucket.DrainWater(toMove);
            StoredWater += drained;
            CurrencyManager.Instance.NotifyWaterChanged();

            // İlk deposit frame'inde text + ses başlat
            if (!_wasDepositing)
            {
                SpawnDepositText();
                SoundManager.Instance?.StartDepositLoop();
                _wasDepositing = true;
            }
            else
            {
                // Biriken miktarı artır
                _activeFloatingText?.AddAmount(drained);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_disabled) return;
            if (other.CompareTag("Player"))
                _drainingBucket = other.GetComponentInChildren<BucketController>();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_disabled) return;
            if (other.CompareTag("Player"))
            {
                if (_wasDepositing)
                    FinalizeDepositText();
                _wasDepositing = false;
                _drainingBucket = null;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>StaticBucket gibi dış kaynakların su eklemesi için. Kabul edilen miktarı döndürür.</summary>
        public float AddWater(float amount)
        {
            float canAccept = Mathf.Max(0f, MaxCapacity - StoredWater);
            float accepted  = Mathf.Min(amount, canAccept);
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

        // ── Floating Text (Deposit) ──────────────────────────────────────────────

        private void SpawnDepositText()
        {
            if (floatingTextPrefab == null) return;

            // Önceki text hala varsa önce uçur
            if (_activeFloatingText != null)
            {
                _activeFloatingText.Finalize();
                _activeFloatingText = null;
            }

            GameObject obj = Instantiate(floatingTextPrefab, transform.position + floatingTextOffset, Quaternion.identity);
            _activeFloatingText = obj.GetComponent<FloatingWaterText>();
            // İlk miktarı 0 ile aç; Update içinde AddAmount ile arttırılacak
            _activeFloatingText?.AddAmount(0f);
        }

        private void FinalizeDepositText()
        {
            _activeFloatingText?.Finalize();
            _activeFloatingText = null;
            SoundManager.Instance?.StopDepositLoop();
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

        /// <summary>level 0-2 → prefab[0], level 3-5 → prefab[1], level 6+ → prefab[2]</summary>
        private void ApplyDepotVisual(int level)
        {
            if (depotPrefabs == null || depotPrefabs.Length == 0) return;

            int targetIndex = 0;
            for (int i = _prefabThresholds.Length - 1; i >= 0; i--)
            {
                if (level >= _prefabThresholds[i]) { targetIndex = i; break; }
            }
            targetIndex = Mathf.Clamp(targetIndex, 0, depotPrefabs.Length - 1);

            if (targetIndex == _currentPrefabIndex) return;

            GameObject prefab = depotPrefabs[targetIndex];
            if (prefab == null) return;

            if (_currentVisual != null) Destroy(_currentVisual);

            _currentVisual = Instantiate(prefab, transform);
            _currentVisual.transform.localPosition = visualOffset;
            _currentVisual.transform.localRotation = Quaternion.identity;
            _currentPrefabIndex = targetIndex;
        }

        // ── Debug ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_disabled) return;
            GUIStyle style = new GUIStyle { fontSize = 16 };
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
