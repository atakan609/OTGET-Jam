using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay;
using Managers;

namespace UI
{
    public class UpgradeNodeUI : MonoBehaviour
    {
        public enum NodeState { Locked, Available, Purchased, Maxed }

        [Header("UI Elements")]
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("State Colors")]
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color availableColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color purchasedColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color maxedColor = new Color(1f, 0.8f, 0.2f, 1f);

        private UpgradeNodeDataSO _nodeData;
        private UpgradeNodeDataSO _parentNodeData;
        private NodeState _currentState;

        public UpgradeNodeDataSO NodeData => _nodeData;

        public void Setup(UpgradeNodeDataSO data, UpgradeNodeDataSO parentData)
        {
            _nodeData = data;
            _parentNodeData = parentData;

            if (icon != null && data.icon != null)
            {
                icon.sprite = data.icon;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClicked);
            }

            RefreshState();
        }

        public void RefreshState()
        {
            if (_nodeData == null || _nodeData.upgradeData == null || UpgradeManager.Instance == null) return;

            UpgradeType type = _nodeData.upgradeData.upgradeType;
            int currentLevel = UpgradeManager.Instance.GetLevel(type);
            int maxLevel = _nodeData.upgradeData.maxLevel;
            
            // Eğer root node ise (parent null) daima kilidi açıktır
            // Değilse, parent en az 1 kere satın alınmışsa kilidi açılır
            bool isUnlocked = _parentNodeData == null || UpgradeManager.Instance.GetLevel(_parentNodeData.upgradeData.upgradeType) > 0;

            if (currentLevel >= maxLevel)
            {
                _currentState = NodeState.Maxed;
            }
            else if (currentLevel > 0)
            {
                _currentState = NodeState.Purchased;
            }
            else if (isUnlocked)
            {
                _currentState = NodeState.Available;
            }
            else
            {
                _currentState = NodeState.Locked;
            }

            UpdateVisuals(currentLevel, maxLevel);
        }

        private void UpdateVisuals(int currentLevel, int maxLevel)
        {
            if (background != null)
            {
                switch (_currentState)
                {
                    case NodeState.Locked: background.color = lockedColor; break;
                    case NodeState.Available: background.color = availableColor; break;
                    case NodeState.Purchased: background.color = purchasedColor; break;
                    case NodeState.Maxed: background.color = maxedColor; break;
                }
            }

            if (levelText != null)
            {
                levelText.text = $"{currentLevel}/{maxLevel}";
            }

            if (costText != null)
            {
                if (_currentState == NodeState.Maxed)
                {
                    costText.text = "MAX";
                }
                else
                {
                    int cost = UpgradeManager.Instance.GetNextCost(_nodeData.upgradeData.upgradeType);
                    costText.text = cost.ToString();
                }
            }

            if (button != null)
            {
                // Locked olanlara da tıklatabiliriz belki açıklama görmek için ama şimdilik disabled
                button.interactable = _currentState != NodeState.Locked; 
            }
        }

        private void OnButtonClicked()
        {
            if (_currentState == NodeState.Locked || _currentState == NodeState.Maxed) return;

            // Satın almayı dene
            if (UpgradeManager.Instance.TryPurchaseUpgrade(_nodeData.upgradeData.upgradeType))
            {
                // UI'ı kendisi refreshleyecek çünkü event dinleyecek
            }
        }
    }
}
