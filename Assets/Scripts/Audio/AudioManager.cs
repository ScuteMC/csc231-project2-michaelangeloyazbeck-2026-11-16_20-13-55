using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Centralized audio controller managing looping twilight background music,
    /// platformer sound effects, volume scaling, and persistent mute state.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string VolumePrefKey = "Vesper_MasterVolume";
        private const string MutePrefKey = "Vesper_IsMuted";

        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        [Header("Sound Effects")]
        public AudioClip jumpClip;
        public AudioClip landClip;
        public AudioClip collectClip;
        public AudioClip hurtClip;
        public AudioClip checkpointClip;
        public AudioClip glideClip;
        public AudioClip winClip;
        public AudioClip zapClip;
        public AudioClip bossRoarClip;

        [Header("Music")]
        public AudioClip bgmClip;

        [Header("Levels")]
        [Range(0f, 1f)] public float masterVolume = 0.75f;
        [Range(0f, 1f)] public float bgmRelativeVolume = 0.5f;
        public bool isMuted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            masterVolume = PlayerPrefs.GetFloat(VolumePrefKey, masterVolume);
            isMuted = PlayerPrefs.GetInt(MutePrefKey, 0) == 1;

            EnsureAudioSources();
            ApplyAudioSettings();
        }

        private void Start()
        {
            if (bgmSource != null && bgmClip != null && !bgmSource.isPlaying)
            {
                bgmSource.clip = bgmClip;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(VolumePrefKey, masterVolume);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;
            PlayerPrefs.SetInt(MutePrefKey, isMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        public void ToggleMute()
        {
            SetMuted(!isMuted);
        }

        private void ApplyAudioSettings()
        {
            float effective = isMuted ? 0f : masterVolume;
            if (bgmSource != null)
            {
                bgmSource.volume = effective * bgmRelativeVolume;
                bgmSource.mute = isMuted;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = effective;
                sfxSource.mute = isMuted;
            }
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (isMuted || clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volumeScale));
        }

        public void PlayJump() => PlaySfx(jumpClip, 0.9f);
        public void PlayLand() => PlaySfx(landClip, 0.7f);
        public void PlayCollect() => PlaySfx(collectClip, 0.95f);
        public void PlayHurt() => PlaySfx(hurtClip, 1f);
        public void PlayCheckpoint() => PlaySfx(checkpointClip, 0.95f);
        public void PlayGlide() => PlaySfx(glideClip, 0.65f);
        public void PlayWin() => PlaySfx(winClip, 1f);
        public void PlayZap() => PlaySfx(zapClip, 0.88f);
        public void PlayBossRoar() => PlaySfx(bossRoarClip, 1f);
    }
}
