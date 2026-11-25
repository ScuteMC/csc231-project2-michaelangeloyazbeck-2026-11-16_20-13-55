using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Crimson-violet projectile fired by Eclipse Plague Casters and Malakor's Phase III Berserk fan.
    /// Deals HP damage to Vesper and can be shot down mid-air with Vesper's Storm-Lightning bolt.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class EnemyProjectile : MonoBehaviour
    {
        public int damageAmount = 20;
        public float lifetime = 2.6f;

        private Vector2 velocity;
        private float elapsed;

        public void Initialize(Vector2 vel, Sprite orbSprite, int damage = 20, float life = 2.6f)
        {
            velocity = vel;
            damageAmount = damage;
            lifetime = life;
            elapsed = 0f;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && orbSprite != null)
            {
                sr.sprite = orbSprite;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, 320f * Time.deltaTime);
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.CompareTag("Player"))
            {
                PlayerHealth health = other.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damageAmount, transform.position);
                }
                Destroy(gameObject);
            }
        }
    }
}
