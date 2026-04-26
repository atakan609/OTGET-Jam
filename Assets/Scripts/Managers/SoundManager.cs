using UnityEngine;
using Core;

namespace Managers
{
    /// <summary>
    /// Üç ayrı ses kanalı: Master, Background (ambient), SFX (yıldırım vb.)
    /// Slider değerleri PlayerPrefs'e kaydedilir, oyun açılışında geri yüklenir.
    /// </summary>
    public class SoundManager : Singleton<SoundManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource backgroundSource; // Ambient / müzik — loop
        [SerializeField] private AudioSource sfxSource;        // One-shot efektler
        [SerializeField] private AudioSource depotSource;      // Deposit loop sesi

        [Header("Startup Clips")]
        [SerializeField] private AudioClip backgroundClip;     // Oyun başlar başlamaz çalar
        [SerializeField] private AudioClip lightningClip;      // Yıldırım SFX

        [Header("Gameplay Clips")]
        [SerializeField] private AudioClip raindropCollectClip; // Su topladığında one-shot
        [SerializeField] private AudioClip depotDepositClip;    // Depoya boşaltırken loop

        // ── Varsayılan ses seviyeleri ────────────────────────────────────────────
        private const float DefaultMaster     = 1f;
        private const float DefaultBackground = 0.5f;
        private const float DefaultSfx        = 1f;

        // ── PlayerPrefs anahtarları ──────────────────────────────────────────────
        private const string KeyMaster     = "vol_master";
        private const string KeyBackground = "vol_background";
        private const string KeySfx        = "vol_sfx";

        // ── Güncel ses seviyeleri ────────────────────────────────────────────────
        public float MasterVolume     { get; private set; }
        public float BackgroundVolume { get; private set; }
        public float SfxVolume        { get; private set; }

        // ── Unity ────────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            // Sahne geçişlerinde yok olmasın; Singleton zaten çift instance'ı engeller
            DontDestroyOnLoad(gameObject);
            LoadVolumes();
        }

        private void Start()
        {
            ApplyAllVolumes();
            PlayBackground();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Yıldırım ya da herhangi bir SFX çal. Pitch değeri isteğe bağlı olarak değiştirilebilir.</summary>
        public void PlaySfx(AudioClip clip, float pitch = 1f)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, SfxVolume * MasterVolume);
        }

        /// <summary>Damla toplandığında one-shot SFX çalınır.</summary>
        public void PlayRaindropCollect()
        {
            if (raindropCollectClip != null)
                PlaySfx(raindropCollectClip, Random.Range(0.9f, 1.1f));
        }

        /// <summary>Depoya boşaltma başlayınca loop ses açılır.</summary>
        public void StartDepositLoop()
        {
            if (depotSource == null || depotDepositClip == null) return;
            if (depotSource.isPlaying) return;
            depotSource.clip   = depotDepositClip;
            depotSource.loop   = true;
            depotSource.volume = SfxVolume * MasterVolume;
            depotSource.Play();
        }

        /// <summary>Depoya boşaltma bitişinde loop durdurulur.</summary>
        public void StopDepositLoop()
        {
            if (depotSource != null) depotSource.Stop();
        }

        /// <summary>Yıldırım sesini çal (Inspector'dan atanmış clip) farklı tonlarda (random pitch) çalar.</summary>
        public void PlayLightningSfx()
        {
            if (lightningClip != null)
            {
                // Yıldırım sesinde belirgin bir farklılık yaratmak için aralığı genişlettik
                float randomPitch = Random.Range(0.6f, 1.4f);
                PlaySfx(lightningClip, randomPitch);
            }
        }

        /// <summary>Master ses seviyesini ayarla. Settings UI tarafından çağrılır.</summary>
        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMaster, MasterVolume);
            ApplyAllVolumes();
        }

        /// <summary>Arka plan ses seviyesini ayarla.</summary>
        public void SetBackgroundVolume(float value)
        {
            BackgroundVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyBackground, BackgroundVolume);
            ApplyBackgroundVolume();
        }

        /// <summary>SFX ses seviyesini ayarla.</summary>
        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeySfx, SfxVolume);
            // One-shot'lar anında etkilenmez; bir sonraki PlaySfx çağrısında geçerli olur
        }

        // Eski API uyumu (tek kaynak varken kullanılan)
        public void PlaySound(AudioClip clip) => PlaySfx(clip);
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (backgroundSource == null || clip == null) return;
            backgroundSource.clip = clip;
            backgroundSource.loop = loop;
            backgroundSource.volume = BackgroundVolume * MasterVolume;
            backgroundSource.Play();
        }
        public void StopMusic()
        {
            if (backgroundSource != null) backgroundSource.Stop();
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void LoadVolumes()
        {
            MasterVolume     = PlayerPrefs.GetFloat(KeyMaster,     DefaultMaster);
            BackgroundVolume = PlayerPrefs.GetFloat(KeyBackground, DefaultBackground);
            SfxVolume        = PlayerPrefs.GetFloat(KeySfx,        DefaultSfx);
        }

        private void ApplyAllVolumes()
        {
            ApplyBackgroundVolume();
        }

        private void ApplyBackgroundVolume()
        {
            if (backgroundSource != null)
                backgroundSource.volume = BackgroundVolume * MasterVolume;
        }

        private void PlayBackground()
        {
            if (backgroundSource == null || backgroundClip == null) return;
            backgroundSource.clip   = backgroundClip;
            backgroundSource.loop   = true;
            backgroundSource.volume = BackgroundVolume * MasterVolume;
            if (!backgroundSource.isPlaying)
                backgroundSource.Play();
        }
    }
}
