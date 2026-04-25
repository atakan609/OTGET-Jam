using UnityEngine;
using Core;
using Gameplay;

namespace Managers
{
    public class LightningManager : Singleton<LightningManager>
    {
        [Header("Lightning Prefab")]
        [SerializeField] private GameObject lightningPrefab;

        [Header("Spawn Interval (Base)")]
        [SerializeField] private float baseMinInterval = 11f;
        [SerializeField] private float baseMaxInterval = 15f;

        [Header("Ground Detection")]
        [Tooltip("Yıldırımın zemini bulmak için attığı Raycast'in filter'ı. Ground layer'ı seçin.")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float raycastMaxDistance = 40f;
        [SerializeField] private float fallbackGroundY = -5f; // Zemin bulunamazsa varsayılan

        [Header("Magnet Values (Base) – İleride kaldırılacak")]
        [SerializeField] private float baseMagnetDuration = 3f;
        [SerializeField] private float baseMagnetRange = 4f;

        private float _nextSpawnTime;

        protected override void Awake()
        {
            base.Awake();
            ScheduleNext();
        }

        private void Update()
        {
            if (Time.time >= _nextSpawnTime)
            {
                TrySpawnFromCloud();
                ScheduleNext();
            }
        }

        // ── Spawn Logic ────────────────────────────────────────────────────────

        /// <summary>
        /// Aktif bulutlardan birinin tam pozisyonundan yıldırım çaktırır.
        /// Bulut yoksa sessizce geçer (spawn olmaz).
        /// </summary>
        private void TrySpawnFromCloud()
        {
            if (lightningPrefab == null) return;
            if (CloudManager.Instance == null) return;

            var clouds = CloudManager.Instance.GetActiveClouds();
            if (clouds == null || clouds.Count == 0) return;

            // Rastgele bir bulut seç; bulutun çerçeve-içi güncel pozisyonunu al
            var cloud = clouds[Random.Range(0, clouds.Count)];
            if (cloud == null) return;

            SpawnAt(cloud.transform.position.x, cloud.transform.position.y);
        }

        /// <summary>StormCloud gibi dış kaynaklar anlık yıldırım tetikleyebilir.</summary>
        public void SpawnImmediateLightning(float cloudX, float cloudY)
        {
            SpawnAt(cloudX, cloudY);
        }

        private void SpawnAt(float cloudX, float cloudY)
        {
            if (lightningPrefab == null) return;

            // Bulutun tam X'i üzerinden aşağıya Raycast at, zemin Y'sini bul
            float groundY = FindGroundY(cloudX, cloudY);

            GameObject obj = Instantiate(lightningPrefab, new Vector3(cloudX, cloudY, 0f), Quaternion.identity);
            Lightning lightning = obj.GetComponent<Lightning>();
            if (lightning != null)
                lightning.Initialize(cloudX, cloudY, groundY);
        }

        private float FindGroundY(float x, float fromY)
        {
            // Bulutun konumundan aşağıya Raycast: yalnızca Ground layer'a çarpar
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(x, fromY),
                Vector2.down,
                raycastMaxDistance,
                groundLayer
            );

            return hit.collider != null ? hit.point.y : fallbackGroundY;
        }

        private void ScheduleNext()
        {
            float freqBonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.LightningFrequency)
                : 0f;

            float min = Mathf.Max(2f, baseMinInterval - freqBonus);
            float max = Mathf.Max(min + 1f, baseMaxInterval - freqBonus);
            _nextSpawnTime = Time.time + Random.Range(min, max);
        }

        // ── Legacy API (BucketController hâlâ bunları okuyabilir) ─────────────
        public float GetMagnetDuration() => baseMagnetDuration;
        public float GetMagnetRange()    => baseMagnetRange;
    }
}
