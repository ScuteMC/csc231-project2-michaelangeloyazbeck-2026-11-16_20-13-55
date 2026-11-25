using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Collectible Storm-Lightning spear fired by Vesper ([E], [F], or Mouse Click).
    /// Sweeps via Physics2D.OverlapBoxAll each frame so hits never miss Kinematic or Trigger colliders.
    /// Destroys rooftop enemies, vaporizes enemy projectiles/shockwaves, and shatters Malakor's Eclipse Ward pips.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class PlayerLightningBolt : MonoBehaviour
    {
        public float speed = 24f;
        public float lifetime = 1.35f;

        private Vector2 velocity = new Vector2(24f, 0f);
        private float elapsed;
        private bool consumed;
        private readonly Collider2D[] hitBuffer = new Collider2D[16];
        private readonly Collider2D[] wallBuffer = new Collider2D[8];

        public void Initialize(float dir, Sprite boltSprite, Vector2? customAimDir = null)
        {
            float sign = dir >= 0f ? 1f : -1f;
            Vector2 moveDir = customAimDir.HasValue && customAimDir.Value.sqrMagnitude > 0.001f
                ? customAimDir.Value.normalized
                : new Vector2(sign, 0f);

            velocity = moveDir * speed;
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
            transform.localScale = new Vector3(Mathf.Abs(s.x) * (moveDir.x >= 0f ? 1f : -1f), s.y, s.z);
            float angleDeg = Mathf.Atan2(moveDir.y, Mathf.Abs(moveDir.x)) * Mathf.Rad2Deg * (moveDir.x >= 0f ? 1f : -1f);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
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

            Vector2 prevPos = transform.position;
            Vector2 nextPos = prevPos + velocity * Time.deltaTime;
            transform.position = nextPos;

            PerformSweptHitCheck(prevPos, nextPos);
            if (consumed)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void PerformSweptHitCheck(Vector2 prevPos, Vector2 nextPos)
        {
            Vector2 mid = (prevPos + nextPos) * 0.5f;
            float step = Vector2.Distance(prevPos, nextPos);
            Vector2 combatBoxSize = new Vector2(Mathf.Max(1.35f, step + 1.0f), 0.96f);

            ContactFilter2D combatFilter = new ContactFilter2D();
            combatFilter.useTriggers = true;
            int count = Physics2D.OverlapBox(mid, combatBoxSize, 0f, combatFilter, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                if (TryConsumeTarget(hitBuffer[i]))
                {
                    return;
                }
            }

            // Inner core solid-terrain check so floor-skimming shots don't detonate prematurely on rooftop tiles
            ContactFilter2D wallFilter = new ContactFilter2D();
            wallFilter.useTriggers = false;
            int wallCount = Physics2D.OverlapBox(nextPos, new Vector2(0.4f, 0.28f), 0f, wallFilter, wallBuffer);
            for (int i = 0; i < wallCount; i++)
            {
                Collider2D col = wallBuffer[i];
                if (col == null || col.CompareTag("Player") || col.gameObject == gameObject)
                {
                    continue;
                }

                if (!col.isTrigger &&
                    col.GetComponentInParent<MovingPlatform>() == null &&
                    col.GetComponentInParent<CrumblingPlatform>() == null)
                {
                    consumed = true;
                    SpawnImpactFlash(nextPos, new Color(0.55f, 0.96f, 1f, 0.9f));
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!consumed)
            {
                TryConsumeTarget(other);
            }
        }

        private bool TryConsumeTarget(Collider2D other)
        {
            if (consumed || other == null || other.CompareTag("Player") || other.gameObject == gameObject)
            {
                return false;
            }

            FinalBossAI boss = other.GetComponentInParent<FinalBossAI>();
            if (boss != null)
            {
                consumed = true;
                SpawnImpactFlash(transform.position, new Color(0.45f, 0.98f, 1f, 1f));
                boss.TakeLightningHit(transform.position);
                Destroy(gameObject);
                return true;
            }

            PatrolEnemy patrol = other.GetComponentInParent<PatrolEnemy>();
            if (patrol != null)
            {
                consumed = true;
                SpawnImpactFlash(transform.position, new Color(0.55f, 0.98f, 1f, 1f));
                if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
                patrol.DefeatByAetherFlash();
                Destroy(gameObject);
                return true;
            }

            SwoopEnemy swoop = other.GetComponentInParent<SwoopEnemy>();
            if (swoop != null)
            {
                consumed = true;
                SpawnImpactFlash(transform.position, new Color(0.55f, 0.98f, 1f, 1f));
                if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
                swoop.DefeatByLightning();
                Destroy(gameObject);
                return true;
            }

            AetherShooterEnemy caster = other.GetComponentInParent<AetherShooterEnemy>();
            if (caster != null)
            {
                consumed = true;
                SpawnImpactFlash(transform.position, new Color(0.55f, 0.98f, 1f, 1f));
                if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
                caster.DefeatByLightning();
                Destroy(gameObject);
                return true;
            }

            EnemyProjectile proj = other.GetComponentInParent<EnemyProjectile>();
            BossShockwave wave = other.GetComponentInParent<BossShockwave>();
            if (proj != null || wave != null)
            {
                consumed = true;
                SpawnImpactFlash(transform.position, new Color(0.55f, 0.98f, 1f, 1f));
                if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
                Destroy(proj != null ? proj.gameObject : wave.gameObject);
                Destroy(gameObject);
                return true;
            }

            return false;
        }

        private static void SpawnImpactFlash(Vector3 pos, Color color)
        {
            GameObject burst = new GameObject("LightningImpactSpark");
            burst.transform.position = pos;
            ParticleSystem ps = burst.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;

            var renderer = burst.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 12;
            ps.Play();
            Destroy(burst, 0.6f);
        }
    }
}
