using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Managers;
using Gameplay;

namespace UI
{
    /// <summary>
    /// GameWin upgrade'i satın alındığında ekrana oyun sonu panelini getirir.
    /// Fade-in, mesaj, Tekrar Oyna ve Kapat butonlarını yönetir.
    /// </summary>
    public class GameEndManager : MonoBehaviour
    {
        // ── UI Referansları ────────────────────────────────────────────────────

        [Header("UI Referansları")]
        [Tooltip("Oyun sonu paneli (başta kapalı olmalı).")]
        [SerializeField] private GameObject gameEndPanel;

        [Tooltip("Kutlama / oyun sonu yazısı.")]
        [SerializeField] private TMP_Text gameEndText;

        [Tooltip("'Tekrar Oyna' butonu.")]
        [SerializeField] private Button restartButton;

        [Tooltip("'Oyunu Kapat' butonu.")]
        [SerializeField] private Button quitButton;

        // ── Görünüm ────────────────────────────────────────────────────────────

        [Header("Görünüm")]
        [Tooltip("Oyun sonu mesajı. \\n ile satır atlayabilirsin.")]
        [TextArea(3, 8)]
        [SerializeField] private string endMessage =
            "🎉 Tebrikler!\n\nTüm su kaynaklarını geliştirdin.\nBölge artık su sıkıntısı çekmiyor!\n\nMükemmel bir iş çıkardın.";

        [Tooltip("Fade-in animasyonunun süresi (saniye).")]
        [SerializeField] private float fadeInDuration = 1.5f;

        [Tooltip("Fade-in başlamadan önceki bekleme süresi.")]
        [SerializeField] private float fadeInDelay = 0.5f;

        // ── Sahne ─────────────────────────────────────────────────────────────

        [Header("Sahne Ayarları")]
        [Tooltip("'Tekrar Oyna' basıldığında yüklenecek sahne adı veya build indeksi.")]
        [SerializeField] private string gameSceneName = "";
        [Tooltip("Sahne adı boşsa build indeksi kullanılır.")]
        [SerializeField] private int gameSceneIndex = 0;

        // ── State ─────────────────────────────────────────────────────────────
        private CanvasGroup _panelCG;
        private bool        _triggered = false;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Panel'i kapat ve CanvasGroup hazırla
            if (gameEndPanel != null)
            {
                _panelCG = gameEndPanel.GetComponent<CanvasGroup>();
                if (_panelCG == null) _panelCG = gameEndPanel.AddComponent<CanvasGroup>();
                _panelCG.alpha          = 0f;
                _panelCG.blocksRaycasts = false;
                gameEndPanel.SetActive(false);
            }
        }

        private void Start()
        {
            UpgradeManager.OnUpgradePurchased += OnUpgradePurchased;

            restartButton?.onClick.AddListener(OnRestartClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= OnUpgradePurchased;
        }

        // ── Upgrade Dinleyici ─────────────────────────────────────────────────

        private void OnUpgradePurchased(UpgradeType type, int level)
        {
            if (_triggered) return;
            if (type != UpgradeType.GameWin) return;

            _triggered = true;
            StartCoroutine(TriggerGameEnd());
        }

        // ── Oyun Sonu Akışı ───────────────────────────────────────────────────

        private IEnumerator TriggerGameEnd()
        {
            // Oyun durumunu güncelle
            GameManager.Instance?.UpdateState(GameState.GameOver);

            // Kısa bekleme
            yield return new WaitForSeconds(fadeInDelay);

            // Mesajı ayarla
            if (gameEndText != null)
                gameEndText.text = endMessage;

            // Paneli aç ve fade-in yap
            gameEndPanel?.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                if (_panelCG != null)
                    _panelCG.alpha = elapsed / fadeInDuration;
                yield return null;
            }

            if (_panelCG != null)
            {
                _panelCG.alpha          = 1f;
                _panelCG.blocksRaycasts = true;
            }
        }

        // ── Buton Aksiyonları ─────────────────────────────────────────────────

        private void OnRestartClicked()
        {
            UpgradeManager.Instance?.ResetAllUpgrades();

            if (!string.IsNullOrWhiteSpace(gameSceneName))
                SceneManager.LoadScene(gameSceneName);
            else
                SceneManager.LoadScene(gameSceneIndex);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
