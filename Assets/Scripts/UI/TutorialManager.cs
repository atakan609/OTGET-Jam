using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using Gameplay;

namespace UI
{
    /// <summary>
    /// Oyunun ilk açılışında adım adım tutorial gösterir.
    /// PlayerPrefs ile bir kez gösterildiğini kaydeder; sonraki açılışlarda atlanır.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        // ── UI Referansları ────────────────────────────────────────────────────

        [Header("UI Referansları")]
        [Tooltip("Tutorial mesajının gösterileceği panel (arka plan kutusu).")]
        [SerializeField] private GameObject tutorialPanel;

        [Tooltip("Tutorial metninin yazılacağı TextMeshPro bileşeni.")]
        [SerializeField] private TMP_Text tutorialText;

        [Tooltip("Devam butonu (isteğe bağlı, sadece son adımda gösterilir).")]
        [SerializeField] private Button continueButton;

        // ── Bağlantılar ────────────────────────────────────────────────────────

        [Header("Bağlantılar")]
        [SerializeField] private PlayerController player;
        [SerializeField] private BucketController bucket;

        // ── Görünüm ────────────────────────────────────────────────────────────

        [Header("Metin Görünümü")]
        [Tooltip("Adım metinlerinin yazı boyutu.")]
        [SerializeField] private float fontSize = 36f;
        [Tooltip("Adım metinlerinin rengi.")]
        [SerializeField] private Color textColor = Color.white;

        // ── Adım Metinleri ─────────────────────────────────────────────────────

        [Header("Adım Metinleri (Inspector'dan Düzenlenebilir)")]

        [Tooltip("Adım 1: Hareketi öğretme mesajı.")]
        [TextArea(3, 6)]
        [SerializeField] private string step1Text =
            "🎮 Hoş Geldin!\n\nHareket etmek için A / D veya ← → tuşlarını kullan.\n\nBiraz hareket et!";

        [Tooltip("Adım 2: Yağmur damlası toplamayı öğretme mesajı. {0} = hedef ml miktarı")]
        [TextArea(3, 6)]
        [SerializeField] private string step2Text =
            "💧 Harika!\n\nŞimdi dur ve yağmur damlalarının kovanına dolmasını bekle.\n\n{0} ml su topla!";

        [Tooltip("Adım 3: Upgrade ağacını ve depo satın almayı öğretme mesajı.")]
        [TextArea(3, 6)]
        [SerializeField] private string step3Text =
            "⬆️ Süper!\n\nU tuşuna bas ve Upgrade Ağacını aç.\n\nDepo yükseltmesini satın al —\nbu, suyu depolamanı sağlayacak!";

        [Tooltip("Adım 4: Deponun işlevini anlatan mesaj.")]
        [TextArea(3, 6)]
        [SerializeField] private string step4Text =
            "🏗️ Mükemmel!\n\nDepona ulaşmak için yanına git.\nKovan otomatik olarak boşalacak.\n\nBiriktirdiğin su para kazanmanda kullanılacak.\n\nİyi şanslar!";

        // ── Ayarlar ────────────────────────────────────────────────────────────

        [Header("Ayarlar")]
        [Tooltip("Kaç ml su toplandıktan sonra 2. adım geçilir.")]
        [SerializeField] private float waterCollectTarget = 10f;
        [Tooltip("Tutorial'ı her başlangıçta tekrar göstermek için işaretle (sadece test için).")]
        [SerializeField] private bool forceShowTutorial = false;
        [Tooltip("Adımlar arası bekleme (saniye).")]
        [SerializeField] private float stepDelay = 0.5f;
        [Tooltip("Tutorial bitince panelin fade-out süresi (saniye).")]
        [SerializeField] private float fadeOutDuration = 1f;

        // ── PlayerPrefs Anahtarı ──────────────────────────────────────────────
        private const string TutorialDoneKey = "TutorialCompleted";

        // ── State ─────────────────────────────────────────────────────────────
        private bool       _waitingForContinue = false;
        private bool       _tutorialDone       = false;
        private CanvasGroup _panelCG;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (player == null) player = FindObjectOfType<PlayerController>();
            if (bucket == null && player != null) bucket = player.Bucket;

