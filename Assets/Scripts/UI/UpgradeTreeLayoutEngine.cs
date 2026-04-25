using System.Collections.Generic;
using UnityEngine;
using Gameplay;

namespace UI
{
    /// <summary>
    /// Upgrade ağacı için radyal pozisyonları hesaplayan layout motoru.
    /// </summary>
    public static class UpgradeTreeLayoutEngine
    {
        [System.Serializable]
        public struct LayoutParameters
        {
            public float baseRadius;       // İlk düğüm halkasının merkeze uzaklığı
            public float radiusStep;       // Her derinlik seviyesinde eklenecek mesafe
            public float minAngleSpacing;  // Düğümler arası minimum derece
            public float angleRandomness;  // Açılara verilecek rastgele sapma (örn. 15 derece)
            public float nodeAvoidanceRadius; // Node'ların birbirini itme mesafesi (Çarpışma yarıçapı)
            public int relaxationIterations;  // İtme fiziğinin gücü/döngü sayısı
            
            public static LayoutParameters Default = new LayoutParameters
            {
                baseRadius = 200f,
                radiusStep = 200f,
                minAngleSpacing = 30f,
                angleRandomness = 20f,
                nodeAvoidanceRadius = 150f,
                relaxationIterations = 0 // Statik motor itme yapmasın, UpgradeTreeUI canlı olarak animasyonlu yapsın
            };
        }

        /// <summary>
        /// Verilen root node'dan başlayarak ağaçtaki tüm node'ların 2D (UI) pozisyonlarını hesaplar.
        /// </summary>
        public static Dictionary<UpgradeNodeDataSO, Vector2> CalculatePositions(UpgradeNodeDataSO rootNode, LayoutParameters parameters)
        {
            var positions = new Dictionary<UpgradeNodeDataSO, Vector2>();
            if (rootNode == null) return positions;

            // Her UI yenilemesinde ağacın şekli değişmesin diye sabit bir seed veriyoruz (Örn: 12345)
            // Bu sayede dalgalanmalar hep aynı ve istikrarlı görünür.
            System.Random prng = new System.Random(12345);

            // Kök node merkeze yerleşir
            positions[rootNode] = Vector2.zero;

            if (rootNode.children != null && rootNode.children.Length > 0)
            {
                // Root'un child'ları 360 dereceye yayılır
                float angleCoverage = 360f;
                float startAngle = 90f; // Başlangıç noktası yukarı (90 derece)

                CalculateChildrenPositions(rootNode.children, 1, startAngle, angleCoverage, Vector2.zero, parameters, positions, prng);
            }

            // Çakışmaları engellemek için Fiziksel Gevşetme (Relaxation / Repulsion) adımı
            ApplyRepulsion(positions, parameters, rootNode);

            return positions;
        }

        private static void ApplyRepulsion(Dictionary<UpgradeNodeDataSO, Vector2> positions, LayoutParameters parameters, UpgradeNodeDataSO rootNode)
        {
            if (parameters.relaxationIterations <= 0 || parameters.nodeAvoidanceRadius <= 0f) return;

            var nodes = new List<UpgradeNodeDataSO>(positions.Keys);
            int count = nodes.Count;

            // Parent-Child bağlarını (Constraint / Yay) çıkarıyoruz
            var edges = new List<(UpgradeNodeDataSO parent, UpgradeNodeDataSO child, float maxDist)>();
            BuildEdges(rootNode, parameters.baseRadius, parameters.radiusStep, 1, edges);

            for (int iter = 0; iter < parameters.relaxationIterations; iter++)
            {
                // 1. İtme Kuvveti (Repulsion - Çakışma önleme)
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        var n1 = nodes[i];
                        var n2 = nodes[j];

                        Vector2 p1 = positions[n1];
                        Vector2 p2 = positions[n2];

                        Vector2 dir = p1 - p2;
                        float dist = dir.magnitude;

                        if (dist < parameters.nodeAvoidanceRadius)
                        {
                            if (dist < 0.01f) // Tam üst üste binmişlerse hafif rastgelelik ekle
                            {
                                dir = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
                                dist = 0.01f;
                            }

                            float overlap = parameters.nodeAvoidanceRadius - dist;
                            // Yumuşak itme katsayısı (her döngüde örtüşmenin %20'si kadar it)
                            Vector2 push = (dir / dist) * (overlap * 0.2f); 

                            // Root yerinden oynamasın
                            if (n1 == rootNode)
                            {
                                positions[n2] -= push * 2f;
                            }
                            else if (n2 == rootNode)
                            {
                                positions[n1] += push * 2f;
                            }
                            else
                            {
                                positions[n1] += push;
                                positions[n2] -= push;
                            }
                        }
                    }
                }

