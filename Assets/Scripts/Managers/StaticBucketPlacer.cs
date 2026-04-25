using System.Collections.Generic;
using UnityEngine;
using Gameplay;
using Core;

namespace Managers
{
    /// <summary>
    /// Oyuncu sol tıkladığında dünyaya StaticBucket yerleştirir.
    /// AutoCollectorCount upgrade'i max yerleştirilebilecek kova sayısını belirler.
    /// Eğer limit doluysa en eski kova kaldırılır (FIFO).
    /// </summary>
    public class StaticBucketPlacer : Singleton<StaticBucketPlacer>
    {
        [Header("References")]
        [SerializeField] private GameObject staticBucketPrefab;
        [SerializeField] private Camera mainCamera;

        [Header("Placement Settings")]
        [Tooltip("StaticBucket'ların yerleştirilebileceği katman (yer, platform vs.)")]
        [SerializeField] private LayerMask placementLayerMask = ~0;
        [SerializeField] private int baseMaxBuckets = 1; // Upgrade olmadan başlangıç limiti

        [Header("Preview")]
        [SerializeField] private GameObject placementPreviewPrefab; // İsteğe bağlı, yarı saydam önizleme
        [SerializeField] private KeyCode placeKey = KeyCode.Mouse0; // Sol tık
        [SerializeField] private KeyCode removeKey = KeyCode.Mouse1; // Sağ tık ile kaldır

        private readonly Queue<GameObject> _placedBuckets = new Queue<GameObject>();
        private GameObject _previewInstance;
        private bool _isPlacementMode = false;

        // ── Unity ─────────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Start()
        {
            UpgradeManager.OnUpgradePurchased += HandleUpgrade;
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= HandleUpgrade;
            if (_previewInstance != null) Destroy(_previewInstance);
        }

        private void Update()
        {
            if (!_isPlacementMode) return;

            UpdatePreview();

            if (Input.GetKeyDown(placeKey))
                TryPlace();

            if (Input.GetKeyDown(removeKey))
                ExitPlacementMode();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Placement modunu açar. UIManager gibi bir yerden çağrılabilir.</summary>
        public void EnterPlacementMode()
        {
            _isPlacementMode = true;

            if (placementPreviewPrefab != null && _previewInstance == null)
                _previewInstance = Instantiate(placementPreviewPrefab);
        }

        public void ExitPlacementMode()
        {
            _isPlacementMode = false;

            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }
        }

        public bool IsInPlacementMode => _isPlacementMode;

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

        private void UpdatePreview()
        {
            if (_previewInstance == null) return;

            Vector2 worldPos = GetMouseWorldPosition();
            _previewInstance.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }

        private void TryPlace()
        {
            if (staticBucketPrefab == null)
            {
                Debug.LogWarning("[StaticBucketPlacer] staticBucketPrefab atanmamış!");
                return;
            }

            Vector2 worldPos = GetMouseWorldPosition();

            // Limit doluysa en eski kovayı kaldır (FIFO)
            while (_placedBuckets.Count >= MaxBuckets)
            {
                var oldest = _placedBuckets.Dequeue();
                if (oldest != null) Destroy(oldest);
            }

            GameObject newBucket = Instantiate(staticBucketPrefab, new Vector3(worldPos.x, worldPos.y, 0f), Quaternion.identity);
            _placedBuckets.Enqueue(newBucket);
        }

        private Vector2 GetMouseWorldPosition()
        {
            if (mainCamera == null) return Vector2.zero;
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = Mathf.Abs(mainCamera.transform.position.z);
            return mainCamera.ScreenToWorldPoint(mouseScreen);
        }

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            // AutoCollectorCount artınca mevcut kova sayısı limitin üstündeyse kaldır
            if (type == UpgradeType.AutoCollectorCount)
            {
                while (_placedBuckets.Count > MaxBuckets)
                {
                    var oldest = _placedBuckets.Dequeue();
                    if (oldest != null) Destroy(oldest);
                }
            }
        }

        // ── Debug ─────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(20, 120, 300, 40));
            GUILayout.Label($"Sabit Kova: {PlacedCount} / {MaxBuckets}{(_isPlacementMode ? " [YERLEŞTIRME MODU]" : "")}", style);
            GUILayout.EndArea();
        }
    }
}
