using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;

    // Gerekli Component Referansları
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    // Input Değişkenleri
    private float horizontalInput;

    private void Awake()
    {
        // Component'leri otomatik referans al
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Karakterin sağa sola giderken devrilmesini / yuvarlanıp kafayı yemesini önler
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }

    private void Update()
    {
        // 1. Klavyeden Girdi Al (A/D veya Sağ/Sol Ok)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 2. Animator'a Hız Bilgisini Gönder
        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        }

        // 3. Karakteri Mirror (Aynalama) Yapma
        FlipCharacter();
    }

    private void FixedUpdate()
    {
        // 4. Fiziksel Hareket (Rigidbody ile)
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void FlipCharacter()
    {
        // Karakterin Transform'unu (LocalScale) kullanarak çeviriyoruz. 
        // Böylece SpriteRenderer'ın flipX'i yerine karakterin tamamı (eğer elinde silah veya child colliderları varsa) döner.
        if (horizontalInput > 0f)
        {
            Vector3 scaler = transform.localScale;
            scaler.x = Mathf.Abs(scaler.x); // x eksenini pozitif yap
            transform.localScale = scaler;
        }
        else if (horizontalInput < 0f)
        {
            Vector3 scaler = transform.localScale;
            scaler.x = -Mathf.Abs(scaler.x); // x eksenini negatif yap
            transform.localScale = scaler;
        }
    }
}