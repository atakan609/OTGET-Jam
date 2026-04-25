using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "UpgradeTreeData", menuName = "OTGET/Upgrade Tree")]
    public class UpgradeTreeDataSO : ScriptableObject
    {
        [Tooltip("Ağacın başlangıç noktası. Genellikle temel oyun döngüsüne ait ilk upgrade olur.")]
        public UpgradeNodeDataSO rootNode;
    }
}
