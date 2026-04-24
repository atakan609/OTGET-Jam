using System;
using UnityEngine;
using Core;
using Gameplay;

namespace Managers
{
    public class CurrencyManager : Singleton<CurrencyManager>
    {
        // Referanslar
        private BucketController _playerBucket;
        private DepotController _depot;

        // Hesaplanan toplam currency (kova + depo)
        public float TotalCurrency => GetBucketWater() + GetDepotWater();

        // Upgrade'lerden gelen ekonomik değerler (Raindrop tarafından okunur)
        public float GlobalMultiplier => 1f + (UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.CurrencyMultiplier)
            : 0f);

        public float CritChance => UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.CriticalChance)
            : 0f;

        public static event Action<float> OnCurrencyChanged;

        private void Start()
        {
            _playerBucket = FindObjectOfType<BucketController>();
            _depot = FindObjectOfType<DepotController>();
        }

        private void Update()
        {
            ApplyInterest();
            ApplyPassiveIncome();
        }

        // ── Faiz & Pasif Gelir ───────────────────────────────────────────────────

        private void ApplyInterest()
        {
            if (UpgradeManager.Instance == null) return;
            float rate = UpgradeManager.Instance.GetCurrentValue(UpgradeType.InterestRate);
            if (rate <= 0f || _depot == null) return;

            // Toplam currency üzerinden saniyede % faiz → depoya eklenir
            float interest = TotalCurrency * rate * Time.deltaTime;
            if (interest > 0f)
            {
                _depot.AddWater(interest);
                OnCurrencyChanged?.Invoke(TotalCurrency);
            }
        }

        private void ApplyPassiveIncome()
        {
            if (UpgradeManager.Instance == null) return;
            float income = UpgradeManager.Instance.GetCurrentValue(UpgradeType.PassiveIncome);
            if (income <= 0f || _depot == null) return;

            float gained = income * Time.deltaTime;
            _depot.AddWater(gained);
            OnCurrencyChanged?.Invoke(TotalCurrency);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>BucketController veya StaticBucket su miktarı değiştiğinde çağrılır.</summary>
        public void NotifyWaterChanged()
        {
            OnCurrencyChanged?.Invoke(TotalCurrency);
        }

        /// <summary>
        /// Önce depodaki su harcanır. Yetmezse kova eksiltilir.
        /// </summary>
        public bool SpendCurrency(int amount)
        {
            if (TotalCurrency < amount) return false;

            float remaining = amount;

            if (_depot != null)
            {
                float fromDepot = Mathf.Min(remaining, _depot.StoredWater);
                _depot.RemoveWater(fromDepot);
                remaining -= fromDepot;
            }

            if (remaining > 0f && _playerBucket != null)
            {
                _playerBucket.DrainWater(remaining);
            }

            OnCurrencyChanged?.Invoke(TotalCurrency);
            return true;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private float GetBucketWater()
        {
            if (_playerBucket == null) _playerBucket = FindObjectOfType<BucketController>();
            return _playerBucket != null ? _playerBucket.CurrentWater : 0f;
        }

        private float GetDepotWater()
        {
            if (_depot == null) _depot = FindObjectOfType<DepotController>();
            return _depot != null ? _depot.StoredWater : 0f;
        }

        // ── Debug ────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;

            float mult = GlobalMultiplier * (ComboManager.Instance != null ? ComboManager.Instance.Multiplier : 1f);

            GUILayout.BeginArea(new Rect(20, 20, 350, 40));
            GUILayout.Label($"💧 {TotalCurrency:F1}  (K:{GetBucketWater():F1} + D:{GetDepotWater():F1})  ×{mult:F2}", style);
            GUILayout.EndArea();
        }
    }
}
