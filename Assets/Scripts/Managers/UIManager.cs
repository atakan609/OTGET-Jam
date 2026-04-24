using UnityEngine;
using Core;

namespace Managers
{
    public class UIManager : Singleton<UIManager>
    {
        // Add references to different UI panels here
        // [SerializeField] private GameObject mainMenuPanel;
        // [SerializeField] private GameObject hudPanel;
        // [SerializeField] private GameObject gameOverPanel;

        private void Start()
        {
            GameManager.OnStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            GameManager.OnStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            // Update UI based on state
            // mainMenuPanel.SetActive(state == GameState.Menu);
            // hudPanel.SetActive(state == GameState.Playing);
            // gameOverPanel.SetActive(state == GameState.GameOver);
        }
    }
}
