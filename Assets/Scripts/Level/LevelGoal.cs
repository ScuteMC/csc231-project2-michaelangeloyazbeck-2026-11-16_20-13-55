using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Sanctum Beacon ending Level 1. Triggers celebratory Particle System bursts,
    /// plays the win chime, and opens the Level Complete screen.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        [Header("Celebration Particles")]
        public ParticleSystem idleBeaconParticles;
        public ParticleSystem victoryBurstParticles;

        [Header("Final Boss Seal")]
        public bool isLockedByBoss;
        public GameObject eclipseSealVisual;
        public ParticleSystem sealBreakParticles;

        private bool completed;
        private float lockNoticeCooldown;

        private void Update()
        {
            if (lockNoticeCooldown > 0f)
            {
                lockNoticeCooldown -= Time.deltaTime;
            }
        }

        public void UnlockBeacon()
        {
            isLockedByBoss = false;

            if (eclipseSealVisual != null)
            {
                eclipseSealVisual.SetActive(false);
            }

            if (sealBreakParticles != null)
            {
                sealBreakParticles.Play();
            }

            if (idleBeaconParticles != null && !idleBeaconParticles.isPlaying)
            {
                idleBeaconParticles.Play();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCheckpoint();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || !other.CompareTag("Player"))
            {
                return;
            }

            if (isLockedByBoss)
            {
                if (lockNoticeCooldown <= 0f)
                {
                    lockNoticeCooldown = 2.0f;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayHurt();
                    }
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowBanner("SEALED BY MALAKOR", 2.0f);
                    }
                }
                return;
            }

            completed = true;

            if (victoryBurstParticles != null)
            {
                victoryBurstParticles.Play();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWin();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }
    }
}
