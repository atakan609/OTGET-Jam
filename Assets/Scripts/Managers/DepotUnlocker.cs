using UnityEngine;
using Managers;
using Gameplay;

namespace Managers
{
    public class DepotUnlocker : MonoBehaviour
    {
        [SerializeField] private GameObject depotObject;

        private void Start()
        {
            if (depotObject == null) return;

            // Eğer zaten satın alınmışsa aktifleştir
            if (UpgradeManager.Instance != null && 
                UpgradeManager.Instance.GetLevel(UpgradeType.BuyWaterDepot) > 0)
            {
                depotObject.SetActive(true);
            }
            else
            {
                depotObject.SetActive(false);
            }

            UpgradeManager.OnUpgradePurchased += OnUpgrade;
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= OnUpgrade;
        }

        private void OnUpgrade(UpgradeType type, int level)
        {
            if (type == UpgradeType.BuyWaterDepot && depotObject != null)
            {
                depotObject.SetActive(true);
            }
        }
    }
}
