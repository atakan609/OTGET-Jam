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

            // Oyuncuya değerse → kovaya su ekle (kombo ve çarpanlar uygulanır)
            if (other.CompareTag("Player"))
            {
                var bucket = other.GetComponentInChildren<BucketController>();
                if (bucket != null)
                {
                    // Kombo çarpanı
                    float comboMult = ComboManager.Instance != null ? ComboManager.Instance.Multiplier : 1f;

                    // Genel çarpan (upgrade)
                    float globalMult = CurrencyManager.Instance != null ? CurrencyManager.Instance.GlobalMultiplier : 1f;

                    // Kritik şans (upgrade)
                    float critChance = CurrencyManager.Instance != null ? CurrencyManager.Instance.CritChance : 0f;
                    float critMult = (Random.value < critChance) ? 2f : 1f;

                    float finalValue = dropValue * comboMult * globalMult * critMult;
                    bucket.TryAddWater(finalValue);

                    // Kombo sayacını artır
                    ComboManager.Instance?.RegisterCollection();
                }
                Destroy(gameObject);
                return;
            }

            // Sabit kova varsa onun içine düş
            var staticBucket = other.GetComponent<StaticBucket>();
            if (staticBucket != null)
            {
                staticBucket.TryAddWater(dropValue);
                Destroy(gameObject);
                return;
            }

            // Diğer her yüzeye değince yok ol
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
