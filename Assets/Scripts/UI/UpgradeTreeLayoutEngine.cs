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
        public struct LayoutParameters
        {
            public float baseRadius;       // İlk düğüm halkasının merkeze uzaklığı
            public float radiusStep;       // Her derinlik seviyesinde eklenecek mesafe
            public float minAngleSpacing;  // Düğümler arası minimum derece
            
            public static LayoutParameters Default = new LayoutParameters
            {
                baseRadius = 250f,
                radiusStep = 200f,
                minAngleSpacing = 30f
            };
        }

        /// <summary>
        /// Verilen root node'dan başlayarak ağaçtaki tüm node'ların 2D (UI) pozisyonlarını hesaplar.
        /// </summary>
        public static Dictionary<UpgradeNodeDataSO, Vector2> CalculatePositions(UpgradeNodeDataSO rootNode, LayoutParameters parameters)
        {
            var positions = new Dictionary<UpgradeNodeDataSO, Vector2>();
            if (rootNode == null) return positions;

            // Kök node merkeze yerleşir
            positions[rootNode] = Vector2.zero;

            if (rootNode.children != null && rootNode.children.Length > 0)
            {
                // Root'un child'ları 360 dereceye yayılır
                float angleCoverage = 360f;
                float startAngle = 90f; // Başlangıç noktası yukarı (90 derece)

                CalculateChildrenPositions(rootNode.children, 1, startAngle, angleCoverage, Vector2.zero, parameters, positions);
            }

            return positions;
        }

        private static void CalculateChildrenPositions(
            UpgradeNodeDataSO[] rawChildren, 
            int depth, 
            float parentAngle, 
            float angleCoverage, 
            Vector2 parentPos, 
            LayoutParameters parameters, 
            Dictionary<UpgradeNodeDataSO, Vector2> positions)
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

            // Merkeze olan uzaklık
            float currentRadius = parameters.baseRadius + (depth - 1) * parameters.radiusStep;

            // Eğer tek child varsa tam parent'ın açısından/yönünden devam etsin
            if (count == 1 && depth > 1)
            {
                Vector2 pos = CalculatePositionFromAngle(parentAngle, currentRadius);
                positions[children[0]] = pos;
                
                if (children[0].children != null && children[0].children.Length > 0)
                {
                    CalculateChildrenPositions(children[0].children, depth + 1, parentAngle, angleCoverage, pos, parameters, positions);
                }
                return;
            }

            // Gerekli toplam boşluk
            float requiredAngle = count * parameters.minAngleSpacing;
            float actualCoverage = Mathf.Max(requiredAngle, angleCoverage);
            
            // Eğer tam 360 derece döneceksek, ilk ve son düğüm üst üste binmesin diye (count) ile böl, 
            // değilse uçlarda boşluk bırakmak için (count-1) ile böl.
            float angleStep = actualCoverage >= 360f ? (360f / count) : (actualCoverage / (count > 1 ? count - 1 : 1));

            // Başlangıç açısı (Parent'ın baktığı yönden dağılmaya başlar)
            float startAngle = parentAngle - (actualCoverage / 2f);
            if (actualCoverage < 360f && count > 1) 
            {
                // Childları tam ortalamak için
                startAngle += angleStep / 2f;
                // Özel durum: root harici çoklu dağılımda ortalamayı düzeltmek
                startAngle = parentAngle - (angleStep * (count - 1) / 2f);
            }

            for (int i = 0; i < count; i++)
            {
                if (children[i] == null) continue;

                float currentAngle = startAngle + (i * angleStep);
                
                // Tam 360 dönerken özel durum
                if (actualCoverage >= 360f) 
                {
                    currentAngle = startAngle - (i * angleStep); // Saat yönüne göre dizebilirsin
                }

                Vector2 pos = CalculatePositionFromAngle(currentAngle, currentRadius);
                positions[children[i]] = pos;

                if (children[i].children != null && children[i].children.Length > 0)
                {
                    // Child'ın kendi child'larına vereceği alan (toplam alanından kendi payına düşen kadar)
                    float childCoverage = actualCoverage >= 360f ? (360f / count) : angleStep;
                    // Bir miktar daraltma yaparak sibling dalların kesişmesini engelle
                    float safeCoverage = Mathf.Min(childCoverage * 0.9f, 180f); 
                    
                    CalculateChildrenPositions(children[i].children, depth + 1, currentAngle, safeCoverage, pos, parameters, positions);
                }
            }
        }

        private static Vector2 CalculatePositionFromAngle(float angleDegrees, float radius)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius);
        }
    }
}
