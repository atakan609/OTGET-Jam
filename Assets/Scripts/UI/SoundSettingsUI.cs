using UnityEngine;
using UnityEngine.UI;
using Managers;

namespace UI
{
    /// <summary>
    /// Ses ayarları paneli.
    /// Inspector'dan üç Slider bağlanır: masterSlider, backgroundSlider, sfxSlider.
    /// Panel açıldığında SoundManager'dan güncel değerleri okur; slider değişince günceller.
    /// </summary>
    public class SoundSettingsUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider backgroundSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Panel (açık/kapalı için)")]
        [SerializeField] private GameObject settingsPanel;
        [Tooltip("Back butonuna basıldığında geri dönülecek panel (örn: Ana Menü veya Pause menüsü paneli).")]
        [SerializeField] private GameObject backPanel;
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

        private void Awake()
        {
            // Slider değer aralıkları
            if (masterSlider     != null) { masterSlider.minValue     = 0f; masterSlider.maxValue     = 1f; }
            if (backgroundSlider != null) { backgroundSlider.minValue = 0f; backgroundSlider.maxValue = 1f; }
            if (sfxSlider        != null) { sfxSlider.minValue        = 0f; sfxSlider.maxValue        = 1f; }

            // Listener'ları bağla (parent SetActive'den bağımsız çalışsın diye Awake'te)
            masterSlider?.onValueChanged.AddListener(OnMasterChanged);
            backgroundSlider?.onValueChanged.AddListener(OnBackgroundChanged);
            sfxSlider?.onValueChanged.AddListener(OnSfxChanged);

            // Başlangıçta settings paneli kapalı
            settingsPanel?.SetActive(false);
        }

        private void Start()
        {
            // SoundManager Awake'ten sonra hazır olduğu için değerleri Start'ta çekiyoruz
            RefreshSliders();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                TogglePanel();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Settings panelini aç/kapat. MainMenuUI butonu buraya bağlanabilir.</summary>
        public void TogglePanel()
        {
            if (settingsPanel == null) return;
            bool next = !settingsPanel.activeSelf;
            settingsPanel.SetActive(next);
            if (next) RefreshSliders(); // Açılırken güncel değerleri çek
        }

        public void OpenPanel()
        {
            settingsPanel?.SetActive(true);
            RefreshSliders();
        }

        public void ClosePanel()
        {
            settingsPanel?.SetActive(false);
        }

        /// <summary>Geri butonuna atanacak fonksiyon. Settings'i kapatıp eski paneli açar.</summary>
        public void OnBackButtonClicked()
        {
            ClosePanel();
            if (backPanel != null)
            {
                backPanel.SetActive(true);
            }
        }

        // ── Slider Callbacks ─────────────────────────────────────────────────────

        private void OnMasterChanged(float value)
        {
            SoundManager.Instance?.SetMasterVolume(value);
        }

        private void OnBackgroundChanged(float value)
        {
            SoundManager.Instance?.SetBackgroundVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            SoundManager.Instance?.SetSfxVolume(value);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void RefreshSliders()
        {
            if (SoundManager.Instance == null) return;

            // onValueChanged'i tetiklememek için SetValueWithoutNotify kullan
            masterSlider?.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
            backgroundSlider?.SetValueWithoutNotify(SoundManager.Instance.BackgroundVolume);
            sfxSlider?.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
        }
    }
}
