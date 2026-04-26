using UnityEngine;
using Managers;

namespace Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Raindrop : MonoBehaviour
    {
        [Header("Drop Settings")]
        [SerializeField] public float dropValue = 1f;
        [SerializeField] public bool isGolden = false;

        [Header("Floating Text")]
        [Tooltip("FloatingWaterText prefabını buraya sürüklein. Her damla toplandığında kovanın üstünde yükselir.")]
        [SerializeField] private GameObject floatingTextPrefab;

        [Header("Visual Scale")]
        [Tooltip("dropValue = 1 olduğunda damlanın referans boyutu. Değer büyüdükçe bu oran'a göre scale'lenir.")]
        [SerializeField] private float referenceDropValue = 1f; // dropValue bu değere eşitken scale = 1x
        [SerializeField] private float minScale = 0.4f;         // En küçük görsel boyut
        [SerializeField] private float maxScale = 2.5f;         // En büyük görsel boyut

        [Header("Wind Effect")]
        [SerializeField] private float windStrength = 2f;
        [SerializeField] private float windChangeSpeed = 0.5f;

        private Rigidbody2D _rb;
        private float _noiseOffset;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _noiseOffset = Random.Range(0f, 100f);

            // Zemine çarpmadan düşsün; StaticBucket trigger alanına girince
            // StaticBucket.OnTriggerEnter2D tetiklensin.
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        /// <summary>
        /// Cloud/StormCloud tarafından dropValue atandıktan sonra çağrılmalı.
        /// Damlanın görsel boyutunu dropValue ile orantılı hale getirir.
        /// </summary>
        public void ApplySize()
        {
            float ratio = referenceDropValue > 0f ? dropValue / referenceDropValue : 1f;
            float scale = Mathf.Clamp(ratio, minScale, maxScale);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void HandleCollision(GameObject other)
        {
            // Başka bir yağmur damlasına veya buluta çarparsa yoksay
            if (other.GetComponent<Raindrop>() != null || other.GetComponent<Cloud>() != null)
                return;

            // Çarpılan collider Player'ın BucketController'ının altındaki
            // bucket prefab'a mı ait? Yoksa yoksay ve yok ol.
            var bucket = other.GetComponentInParent<BucketController>();
            if (bucket == null)
            {
                Destroy(gameObject);
                return;
            }

            // Kova bulundu → su ekle (kombo ve çarpanlar uygulanır)
            float comboMult  = ComboManager.Instance   != null ? ComboManager.Instance.Multiplier           : 1f;
            float globalMult = CurrencyManager.Instance != null ? CurrencyManager.Instance.GlobalMultiplier : 1f;
            float critChance = CurrencyManager.Instance != null ? CurrencyManager.Instance.CritChance       : 0f;
            float critMult   = (Random.value < critChance) ? 2f : 1f;

            float finalValue = dropValue * comboMult * globalMult * critMult;
            bool added = bucket.TryAddWater(finalValue);

            if (added)
            {
                if (floatingTextPrefab != null)
                {
                    Color dropColor = isGolden
                        ? new Color(1f, 0.84f, 0f)
                        : new Color(0.3f, 0.7f, 1f);
                    Vector3 spawnPos = bucket.transform.position + Vector3.up * 0.8f;
                    var obj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
                    var ft  = obj.GetComponent<FloatingWaterText>();
                    ft?.SetupAndFly(finalValue, dropColor);
                }
                SoundManager.Instance?.PlayRaindropCollect();
                ComboManager.Instance?.RegisterCollection();
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            if (transform.position.y < -15f)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            float noise = Mathf.PerlinNoise(Time.time * windChangeSpeed + _noiseOffset, 0f);
            float windForce = Mathf.Lerp(-windStrength, windStrength, noise);
            if (_rb != null)
                _rb.linearVelocity = new Vector2(windForce, _rb.linearVelocity.y);
        }
    }
}
