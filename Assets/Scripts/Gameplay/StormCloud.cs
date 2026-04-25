using UnityEngine;
using Managers;

namespace Gameplay
{
    /// <summary>
    /// Normal Cloud'dan farklı olarak yoğun yağmur yağdırır, yıldırım üretir
    /// ve belirli bir süre sonra kendiliğinden sahneyi terk eder.
    /// </summary>
    public class StormCloud : MonoBehaviour
    {
        [Header("Rain Settings")]
        [SerializeField] private GameObject raindropPrefab;
        [SerializeField] private GameObject goldenRaindropPrefab;
        [SerializeField] private float minDropInterval = 0.05f;
        [SerializeField] private float maxDropInterval = 0.2f;
        [SerializeField] private float spawnRangeX = 3f;
        [SerializeField] private Transform dropSpawnPoint;

        [Header("Lightning")]
        [SerializeField] private float lightningChance = 0.15f; // Her damla spawnnda yıldırım şansı

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.2f;
        public float moveDirection = 1f;

        [Header("Screen Bounds")]
        [SerializeField] private float screenLeftX = -22f;
        [SerializeField] private float screenRightX = 22f;

        private float _rainTimer;
        private float _nextDropTime;
        private float _lifetime;
        private float _lifetimeTimer;

        private void Start()
        {
            SetNextRainTime();

            // Fırtına süresi upgrade'den hesaplanır
            float baseDuration = 10f;
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.StormDuration)
                : 0f;
            _lifetime = baseDuration + bonus;
        }

        private void Update()
        {
            HandleMovement();
            HandleRaining();
            HandleLifetime();
        }

        private void HandleMovement()
        {
            Vector3 pos = transform.position;
            pos.x += moveDirection * moveSpeed * Time.deltaTime;

            if (moveDirection > 0 && pos.x > screenRightX) Destroy(gameObject);
            else if (moveDirection < 0 && pos.x < screenLeftX) Destroy(gameObject);

            transform.position = pos;
        }

        private void HandleRaining()
        {
            if (raindropPrefab == null) return;

            _rainTimer += Time.deltaTime;
            if (_rainTimer >= _nextDropTime)
            {
                SpawnRaindrop();
                _rainTimer = 0f;
                SetNextRainTime();
            }
        }

        private void HandleLifetime()
        {
            _lifetimeTimer += Time.deltaTime;
            if (_lifetimeTimer >= _lifetime)
                Destroy(gameObject);
        }

        [SerializeField] private float baseMinDrop = 1.0f;
        [SerializeField] private float baseMaxDrop = 2.0f;

        private void SpawnRaindrop()
        {
            Vector3 spawnPos = dropSpawnPoint != null ? dropSpawnPoint.position : transform.position;
            spawnPos.x += Random.Range(-spawnRangeX, spawnRangeX);

            // Altın damla şansı
            float goldenChance = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.GoldenDropChance)
                : 0f;

            bool spawnGolden = goldenRaindropPrefab != null && Random.value < goldenChance;
            GameObject drop = Instantiate(spawnGolden ? goldenRaindropPrefab : raindropPrefab, spawnPos, Quaternion.identity);

            // Boyut ve Çarpan hesaplama
            var raindrop = drop.GetComponent<Raindrop>();
            if (raindrop != null)
            {
                float minSize = baseMinDrop;
                float maxSize = baseMaxDrop;
                float multiplier = 1f;

                if (UpgradeManager.Instance != null)
                {
                    minSize += UpgradeManager.Instance.GetCurrentValue(UpgradeType.MinDropSize);
                    maxSize += UpgradeManager.Instance.GetCurrentValue(UpgradeType.MaxDropSize);
                    multiplier += UpgradeManager.Instance.GetCurrentValue(UpgradeType.DropMultiplier);
                }

                raindrop.dropValue = Random.Range(minSize, maxSize) * multiplier;
            }

            // Yıldırım şansı: bağımsız rastgele X konumunda yıldırım düşür
            if (Random.value < lightningChance && LightningManager.Instance != null)
            {
                float lightningX = Random.Range(screenLeftX * 0.6f, screenRightX * 0.6f);
                LightningManager.Instance.SpawnImmediateLightning(lightningX);
            }
        }

        private void SetNextRainTime()
        {
            _nextDropTime = Random.Range(minDropInterval, maxDropInterval);
        }
    }
}
