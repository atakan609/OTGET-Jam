using System.Collections;
using UnityEngine;
using Managers;

namespace Gameplay
{
    /// <summary>
    /// Gökyüzünden düşen yıldırım objesi.
    /// Düşmeden önce uyarı çizgisi gösterir, oyuncuya değerse kovayı mıknatıslar.
    /// </summary>
    public class Lightning : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float fallSpeed = 15f;

        [Header("Warning")]
        [SerializeField] private float warningDuration = 0.8f; // Düşmeden önce uyarı süresi
        [SerializeField] private SpriteRenderer warningSprite;  // Dikey çizgi / flash sprite

        [Header("Bounds")]
        [SerializeField] private float destroyY = -12f;

        private bool _falling = false;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            StartCoroutine(WarningThenFall());
        }

        private IEnumerator WarningThenFall()
        {
            // Uyarı göster, yıldırım henüz düşmüyor
            if (warningSprite != null) warningSprite.enabled = true;
            yield return new WaitForSeconds(warningDuration);
            if (warningSprite != null) warningSprite.enabled = false;

            _falling = true;
        }

        private void FixedUpdate()
        {
            if (!_falling || _rb == null) return;
            _rb.linearVelocity = new Vector2(0f, -fallSpeed);
        }

        private void Update()
        {
            if (transform.position.y < destroyY) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_falling) return;

            if (other.CompareTag("Player"))
            {
                // Kovayı mıknatısla
                var bucket = other.GetComponentInChildren<BucketController>();
                if (bucket != null)
                {
                    float duration = LightningManager.Instance != null
                        ? LightningManager.Instance.GetMagnetDuration()
                        : 3f;
                    float range = LightningManager.Instance != null
                        ? LightningManager.Instance.GetMagnetRange()
                        : 4f;
                    bucket.ActivateMagnet(duration, range);
                }
                Destroy(gameObject);
            }
            else if (!other.CompareTag("Enemy")) // Başka şeylere çarparsa yok ol
            {
                Destroy(gameObject);
            }
        }
    }
}
