using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "UpgradeTreeData", menuName = "OTGET/Upgrade Tree")]
    public class UpgradeTreeDataSO : ScriptableObject
    {
        [Tooltip("Ağacın başlangıç noktası. Genellikle temel oyun döngüsüne ait ilk upgrade olur.")]
        public UpgradeNodeDataSO rootNode;

        [Header("Prerequisites (Ön Koşullar)")]
        [Tooltip("Eğer seçilirse, bu orman/ağaç ancak belirtilen parent ağaçtaki sonsuz olmayan tüm upgradeler en az Lv1 yapıldığında açılır (görünür olur veya alınabilir olur).")]
        public UpgradeTreeDataSO prerequisiteTree;

        [Tooltip("Ağaç kilitliyken üzerine gelindiğinde gösterilecek uyarı mesajı.")]
        public string lockedTooltipMessage = "Bu ağacı açmak için önce önceki ağaçtaki tüm yükseltmeleri açmalısın!";
    }
}
