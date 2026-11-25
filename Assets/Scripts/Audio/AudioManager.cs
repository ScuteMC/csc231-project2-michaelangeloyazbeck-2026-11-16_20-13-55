using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Centralized adaptive audio controller with a 6-voice SFX pool, dynamic pitch variation,
    /// pentatonic collectible combo ladder, BGM ducking on heavy impacts, and persistent mute state.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string VolumePrefKey = "Vesper_MasterVolume";
        private const string MutePrefKey = "Vesper_IsMuted";
        private const int SfxPoolSize = 6;

        private static readonly float[] PentatonicPitches =
        {
            1.00f, 1.06f, 1.122f, 1.189f, 1.26f, 1.335f, 1.414f, 1.498f
        };

        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        [Header("Sound Effects")]
        public AudioClip jumpClip;
        public AudioClip landClip;
        public AudioClip hardLandClip;
        public AudioClip collectClip;
        public AudioClip hurtClip;
        public AudioClip checkpointClip;
        public AudioClip glideClip;
        public AudioClip winClip;
        public AudioClip zapClip;
        public AudioClip bossRoarClip;
        public AudioClip wardShatterClip;
        public AudioClip footstepClip;
        public AudioClip bossTelegraphClip;
        public AudioClip uiHoverClip;
        public AudioClip uiClickClip;

        [Header("Music")]
        public AudioClip bgmClip;
        public AudioClip bossBgmClip;

        [Header("Levels")]
        [Range(0f, 1f)] public float masterVolume = 0.75f;
        [Range(0f, 1f)] public float bgmRelativeVolume = 0.68f;
        public bool isMuted;

        private AudioSource[] sfxPool;
        private int nextPoolIndex;
        private float musicBoost = 1f;
        private float bgmDuckMultiplier = 1f;
        private float bgmDuckTarget = 1f;
        private float bgmDuckHoldTimer;
        private float bgmDuckRecoverSpeed = 1.6f;
        private float lastCollectTime = -10f;
        private int collectStreak;

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

        private void Update()
        {
            if (bgmDuckHoldTimer > 0f)
            {
                bgmDuckHoldTimer -= Time.unscaledDeltaTime;
                bgmDuckMultiplier = Mathf.MoveTowards(bgmDuckMultiplier, bgmDuckTarget, Time.unscaledDeltaTime * 10f);
                ApplyAudioSettings();
            }
            else if (bgmDuckMultiplier < 0.999f)
            {
                bgmDuckMultiplier = Mathf.MoveTowards(bgmDuckMultiplier, 1f, Time.unscaledDeltaTime * bgmDuckRecoverSpeed);
                ApplyAudioSettings();
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

            if (sfxPool == null || sfxPool.Length != SfxPoolSize)
            {
                sfxPool = new AudioSource[SfxPoolSize];
                sfxPool[0] = sfxSource;
                for (int i = 1; i < SfxPoolSize; i++)
                {
                    AudioSource voice = gameObject.AddComponent<AudioSource>();
                    voice.playOnAwake = false;
                    voice.loop = false;
                    sfxPool[i] = voice;
                }
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
                bgmSource.volume = Mathf.Clamp01(effective * bgmRelativeVolume * musicBoost * bgmDuckMultiplier);
                bgmSource.mute = isMuted;
            }

            if (sfxPool != null)
            {
                for (int i = 0; i < sfxPool.Length; i++)
                {
                    if (sfxPool[i] == null) continue;
                    sfxPool[i].volume = effective;
                    sfxPool[i].mute = isMuted;
                }
            }
            else if (sfxSource != null)
            {
                sfxSource.volume = effective;
                sfxSource.mute = isMuted;
            }
        }

        public void DuckBgm(float duckFactor = 0.35f, float holdSec = 0.35f, float recoverSec = 0.40f)
        {
            bgmDuckTarget = Mathf.Clamp(duckFactor, 0.1f, 1f);
            bgmDuckHoldTimer = Mathf.Max(bgmDuckHoldTimer, holdSec);
            bgmDuckRecoverSpeed = (1f - bgmDuckTarget) / Mathf.Max(0.08f, recoverSec);
            bgmDuckMultiplier = Mathf.Min(bgmDuckMultiplier, bgmDuckTarget);
            ApplyAudioSettings();
        }

        public void PlayBossMusic(int phase = 1)
        {
            if (bgmSource == null || bossBgmClip == null)
            {
                return;
            }

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
            PlaySfxVaried(clip, volumeScale, 1.0f, 1.0f);
        }

        public void PlaySfxVaried(AudioClip clip, float volumeScale = 1f, float pitchMin = 0.94f, float pitchMax = 1.06f)
        {
            if (isMuted || clip == null)
            {
                return;
            }

            EnsureAudioSources();
            AudioSource voice = sfxPool[nextPoolIndex];
            nextPoolIndex = (nextPoolIndex + 1) % sfxPool.Length;
            if (voice == null)
            {
                return;
            }

            voice.pitch = pitchMin >= pitchMax ? pitchMin : Random.Range(pitchMin, pitchMax);
            voice.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volumeScale));
        }

        public void PlayJump() => PlaySfxVaried(jumpClip, 0.88f, 0.95f, 1.08f);
        public void PlayLand() => PlaySfxVaried(landClip, 0.68f, 0.90f, 1.08f);
        public void PlayHardLand() => PlaySfxVaried(hardLandClip != null ? hardLandClip : landClip, 0.95f, 0.92f, 1.02f);

        public void PlayCollect()
        {
            if (Time.unscaledTime - lastCollectTime < 1.65f)
            {
                collectStreak = Mathf.Min(collectStreak + 1, PentatonicPitches.Length - 1);
            }
            else
            {
                collectStreak = 0;
            }
            lastCollectTime = Time.unscaledTime;
            float p = PentatonicPitches[collectStreak];
            PlaySfxVaried(collectClip, 0.94f, p, p);
        }

        public void PlayHurt() => PlaySfxVaried(hurtClip, 0.98f, 0.93f, 1.04f);
        public void PlayCheckpoint() => PlaySfxVaried(checkpointClip, 0.95f, 0.98f, 1.03f);
        public void PlayGlide() => PlaySfxVaried(glideClip, 0.65f, 0.95f, 1.06f);
        public void PlayZap() => PlaySfxVaried(zapClip, 0.88f, 0.94f, 1.10f);
        public void PlayFootstep() => PlaySfxVaried(footstepClip, 0.42f, 0.88f, 1.14f);
        public void PlayBossTelegraph() => PlaySfxVaried(bossTelegraphClip, 0.92f, 0.96f, 1.04f);
        public void PlayUiHover() => PlaySfxVaried(uiHoverClip, 0.55f, 0.98f, 1.04f);
        public void PlayUiClick() => PlaySfxVaried(uiClickClip, 0.78f, 0.98f, 1.03f);

        public void PlayBossRoar()
        {
            DuckBgm(0.38f, 0.32f, 0.40f);
            PlaySfxVaried(bossRoarClip, 1f, 0.95f, 1.03f);
        }

        public void PlayWardShatter()
        {
            DuckBgm(0.38f, 0.32f, 0.40f);
            PlaySfxVaried(wardShatterClip != null ? wardShatterClip : zapClip, 1f, 0.96f, 1.05f);
        }

        public void PlayWin()
        {
            DuckBgm(0.25f, 0.55f, 0.50f);
            PlaySfx(winClip, 1f);
        }
    }
}
