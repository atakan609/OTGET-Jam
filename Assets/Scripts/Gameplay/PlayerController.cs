using UnityEngine;
using Managers;

namespace Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 8f;

        [Header("References")]
        public BucketController Bucket;

        private Rigidbody2D _rb;
        private Animator _anim;
        private float _horizontalInput;
        private float _currentMoveSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();
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
            _horizontalInput = Input.GetAxisRaw("Horizontal");

            // Animator güncelleme
            if (_anim != null)
            {
                _anim.SetFloat("Speed", Mathf.Abs(_horizontalInput));
            }

            // Karakteri yönüne göre çevir
            FlipCharacter();
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
            _rb.linearVelocity = new Vector2(_horizontalInput * _currentMoveSpeed, _rb.linearVelocity.y);
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
