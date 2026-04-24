using UnityEngine;
using Core;
using Gameplay;

namespace Managers
{
    public class LightningManager : Singleton<LightningManager>
    {
        [Header("Lightning Settings")]
        [SerializeField] private GameObject lightningPrefab;

        [Header("Spawn Interval (Base)")]
        [SerializeField] private float baseMinInterval = 8f;
        [SerializeField] private float baseMaxInterval = 20f;

        [Header("Spawn Bounds")]
        [SerializeField] private float spawnMinX = -10f;
        [SerializeField] private float spawnMaxX = 10f;
        [SerializeField] private float spawnY = 12f;

        [Header("Magnet Values (Base)")]
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
                SpawnLightning();
                ScheduleNext();
            }
        }

        private void SpawnLightning()
        {
            if (lightningPrefab == null) return;

            float x = Random.Range(spawnMinX, spawnMaxX);
            Vector3 pos = new Vector3(x, spawnY, 0f);
            Instantiate(lightningPrefab, pos, Quaternion.identity);
        }

        private void ScheduleNext()
        {
            // LightningFrequency upgrade: interval azaltır
            float freqBonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.LightningFrequency)
                : 0f;

            float min = Mathf.Max(2f, baseMinInterval - freqBonus);
            float max = Mathf.Max(min + 1f, baseMaxInterval - freqBonus);
            _nextSpawnTime = Time.time + Random.Range(min, max);
        }

        // ── Public API (Lightning.cs tarafından okunur) ─────────────────────────

        public float GetMagnetDuration()
        {
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.MagnetDuration)
                : 0f;
            return baseMagnetDuration + bonus;
        }

        public float GetMagnetRange()
        {
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.MagnetRange)
                : 0f;
            return baseMagnetRange + bonus;
        }

        /// <summary>Fırtına bulutu gibi dış kaynaklar anlık yıldırım spawn edebilir.</summary>
        public void SpawnImmediateLightning(float x)
        {
            if (lightningPrefab == null) return;
            Vector3 pos = new Vector3(x, spawnY, 0f);
            Instantiate(lightningPrefab, pos, Quaternion.identity);
        }
    }
}
