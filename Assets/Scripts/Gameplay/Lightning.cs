using System.Collections;
using UnityEngine;
using Managers;

namespace Gameplay
{
    /// <summary>
    /// Prefab yapısına sadık kalarak çalışan yıldırım kontrolcüsü.
    /// Ana objeyi zemine konumlandırır, çocuk objelerin hiyerarşisine dokunmaz.
    /// </summary>
    public class Lightning : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float warningDuration = 0.8f;   // Uyarının gösterildiği süre
        [SerializeField] private float flashDuration = 1f;       // Yıldırımın göründüğü süre

        [Header("References")]
        [SerializeField] private GameObject warningObject;        // Prefab içindeki uyarı objesi
        [SerializeField] private GameObject lightningBoltObject;  // Prefab içindeki yıldırım objesi

        [Header("Detection")]
        [SerializeField] private float strikeWidth = 0.6f;        // Hasar genişliği
        [SerializeField] private float strikeHeight = 8f;         // Hasar yüksekliği (Yerden yukarı doğru)
        [SerializeField] private LayerMask playerLayer;           // Player Layer

        private float _strikeX;
        private float _groundY;

        /// <summary>LightningManager tarafından çağrılır.</summary>
        public void Initialize(float strikeX, float cloudY, float groundY)
        {
            _strikeX = strikeX;
            _groundY = groundY;
            
            // Ana objeyi vuruş (zemin) noktasına yerleştiriyoruz.
            // Prefab'da uyarı ve yıldırım buna göre hizalanmış olmalıdır.
            transform.position = new Vector3(_strikeX, _groundY, 0f);
        }

        private void Start()
        {
            // Başlangıçta her ikisini de gizle
            if (warningObject != null) warningObject.SetActive(false);
            if (lightningBoltObject != null) lightningBoltObject.SetActive(false);

            StartCoroutine(LightningSequence());
        }

        private IEnumerator LightningSequence()
        {
            // 1. UYARI FAZI
            if (warningObject != null) warningObject.SetActive(true);
            yield return new WaitForSeconds(warningDuration);

            // 2. YILDIRIM FAZI
            if (warningObject != null) warningObject.SetActive(false);
            if (lightningBoltObject != null) lightningBoltObject.SetActive(true);

            // 3. HASAR KONTROLÜ
            CheckPlayerHit();

            yield return new WaitForSeconds(flashDuration);

            // 4. YOK OLMA
            Destroy(gameObject);
        }

        private void CheckPlayerHit()
        {
            // Zemin noktasından yukarı doğru bir kutu ile oyuncu var mı bakıyoruz
            Vector2 boxCenter = new Vector2(_strikeX, _groundY + (strikeHeight * 0.5f));
            Vector2 boxSize = new Vector2(strikeWidth, strikeHeight);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    var bucket = hit.GetComponentInChildren<BucketController>();
                    if (bucket == null) bucket = hit.GetComponent<BucketController>();

                    if (bucket != null)
                    {
                        float duration = LightningManager.Instance != null 
                            ? LightningManager.Instance.GetMagnetDuration() : 3f;
                        float range = LightningManager.Instance != null 
                            ? LightningManager.Instance.GetMagnetRange() : 4f;
                        
                        bucket.ActivateMagnet(duration, range);
                    }
                    break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Hasar alanını editörde sarı bir kutu olarak gösterir
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                new Vector3(_strikeX, _groundY + (strikeHeight * 0.5f), 0f),
                new Vector3(strikeWidth, strikeHeight, 0f)
            );
        }
    }
}
