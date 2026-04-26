using System.Collections.Generic;
using Gameplay;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UpgradeTreeUI : MonoBehaviour
    {
        public static UpgradeTreeUI Instance { get; private set; }

        [Header("Data")]
        [SerializeField]
        private UpgradeTreeDataSO treeData;
        public UpgradeTreeDataSO TreeData => treeData;

        [Header("References")]
        [SerializeField]
        private RectTransform contentTree; // Node'ların içine koyulacağı container (pan/zoom için)

        [SerializeField]
        private GameObject nodePrefab;

        [SerializeField]
        private GameObject linePrefab;

        [Header("Layout Parameters")]
        public UpgradeTreeLayoutEngine.LayoutParameters layoutParameters = UpgradeTreeLayoutEngine
            .LayoutParameters
            .Default;

        [Header("UI Toggle")]
        [SerializeField]
        private KeyCode toggleKey = KeyCode.U;

        [Tooltip(
            "Açılıp kapanacak asıl görsel Panel. (Boş bırakılırsa bu objenin altındaki her şey kapatılıp açılır)"
        )]
        [SerializeField]
        private GameObject mainPanel;

        [Header("Zoom Settings")]
        [SerializeField]
        private float zoomSpeed = 0.1f;

        [SerializeField]
        private float minZoom = 0.3f;

        [SerializeField]
        private float maxZoom = 2f;

        [Header("Tooltip")]
        [SerializeField]
        private GameObject tooltipPanel;

        [SerializeField]
        private TMPro.TextMeshProUGUI tooltipTitleText;

        [SerializeField]
        private TMPro.TextMeshProUGUI tooltipDescText;

        [SerializeField]
        private TMPro.TextMeshProUGUI tooltipCostText;

        private class TreeConnection
        {
            public UpgradeNodeDataSO parent;
            public UpgradeNodeDataSO child;
            public RectTransform lineRect;
            public float maxDist;
        }

        // Tutulan referanslar
        private Dictionary<UpgradeNodeDataSO, UpgradeNodeUI> _createdNodes =
            new Dictionary<UpgradeNodeDataSO, UpgradeNodeUI>();
        private Dictionary<UpgradeNodeDataSO, RectTransform> _nodeRects =
            new Dictionary<UpgradeNodeDataSO, RectTransform>();
        private List<TreeConnection> _connections = new List<TreeConnection>();

        private float _currentZoom = 1f;
        private bool _isOpen = false;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            BuildTree();

            // Başlangıçta kapalı olmasını sağla
            if (mainPanel != null)
            {
                CanvasGroup cg = mainPanel.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = mainPanel.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                _isOpen = false;
            }

            // Event'i dinlemeye başla
            UpgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleUI();
            }

            // UI Açıkken Mouse Scroll ile Zoom yap
            if (_isOpen && contentTree != null)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0)
                {
                    _currentZoom += scroll * zoomSpeed;
                    _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
                    contentTree.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
                }

                // Canlı fizik ve çizgi güncellemesi
                ApplyLivePhysics();
            }

            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                RectTransform tooltipRect = tooltipPanel.transform as RectTransform;
                Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();

                // Mouse pozisyonuna offset ekleyerek hedefi belirle (Böylece birikme yapmaz)
                Vector3 mouseWithOffset = Input.mousePosition + new Vector3(25f, -25f, 0f);
                Vector3 targetPos = mouseWithOffset;

                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
                {
                    mouseWithOffset.z = canvas.planeDistance;
                    targetPos = canvas.worldCamera.ScreenToWorldPoint(mouseWithOffset);
                }

                // Smooth follow (Damping) - Tek seferde hedefe Lerp yapıyoruz
                tooltipRect.position = Vector3.Lerp(tooltipRect.position, targetPos, Time.unscaledDeltaTime * 20f);
            }
        }

        private void ApplyLivePhysics()
        {
            if (_nodeRects.Count == 0)
                return;

            var nodes = new List<UpgradeNodeDataSO>(_nodeRects.Keys);
            int count = nodes.Count;

            // 1. Canlı İtme Kuvveti (Çakışma Önleme)
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    var n1 = nodes[i];
                    var n2 = nodes[j];

                    Vector2 p1 = _nodeRects[n1].anchoredPosition;
                    Vector2 p2 = _nodeRects[n2].anchoredPosition;

                    Vector2 dir = p1 - p2;
                    float dist = dir.magnitude;

                    if (dist < layoutParameters.nodeAvoidanceRadius)
                    {
                        if (dist < 0.01f)
                        {
                            dir = new Vector2(
                                UnityEngine.Random.Range(-1f, 1f),
                                UnityEngine.Random.Range(-1f, 1f)
                            ).normalized;
                            dist = 0.01f;
                        }

                        float overlap = layoutParameters.nodeAvoidanceRadius - dist;
                        Vector2 push = (dir / dist) * (overlap * 5f * Time.deltaTime);

                        // Root sabit kalsın
                        if (n1 == treeData.rootNode)
                            _nodeRects[n2].anchoredPosition -= push * 2f;
                        else if (n2 == treeData.rootNode)
                            _nodeRects[n1].anchoredPosition += push * 2f;
                        else
                        {
                            _nodeRects[n1].anchoredPosition += push;
                            _nodeRects[n2].anchoredPosition -= push;
                        }
                    }
                }
            }

            // 2. Canlı Yay Kısıtlaması ve Çizgi Pozisyonlarını Güncelleme
            foreach (var conn in _connections)
            {
                if (!_nodeRects.ContainsKey(conn.parent) || !_nodeRects.ContainsKey(conn.child))
                    continue;

                Vector2 pParent = _nodeRects[conn.parent].anchoredPosition;
                Vector2 pChild = _nodeRects[conn.child].anchoredPosition;

                Vector2 dir = pChild - pParent;
                float dist = dir.magnitude;

                // Asla uzayamasın (Yay mantığı)
                if (dist > conn.maxDist)
                {
                    float excess = dist - conn.maxDist;
                    Vector2 pull = (dir / dist) * (excess * 10f * Time.deltaTime);

                    if (conn.parent == treeData.rootNode)
                        _nodeRects[conn.child].anchoredPosition -= pull * 2f;
                    else
                    {
                        _nodeRects[conn.parent].anchoredPosition += pull;
                        _nodeRects[conn.child].anchoredPosition -= pull;
                    }

                    // İşlem sonrası mesafeyi çizgiler için tazele
                    pParent = _nodeRects[conn.parent].anchoredPosition;
                    pChild = _nodeRects[conn.child].anchoredPosition;
                    dir = pChild - pParent;
                    dist = dir.magnitude;
                }

                // Çizgiyi anlık olarak node'lar arasında tut
                if (conn.lineRect != null)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    conn.lineRect.anchoredPosition = pParent + (dir / 2f);
                    conn.lineRect.sizeDelta = new Vector2(dist, conn.lineRect.sizeDelta.y);
                    conn.lineRect.localRotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }

        private bool _firstTimeOpen = true;

        public void ToggleUI()
        {
            if (mainPanel != null)
            {
                // Objenin kapalı olup tamamen durmasını engellemek için CanvasGroup kullanıyoruz
                CanvasGroup cg = mainPanel.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = mainPanel.AddComponent<CanvasGroup>();

                _isOpen = cg.alpha < 0.5f; // Şu an kapalıysa açılacak (true olacak)
                cg.alpha = _isOpen ? 1f : 0f;
                cg.interactable = _isOpen;
                cg.blocksRaycasts = _isOpen;

                if (_isOpen && _firstTimeOpen)
                {
                    CenterView();
                    _firstTimeOpen = false;
                }
            }
            else
            {
                // Fallback: mainPanel atanmamışsa kendi altındaki tüm objeleri aç/kapat
                _isOpen = !_isOpen;
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(_isOpen);
                }

                if (_isOpen && _firstTimeOpen)
                {
                    CenterView();
                    _firstTimeOpen = false;
                }
            }
        }

        public void CenterView()
        {
            if (contentTree == null) return;

            // Zoom'u sıfırla
            _currentZoom = 1f;
            contentTree.localScale = Vector3.one;

            // Pivot ve Anchor'ları merkeze zorla (Garantiye almak için)
            contentTree.pivot = new Vector2(0.5f, 0.5f);
            contentTree.anchorMin = new Vector2(0.5f, 0.5f);
            contentTree.anchorMax = new Vector2(0.5f, 0.5f);

            // Pozisyonu merkeze al
            contentTree.anchoredPosition = Vector2.zero;

            // Eğer bir ScrollRect varsa onun da değerlerini sıfırla
            ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
            if (scrollRect == null) scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);
            }
        }

        private void BuildTree()
        {
            if (treeData == null || treeData.rootNode == null)
                return;
            if (contentTree == null || nodePrefab == null)
                return;

            // Önce mevcut içeriği temizle (Refresh yapılırken kolaylık olsun)
            foreach (Transform child in contentTree)
            {
                Destroy(child.gameObject);
            }
            _createdNodes.Clear();
            _nodeRects.Clear();
            _connections.Clear();

            // 1. Başlangıç Pozisyonlarını hesapla (Engine içindeki statik hesaplama iterasyonlarını 0 varsayıyoruz)
            var positions = UpgradeTreeLayoutEngine.CalculatePositions(
                treeData.rootNode,
                layoutParameters
            );

            // 2. Başlangıç bağlantıları artık CreateNodeUI içinde veya dinamik spawn sırasında çizilecek
            // DrawConnections(treeData.rootNode, positions, 1);

            // 3. Node'ları oluştur (Sadece alınanlar ve onların bir alt kademesi oluşturulacak)
            CreateNodeUI(treeData.rootNode, null, positions);

            // 4. Content boyutunu sınırla (ScrollView'in dışarı taşmaması için)
            float maxExtentsX = 0f;
            float maxExtentsY = 0f;
            foreach (var pos in positions.Values)
            {
                if (Mathf.Abs(pos.x) > maxExtentsX)
                    maxExtentsX = Mathf.Abs(pos.x);
                if (Mathf.Abs(pos.y) > maxExtentsY)
                    maxExtentsY = Mathf.Abs(pos.y);
            }

            // Düğümlerin kendi kapladığı alanı (padding) da hesaba katalım
            float paddingX = 400f;
            float paddingY = 800f; // Y sınırlarını özellikle daha fazla artırdık
            
            float totalSizeX = (maxExtentsX * 2f) + paddingX;
            float totalSizeY = (maxExtentsY * 2f) + paddingY;

            // Merkezin (0,0) içerik alanının tam ortası olması için pivot'u ayarla
            contentTree.pivot = new Vector2(0.5f, 0.5f);

            // Yeni sınırları uygula
            contentTree.sizeDelta = new Vector2(totalSizeX, totalSizeY);
        }

        private void CreateNodeUI(
            UpgradeNodeDataSO node,
            UpgradeNodeDataSO parent,
            Dictionary<UpgradeNodeDataSO, Vector2> positions
        )
        {
            if (node == null || !positions.ContainsKey(node))
                return;

            // Zaten oluşturulmuşsa atla
            if (_createdNodes.ContainsKey(node))
                return;

            GameObject obj = Instantiate(nodePrefab, contentTree);
            obj.transform.SetAsLastSibling(); // Node'lar daima en üstte kalsın
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = positions[node];
            _nodeRects[node] = rect;

            var nodeUI = obj.GetComponent<UpgradeNodeUI>();
            if (nodeUI != null)
            {
                nodeUI.Setup(node, parent);
                _createdNodes[node] = nodeUI;
            }

            // Child'lara devam: Sadece bu node satın alınmışsa child'ları oluştur
            if (node.children != null)
            {
                bool purchased = UpgradeManager.Instance.GetLevel(node.upgradeData.upgradeType) > 0;
                bool isRoot = (node == treeData.rootNode);

                if (purchased || isRoot)
                {
                    foreach (var child in node.children)
                    {
                        if (child == null) continue;
                        CreateNodeUI(child, node, positions);
                        CreateConnection(node, child, positions);
                    }
                }
            }
        }

        private void DrawConnections(
            UpgradeNodeDataSO current,
            Dictionary<UpgradeNodeDataSO, Vector2> positions,
            int depth
        )
        {
            if (current == null || current.children == null || current.children.Length == 0)
                return;

            float maxDist = depth == 1 ? layoutParameters.baseRadius : layoutParameters.radiusStep;

            foreach (var child in current.children)
            {
                if (child == null || !positions.ContainsKey(child))
                    continue;

                RectTransform lineRect = null;
                // Çizgi oluştur
                if (linePrefab != null)
                {
                    GameObject lineObj = Instantiate(linePrefab, contentTree);
                    lineRect = lineObj.GetComponent<RectTransform>();
                    // Çizgi pozisyonu Update'te (ApplyLivePhysics) hesaplanacak
                }

                _connections.Add(
                    new TreeConnection
                    {
                        parent = current,
                        child = child,
                        lineRect = lineRect,
                        maxDist = maxDist,
                    }
                );

                // Recursive devam
                DrawConnections(child, positions, depth + 1);
            }
        }

        private void HandleUpgradePurchased(UpgradeType type, int newLevel)
        {
            // Eğer bir şey alındıysa, child'ların kilit durumu da değişebilir diye tüm node'ları refreshliyoruz
            foreach (var node in _createdNodes.Values)
            {
                node.RefreshState();
            }

            SpawnNewChildren(type);
        }

        private void SpawnNewChildren(UpgradeType purchasedType)
        {
            UpgradeNodeDataSO purchasedNode = FindNodeByType(purchasedType);
            if (purchasedNode == null || purchasedNode.children == null) return;

            var positions = UpgradeTreeLayoutEngine.CalculatePositions(treeData.rootNode, layoutParameters);

            foreach (var child in purchasedNode.children)
            {
                // child veya child.upgradeData null olabilir, güvenli kontrol
                if (child == null) continue;

                if (!_createdNodes.ContainsKey(child))
                {
                    CreateNodeUI(child, purchasedNode, positions);
                    CreateConnection(purchasedNode, child, positions);
                }
            }

            RecalculateContentSize(positions);
            CenterView();
        }

        private UpgradeNodeDataSO FindNodeByType(UpgradeType type)
        {
            foreach (var node in _createdNodes.Keys)
            {
                // node veya upgradeData null olabilir
                if (node == null || node.upgradeData == null) continue;
                if (node.upgradeData.upgradeType == type) return node;
            }
            return null;
        }

        private void CreateConnection(UpgradeNodeDataSO parent, UpgradeNodeDataSO child, Dictionary<UpgradeNodeDataSO, Vector2> positions)
        {
            // Zaten bağlantı var mı kontrol et
            foreach (var conn in _connections)
            {
                if (conn.parent == parent && conn.child == child) return;
            }

            // Derinlik bul (Root'tan itibaren)
            int depth = GetDepth(parent);
            float maxDist = depth == 1 ? layoutParameters.baseRadius : layoutParameters.radiusStep;

            RectTransform lineRect = null;
            if (linePrefab != null)
            {
                GameObject lineObj = Instantiate(linePrefab, contentTree);
                lineObj.transform.SetAsFirstSibling(); // Çizgiler daima en arkada kalsın
                lineRect = lineObj.GetComponent<RectTransform>();
                
                // İlk pozisyonu tahmini atayalım, ApplyLivePhysics onu toparlar
                if (_nodeRects.ContainsKey(parent) && _nodeRects.ContainsKey(child))
                {
                    Vector2 pParent = _nodeRects[parent].anchoredPosition;
                    Vector2 pChild = _nodeRects[child].anchoredPosition;
                    Vector2 dir = pChild - pParent;
                    lineRect.anchoredPosition = pParent + (dir / 2f);
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    lineRect.rotation = Quaternion.Euler(0, 0, angle);
                    lineRect.sizeDelta = new Vector2(dir.magnitude, lineRect.sizeDelta.y);
                }
            }

            _connections.Add(new TreeConnection
            {
                parent = parent,
                child = child,
                lineRect = lineRect,
                maxDist = maxDist,
            });
        }

        private int GetDepth(UpgradeNodeDataSO node)
        {
            if (node == treeData.rootNode) return 1;
            // Kaba bir tahmin:
            return 2;
        }

        private void RecalculateContentSize(Dictionary<UpgradeNodeDataSO, Vector2> positions)
        {
            float maxExtentsX = 0f;
            float maxExtentsY = 0f;
            foreach (var pos in positions.Values)
            {
                if (Mathf.Abs(pos.x) > maxExtentsX) maxExtentsX = Mathf.Abs(pos.x);
                if (Mathf.Abs(pos.y) > maxExtentsY) maxExtentsY = Mathf.Abs(pos.y);
            }

            float paddingX = 400f;
            float paddingY = 800f;
            float totalSizeX = (maxExtentsX * 2f) + paddingX;
            float totalSizeY = (maxExtentsY * 2f) + paddingY;

            contentTree.sizeDelta = new Vector2(totalSizeX, totalSizeY);
        }

        // Ağacı baştan kurdurma
        public void Rebuild()
        {
            BuildTree();
        }

        public void ShowTooltip(UpgradeDataSO data, int currentLevel, int nextCost, bool isMax)
        {
            if (tooltipPanel == null || data == null)
                return;

            tooltipPanel.SetActive(true);

            // 1. Yazıları hazırla
            if (tooltipTitleText != null) 
                tooltipTitleText.text = $"<b>{data.upgradeName}</b>";

            if (tooltipDescText != null)
            {
                float curVal = data.GetValue(currentLevel);
                float nextVal = data.GetValue(currentLevel + 1);
                string unit = "mL";

                string desc = data.description + "\n";
                desc += $"<color=#A0A0A0>Current:</color> <color=#FFFFFF>{curVal}{unit}</color>\n";
                if (!isMax) desc += $"<color=#A0A0A0>Next:</color> <color=#00FF00>+{nextVal - curVal}{unit} ({nextVal}{unit})</color>\n";
                
                string lvlTag = data.isInfinite ? $"Lv{currentLevel}" : $"Lv{currentLevel}/{data.maxLevel}";
                desc += $"<size=80%><color=#FFD700>{lvlTag}</color></size>";

                tooltipDescText.text = desc;
            }

            if (tooltipCostText != null)
            {
                if (isMax) tooltipCostText.text = "<color=#FF4444>MAXED OUT</color>";
                else tooltipCostText.text = $"Cost: <color=#00CCFF>{nextCost} mL</color>";
            }

            // 2. KRİTİK: En uzun olanın genişliğini bul ve hepsini ona eşitle
            float maxWidth = 0f;
            if (tooltipTitleText != null) maxWidth = Mathf.Max(maxWidth, tooltipTitleText.GetPreferredValues().x);
            if (tooltipDescText != null) maxWidth = Mathf.Max(maxWidth, tooltipDescText.GetPreferredValues().x);
            if (tooltipCostText != null) maxWidth = Mathf.Max(maxWidth, tooltipCostText.GetPreferredValues().x);

            // Genişlik için makul bir sınır koyalım (Ekrana sığması için)
            maxWidth = Mathf.Clamp(maxWidth, 150f, 400f);

            // Tüm elemanları bu genişliğe zorla, yüksekliklerini kendi içeriklerine göre hesaplat
            SetTextSize(tooltipTitleText, maxWidth);
            SetTextSize(tooltipDescText, maxWidth);
            SetTextSize(tooltipCostText, maxWidth);

            // 3. Tüm sistemin boyutlarını tazeleyip hizala
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.transform as RectTransform);
        }

        private void SetTextSize(TMPro.TextMeshProUGUI text, float width)
        {
            if (text == null) return;
            
            // Verilen genişliğe göre ihtiyacı olan yüksekliği hesaplat
            float height = text.GetPreferredValues(width, 0f).y;
            text.rectTransform.sizeDelta = new Vector2(width, height);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }
    }
}
