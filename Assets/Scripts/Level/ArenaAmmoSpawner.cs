using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Recharging Storm-Lightning Wisp spawner inside the Sanctum Boss Arena.
    /// Grants +2 Lightning Ammo (and +10 HP) when touched and recharges every 9 seconds
    /// so Vesper can never run out of ammo against Malakor's Eclipse Ward.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class ArenaAmmoSpawner : MonoBehaviour
    {
        public float respawnInterval = 9.0f;
        public int ammoGranted = 2;
        public int healAmount = 10;
        public float bobAmplitude = 0.2f;
        public ParticleSystem idleParticles;
        public ParticleSystem collectBurstParticles;

        private Vector3 basePos;
        private SpriteRenderer sr;
        private Collider2D triggerCol;
        private float respawnTimer;
        private bool available = true;

        private void Awake()
        {
            basePos = transform.position;
            sr = GetComponent<SpriteRenderer>();
            triggerCol = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (!available)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    SetAvailable(true);
                }
                return;
            }

            float offset = Mathf.Sin(Time.time * 3.1f + basePos.x * 0.5f) * bobAmplitude;
            transform.position = basePos + new Vector3(0f, offset, 0f);
        }

        private void SetAvailable(bool state)
        {
            available = state;
            if (sr != null) sr.enabled = state;
            if (triggerCol != null) triggerCol.enabled = state;
            if (idleParticles != null)
            {
                if (state) idleParticles.Play();
                else idleParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!available || !other.CompareTag("Player"))
            {
                return;
            }

            SetAvailable(false);
            respawnTimer = respawnInterval;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GrantArenaLightning(ammoGranted, healAmount);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCollect();
            }

            if (collectBurstParticles != null)
            {
                collectBurstParticles.transform.position = transform.position;
                collectBurstParticles.Play();
            }
        }
    }
}
