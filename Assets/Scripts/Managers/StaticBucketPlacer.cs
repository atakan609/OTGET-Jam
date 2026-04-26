using System.Collections.Generic;
using UnityEngine;
using Gameplay;
using Core;

namespace Managers
{
    /// <summary>
    /// AutoCollectorCount upgrade'i satın alındığında Ground layer üzerinde
    /// eşit aralıklarla StaticBucket'ları otomatik spawn eder.
    /// Upgrade seviyesi düşerse fazla kovalar silinir; artar ise yenileri eklenir.
    /// </summary>
    public class StaticBucketPlacer : Singleton<StaticBucketPlacer>
    {
        [Header("References")]
        [SerializeField] private GameObject staticBucketPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Ground layer'ı seçin. Kovalar bu layer'ın yüzeyine yerleştirilir.")]
        [SerializeField] private LayerMask groundLayer;

        [Tooltip("Kova spawn noktalarının yayıldığı X ekseni aralığı. (örn. -8 ile +8 arası)")]
        [SerializeField] private float spawnRangeMinX = -8f;
        [SerializeField] private float spawnRangeMaxX =  8f;

        [Tooltip("Raycast'in başlayacağı Y yüksekliği.")]
        [SerializeField] private float raycastFromY = 10f;

        [Tooltip("Upgrade olmadan başlangıçta spawn edilecek kova sayısı (0 = başta hiç yok).")]
        [SerializeField] private int baseMaxBuckets = 0;

        // Aktif kovalar; index sırayla yönetilir
        private readonly List<GameObject> _placedBuckets = new List<GameObject>();

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Start()
        {
            UpgradeManager.OnUpgradePurchased += HandleUpgrade;
            SyncBuckets(); // Başlangıçta baseMaxBuckets kadar kova spawn et
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= HandleUpgrade;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Şu an kaç kova yerleştirilmiş?</summary>
        public int PlacedCount => _placedBuckets.Count;

        /// <summary>Mevcut upgrade ile kaç kovaya izin veriliyor?</summary>
        public int MaxBuckets
        {
            get
            {
                if (UpgradeManager.Instance == null) return baseMaxBuckets;
                int bonus = Mathf.RoundToInt(UpgradeManager.Instance.GetCurrentValue(UpgradeType.AutoCollectorCount));
                return baseMaxBuckets + bonus;
            }
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            if (type == UpgradeType.AutoCollectorCount)
                SyncBuckets();
        }

        /// <summary>
        /// Aktif kova sayısını MaxBuckets'a eşitler.
        /// Fazla varsa siler, eksik varsa ekler.
        /// </summary>
        private void SyncBuckets()
        {
            int target = MaxBuckets;

            // Fazla kovaları sil (upgrade downsell vs.)
            while (_placedBuckets.Count > target)
            {
                int last = _placedBuckets.Count - 1;
                if (_placedBuckets[last] != null)
                    Destroy(_placedBuckets[last]);
                _placedBuckets.RemoveAt(last);
            }

            // Eksik kovaları ekle
            while (_placedBuckets.Count < target)
                SpawnOneBucket();
        }

        /// <summary>
        /// Ground layer üzerinde mevcut kova sayısını göz önüne alarak
        /// eşit aralıklı bir X pozisyonu hesaplar ve oraya Raycast ile kova yerleştirir.
        /// </summary>
        private void SpawnOneBucket()
        {
            if (staticBucketPrefab == null)
            {
                Debug.LogWarning("[StaticBucketPlacer] staticBucketPrefab atanmamış!");
                return;
            }

            // Toplam kova sayısına (hedef) göre eşit aralık hesapla
            int target     = MaxBuckets;
            int index      = _placedBuckets.Count; // Şu anki index (0-based)
            float rangeW   = spawnRangeMaxX - spawnRangeMinX;

            // Tek kova ise ortaya, birden fazlaysa eşit aralıklı
            float x;
            if (target <= 1)
            {
                x = (spawnRangeMinX + spawnRangeMaxX) * 0.5f;
            }
            else
            {
                float step = rangeW / (target - 1);
                x = spawnRangeMinX + index * step;
            }

            // Ground layer'ı bul
            Vector2 rayOrigin = new Vector2(x, raycastFromY);
            RaycastHit2D hit  = Physics2D.Raycast(rayOrigin, Vector2.down, raycastFromY * 2f, groundLayer);

            Vector3 spawnPos;
            if (hit.collider != null)
            {
                // Zeminin tam üzerine koy
                spawnPos = new Vector3(x, hit.point.y, 0f);
            }
            else
            {
                // Zemin bulunamazsa varsayılan Y = 0
                Debug.LogWarning($"[StaticBucketPlacer] X={x} konumunda Ground bulunamadı, Y=0 kullanılıyor.");
                spawnPos = new Vector3(x, 0f, 0f);
            }

            GameObject bucket = Instantiate(staticBucketPrefab, spawnPos, Quaternion.identity);
            _placedBuckets.Add(bucket);
        }

        // ── Debug ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (GameManager.Instance != null && !GameManager.Instance.ShowDebugUI) return;

            GUIStyle style = new GUIStyle { fontSize = 14 };
            style.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(20, 120, 300, 40));
            GUILayout.Label($"Sabit Kova: {PlacedCount} / {MaxBuckets}", style);
            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            // Spawn aralığını editörde göster
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(spawnRangeMinX, -1f, 0f), new Vector3(spawnRangeMaxX, -1f, 0f));

            // Her potansiyel spawn noktasını göster
            int target = MaxBuckets > 0 ? MaxBuckets : 1;
            for (int i = 0; i < target; i++)
            {
                float x;
                if (target <= 1)
                    x = (spawnRangeMinX + spawnRangeMaxX) * 0.5f;
                else
                    x = spawnRangeMinX + i * ((spawnRangeMaxX - spawnRangeMinX) / (target - 1));

                Gizmos.DrawWireSphere(new Vector3(x, 0f, 0f), 0.3f);
            }
        }
    }
}
