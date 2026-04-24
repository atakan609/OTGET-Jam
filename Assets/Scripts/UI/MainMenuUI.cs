using UnityEngine;
using UnityEngine.SceneManagement;

namespace OTGETJam.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("Sadece Main Menu'yü kapatıp açmak isterseniz atayın.")]
        [SerializeField] private GameObject mainMenuPanel;
        [Tooltip("Settings paneli GameObject'ini buraya sürükleyin.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Scene Settings")]
        [Tooltip("Play butonuna basıldığında yüklenecek olan sahnenin adını yazın. Boş bırakırsanız Build Index'teki bir sonraki sahneyi yükler.")]
        [SerializeField] private string gameSceneName = "GameScene";

        private void Start()
        {
            // Oyun başladığında Settings panelini kapalı başlatalım
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
                
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }

        // Play butonunun OnClick() kısmına bu fonksiyonu atamalısın
        public void OnPlayButtonClicked()
        {
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
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        // Quit butonunun OnClick() kısmına bu fonksiyonu atamalısın
        public void OnQuitButtonClicked()
        {
            Debug.Log("Oyundan çıkılıyor..."); // Editor içerisinde çalıştığını görmek için eklendi
            Application.Quit();
        }
    }
}