            // CanvasGroup'u panelden al ya da ekle — raycasts için lazım
            if (tutorialPanel != null)
            {
                _panelCG = tutorialPanel.GetComponent<CanvasGroup>();
                if (_panelCG == null) _panelCG = tutorialPanel.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            if (!forceShowTutorial && PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1)
            {
                tutorialPanel?.SetActive(false);
                _tutorialDone = true;
                return;
            }

            continueButton?.onClick.AddListener(OnContinueClicked);
            continueButton?.gameObject.SetActive(false);

            // Metin görünümünü uygula
            if (tutorialText != null)
            {
                tutorialText.fontSize = fontSize;
                tutorialText.color    = textColor;
            }

            SetPanelBlocksRaycasts(true);
            tutorialPanel?.SetActive(true);
            StartCoroutine(RunTutorial());
        }

        private void OnDestroy()
        {
            continueButton?.onClick.RemoveListener(OnContinueClicked);
        }

        // ── Tutorial Akışı ────────────────────────────────────────────────────

        private IEnumerator RunTutorial()
        {
            // ─── ADIM 1: HAREKET ─────────────────────────────────────────────
            SetPanelBlocksRaycasts(true);
            SetText(step1Text);
            yield return new WaitForSeconds(stepDelay);
            yield return WaitUntilPlayerMoves();

            // ─── ADIM 2: DAMLA TOPLAMA ────────────────────────────────────────
            yield return new WaitForSeconds(stepDelay);
            SetText(string.Format(step2Text, waterCollectTarget.ToString("F0")));
            yield return WaitUntilWaterCollected(waterCollectTarget);

            // ─── ADIM 3: UPGRADE / DEPO ALMA ─────────────────────────────────
            yield return new WaitForSeconds(stepDelay);
            SetText(step3Text);
            // Panelin tıklamaları bloke ETMEMESİ lazım ki upgrade ağacı açılsın
            SetPanelBlocksRaycasts(false);
            yield return WaitUntilDepotPurchased();

            // ─── ADIM 4: DEPO AÇIKLAMASI ──────────────────────────────────────
            yield return new WaitForSeconds(stepDelay);
            SetPanelBlocksRaycasts(true);
            SetText(step4Text);

            ShowContinueButton(true);
            yield return new WaitUntil(() => !_waitingForContinue);
            ShowContinueButton(false);

            // ─── Tutorial Bitti ───────────────────────────────────────────────
            PlayerPrefs.SetInt(TutorialDoneKey, 1);
            PlayerPrefs.Save();
            _tutorialDone = true;

            yield return FadeOutPanel();
        }

        // ── Bekleme Koşulları ─────────────────────────────────────────────────

        private IEnumerator WaitUntilPlayerMoves()
        {
            bool movedRight = false;
            bool movedLeft  = false;

            while (!(movedRight && movedLeft))
            {
                float h = Input.GetAxisRaw("Horizontal");
                if (h > 0.1f)  movedRight = true;
                if (h < -0.1f) movedLeft  = true;
                yield return null;
            }
        }

        private IEnumerator WaitUntilWaterCollected(float targetMl)
        {
            while (bucket == null || bucket.CurrentWater < targetMl)
            {
                if (bucket == null && player != null) bucket = player.Bucket;
                yield return null;
            }
        }

        private IEnumerator WaitUntilDepotPurchased()
        {
            bool purchased = false;

            void Handler(UpgradeType type, int level)
            {
                if (type == UpgradeType.BuyWaterDepot) purchased = true;
            }

            UpgradeManager.OnUpgradePurchased += Handler;

            // Zaten daha önce satın alınmışsa direkt geç
            if (UpgradeManager.Instance != null &&
                UpgradeManager.Instance.GetLevel(UpgradeType.BuyWaterDepot) > 0)
                purchased = true;

            while (!purchased) yield return null;

            UpgradeManager.OnUpgradePurchased -= Handler;
        }

        // ── Yardımcılar ───────────────────────────────────────────────────────

        private void SetText(string message)
        {
            if (tutorialText != null)
                tutorialText.text = message;
        }

        /// <summary>
        /// Panelin fare tıklamalarını bloklayıp bloklamayacağını ayarlar.
        /// false → oyuncu arkadaki upgrade ağacı gibi UI elemanlarıyla etkileşebilir.
        /// </summary>
        private void SetPanelBlocksRaycasts(bool blocks)
        {
            if (_panelCG != null)
                _panelCG.blocksRaycasts = blocks;
        }

        private void ShowContinueButton(bool show)
        {
            _waitingForContinue = show;
            continueButton?.gameObject.SetActive(show);
        }

        private void OnContinueClicked()
        {
            _waitingForContinue = false;
        }

        private IEnumerator FadeOutPanel()
        {
            if (_panelCG == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _panelCG.alpha = 1f - (elapsed / fadeOutDuration);
                yield return null;
            }

            tutorialPanel?.SetActive(false);
        }
    }
}
