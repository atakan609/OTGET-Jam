using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Gameplay;
using Managers;

namespace UI
{
    public class UpgradeTreeUI : MonoBehaviour
    {
        public static UpgradeTreeUI Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private UpgradeTreeDataSO treeData;
        public UpgradeTreeDataSO TreeData => treeData;

        [Header("References")]
        [SerializeField] private RectTransform contentTree; // Node'ların içine koyulacağı container (pan/zoom için)
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private GameObject linePrefab;

        [Header("Layout Parameters")]
        public UpgradeTreeLayoutEngine.LayoutParameters layoutParameters = UpgradeTreeLayoutEngine.LayoutParameters.Default;

        // Tutulan Node referansları
        private Dictionary<UpgradeNodeDataSO, UpgradeNodeUI> _createdNodes = new Dictionary<UpgradeNodeDataSO, UpgradeNodeUI>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            BuildTree();
            
            // Event'i dinlemeye başla
            UpgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
        }

        private void BuildTree()
        {
            if (treeData == null || treeData.rootNode == null) return;
            if (contentTree == null || nodePrefab == null) return;

            // Önce mevcut içeriği temizle (Refresh yapılırken kolaylık olsun)
            foreach (Transform child in contentTree)
            {
                Destroy(child.gameObject);
            }
            _createdNodes.Clear();

            // 1. Pozisyonları hesapla
            var positions = UpgradeTreeLayoutEngine.CalculatePositions(treeData.rootNode, layoutParameters);

            // 2. Çizgileri oluştur (Node'lar üstte kalmalı)
            DrawConnections(treeData.rootNode, positions);

            // 3. Parent ilişkilerini yakalamak için recursive oluştur (root'dan başla)
            CreateNodeUI(treeData.rootNode, null, positions);
        }

        private void CreateNodeUI(UpgradeNodeDataSO node, UpgradeNodeDataSO parent, Dictionary<UpgradeNodeDataSO, Vector2> positions)
        {
            if (node == null || !positions.ContainsKey(node)) return;

            // Zaten oluşturulmuşsa atla
            if (_createdNodes.ContainsKey(node)) return;

            GameObject obj = Instantiate(nodePrefab, contentTree);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = positions[node];

            var nodeUI = obj.GetComponent<UpgradeNodeUI>();
            if (nodeUI != null)
            {
                nodeUI.Setup(node, parent);
                _createdNodes[node] = nodeUI;
            }

            // Child'lara devam
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    CreateNodeUI(child, node, positions);
                }
            }
        }

        private void DrawConnections(UpgradeNodeDataSO current, Dictionary<UpgradeNodeDataSO, Vector2> positions)
        {
            if (current == null || current.children == null || current.children.Length == 0) return;

            Vector2 startPos = positions[current];

            foreach (var child in current.children)
            {
                if (child == null || !positions.ContainsKey(child)) continue;

                Vector2 endPos = positions[child];

                // Çizgi oluştur
                if (linePrefab != null)
                {
                    GameObject lineObj = Instantiate(linePrefab, contentTree);
                    var lineRect = lineObj.GetComponent<RectTransform>();

                    // Çizginin uzunluğunu ve açısını hesapla
                    Vector2 dir = endPos - startPos;
                    float distance = dir.magnitude;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    lineRect.anchoredPosition = startPos + (dir / 2f);
                    lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y); // Yükseklik (kalınlık) prefabdan kalır
                    lineRect.localRotation = Quaternion.Euler(0, 0, angle);
                }

                // Recursive devam
                DrawConnections(child, positions);
            }
        }

        private void HandleUpgradePurchased(UpgradeType type, int newLevel)
        {
            // Eğer bir şey alındıysa, child'ların kilit durumu da değişebilir diye tüm node'ları refreshliyoruz
            foreach (var node in _createdNodes.Values)
            {
                node.RefreshState();
            }
        }
        
        // Ağacı baştan kurdurma
        public void Rebuild()
        {
            BuildTree();
        }
    }
}
