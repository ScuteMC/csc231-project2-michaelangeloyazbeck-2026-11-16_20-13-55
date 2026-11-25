using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Golden Vesper Soul-Mask Relic pickup placed on perilous optional routes.
    /// Grants +1 extra Life and restores +50 HP.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SoulLifePickup : MonoBehaviour
    {
        public float bobAmplitude = 0.2f;
        public float bobFrequency = 2.6f;
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

            float offset = Mathf.Sin(Time.time * bobFrequency + startPosition.x * 0.4f) * bobAmplitude;
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
                GameManager.Instance.AddLife(1);
            }

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(50);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCheckpoint();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowBanner("+1 LIFE", 1.8f);
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
