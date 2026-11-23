using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Low crimson seismic wave spawned by Malakor's Eclipse Halberd slam.
    /// Travels horizontally along the Sanctum Terrace until leaving the arena or hitting Vesper.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BossShockwave : MonoBehaviour
    {
        [Header("Shockwave Motion")]
        public float moveSpeed = 9.5f;
        public float lifetime = 2.4f;
        public int damageAmount = 1;
        public float minArenaX = 210.5f;
        public float maxArenaX = 238.5f;

        private float moveDirection = -1f;
        private float elapsed;

        public void Initialize(float direction, Sprite waveSprite, float arenaMinX, float arenaMaxX)
        {
            moveDirection = direction >= 0f ? 1f : -1f;
            minArenaX = arenaMinX;
            maxArenaX = arenaMaxX;
            elapsed = 0f;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && waveSprite != null)
            {
                sr.sprite = waveSprite;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(scale.x) * moveDirection, scale.y, scale.z);
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            transform.position += Vector3.right * (moveDirection * moveSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;

            if (elapsed >= lifetime || transform.position.x < minArenaX || transform.position.x > maxArenaX)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damageAmount, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
