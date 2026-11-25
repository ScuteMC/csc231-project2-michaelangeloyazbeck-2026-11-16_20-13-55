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
        public AudioClip wardShatterClip;

        [Header("Music")]
        public AudioClip bgmClip;
        public AudioClip bossBgmClip;

        [Header("Levels")]
        [Range(0f, 1f)] public float masterVolume = 0.75f;
        [Range(0f, 1f)] public float bgmRelativeVolume = 0.68f;
        public bool isMuted;

        private float musicBoost = 1f;
        private bool bossMusicActive;

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
                bgmSource.volume = Mathf.Clamp01(effective * bgmRelativeVolume * musicBoost);
                bgmSource.mute = isMuted;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = effective;
                sfxSource.mute = isMuted;
            }
        }

        public void PlayBossMusic(int phase = 1)
        {
            if (bgmSource == null || bossBgmClip == null)
            {
                return;
            }

            bossMusicActive = true;
            musicBoost = 1.22f;
            bgmSource.pitch = phase >= 3 ? 1.12f : (phase == 2 ? 1.06f : 1.0f);
            if (bgmSource.clip != bossBgmClip || !bgmSource.isPlaying)
            {
                bgmSource.clip = bossBgmClip;
                bgmSource.loop = true;
                bgmSource.Play();
            }
            ApplyAudioSettings();
        }

        public void PlayLevelMusic()
        {
            bossMusicActive = false;
            musicBoost = 1.0f;
            if (bgmSource == null)
            {
                return;
            }

            bgmSource.pitch = 1.0f;
            if (bgmClip != null && (bgmSource.clip != bgmClip || !bgmSource.isPlaying))
            {
                bgmSource.clip = bgmClip;
                bgmSource.loop = true;
                bgmSource.Play();
            }
            ApplyAudioSettings();
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
        public void PlayWardShatter() => PlaySfx(wardShatterClip != null ? wardShatterClip : zapClip, 1f);
    }
}
