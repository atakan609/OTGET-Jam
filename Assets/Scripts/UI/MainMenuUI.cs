using UnityEngine;
using UnityEngine.SceneManagement;

namespace OTGETJam.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Type")]
        [Tooltip("Eğer bu script Ana Menü sahnesindeyse işaretleyin. Oyun içi (Pause) menüsü ise işareti kaldırın.")]
        [SerializeField] private bool isMainMenu = true;

        [Header("Panels")]
        [Tooltip("Sadece Main Menu'yü kapatıp açmak isterseniz atayın.")]
        [SerializeField] private GameObject mainMenuPanel;
        [Tooltip("Settings paneli GameObject'ini buraya sürükleyin.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Scene Settings")]
        [Tooltip("Play butonuna basıldığında yüklenecek olan sahnenin adını yazın. Boş bırakırsanız Build Index'teki bir sonraki sahneyi yükler.")]
        [SerializeField] private string gameSceneName = "GameScene";
        [Tooltip("Oyun içindeyken 'Menu' butonuna basıldığında dönülecek ana menü sahnesinin adı.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool _isPaused = false;

        private void Start()
        {
            // Oyun başladığında Settings panelini kapalı başlatalım
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
                
            if (mainMenuPanel != null)
            {
                // Eğer ana menüdeysek paneli açık başlat, oyun içi menüysek kapalı başlat.
                mainMenuPanel.SetActive(isMainMenu);
            }

            Time.timeScale = 1f; // Her ihtimale karşı oyun hızı normal başlasın
        }

        private void Update()
        {
            // Oyun içi pause menüsü davranışı (Escape tuşu ile aç/kapat)
            if (!isMainMenu && Input.GetKeyDown(KeyCode.Escape))
            {
                // Eğer settings paneli açıksa, escape'e basınca onu kapatsın
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    OnCloseSettingsButtonClicked();
                }
                else
                {
                    TogglePause();
                }
            }
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            if (mainMenuPanel != null) mainMenuPanel.SetActive(_isPaused);
            Time.timeScale = _isPaused ? 0f : 1f;
        }

        // Play butonunun OnClick() kısmına bu fonksiyonu atamalısın
        public void OnPlayButtonClicked()
        {
            Time.timeScale = 1f; // Sahne değişmeden önce zamanı normale döndür
            if (!string.IsNullOrEmpty(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                // gameSceneName boş ise Build Settings'teki bir sonraki sahneyi yükler
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        // Oyun İçinde "Continue" veya "Resume" butonuna atayabilirsiniz
        public void OnContinueButtonClicked()
        {
            TogglePause();
        }

        // Oyun İçinde "Menu" veya "Home" butonuna atayabilirsiniz
        public void OnHomeButtonClicked()
        {
            Time.timeScale = 1f; // Ana menüye dönerken zamanı düzelt
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // Settings butonunun OnClick() kısmına bu fonksiyonu atamalısın
        public void OnSettingsButtonClicked()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        // Settings panelini kapatmak (Geri dönmek) için ekstra bir butonunuz varsa ona da bunu verebilirsiniz
        public void OnCloseSettingsButtonClicked()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true); // Settings kapanınca asıl paneli geri aç
        }

        // Quit butonunun OnClick() kısmına bu fonksiyonu atamalısın
        public void OnQuitButtonClicked()
        {
            Debug.Log("Oyundan çıkılıyor..."); // Editor içerisinde çalıştığını görmek için eklendi
            Application.Quit();
        }
    }
}
