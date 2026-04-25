using Core;
using Gameplay;
using UnityEngine;

namespace Managers
{
    public class LightningManager : Singleton<LightningManager>
    {
        [Header("Lightning Settings")]
        [SerializeField]
        private GameObject lightningPrefab;

        [Header("Spawn Interval (Base)")]
        [SerializeField]
        private float baseMinInterval = 8f;

        [SerializeField]
        private float baseMaxInterval = 20f;

        [Header("Fallback Spawn (Bulut Yoksa)")]
        [SerializeField]
        private float spawnMinX = -10f;

        [SerializeField]
        private float spawnMaxX = 10f;

        [SerializeField]
        private float fallbackSpawnY = 12f;

        [Header("Lightning Y Offset (Bulutun Altından)")]
        [SerializeField]
        private float cloudSpawnYOffset = -0.5f;

        [Header("Magnet Values (Base)")]
        [SerializeField]
        private float baseMagnetDuration = 3f;

        [SerializeField]
        private float baseMagnetRange = 4f;

        [Header("Visuals")]
        [SerializeField]
        private float lightningZOffset = 1f; // Bulutların arkasında kalması için (Pozitif Z)

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
            if (lightningPrefab == null)
                return;

            Vector3 cloudPos = GetRandomCloudPosition();
            float groundY = FindGroundY(cloudPos.x, cloudPos.y);

            // Z eksenini bulutun arkasına atıyoruz
            Vector3 spawnPos = new Vector3(cloudPos.x, cloudPos.y, lightningZOffset);

            GameObject obj = Instantiate(lightningPrefab, spawnPos, Quaternion.identity);
            var lightning = obj.GetComponent<Lightning>();
            lightning?.Initialize(cloudPos.x, cloudPos.y, groundY);
        }

        /// <summary>Sahnedeki aktif bulutlardan birini rastgele seçer.</summary>
        private Vector3 GetRandomCloudPosition()
        {
            var clouds = FindObjectsByType<Cloud>(FindObjectsSortMode.None);
            var storms = FindObjectsByType<StormCloud>(FindObjectsSortMode.None);
            int total = clouds.Length + storms.Length;

            if (total == 0)
                return new Vector3(Random.Range(spawnMinX, spawnMaxX), fallbackSpawnY, 0f);

            int index = Random.Range(0, total);
            return index < clouds.Length
                ? clouds[index].transform.position
                : storms[index - clouds.Length].transform.position;
        }

        /// <summary>Verilen X konumundan aşağıya Raycast atarak zemin Y'sini bulur.</summary>
        private float FindGroundY(float x, float fromY)
        {
            // Raycast'i ekranın yeterince yukarısından başlat (zemin kaçmasın)
            Vector2 startPos = new Vector2(x, fromY);

            RaycastHit2D hit = Physics2D.Raycast(
                startPos,
                Vector2.down,
                10f,
                LayerMask.GetMask("Ground")
            );

            // Editörde görmek için sahne ekranına çiz (Scene view)
            Debug.DrawRay(
                startPos,
                Vector2.down * 10f,
                hit.collider != null ? Color.green : Color.red,
                0.5f
            );

            if (hit.collider != null)
            {
                return hit.point.y + 5f;
            }

            // Geçici fallback (kendi zemin seviyenize göre bu rakamı güncelleyebilirsiniz)
            return 2.25f;
        }

        private void ScheduleNext()
        {
            float freqBonus =
                UpgradeManager.Instance != null
                    ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.LightningFrequency)
                    : 0f;

            float min = Mathf.Max(2f, baseMinInterval - freqBonus);
            float max = Mathf.Max(min + 1f, baseMaxInterval - freqBonus);
            _nextSpawnTime = Time.time + Random.Range(min, max);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public float GetMagnetDuration()
        {
            float bonus =
                UpgradeManager.Instance != null
                    ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.MagnetDuration)
                    : 0f;
            return baseMagnetDuration + bonus;
        }

        public float GetMagnetRange()
        {
            float bonus =
                UpgradeManager.Instance != null
                    ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.MagnetRange)
                    : 0f;
            return baseMagnetRange + bonus;
        }

        /// <summary>StormCloud gibi dış kaynaklar belirli bir X konumunda yıldırım spawn edebilir.</summary>
        public void SpawnImmediateLightning(float x)
        {
            if (lightningPrefab == null)
                return;

            // En yakın bulutu bul, oradan başlat
            float cloudY = fallbackSpawnY;
            var clouds = FindObjectsByType<Cloud>(FindObjectsSortMode.None);
            float closestDist = float.MaxValue;
            foreach (var c in clouds)
            {
                float dist = Mathf.Abs(c.transform.position.x - x);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    cloudY = c.transform.position.y;
                }
            }

            float groundY = FindGroundY(x, cloudY);

            // Z eksenini bulutun arkasına atıyoruz
            Vector3 spawnPos = new Vector3(x, cloudY, lightningZOffset);

            GameObject obj = Instantiate(lightningPrefab, spawnPos, Quaternion.identity);
            var lightning = obj.GetComponent<Lightning>();
            lightning?.Initialize(x, cloudY, groundY);
        }
    }
}
