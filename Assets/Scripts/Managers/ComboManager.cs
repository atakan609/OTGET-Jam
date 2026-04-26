using System;
using UnityEngine;
using Core;
using Gameplay;

namespace Managers
{
    public class ComboManager : Singleton<ComboManager>
    {
        private int _comboCount;
        private float _decayTimer;

        public int ComboCount => _comboCount;
        public float Multiplier => CalculateMultiplier();

        public static event Action<int, float> OnComboChanged; // (count, multiplier)

        private void Update()
        {
            if (_comboCount <= 0) return;

            _decayTimer -= Time.deltaTime;
            if (_decayTimer <= 0f)
            {
                ResetCombo();
            }
        }

        /// <summary>Bir damla toplandığında çağrılır.</summary>
        public void RegisterCollection()
        {
            _comboCount++;
            ResetDecayTimer();
            OnComboChanged?.Invoke(_comboCount, Multiplier);
        }

        private void ResetCombo()
        {
            _comboCount = 0;
            OnComboChanged?.Invoke(0, 1f);
        }

        private void ResetDecayTimer()
        {
            float decayTime = UpgradeManager.Instance != null
                ? 1.5f + UpgradeManager.Instance.GetCurrentValue(UpgradeType.ComboDecayTime)
                : 1.5f;
            _decayTimer = decayTime;
        }

        private float CalculateMultiplier()
        {
            if (_comboCount <= 0) return 1f;

            float maxMult = UpgradeManager.Instance != null
                ? 2f + UpgradeManager.Instance.GetCurrentValue(UpgradeType.ComboMaxMultiplier)
                : 2f;

            // Her 5 kombo +0.1x, max'a kadar
            float mult = 1f + (_comboCount / 5) * 0.1f;
            return Mathf.Min(mult, maxMult);
        }

        public float DecayTimer => _decayTimer;
    }
}
