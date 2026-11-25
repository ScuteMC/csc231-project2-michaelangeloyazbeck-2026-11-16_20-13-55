using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Eclipse Plague Caster cultist guarding rooftops and firing crimson-violet Eclipse Orbs.
    /// Can be stomped from above or destroyed from range with Vesper's Storm-Lightning bolt.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class AetherShooterEnemy : MonoBehaviour
    {
        [Header("Combat & Casting")]
        public float detectRangeX = 10.5f;
        public float detectRangeY = 4.5f;
        public float fireInterval = 2.35f;
        public float projectileSpeed = 8.8f;
        public int contactDamage = 20;
        public float stompBounceVelocity = 12.8f;
        public Sprite projectileSprite;
        public ParticleSystem defeatParticles;

        private float fireTimer = 0.9f;
        private float facingDir = -1f;
        private bool defeated;
        private SpriteRenderer sr;
        private Transform playerTransform;
        private Color baseColor = Color.white;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                baseColor = sr.color;
            }
        }

        private void Update()
        {
            if (defeated || (GameManager.Instance != null && !GameManager.Instance.IsPlaying))
            {
                return;
            }

            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform == null)
            {
                return;
            }

            float dx = playerTransform.position.x - transform.position.x;
            float dy = Mathf.Abs(playerTransform.position.y - transform.position.y);
            if (Mathf.Abs(dx) > detectRangeX || dy > detectRangeY)
            {
                return;
            }

            facingDir = dx >= 0f ? 1f : -1f;
            if (sr != null)
            {
                sr.flipX = facingDir < 0f;
                sr.color = fireTimer <= 0.35f
                    ? new Color(1f, 0.42f, 0.92f, 1f)
                    : baseColor;
            }

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                fireTimer = fireInterval;
                if (sr != null) sr.color = baseColor;
                FireOrb();
            }
        }

        private void FireOrb()
        {
            GameObject orbGo = new GameObject("EclipseOrb");
            orbGo.transform.position = transform.position + new Vector3(facingDir * 0.65f, 1.05f, 0f);

            SpriteRenderer orbSr = orbGo.AddComponent<SpriteRenderer>();
            orbSr.sortingOrder = 8;

            CircleCollider2D col = orbGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.28f;

            Vector2 dir = new Vector2(facingDir, 0f);
            if (playerTransform != null)
            {
                Vector2 aim = ((Vector2)playerTransform.position + new Vector2(0f, 0.75f) - (Vector2)orbGo.transform.position);
                if (aim.sqrMagnitude > 0.01f)
                {
                    aim.Normalize();
                    // Clamp slight vertical angle so shots stay crisp & readable
                    aim.y = Mathf.Clamp(aim.y, -0.32f, 0.32f);
                    dir = aim.normalized;
                }
            }

            EnemyProjectile proj = orbGo.AddComponent<EnemyProjectile>();
            proj.Initialize(dir * projectileSpeed, projectileSprite, 20, 2.6f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (defeated || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            Rigidbody2D playerRb = other.attachedRigidbody;
            if (player == null || health == null || playerRb == null)
            {
                return;
            }

            bool fallingDown = playerRb.linearVelocity.y <= 0.8f;
            bool above = other.bounds.min.y >= transform.position.y + 0.75f;
            if (fallingDown && above)
            {
                health.GrantBriefInvulnerability(0.35f);
                player.Bounce(stompBounceVelocity);
                DefeatByLightning();
            }
            else
            {
                health.TakeDamage(contactDamage, transform.position);
            }
        }

        public void DefeatByLightning()
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
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.14f, 0.2f);
            }
            if (GameFeelManager.Instance != null)
            {
                GameFeelManager.Instance.TriggerHitStop(0.035f);
                GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 0.9f, 0f), "KO!", new Color(0.55f, 0.98f, 1f, 1f), 0.95f);
            }
            if (defeatParticles != null)
            {
                defeatParticles.transform.SetParent(null, true);
                defeatParticles.transform.position = transform.position + new Vector3(0f, 0.8f, 0f);
                defeatParticles.Play();
                Destroy(defeatParticles.gameObject, 2.0f);
            }
            gameObject.SetActive(false);
        }
    }
}
