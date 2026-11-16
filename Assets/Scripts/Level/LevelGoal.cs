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

        private bool completed;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || !other.CompareTag("Player"))
            {
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
