using UnityEngine;
using Managers;

namespace Gameplay
{
    public class Cloud : MonoBehaviour
    {
        [Header("Rain Settings")]
        public bool isRaining = false;
        [SerializeField] private GameObject raindropPrefab;
        [SerializeField] private GameObject goldenRaindropPrefab; // Altın damla prefab'ı
        [SerializeField] private float minDropInterval = 0.5f;
        [SerializeField] private float maxDropInterval = 2f;
        [SerializeField] private Transform dropSpawnPoint;
        [SerializeField] private float spawnRangeX = 1.5f;

        [Header("Movement (Perlin Noise)")]
        [SerializeField] private float baseSpeedMultiplier = 1f;
        [SerializeField] private float windChangeSpeed = 0.1f;
        [SerializeField] private float minWindSpeed = 0.5f;
        [SerializeField] private float maxWindSpeed = 2.5f;

        // 1 = Sağa doğru, -1 = Sola doğru ilerler
        public float moveDirection = 1f;

        [Header("Vertical Bobbing")]
        [SerializeField] private float bobAmplitude = 0.3f;
        [SerializeField] private float bobFrequency = 0.5f;

        [Header("Screen Bounds")]
        [SerializeField] private float screenLeftX = -20f;
        [SerializeField] private float screenRightX = 20f;

        private float _noiseOffset;
        private float _phaseOffset;
        private float _rainTimer;
        private float _nextDropTime;
        private float _baseY;

        private void Start()
        {
            _noiseOffset = Random.Range(0f, 1000f);
            _phaseOffset = Random.Range(0f, Mathf.PI * 2);
            _baseY = transform.position.y;
            SetNextRainTime();
        }

        private void Update()
        {
            HandleMovement();
            if (isRaining) HandleRaining();
        }

        private void HandleMovement()
        {
            float noiseValue = Mathf.PerlinNoise(Time.time * windChangeSpeed + _noiseOffset, 0f);
            float windForce = Mathf.Lerp(minWindSpeed, maxWindSpeed, noiseValue) * moveDirection;
            float verticalOffset = Mathf.Sin(Time.time * bobFrequency + _phaseOffset) * bobAmplitude;

            Vector3 pos = transform.position;
            pos.x += windForce * baseSpeedMultiplier * Time.deltaTime;
            pos.y = _baseY + verticalOffset;

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

        [SerializeField] private float baseMinDrop = 0.5f;
        [SerializeField] private float baseMaxDrop = 1.5f;

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
        }

        private void SetNextRainTime()
        {
            // RainFrequency upgrade: interval azaltır (max 0.1f alt sınır)
            float freqBonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.RainFrequency)
                : 0f;

            float adjustedMin = Mathf.Max(0.1f, minDropInterval - freqBonus);
            float adjustedMax = Mathf.Max(adjustedMin + 0.1f, maxDropInterval - freqBonus);
            _nextDropTime = Random.Range(adjustedMin, adjustedMax);
        }
    }
}
