using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "UpgradeNodeData", menuName = "OTGET/Upgrade Node")]
    public class UpgradeNodeDataSO : ScriptableObject
    {
        [Tooltip("Bu node'un temsil ettiği temel upgrade verisi")]
        public UpgradeDataSO upgradeData;
        
        [Tooltip("Bu node'dan ağaçta dışa doğru açılacak child upgrade node'ları")]
        public UpgradeNodeDataSO[] children;
        
        [Tooltip("UI'da gösterilecek baloncuk ikonu")]
        public Sprite icon;
    }
}
