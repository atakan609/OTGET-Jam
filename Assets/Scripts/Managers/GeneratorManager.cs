using System;
using System.Collections.Generic;
using UnityEngine;
using Gameplay;

namespace Managers
{
    [Serializable]
    public class GeneratorDefinition
    {
        public UpgradeType upgradeType;
        [Tooltip("Her seviye başına saniyede verilecek mL")]
        public float mlPerSecondPerLevel = 1f;
        [Tooltip("Oluşturulacak arka plan sprite prefabı")]
        public GameObject prefab;
        [Tooltip("İlk prefabın yerleşeceği pozisyon")]
        public Vector3 baseSpawnPosition;
        [Tooltip("Yeni seviyelerde ne kadar yukarı ekleneceği")]
        public float yStep = 0.3f;
        [Tooltip("Her seviyede ne kadar küçüleceği (0.05 = %5)")]
        public float shrinkFactor = 0.05f;
    }

    public class GeneratorManager : MonoBehaviour
    {
        [SerializeField] private GeneratorDefinition[] generators;

        private DepotController _depot;
        private Dictionary<UpgradeType, GeneratorDefinition> _lookup = new Dictionary<UpgradeType, GeneratorDefinition>();

        private void Awake()
        {
            if (generators == null) return;
            foreach (var gen in generators)
            {
                if (gen != null && !_lookup.ContainsKey(gen.upgradeType))
                    _lookup[gen.upgradeType] = gen;
            }
        }

        private void Start()
        {
            _depot = FindObjectOfType<DepotController>(true);
            UpgradeManager.OnUpgradePurchased += HandleUpgrade;

            // Oyun başladığında önceden alınmış seviyeler varsa objeleri spawn et
            if (UpgradeManager.Instance != null && generators != null)
            {
                foreach (var gen in generators)
                {
                    int lvl = UpgradeManager.Instance.GetLevel(gen.upgradeType);
                    for (int i = 1; i <= lvl; i++)
                    {
                        SpawnGeneratorSprite(gen, i);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= HandleUpgrade;
        }

        private void Update()
        {
            if (_depot == null || !_depot.gameObject.activeInHierarchy || UpgradeManager.Instance == null) return;

            float totalIncome = 0f;

            foreach (var kvp in _lookup)
            {
                int level = UpgradeManager.Instance.GetLevel(kvp.Key);
                if (level > 0)
                {
                    totalIncome += level * kvp.Value.mlPerSecondPerLevel;
                }
            }

            if (totalIncome > 0f)
            {
                _depot.AddWater(totalIncome * Time.deltaTime);
            }
        }

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            if (_lookup.TryGetValue(type, out var gen))
            {
                // Sadece yeni alınan level için bir tane spawn et
                SpawnGeneratorSprite(gen, newLevel);
            }
        }

        private void SpawnGeneratorSprite(GeneratorDefinition gen, int level)
        {
            if (gen.prefab == null) return;

            Vector3 pos = gen.baseSpawnPosition;
            pos.y += (level - 1) * gen.yStep; // Level 1 = 0 offset

            GameObject obj = Instantiate(gen.prefab, pos, Quaternion.identity);
            
            // Sıralamayı doğru yapmak için Z pozisyonunu da ayarlayabiliriz (arkaya doğru)
            Vector3 finalPos = obj.transform.position;
            finalPos.z += (level - 1) * 0.1f;
            obj.transform.position = finalPos;

            float shrink = Mathf.Pow(1f - gen.shrinkFactor, level - 1);
            obj.transform.localScale = obj.transform.localScale * shrink;
        }
    }
}
