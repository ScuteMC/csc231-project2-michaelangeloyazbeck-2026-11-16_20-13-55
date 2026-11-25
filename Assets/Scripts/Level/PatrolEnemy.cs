using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Clockwork Sentinel Drone patrolling between left and right horizontal bounds.
    /// Jumping onto its top defeats the drone and bounces Vesper upward;
    /// colliding from the side or below deals damage to Vesper.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class PatrolEnemy : MonoBehaviour
    {
        [Header("Patrol Settings")]
        public float leftOffset = -3.0f;
        public float rightOffset = 3.0f;
        public float patrolSpeed = 2.6f;
        public float hoverAmplitude = 0.16f;
        public float hoverFrequency = 2.8f;

        [Header("Combat")]
        public int contactDamage = 20;
        public float stompBounceVelocity = 12.5f;
        public float stompVerticalTolerance = 0.35f;
        public ParticleSystem defeatParticles;

        private Vector3 originPosition;
        private float direction = 1f;
        private bool defeated;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            originPosition = transform.position;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (defeated)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            float nextX = transform.position.x + direction * patrolSpeed * Time.deltaTime;
            float minX = originPosition.x + leftOffset;
            float maxX = originPosition.x + rightOffset;

            if (nextX >= maxX)
            {
                nextX = maxX;
                direction = -1f;
            }
            else if (nextX <= minX)
            {
                nextX = minX;
                direction = 1f;
            }

            float nextY = originPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            transform.position = new Vector3(nextX, nextY, originPosition.z);

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction < 0f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerContact(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerContact(collision.collider);
        }

        private void HandlePlayerContact(Collider2D other)
        {
            if (defeated || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

            if (player == null || health == null || playerRb == null)
            {
                return;
            }

            bool fallingDown = playerRb.linearVelocity.y <= 0.8f;
            bool aboveDrone = other.transform.position.y >= transform.position.y + stompVerticalTolerance;

            if (fallingDown && aboveDrone)
            {
                health.GrantBriefInvulnerability(0.35f);
                DefeatEnemy(player);
            }
            else
            {
                health.TakeDamage(contactDamage, transform.position);
            }
        }

        private void DefeatEnemy(PlayerController player)
        {
            defeated = true;
            player.Bounce(stompBounceVelocity);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLand();
                AudioManager.Instance.PlayCollect();
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.14f, 0.2f);
            }

            if (defeatParticles != null)
            {
                defeatParticles.transform.SetParent(null, true);
                defeatParticles.transform.position = transform.position;
                defeatParticles.Play();
                Destroy(defeatParticles.gameObject, 2.0f);
            }

            gameObject.SetActive(false);
        }

        public void DefeatByAetherFlash()
        {
            if (defeated)
            {
                return;
            }

            defeated = true;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCollect();
            }
            if (defeatParticles != null)
            {
                defeatParticles.transform.SetParent(null, true);
                defeatParticles.transform.position = transform.position;
                defeatParticles.Play();
                Destroy(defeatParticles.gameObject, 2.0f);
            }

            gameObject.SetActive(false);
        }
    }
}
