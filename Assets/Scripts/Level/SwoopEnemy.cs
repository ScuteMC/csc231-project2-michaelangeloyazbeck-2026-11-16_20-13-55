using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Flying Abyssal Gargoyle Swooper patrolling rooftops and diving aggressively at Vesper when nearby.
    /// Can be stomped from above or destroyed with a Storm-Lightning bolt.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class SwoopEnemy : MonoBehaviour
    {
        [Header("Patrol & Swoop")]
        public float patrolRange = 3.6f;
        public float patrolSpeed = 2.9f;
        public float hoverAmplitude = 0.25f;
        public float hoverFrequency = 3.2f;
        public float detectRangeX = 7.5f;
        public float detectRangeY = 4.5f;
        public float diveDepth = 2.6f;
        public float diveDuration = 0.92f;
        public float telegraphDuration = 0.30f;
        public float diveCooldown = 1.7f;

        [Header("Combat")]
        public int contactDamage = 20;
        public float stompBounceVelocity = 13.2f;
        public float stompVerticalTolerance = 0.25f;
        public ParticleSystem defeatParticles;

        private enum SwoopState { Patrol, Telegraph, Dive, Cooldown }

        private Vector3 anchorPos;
        private float patrolDir = 1f;
        private float stateTimer;
        private float cooldownTimer;
        private Vector3 diveStartPos;
        private float diveDirX = 1f;
        private SwoopState state = SwoopState.Patrol;
        private bool defeated;
        private SpriteRenderer sr;
        private Transform playerTransform;
        private Color baseColor = Color.white;

        private void Awake()
        {
            anchorPos = transform.position;
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

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            switch (state)
            {
                case SwoopState.Patrol:
                    TickPatrol();
                    break;
                case SwoopState.Telegraph:
                    TickTelegraph();
                    break;
                case SwoopState.Dive:
                    TickDive();
                    break;
                case SwoopState.Cooldown:
                    TickPatrol();
                    if (cooldownTimer <= 0f)
                    {
                        state = SwoopState.Patrol;
                    }
                    break;
            }
        }

        private void TickPatrol()
        {
            float nextX = transform.position.x + patrolDir * patrolSpeed * Time.deltaTime;
            float minX = anchorPos.x - patrolRange;
            float maxX = anchorPos.x + patrolRange;
            if (nextX >= maxX) { nextX = maxX; patrolDir = -1f; }
            else if (nextX <= minX) { nextX = minX; patrolDir = 1f; }

            float nextY = anchorPos.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            transform.position = Vector3.Lerp(transform.position, new Vector3(nextX, nextY, anchorPos.z), Time.deltaTime * 10f);
            transform.position = new Vector3(nextX, transform.position.y, anchorPos.z);

            if (sr != null)
            {
                sr.flipX = patrolDir < 0f;
                sr.color = baseColor;
            }

            if (state == SwoopState.Patrol && cooldownTimer <= 0f && playerTransform != null)
            {
                float dx = playerTransform.position.x - transform.position.x;
                float dy = transform.position.y - playerTransform.position.y;
                if (Mathf.Abs(dx) <= detectRangeX && dy >= -1.0f && dy <= detectRangeY)
                {
                    diveDirX = Mathf.Sign(dx != 0f ? dx : patrolDir);
                    patrolDir = diveDirX;
                    state = SwoopState.Telegraph;
                    stateTimer = 0f;
                }
            }
        }

        private void TickTelegraph()
        {
            stateTimer += Time.deltaTime;
            if (sr != null)
            {
                sr.flipX = diveDirX < 0f;
                sr.color = new Color(1f, 0.52f, 0.55f, 1f);
            }

            if (stateTimer >= telegraphDuration)
            {
                diveStartPos = transform.position;
                state = SwoopState.Dive;
                stateTimer = 0f;
            }
        }

        private void TickDive()
        {
            stateTimer += Time.deltaTime;
            float u = Mathf.Clamp01(stateTimer / diveDuration);
            // Smooth parabolic swoop down to diveDepth at u = 0.5 then back up to hover altitude
            float curveY = -Mathf.Sin(u * Mathf.PI) * diveDepth;
            float travelX = diveDirX * (patrolSpeed * 1.95f) * (stateTimer);
            float clampedX = Mathf.Clamp(diveStartPos.x + travelX, anchorPos.x - patrolRange - 1.5f, anchorPos.x + patrolRange + 1.5f);
            transform.position = new Vector3(clampedX, Mathf.Lerp(diveStartPos.y, anchorPos.y, u) + curveY, anchorPos.z);

            if (sr != null)
            {
                sr.flipX = diveDirX < 0f;
                sr.color = baseColor;
            }

            if (u >= 1f)
            {
                cooldownTimer = diveCooldown;
                state = SwoopState.Cooldown;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void HandleContact(Collider2D other)
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
            bool above = other.bounds.min.y >= transform.position.y - stompVerticalTolerance;
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
