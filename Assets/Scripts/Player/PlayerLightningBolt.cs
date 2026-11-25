using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Collectible Storm-Lightning spear fired by Vesper ([E], [F], or Mouse Click).
    /// Destroys rooftop enemies, vaporizes enemy projectiles/shockwaves, and shatters Malakor's Eclipse Ward pips.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class PlayerLightningBolt : MonoBehaviour
    {
        public float speed = 22f;
        public float lifetime = 1.35f;

        private float direction = 1f;
        private float elapsed;
        private bool consumed;

        public void Initialize(float dir, Sprite boltSprite)
        {
            direction = dir >= 0f ? 1f : -1f;
            elapsed = 0f;
            consumed = false;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && boltSprite != null)
            {
                sr.sprite = boltSprite;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * direction, s.y, s.z);
        }

        private void Update()
        {
            if (consumed)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            transform.position += Vector3.right * (direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null || other.CompareTag("Player"))
            {
                return;
            }

            FinalBossAI boss = other.GetComponent<FinalBossAI>();
            if (boss != null)
            {
                consumed = true;
                boss.TakeLightningHit(transform.position);
                Destroy(gameObject);
                return;
            }

            PatrolEnemy patrol = other.GetComponent<PatrolEnemy>();
            if (patrol != null)
            {
                consumed = true;
                patrol.DefeatByAetherFlash();
                Destroy(gameObject);
                return;
            }

            SwoopEnemy swoop = other.GetComponent<SwoopEnemy>();
            if (swoop != null)
            {
                consumed = true;
                swoop.DefeatByLightning();
                Destroy(gameObject);
                return;
            }

            AetherShooterEnemy caster = other.GetComponent<AetherShooterEnemy>();
            if (caster != null)
            {
                consumed = true;
                caster.DefeatByLightning();
                Destroy(gameObject);
                return;
            }

            if (other.GetComponent<EnemyProjectile>() != null || other.GetComponent<BossShockwave>() != null)
            {
                consumed = true;
                Destroy(other.gameObject);
                Destroy(gameObject);
                return;
            }

            // Solid terrain/walls stop the bolt cleanly
            if (!other.isTrigger &&
                other.GetComponent<MovingPlatform>() == null &&
                other.GetComponent<CrumblingPlatform>() == null)
            {
                consumed = true;
                Destroy(gameObject);
            }
        }
    }
}
