using UnityEngine;

namespace Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;

        private Rigidbody2D _rb;
        private float _horizontalInput;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            
            // X ekseni rotasyonunu kilitleyelim (yanlışlıkla devrilmemesi için)
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            // Sadece yatay girdileri (A/D veya Sağ/Sol ok tuşları) alıyoruz
            _horizontalInput = Input.GetAxisRaw("Horizontal");
        }

        private void FixedUpdate()
        {
            // Yalnızca x eksenine hız veriyor, y ekseni (yerçekimi vb. için) aynı bırakılıyor.
            _rb.linearVelocity = new Vector2(_horizontalInput * moveSpeed, _rb.linearVelocity.y);
        }
    }
}
