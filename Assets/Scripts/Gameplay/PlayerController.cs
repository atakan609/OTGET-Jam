using System.Collections;
using UnityEngine;
using Managers;

namespace Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 8f;

        [Header("Lightning Stun")]
        [SerializeField] private float blinkInterval = 0.1f; // Blink hızı (saniye)

        [Header("References")]
        public BucketController Bucket;

        private Rigidbody2D _rb;
        private Animator _anim;
        private SpriteRenderer _spriteRenderer;
        private float _horizontalInput;
        private float _currentMoveSpeed;
        private bool _isStunned;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _rb.freezeRotation = true;
            gameObject.tag = "Player";
            _currentMoveSpeed = baseMoveSpeed;
        }

        private void Start()
        {
            UpgradeManager.OnUpgradePurchased += HandleUpgrade;
            RefreshSpeed();
        }

        private void OnDestroy()
        {
            UpgradeManager.OnUpgradePurchased -= HandleUpgrade;
        }

        private void Update()
        {
            if (_isStunned)
            {
                _horizontalInput = 0f;
                if (_anim != null) _anim.SetFloat("Speed", 0f);
                // Stun'dayken de kova kapalı
                Bucket?.SetOpen(false);
                return;
            }

            _horizontalInput = Input.GetAxisRaw("Horizontal");

            if (_anim != null)
                _anim.SetFloat("Speed", Mathf.Abs(_horizontalInput));

            FlipCharacter();

            // Hareket ediyorsa kova kapalı, idle'daysa açık
            bool idle = Mathf.Abs(_horizontalInput) < 0.01f;
            Bucket?.SetOpen(idle);
        }

        private void FlipCharacter()
        {
            if (_horizontalInput > 0.01f)
            {
                Vector3 scaler = transform.localScale;
                scaler.x = Mathf.Abs(scaler.x);
                transform.localScale = scaler;
            }
            else if (_horizontalInput < -0.01f)
            {
                Vector3 scaler = transform.localScale;
                scaler.x = -Mathf.Abs(scaler.x);
                transform.localScale = scaler;
            }
        }

        private void FixedUpdate()
        {
            float vx = _isStunned ? 0f : _horizontalInput * _currentMoveSpeed;
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Yıldırım çarptığında çağrılır. Karakteri stun'lar ve blink efekti başlatır.</summary>
        public void ApplyLightningStun(float duration)
        {
            if (_isStunned) return; // Zaten stun'daysa tekrar uygulama
            StartCoroutine(StunRoutine(duration));
        }

        // ── Private ────────────────────────────────────────────────────────────

        private IEnumerator StunRoutine(float duration)
        {
            _isStunned = true;

            // Blink efekti: süre boyunca sprite'ı yakıp söndür
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_spriteRenderer != null)
                    _spriteRenderer.enabled = !_spriteRenderer.enabled;

                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            // Stun bitti, sprite'ın görünür olduğundan emin ol
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
            _isStunned = false;
        }

        private void HandleUpgrade(UpgradeType type, int newLevel)
        {
            if (type == UpgradeType.PlayerSpeed) RefreshSpeed();
        }

        private void RefreshSpeed()
        {
            float bonus = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetCurrentValue(UpgradeType.PlayerSpeed)
                : 0f;
            _currentMoveSpeed = baseMoveSpeed + bonus;
        }
    }
}

