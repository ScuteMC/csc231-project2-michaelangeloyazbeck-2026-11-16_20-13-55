using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Watchtower Lantern checkpoint that saves Vesper's respawn position,
    /// ignites warm lantern glow and celebratory particles, and notifies the HUD.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Identity")]
        public int checkpointIndex = 1;
        public string checkpointName = "Watchtower Lantern";
        public Vector3 respawnOffset = new Vector3(0f, 0.5f, 0f);

        [Header("Visuals & Particles")]
        public SpriteRenderer lanternRenderer;
        public Color unlitTint = new Color(0.75f, 0.78f, 0.9f, 1f);
        public Color litTint = new Color(1f, 0.95f, 0.65f, 1f);
        public ParticleSystem activationParticles;
        public ParticleSystem idleFlameParticles;

        private bool isActivated;

        private void Awake()
        {
            if (lanternRenderer == null)
            {
                lanternRenderer = GetComponent<SpriteRenderer>();
            }

            if (lanternRenderer != null)
            {
                lanternRenderer.color = unlitTint;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated || !other.CompareTag("Player"))
            {
                return;
            }

            isActivated = true;
            Vector3 respawnWorldPos = transform.position + respawnOffset;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ActivateCheckpoint(checkpointIndex, respawnWorldPos, checkpointName);
            }

            if (lanternRenderer != null)
            {
                lanternRenderer.color = litTint;
            }

            if (activationParticles != null)
            {
                activationParticles.Play();
            }

            if (idleFlameParticles != null)
            {
                idleFlameParticles.Play();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCheckpoint();
            }
        }
    }
}
