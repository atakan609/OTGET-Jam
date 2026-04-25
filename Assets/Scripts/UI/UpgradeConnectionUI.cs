using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UpgradeConnectionUI : MonoBehaviour
    {
        [SerializeField] private Image lineImage;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color unlockedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        // İleride ağaç güncellendiğinde çizgilerin rengini tazelemek için kullanılabilir
        public void SetState(bool isUnlocked)
        {
            if (lineImage != null)
            {
                lineImage.color = isUnlocked ? unlockedColor : lockedColor;
            }
        }
    }
}
