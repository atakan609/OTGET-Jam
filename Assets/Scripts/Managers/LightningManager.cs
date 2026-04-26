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

            // Rastgele X ofseti (bulutun hep merkezinden çıkmasın diye)
            float randomX = cloudX + Random.Range(-3f, 3f);

            // 15 birimlik aşağı doğru raycast at (Sadece spawn şartı)
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(randomX, cloudY),
                Vector2.down,
                15f, // Sadece 15 birim uzağa kadar bakar
                groundLayer
            );

            // Eğer zemin bulamazsa çık (spawn iptal)
            if (hit.collider == null)
            {
                return;
            }

            // "Yıldırımların konumu vb hiçbir şeyi bu raycast'e bağlama"
            // Raycast sadece şart sağlasın diye kullanıldı, pozisyonlar kendi fallback'imizden devam eder
            float groundY = fallbackGroundY;

            GameObject obj = Instantiate(lightningPrefab, new Vector3(randomX, cloudY, 0f), Quaternion.identity);
            Lightning lightning = obj.GetComponent<Lightning>();
            if (lightning != null)
                lightning.Initialize(randomX, cloudY, groundY);
        }

        private void ScheduleNext()
        {
            float min = Mathf.Max(2f, baseMinInterval);
            float max = Mathf.Max(min + 1f, baseMaxInterval);
            _nextSpawnTime = Time.time + Random.Range(min, max);
        }
    }
}
