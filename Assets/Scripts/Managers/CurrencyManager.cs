using System;
using UnityEngine;
using Core;
using Gameplay;

namespace Managers
{
    public class CurrencyManager : Singleton<CurrencyManager>
    {
        // ── Currency UI Font Ayarları ──────────────────────────────────────────
        [Header("Currency UI")]
        [SerializeField] private int totalFontSize  = 36;
        [SerializeField] private int subFontSize     = 22;

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


        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>BucketController veya StaticBucket su miktarı değiştiğinde çağrılır.</summary>
        public void NotifyWaterChanged()
        {
            OnCurrencyChanged?.Invoke(TotalCurrency);
        }

        /// <summary>
        /// Önce depodaki su harcanır. Yetmezse kova eksiltilir.
        /// </summary>
        public bool SpendCurrency(float amount)
        {
            // Küçük float yuvarlama farklarını tolere etmek için epsilon kullan
            if (TotalCurrency < amount - 0.001f) return false;

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

        // ── Yardımcı Araçlar ───────────────────────────────────────────────────
        public static string FormatWater(float ml)
        {
            if (ml >= 1000f)
            {
                return (ml / 1000f).ToString("F1") + " L";
            }
            return ml.ToString("F1") + " mL";
        }

        // ── Gelir Hesaplama ──────────────────────────────────────────────────────
        /// <summary>Toplam saniyedeki su kazancını hesaplar (generator + pasif gelir + faiz).</summary>
        public float GetIncomePerSecond()
        {
            if (UpgradeManager.Instance == null) return 0f;

            // Faiz (mevcut total currency × oran)
            float interestRate = UpgradeManager.Instance.GetCurrentValue(UpgradeType.InterestRate);
            float income = TotalCurrency * interestRate;

            // Generator pasif gelirleri
            var genMgr = FindObjectOfType<GeneratorManager>();
            if (genMgr != null)
                income += genMgr.GetTotalIncomePerSecond();

            return income;
        }

        // ── Debug / Currency UI ──────────────────────────────────────────────────
        private void OnGUI()
        {

            float bucketWater    = GetBucketWater();
            float bucketMax      = _playerBucket != null ? _playerBucket.MaxCapacity : 0f;
            float depotWater     = GetDepotWater();
            float depotMax       = _depot != null ? _depot.MaxCapacity : 0f;
            float total          = TotalCurrency;
            float incomePS       = GetIncomePerSecond();
            var   combo          = ComboManager.Instance;
            bool  hasDepot    = UpgradeManager.Instance != null &&
                                  UpgradeManager.Instance.GetLevel(UpgradeType.BuyWaterDepot) >= 1;
            bool  showCombo   = combo != null && combo.ComboCount > 1;

            // ── Stiller ─────────────────────────────────────────────────────────
            GUIStyle MakeStyle(int size, Color color, bool bold = true)
            {
                var s = new GUIStyle(GUI.skin.label);
                s.fontSize  = size;
                s.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
                s.normal.textColor = color;
                s.font = null;
                return s;
            }

            var totalStyle  = MakeStyle(totalFontSize, Color.white);
            var bucketStyle = MakeStyle(subFontSize,   new Color(0.3f, 0.65f, 1f));
            var depotStyle  = MakeStyle(subFontSize,   new Color(0.1f, 0.9f,  0.4f));
            var incomeStyle = MakeStyle(subFontSize,   new Color(0.1f, 0.9f,  0.4f));
            var comboStyle  = MakeStyle(subFontSize,   Color.yellow);

            float rowH = subFontSize + 8f;
            float topH = totalFontSize + 12f;
            float x    = 20f;
            float y    = 16f;

            // Satır 1: Toplam + gelir/sn
            GUILayout.BeginArea(new Rect(x, y, 440f, topH));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"💧 {FormatWater(total)}", totalStyle);
            GUILayout.FlexibleSpace();
            if (incomePS > 0f)
                GUILayout.Label($"+{FormatWater(incomePS)}/sn", incomeStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            y += topH;

            // Satır 2: Kova
            GUILayout.BeginArea(new Rect(x, y, 440f, rowH));
            bool kovaDolu        = _playerBucket != null && _playerBucket.IsFull;
            var  realBucketStyle = kovaDolu
                ? MakeStyle(subFontSize, new Color(1f, 0.35f, 0.35f))
                : bucketStyle;
            GUILayout.Label($"🪣 Kova: {FormatWater(bucketWater)} / {FormatWater(bucketMax)}", realBucketStyle);
            GUILayout.EndArea();
            y += rowH;

            // Satır 3: Depo (sadece BuyWaterDepot alındıktan sonra)
            if (hasDepot)
            {
                GUILayout.BeginArea(new Rect(x, y, 440f, rowH));
                GUILayout.Label($"🏛 Depo: {FormatWater(depotWater)} / {FormatWater(depotMax)}", depotStyle);
                GUILayout.EndArea();
                y += rowH;
            }

            // Satır 4 (opsiyonel): Kombo
            if (showCombo)
            {
                GUILayout.BeginArea(new Rect(x, y, 440f, rowH));
                GUILayout.Label($"⚡ Kombo x{combo.ComboCount}  →  {combo.Multiplier:F2}x  ({combo.DecayTimer:F1}s)", comboStyle);
                GUILayout.EndArea();
            }
        }
    }
}
