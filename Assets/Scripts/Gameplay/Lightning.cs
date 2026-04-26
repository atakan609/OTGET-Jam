using System.Collections;
using UnityEngine;
using Managers;

namespace Gameplay
{
    /// <summary>
    /// Anlık çakan yıldırım. LightningManager tarafından Initialize çağrılarak kullanılır.
    /// Oyuncuya çarpınca: stun (blink), kova suyu azaltma.
    /// </summary>
    public class Lightning : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float warningDuration = 0.8f;
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float soundDelay = 0.2f; // Sesi ne kadar sonra çalacak

        [Header("References")]
        [SerializeField] private GameObject warningObject;
        [SerializeField] private GameObject lightningBoltObject;

        [Header("Hit Settings")]
        [SerializeField] private float strikeWidth = 0.6f;
        [SerializeField] private float strikeHeight = 20f;
        [SerializeField] private float stunDuration = 1f;
        [SerializeField] private float waterSpillAmount = 2f;

        // Spawn'da LightningManager tarafından set edilir
        private float _strikeX;
        private float _groundY;
        private float _cloudY;

        private void Awake()
        {
            // Yıldırım görseli bulutların arkasında görünsün
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = -5;
        }

        /// <summary>LightningManager tarafından Instantiate sonrası çağrılır.</summary>
        public void Initialize(float strikeX, float cloudY, float groundY)
        {
            _strikeX = strikeX;
            _cloudY  = cloudY;
            _groundY = groundY;

            // Yıldırımı bulutun pozisyonuna yerleştir; görseller oradan aşağı uzanmalı
            transform.position = new Vector3(_strikeX, _cloudY - lightningBoltObject.transform.position.y, 0f);
        }

        private void Start()
        {
            if (warningObject    != null) warningObject.SetActive(false);
            if (lightningBoltObject != null) lightningBoltObject.SetActive(false);
            StartCoroutine(LightningSequence());
        }

        private IEnumerator LightningSequence()
        {
            // ── 1. UYARI ──
            if (warningObject != null) warningObject.SetActive(true);
            yield return new WaitForSeconds(warningDuration);

            // ── 2. FLAŞ ──
            if (warningObject    != null) warningObject.SetActive(false);
            if (lightningBoltObject != null) lightningBoltObject.SetActive(true);

            // Yıldırım SFX Delay ile Çal
            StartCoroutine(PlaySoundWithDelay(soundDelay));

            CheckAndHitPlayer();

            yield return new WaitForSeconds(flashDuration);
            
            // Flaş bitti, görseli gizle
            if (lightningBoltObject != null) lightningBoltObject.SetActive(false);

            // ── 3. YOK OL (Sesin çalmasını bekle) ──
            if (soundDelay > flashDuration)
            {
                yield return new WaitForSeconds(soundDelay - flashDuration);
            }

            Destroy(gameObject);
        }

        private IEnumerator PlaySoundWithDelay(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Managers.SoundManager.Instance?.PlayLightningSfx();
        }

        private void CheckAndHitPlayer()
        {
            // Buluttan zeminine kadar dikey bir kutu tarar
            float height   = Mathf.Abs(_cloudY - _groundY);
            float centerY  = _groundY + height * 0.5f;
            Vector2 center = new Vector2(_strikeX, centerY);
            Vector2 size   = new Vector2(strikeWidth, height > 0.1f ? height : strikeHeight);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
            foreach (var col in hits)
            {
                if (!col.CompareTag("Player")) continue;

                // Stun
                var player = col.GetComponent<PlayerController>();
                if (player != null)
                    player.ApplyLightningStun(stunDuration);

                // Su azalt
                var bucket = col.GetComponentInChildren<BucketController>()
                          ?? col.GetComponent<BucketController>();
                if (bucket != null)
                    bucket.SpillWater(waterSpillAmount);

                break; // Aynı oyuncuya birden fazla defa vurma
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            float height  = Mathf.Abs(_cloudY - _groundY);
            float centerY = _groundY + height * 0.5f;
            Gizmos.DrawWireCube(new Vector3(_strikeX, centerY, 0f),
                                new Vector3(strikeWidth, height > 0.1f ? height : strikeHeight, 0f));
        }
    }
}
