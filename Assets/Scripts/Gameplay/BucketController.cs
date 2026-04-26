using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Gameplay
{
    // RequireComponent kaldırıldı — collider artık child prefabdan geliyor
    public class BucketController : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField] private float baseCapacity = 10f;



        [Header("Bucket Prefabs per Level (index = level / 2)")]
        [Tooltip("Her 2 BucketSize level'i için bir prefab. " +
                 "Prefab'ın içinde SpriteRenderer ve BoxCollider2D bulunmalı. " +
                 "Collider doğrudan prefab üzerinde kullanılır, parent'a kopyalanmaz.")]
        [SerializeField] private GameObject[] bucketPrefabs;

        [Header("Scale Settings")]
        [Tooltip("Her 2 level'den biri prefabı büyütür. Artış miktarı.")]
        [SerializeField] private float _scalePerStep = 0.25f;

        // ── State ──────────────────────────────────────────────────────────────
        public float CurrentWater { get; private set; }
        public float MaxCapacity  { get; private set; }
        public bool  IsFull       => CurrentWater >= MaxCapacity;
        public float FillRatio    => MaxCapacity > 0 ? CurrentWater / MaxCapacity : 0f;

        /// <summary>Idle'dayken true → kova açık ve su toplayabilir.</summary>
        public bool IsOpen { get; private set; } = true;

        // Görsel / collider referansları (child prefabdan)
        private SpriteRenderer _spriteRenderer;      // Aktif child'ın SpriteRenderer'ı
        private BoxCollider2D  _activeChildCollider; // Aktif child'ın BoxCollider2D'si
        private GameObject     _currentVisual;       // Instantiate edilmiş child objesi
        private int            _currentPrefabIndex = -1;

        // ── Unity ──────────────────────────────────────────────────────────────
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



        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// PlayerController tarafından çağrılır.
        /// true  → idle: kova açık, child collider aktif, su toplayabilir.
        /// false → koşuyor / stun: kova kapalı, child collider devre dışı.
        /// </summary>
        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;

            // Child prefabın collider'ını aç/kapat
            if (_activeChildCollider != null)
                _activeChildCollider.enabled = open;

            // Child prefabın sprite'ını göster/gizle
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = open;
        }

        /// <summary>Kovaya su ekler. Kova kapalı veya doluysa false döner.</summary>
        public bool TryAddWater(float amount)
        {
            if (!IsOpen) return false;
            if (IsFull)  return false;
            CurrentWater = Mathf.Min(CurrentWater + amount, MaxCapacity);
            CurrencyManager.Instance.NotifyWaterChanged();
            return true;
        }

        /// <summary>Belirtilen miktarda suyu kovadan boşaltır (depoya aktarım için).</summary>
        public float DrainWater(float amount)
        {
            float drained = Mathf.Min(amount, CurrentWater);
            CurrentWater -= drained;
            CurrencyManager.Instance.NotifyWaterChanged();
            return drained;
        }

        /// <summary>Yıldırım çarptığında belirtilen miktarda suyu sıçratır.</summary>
        public void SpillWater(float amount)
        {
            CurrentWater = Mathf.Max(0f, CurrentWater - amount);
            CurrencyManager.Instance.NotifyWaterChanged();
        }



        // ── Upgrade ────────────────────────────────────────────────────────────

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            if (type == UpgradeType.BucketSize) ApplyBucketSize(newLevel);
        }

        private void RefreshFromUpgrades()
        {
            if (UpgradeManager.Instance == null) return;
            int level = UpgradeManager.Instance.GetLevel(UpgradeType.BucketSize);
            ApplyBucketSize(level);
        }

        private void ApplyBucketSize(int level)
        {
            // ── Kapasite ────────────────────────────────────────────────────────
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.BucketSize)
                : 0f;
            MaxCapacity  = baseCapacity + bonus;
            CurrentWater = Mathf.Min(CurrentWater, MaxCapacity);

            // ── Prefab yoksa erken çık ──────────────────────────────────────────
            if (bucketPrefabs == null || bucketPrefabs.Length == 0) return;

            // Her 2 level'de bir prefab değişir:
            //   level 0 → prefab[0], scale ×1.00
            //   level 1 → prefab[0], scale ×1.25
            //   level 2 → prefab[1], scale ×1.00
            //   level 3 → prefab[1], scale ×1.25
            int prefabIndex  = Mathf.Clamp(level / 2, 0, bucketPrefabs.Length - 1);
            int stepInPrefab = level % 2;   // 0 = baz, 1 = büyütülmüş

            GameObject prefab = bucketPrefabs[prefabIndex];
            if (prefab == null) return;

            // ── Prefab değiştiyse yeniden instantiate et ───────────────────────
            if (_currentVisual == null || _currentPrefabIndex != prefabIndex)
            {
                if (_currentVisual != null) Destroy(_currentVisual);

                _currentVisual = Instantiate(prefab, transform);
                _currentVisual.transform.localPosition = Vector3.zero;
                _currentVisual.transform.localRotation = Quaternion.identity;
                _currentPrefabIndex = prefabIndex;

                // Child collider ve sprite referanslarını yenile
                _activeChildCollider = _currentVisual.GetComponentInChildren<BoxCollider2D>();
                _spriteRenderer      = _currentVisual.GetComponentInChildren<SpriteRenderer>();

                // Damlaların kovaya kuvvet uygulamaması için trigger'a çevir
                // (OnTriggerEnter2D hâlâ çalışır, fiziksel itme olmaz)
                if (_activeChildCollider != null)
                    _activeChildCollider.isTrigger = true;
            }

            // ── Scale (aynı prefab içinde büyüme) ─────────────────────────────
            float scaleMult = 1f + stepInPrefab * _scalePerStep;
            _currentVisual.transform.localScale = Vector3.one * scaleMult;

            // ── Açık/Kapalı durumu yeni child'a uygula ────────────────────────
            // SetOpen'ın guard'ını bypass etmek için önce IsOpen'ı ters yap
            bool wasOpen = IsOpen;
            IsOpen = !wasOpen;           // guard'ı aşmak için
            SetOpen(wasOpen);            // gerçek değeri tekrar gönder
        }

        // ── Debug ──────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle { fontSize = 18 };
            style.normal.textColor = IsFull ? Color.red : Color.cyan;

            GUILayout.BeginArea(new Rect(20, 50, 300, 60));
            GUILayout.Label($"Kova: {CurrentWater:F1} / {MaxCapacity:F0} mL", style);
            GUILayout.EndArea();
        }
    }
}
