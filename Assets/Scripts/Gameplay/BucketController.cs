using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;
using Gameplay;

namespace Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class BucketController : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField] private float baseCapacity = 10f;

        [Header("Magnet")]
        [SerializeField] private float magnetAttractionSpeed = 8f;

        [Header("Sprite per Bucket Level (index = level)")]
        [SerializeField] private Sprite[] bucketSprites;

        [Header("Collider Width per Bucket Level (index = level)")]
        [SerializeField] private float[] bucketColliderWidths;

        // ── State ──────────────────────────────────────────────────────────────
        public float CurrentWater { get; private set; }
        public float MaxCapacity { get; private set; }
        public bool IsFull => CurrentWater >= MaxCapacity;
        public float FillRatio => MaxCapacity > 0 ? CurrentWater / MaxCapacity : 0f;

        public bool IsMagnetized { get; private set; }
        public float MagnetRange { get; private set; }

        private float _magnetTimer;
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _collider;

        // ── Unity ──────────────────────────────────────────────────────────────
        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
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
            // Mıknatıs geri sayımı
            if (IsMagnetized)
            {
                _magnetTimer -= Time.deltaTime;
                if (_magnetTimer <= 0f) IsMagnetized = false;
            }
        }

        private void FixedUpdate()
        {
            if (!IsMagnetized) return;

            // Yarıçap içindeki tüm Raindrop'ları kovaya doğru çek
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, MagnetRange);
            foreach (var col in nearby)
            {
                if (col.TryGetComponent<Raindrop>(out _))
                {
                    var rb = col.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 dir = ((Vector2)transform.position - rb.position).normalized;
                        rb.linearVelocity = dir * magnetAttractionSpeed;
                    }
                }
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Kovaya su eklemeye çalışır. Kova doluysa false döner.</summary>
        public bool TryAddWater(float amount)
        {
            if (IsFull) return false;
            CurrentWater = Mathf.Min(CurrentWater + amount, MaxCapacity);

            // CurrencyManager'ı haberdar et
            CurrencyManager.Instance.NotifyWaterChanged();
            return true;
        }

        /// <summary>Kovadaki tüm suyu döndürür ve kovayı sıfırlar (depoya aktarım için).</summary>
        public float DrainWater(float amount)
        {
            float drained = Mathf.Min(amount, CurrentWater);
            CurrentWater -= drained;
            CurrencyManager.Instance.NotifyWaterChanged();
            return drained;
        }

        /// <summary>Mıknatısı aktifleştirir.</summary>
        public void ActivateMagnet(float duration, float range)
        {
            IsMagnetized = true;
            _magnetTimer = duration;
            MagnetRange = range;
        }

        // ── Upgrade ────────────────────────────────────────────────────────────

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            if (type == UpgradeType.BucketSize)         ApplyBucketSize(newLevel);
            if (type == UpgradeType.MagnetDuration)     { /* UpgradeManager'dan okunur, burada saklamaya gerek yok */ }
            if (type == UpgradeType.MagnetRange)        { }
        }

        private void RefreshFromUpgrades()
        {
            if (UpgradeManager.Instance == null) return;
            int bucketLevel = UpgradeManager.Instance.GetLevel(UpgradeType.BucketSize);
            ApplyBucketSize(bucketLevel);
        }

        private void ApplyBucketSize(int level)
        {
            // Kapasite
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.BucketSize)
                : 0f;
            MaxCapacity = baseCapacity + bonus;
            CurrentWater = Mathf.Min(CurrentWater, MaxCapacity);

            // Collider genişliği
            if (_collider != null && bucketColliderWidths != null && level < bucketColliderWidths.Length)
            {
                var size = _collider.size;
                size.x = bucketColliderWidths[level];
                _collider.size = size;
            }

            // Sprite
            if (_spriteRenderer != null && bucketSprites != null && level < bucketSprites.Length)
            {
                _spriteRenderer.sprite = bucketSprites[level];
            }
        }

        // ── Debug ──────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = IsFull ? Color.red : Color.cyan;

            string magnetInfo = IsMagnetized ? $" [MAG {_magnetTimer:F1}s]" : "";
            GUILayout.BeginArea(new Rect(20, 50, 300, 60));
            GUILayout.Label($"Kova: {CurrentWater:F1} / {MaxCapacity:F0} mL{magnetInfo}", style);
            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsMagnetized) return;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, MagnetRange);
        }
    }
}
