using UnityEngine;
using TMPro;

namespace Gameplay
{
    /// <summary>
    /// İki modda çalışır:
    ///   FlyUp      – Spawn olunca sayıyı gösterir, yukarı süzülerek solar. (raindrop / lightning)
    ///   Accumulate – Sabit durur, sayı birikir; Finalize() çağrılınca uçup solar. (depot)
    /// Renk dışarıdan verilir (mavi=normal, altın=golden, kırmızı=hasar, yeşil=depot).
    /// </summary>
    public class FloatingWaterText : MonoBehaviour
    {
        [Header("Referanslar")]
        [Tooltip("World-space TextMeshPro. Atanmazsa GetComponentInChildren ile bulunur.")]
        public TMP_Text textComponent;

        [Tooltip("Damla sprite'ı (PNG). Atanırsa otomatik ikon oluşturulur.")]
        public Sprite dropSprite;

        [Header("Metin Offset")]
        [Tooltip("Metin bileşeninin local pozisyon ofseti.")]
        public Vector3 textOffset = Vector3.zero;

        [Header("İkon Ayarları")]
        [Tooltip("İkonun local pozisyon ofseti. X negatif = sola.")]
        public Vector3 iconOffset    = new Vector3(-0.7f, 0f, 0f);
        [Tooltip("İkon local scale.")]
        public float   iconScale     = 0.45f;
        [Tooltip("İkon Sorting Order.")]
        public int     iconSortOrder = 10;

        [Header("Hareket / Görünüm")]
        public float moveSpeed = 1.5f;
        public float fadeSpeed = 1.0f;
        public float lifetime  = 2.0f;

        // ── Runtime ────────────────────────────────────────────────────────────
        private SpriteRenderer _iconSR;
        private Color _baseTextColor = Color.white;
        private Color _iconColor     = Color.white;

        private bool  _isNegative = false;
        private bool  _flying      = false;
        private float _flyTimer    = 0f;
        private float _accumulated = 0f;
        private bool  _initialized = false;

        // ── Init ───────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (textComponent == null)
                textComponent = GetComponentInChildren<TMP_Text>();

            if (textComponent != null)
                textComponent.transform.localPosition = textOffset;

            if (dropSprite != null)
            {
                var iconObj = new GameObject("DropIcon");
                iconObj.transform.SetParent(transform, false);
                iconObj.transform.localPosition = iconOffset;
                iconObj.transform.localScale    = Vector3.one * iconScale;

                _iconSR = iconObj.AddComponent<SpriteRenderer>();
                _iconSR.sprite       = dropSprite;
                _iconSR.sortingOrder = iconSortOrder;
            }

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // inspector'dan renk al; alpha 0 ise beyaz varsay
            if (textComponent != null)
            {
                _baseTextColor = textComponent.color;
                if (_baseTextColor.a < 0.01f) _baseTextColor = Color.white;
            }
            _iconColor = (_iconSR != null && _iconSR.color.a > 0.01f) ? _iconSR.color : Color.white;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Raindrop / Lightning modu: sayıyı ve rengi set edip hemen uçmaya başlar.
        /// negative=true → eksi işareti gösterilir.
        /// </summary>
        public void SetupAndFly(float amount, Color textColor, bool negative = false)
        {
            EnsureInitialized();
            _isNegative    = negative;
            _accumulated   = amount;
            _baseTextColor = textColor;
            _baseTextColor.a = 1f;
            RefreshText();
            _flying = true;
            Destroy(gameObject, lifetime + 0.2f);
        }

        /// <summary>Depot modu – başlangıç rengi ver, birikimi 0'dan başlat.</summary>
        public void InitDeposit(Color textColor)
        {
            EnsureInitialized();
            _baseTextColor = textColor;
            _baseTextColor.a = 1f;
            _accumulated = 0f;
            RefreshText();
        }

        /// <summary>Depot modu – birikimi artır, metin güncelle (obje sabit durur).</summary>
        public void AddAmount(float delta)
        {
            EnsureInitialized();
            _accumulated += delta;
            RefreshText();
        }

        /// <summary>Depot modu – uçuşu başlat ve yok et.</summary>
        public void Finalize()
        {
            if (_flying) return;
            _flying = true;
            Destroy(gameObject, lifetime + 0.2f);
        }

        // ── Update ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_flying) return;

            _flyTimer += Time.deltaTime;
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            float alpha = Mathf.Clamp01(1f - _flyTimer * fadeSpeed);

            if (textComponent != null)
            {
                Color c = _baseTextColor;
                c.a = alpha;
                textComponent.color = c;
            }
            if (_iconSR != null)
            {
                _iconColor.a = alpha;
                _iconSR.color = _iconColor;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void RefreshText()
        {
            if (textComponent == null) return;
            string sign = _isNegative ? "-" : "+";
            textComponent.text  = sign + _accumulated.ToString("F1") + " ml";
            Color c = _baseTextColor;
            c.a = 1f;
            textComponent.color = c;
        }
    }
}
