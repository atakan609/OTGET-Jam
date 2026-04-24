using System.Collections.Generic;
using UnityEngine;
using Core;
using Gameplay;

namespace Managers
{
    public class CloudManager : Singleton<CloudManager>
    {
        [Header("Cloud Spawn Settings")]
        [SerializeField] private GameObject[] cloudPrefabs;
        [SerializeField] private int baseCloudCount = 5;

        [Range(0f, 1f)]
        [SerializeField] private float baseRainingCloudChance = 0.4f;

        [Header("Spawn Settings")]
        [SerializeField] private float minSpawnY = 3f;
        [SerializeField] private float maxSpawnY = 6f;
        [SerializeField] private float offScreenOffset = 22f;
        [SerializeField] private float minSpawnInterval = 2f;

        [Header("Scale Range")]
        [SerializeField] private float minCloudScale = 0.8f;
        [SerializeField] private float maxCloudScale = 1.5f;

        [Header("Storm Cloud")]
        [SerializeField] private GameObject stormCloudPrefab;
        [SerializeField] private float baseStormMinInterval = 30f;
        [SerializeField] private float baseStormMaxInterval = 90f;

        private List<Cloud> _activeClouds = new List<Cloud>();
        private float _lastSpawnTime;
        private float _nextStormTime;

        // ── Upgrade-driven properties ──────────────────────────────────────────

        private int TargetCloudCount => baseCloudCount + (UpgradeManager.Instance != null
            ? Mathf.RoundToInt(UpgradeManager.Instance.GetCurrentValue(UpgradeType.CloudCount))
            : 0);

        private float RainingChance => Mathf.Clamp01(baseRainingCloudChance + (UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.RainingCloudChance)
            : 0f));

        // ── Unity ─────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            ScheduleNextStorm();
        }

        private void Update()
        {
            // Normal bulut kontrolü
            _activeClouds.RemoveAll(item => item == null);
            if (_activeClouds.Count < TargetCloudCount && Time.time >= _lastSpawnTime + minSpawnInterval)
            {
                SpawnSingleCloud();
                _lastSpawnTime = Time.time;
            }

            // Fırtına bulutu kontrolü
            if (Time.time >= _nextStormTime)
            {
                SpawnStormCloud();
                ScheduleNextStorm();
            }
        }

        // ── Normal Cloud ───────────────────────────────────────────────────────

        private void SpawnSingleCloud()
        {
            if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;

            float direction = Random.value > 0.5f ? 1f : -1f;
            float spawnX = (direction > 0) ? -offScreenOffset : offScreenOffset;
            spawnX += Random.Range(-5f, 5f);

            Vector3 spawnPos = new Vector3(spawnX, Random.Range(minSpawnY, maxSpawnY), 0f);

            GameObject selectedPrefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
            GameObject cloudObj = Instantiate(selectedPrefab, spawnPos, Quaternion.identity, transform);

            float randomScale = Random.Range(minCloudScale, maxCloudScale);
            cloudObj.transform.localScale = new Vector3(randomScale, randomScale, 1f);

            Cloud cloudComp = cloudObj.GetComponent<Cloud>();
            if (cloudComp != null)
            {
                cloudComp.moveDirection = direction;
                cloudComp.isRaining = (Random.value < RainingChance);
                _activeClouds.Add(cloudComp);
            }
        }

        // ── Storm Cloud ────────────────────────────────────────────────────────

        private void SpawnStormCloud()
        {
            if (stormCloudPrefab == null) return;

            float direction = Random.value > 0.5f ? 1f : -1f;
            float spawnX = (direction > 0) ? -offScreenOffset : offScreenOffset;

            Vector3 spawnPos = new Vector3(spawnX, Random.Range(minSpawnY, maxSpawnY), 0f);
            GameObject stormObj = Instantiate(stormCloudPrefab, spawnPos, Quaternion.identity, transform);

            // Fırtına bulutları her zaman büyük
            stormObj.transform.localScale = Vector3.one * maxCloudScale * 1.4f;

            StormCloud stormComp = stormObj.GetComponent<StormCloud>();
            if (stormComp != null)
                stormComp.moveDirection = direction;
        }

        private void ScheduleNextStorm()
        {
            // StormFrequency upgrade: interval azaltır
            float freqBonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.StormFrequency)
                : 0f;

            float min = Mathf.Max(10f, baseStormMinInterval - freqBonus);
            float max = Mathf.Max(min + 5f, baseStormMaxInterval - freqBonus);
            _nextStormTime = Time.time + Random.Range(min, max);
        }
    }
}