                // 2. Çekme Kuvveti (Yay Kısıtlaması - Bağlantılar Asla Uzayamasın)
                foreach (var edge in edges)
                {
                    if (!positions.ContainsKey(edge.parent) || !positions.ContainsKey(edge.child)) continue;

                    Vector2 pParent = positions[edge.parent];
                    Vector2 pChild = positions[edge.child];

                    Vector2 dir = pChild - pParent;
                    float dist = dir.magnitude;

                    // Eğer olması gerekenden (maxDist) fazla uzadıysa onları geri çek
                    if (dist > edge.maxDist)
                    {
                        float excess = dist - edge.maxDist;
                        Vector2 pull = (dir / dist) * (excess * 0.5f); // Yarı yarıya çek

                        // Root sabit kalmalı
                        if (edge.parent == rootNode)
                        {
                            positions[edge.child] -= pull * 2f; // Child'ı tam güçle geri çek
                        }
                        else
                        {
                            positions[edge.parent] += pull; // Parent da esneyebilir
                            positions[edge.child] -= pull; // Child da esneyebilir
                        }
                    }
                }
            }
        }

        // Ağaçtaki tüm Ebeveyn-Çocuk ilişkilerini listeleyen yardımcı fonksiyon
        private static void BuildEdges(UpgradeNodeDataSO node, float baseRadius, float radiusStep, int depth, List<(UpgradeNodeDataSO parent, UpgradeNodeDataSO child, float maxDist)> edges)
        {
            if (node == null || node.children == null) return;
            
            float maxDist = depth == 1 ? baseRadius : radiusStep;
            foreach (var child in node.children)
            {
                if (child != null)
                {
                    edges.Add((node, child, maxDist));
                    BuildEdges(child, baseRadius, radiusStep, depth + 1, edges);
                }
            }
        }

        private static void CalculateChildrenPositions(
            UpgradeNodeDataSO[] rawChildren, 
            int depth, 
            float parentAngle, 
            float angleCoverage, 
            Vector2 parentPos, 
            LayoutParameters parameters, 
            Dictionary<UpgradeNodeDataSO, Vector2> positions,
            System.Random prng)
        {
            if (rawChildren == null) return;

            // Null olan elemanları ayıkla
            var childrenList = new List<UpgradeNodeDataSO>();
            foreach (var child in rawChildren)
            {
                if (child != null) childrenList.Add(child);
            }

            UpgradeNodeDataSO[] children = childrenList.ToArray();
            int count = children.Length;
            if (count == 0) return;

            // Ebeveyne olan tam mesafe
            float distance = depth == 1 ? parameters.baseRadius : parameters.radiusStep;

            // Pürüzsüz ama rastgele bir açı değişimi (Jitter) hesapla
            float NextJitter() => (float)(prng.NextDouble() * 2.0 - 1.0) * parameters.angleRandomness;

            // Eğer tek child varsa tam parent'ın açısından/yönünden devam etsin (ama randomize)
            if (count == 1 && depth > 1)
            {
                float wriggleAngle = parentAngle + NextJitter();
                
                Vector2 pos = parentPos + CalculatePositionFromAngle(wriggleAngle, distance);
                positions[children[0]] = pos;
                
                if (children[0].children != null && children[0].children.Length > 0)
                {
                    CalculateChildrenPositions(children[0].children, depth + 1, wriggleAngle, angleCoverage, pos, parameters, positions, prng);
                }
                return;
            }

            // Gerekli toplam açı boşluğu
            float requiredAngle = count * parameters.minAngleSpacing;
            float actualCoverage = Mathf.Max(requiredAngle, angleCoverage);
            
            float angleStep = actualCoverage >= 360f ? (360f / count) : (actualCoverage / (count > 1 ? count - 1 : 1));

            // Başlangıç açısı (Parent'ın baktığı yönden dağılmaya başlar)
            float startAngle = parentAngle - (actualCoverage / 2f);
            if (actualCoverage < 360f && count > 1) 
            {
                startAngle = parentAngle - (angleStep * (count - 1) / 2f);
            }

            for (int i = 0; i < count; i++)
            {
                if (children[i] == null) continue;

                float currentAngle = startAngle + (i * angleStep);
                
                if (actualCoverage >= 360f) 
                {
                    currentAngle = startAngle - (i * angleStep);
                }

                // Node kendi açısını hesaplarken biraz sağa veya sola sapar
                float finalAngle = currentAngle + NextJitter();

                Vector2 pos = parentPos + CalculatePositionFromAngle(finalAngle, distance);
                positions[children[i]] = pos;

                if (children[i].children != null && children[i].children.Length > 0)
                {
                    float childCoverage = actualCoverage >= 360f ? (360f / count) : angleStep;
                    float safeCoverage = Mathf.Min(childCoverage * 0.9f, 160f); 
                    
                    // Branch'ın devamı, sapan açıyı ('finalAngle') temel alır, böylece kollar birbirine girmez
                    CalculateChildrenPositions(children[i].children, depth + 1, finalAngle, safeCoverage, pos, parameters, positions, prng);
                }
            }
        }

        private static Vector2 CalculatePositionFromAngle(float angleDegrees, float distance)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRad) * distance, Mathf.Sin(angleRad) * distance);
        }
    }
}
