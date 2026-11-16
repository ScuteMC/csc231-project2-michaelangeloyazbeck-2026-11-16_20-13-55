using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Bobbing Memory Shard collectible that increments the player's HUD shard counter,
    /// triggers an emerald/cyan particle burst, and plays the collect sound effect.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollectibleShard : MonoBehaviour
    {
        [Header("Idle Motion")]
        public float bobAmplitude = 0.18f;
        public float bobFrequency = 3.0f;

        [Header("Effects")]
        public ParticleSystem pickupParticles;

        private Vector3 startPosition;
        private bool collected;

        private void Awake()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            float offset = Mathf.Sin(Time.time * bobFrequency + startPosition.x * 0.45f) * bobAmplitude;
            transform.position = startPosition + new Vector3(0f, offset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag("Player"))
            {
                return;
            }

            collected = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectShard();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCollect();
            }

            if (pickupParticles != null)
            {
                pickupParticles.transform.SetParent(null, true);
                pickupParticles.transform.position = transform.position;
                pickupParticles.Play();
                Destroy(pickupParticles.gameObject, 2.0f);
            }

            gameObject.SetActive(false);
        }
    }
}
