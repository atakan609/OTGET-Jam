using UnityEngine;
using Core;

namespace Managers
{
    public class SoundManager : Singleton<SoundManager>
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        public void PlaySound(AudioClip clip)
        {
            if (sfxSource && clip)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource && clip)
            {
                musicSource.clip = clip;
                musicSource.loop = loop;
                musicSource.Play();
            }
        }
        
        public void StopMusic()
        {
            if (musicSource)
            {
                musicSource.Stop();
            }
        }
    }
}
