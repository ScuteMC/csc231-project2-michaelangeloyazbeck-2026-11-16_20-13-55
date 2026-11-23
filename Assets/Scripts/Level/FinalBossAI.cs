using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Final Boss AI controller for Malakor, The Eclipse Warden.
    /// Explicitly cycles across 3 distinct states:
    /// 0 = Wandering (armored patrol with Eclipse Halberd guard),
    /// 1 = Attacking (overhead Halberd telegraph -> cleave + crimson shockwave),
    /// 2 = Stunned (kneeling exhaustion exposing his cyan-gold Aether Crown Core for a Stomp).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FinalBossAI : MonoBehaviour
    {
        public enum BossState
        {
            Wandering = 0,
            Attacking = 1,
            Stunned = 2
        }

        [Header("Identity & Vitals")]
        public string bossTitle = "MALAKOR, THE ECLIPSE WARDEN";
        public int maxHealth = 3;
        public BossState currentState = BossState.Wandering;

        [Header("Arena Bounds & Movement")]
        public float arenaActivationX = 212.5f;
        public float minPatrolX = 216.2f;
        public float maxPatrolX = 231.8f;
        public float wanderSpeed = 2.8f;
        public float attackTriggerRange = 6.2f;
        public float attackCooldown = 1.55f;

        [Header("Attack & Stun Timings")]
        public float windupDuration = 0.62f;
        public float attackTotalDuration = 1.05f;
        public float stunDuration = 3.25f;
        public float stompBounceSpeed = 14.2f;
        public float armorDeflectBounceSpeed = 10.2f;

        [Header("References")]
        public Animator animator;
        public SpriteRenderer spriteRenderer;
        public LevelGoal linkedGoal;
        public Sprite shockwaveSprite;
        public Sprite attackPreviewSprite;
        public Sprite stunnedPreviewSprite;
        public Sprite wanderPreviewSprite;

        [Header("Particle Systems")]
        public ParticleSystem slamParticles;
        public ParticleSystem hitSparkParticles;
        public ParticleSystem stunLoopParticles;
        public ParticleSystem defeatExplosionParticles;

        private static readonly int AnimStateHash = Animator.StringToHash("State");

        private Collider2D bossCollider;
        private Transform playerTransform;
        private int currentHealth;
        private float facingDirection = -1f;
        private float stateTimer;
        private float attackCooldownTimer = 0.65f;
        private float armorWarningCooldown;
        private float postStompSafeTimer;
        private float hitFlashTimer;
        private bool slamExecuted;
        private bool arenaEngaged;
        private bool defeated;
        private Vector3 spawnPosition;
        private Color baseColor = Color.white;

        private void Awake()
        {
            bossCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            spawnPosition = transform.position;
            currentHealth = maxHealth;
        }

        private void Start()
        {
            FindPlayer();
            SetState(BossState.Wandering);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBossHud(false, currentHealth, maxHealth, GetStateHudLabel());
            }
        }

        private void Update()
        {
            if (defeated)
            {
                return;
            }

            if (hitFlashTimer > 0f && spriteRenderer != null)
            {
                hitFlashTimer -= Time.deltaTime;
                spriteRenderer.color = hitFlashTimer > 0f
                    ? new Color(1f, 0.92f, 0.72f, 1f)
                    : baseColor;
            }

            if (armorWarningCooldown > 0f)
            {
                armorWarningCooldown -= Time.deltaTime;
            }

            if (postStompSafeTimer > 0f)
            {
                postStompSafeTimer -= Time.deltaTime;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            FindPlayer();
            if (playerTransform == null)
            {
                return;
            }

            // Activate Sanctum arena HUD when Vesper steps onto the Sanctum Terrace
            if (!arenaEngaged && playerTransform.position.x >= arenaActivationX)
            {
                arenaEngaged = true;
                RefreshBossHud(true);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBanner("MALAKOR BLOCKS THE BEACON! Bait his Halberd Slam, then stomp his exposed Crown!", 4.5f);
                }
            }

            float speedMultiplier = GetPhaseSpeedMultiplier();

            switch (currentState)
            {
                case BossState.Wandering:
                    TickWandering(speedMultiplier);
                    break;
                case BossState.Attacking:
                    TickAttacking(speedMultiplier);
                    break;
                case BossState.Stunned:
                    TickStunned();
                    break;
            }
        }

        private void FindPlayer()
        {
            if (playerTransform != null)
            {
                return;
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private float GetPhaseSpeedMultiplier()
        {
            int hitsTaken = Mathf.Clamp(maxHealth - currentHealth, 0, 2);
            return hitsTaken switch
            {
                0 => 1.0f,
                1 => 1.18f,
                _ => 1.35f
            };
        }

        private void TickWandering(float speedMult)
        {
            float dxToPlayer = playerTransform.position.x - transform.position.x;

            // Patrol smoothly while subtly orienting toward Vesper inside the arena
            if (arenaEngaged && Mathf.Abs(dxToPlayer) > 0.75f)
            {
                float desiredDir = Mathf.Sign(dxToPlayer);
                if ((transform.position.x <= minPatrolX + 0.1f && desiredDir < 0f) ||
                    (transform.position.x >= maxPatrolX - 0.1f && desiredDir > 0f))
                {
                    facingDirection = desiredDir;
                }
                else
                {
                    facingDirection = desiredDir;
                }
            }

            Vector3 pos = transform.position;
            pos.x += facingDirection * wanderSpeed * speedMult * Time.deltaTime;
            if (pos.x <= minPatrolX)
            {
                pos.x = minPatrolX;
                facingDirection = 1f;
            }
            else if (pos.x >= maxPatrolX)
            {
                pos.x = maxPatrolX;
                facingDirection = -1f;
            }

            transform.position = pos;
            ApplyFacingScale();

            if (!arenaEngaged)
            {
                return;
            }

            attackCooldownTimer -= Time.deltaTime * speedMult;
            if (attackCooldownTimer <= 0f && Mathf.Abs(dxToPlayer) <= attackTriggerRange)
            {
                facingDirection = Mathf.Sign(dxToPlayer != 0f ? dxToPlayer : facingDirection);
                ApplyFacingScale();
                SetState(BossState.Attacking);
            }
        }

        private void TickAttacking(float speedMult)
        {
            stateTimer += Time.deltaTime * speedMult;

            if (!slamExecuted && stateTimer >= windupDuration)
            {
                slamExecuted = true;
                ExecuteHalberdSlam();
            }

            if (stateTimer >= attackTotalDuration)
            {
                SetState(BossState.Stunned);
            }
        }

        private void ExecuteHalberdSlam()
        {
            if (slamParticles != null)
            {
                slamParticles.transform.position = transform.position + new Vector3(facingDirection * 1.45f, 0.15f, 0f);
                slamParticles.Play();
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.24f, 0.28f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLand();
            }

            // Direct Halberd cleave hit check in front of Malakor
            if (playerTransform != null)
            {
                Vector2 cleaveCenter = (Vector2)transform.position + new Vector2(facingDirection * 2.0f, 1.15f);
                Vector2 delta = (Vector2)playerTransform.position - cleaveCenter;
                if (Mathf.Abs(delta.x) <= 1.55f && Mathf.Abs(delta.y) <= 1.35f)
                {
                    PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(1, transform.position);
                    }
                }
            }

            SpawnShockwave();
        }

        private void SpawnShockwave()
        {
            GameObject waveObj = new GameObject("BossShockwave");
            waveObj.transform.position = transform.position + new Vector3(facingDirection * 1.75f, 0.05f, 0f);

            SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 6;
            sr.sprite = shockwaveSprite;

            BoxCollider2D col = waveObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.05f, 0.85f);
            col.offset = new Vector2(0f, 0.44f);

            BossShockwave wave = waveObj.AddComponent<BossShockwave>();
            wave.Initialize(facingDirection, shockwaveSprite, minPatrolX - 6.0f, maxPatrolX + 7.5f);
        }

        private void TickStunned()
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= stunDuration)
            {
                attackCooldownTimer = 1.1f;
                SetState(BossState.Wandering);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerContact(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandlePlayerContact(other);
        }

        private void HandlePlayerContact(Collider2D other)
        {
            if (defeated || postStompSafeTimer > 0f || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            Rigidbody2D playerRb = other.attachedRigidbody;
            PlayerHealth health = other.GetComponent<PlayerHealth>();

            if (currentState == BossState.Stunned)
            {
                // Stunned: generous stomp onto his lowered body/exposed Aether Crown; never deal contact damage here!
                bool stunnedStomp = playerRb != null && playerRb.linearVelocity.y <= 1.2f &&
                                    other.bounds.min.y >= transform.position.y + 0.70f;
                if (stunnedStomp)
                {
                    StompCrown(player, health);
                }
                return;
            }

            // Wandering or Attacking: landing atop Malakor's armored helm bounces Vesper harmlessly
            bool comingFromAbove = playerRb != null && playerRb.linearVelocity.y <= 0.8f &&
                                   other.bounds.min.y >= transform.position.y + 1.15f;
            if (comingFromAbove && player != null)
            {
                postStompSafeTimer = 0.55f;
                if (health != null)
                {
                    health.GrantBriefInvulnerability(0.45f);
                }

                player.Bounce(armorDeflectBounceSpeed);
                if (hitSparkParticles != null)
                {
                    hitSparkParticles.transform.position = other.bounds.min + new Vector3(0f, 0.1f, 0f);
                    hitSparkParticles.Play();
                }

                if (armorWarningCooldown <= 0f)
                {
                    armorWarningCooldown = 2.0f;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayLand();
                    }
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowBanner("ARMORED HELM! Bait his Halberd Slam to expose his Aether Crown!", 3.2f);
                    }
                }
                return;
            }

            // Side or front contact while Wandering or Attacking hurts Vesper
            if (health != null)
            {
                health.TakeDamage(1, transform.position);
            }
        }

        private void StompCrown(PlayerController player, PlayerHealth health)
        {
            postStompSafeTimer = 0.75f;
            if (health != null)
            {
                health.GrantBriefInvulnerability(0.65f);
            }

            if (player != null)
            {
                player.Bounce(stompBounceSpeed);
            }

            int stompDamage = (GameManager.Instance != null && GameManager.Instance.HasCrownBreakerPower) ? 2 : 1;
            currentHealth = Mathf.Max(0, currentHealth - stompDamage);
            hitFlashTimer = 0.25f;

            if (hitSparkParticles != null)
            {
                hitSparkParticles.transform.position = transform.position + new Vector3(0f, 2.0f, 0f);
                hitSparkParticles.Play();
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.22f, 0.25f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCollect();
            }

            if (currentHealth <= 0)
            {
                DefeatBoss();
                return;
            }

            // Snap back into Wandering at heightened phase tempo
            attackCooldownTimer = 0.85f;
            SetState(BossState.Wandering);

            if (UIManager.Instance != null)
            {
                string prefix = stompDamage > 1 ? "CROWN-BREAKER CRIT!" : "AETHER CROWN STRUCK!";
                UIManager.Instance.ShowBanner($"{prefix} Malakor has {currentHealth} core charge(s) left!", 3.2f);
            }
        }

        public bool ForceStunFromAetherFlash()
        {
            if (defeated || currentState == BossState.Stunned)
            {
                return false;
            }

            ClearAllShockwaves();
            if (hitSparkParticles != null)
            {
                hitSparkParticles.transform.position = transform.position + new Vector3(0f, 1.8f, 0f);
                hitSparkParticles.Play();
            }

            SetState(BossState.Stunned);
            return true;
        }

        private void DefeatBoss()
        {
            defeated = true;
            ClearAllShockwaves();

            if (stunLoopParticles != null)
            {
                stunLoopParticles.Stop();
            }

            if (defeatExplosionParticles != null)
            {
                defeatExplosionParticles.transform.position = transform.position + new Vector3(0f, 1.4f, 0f);
                defeatExplosionParticles.Play();
            }

            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            if (linkedGoal != null)
            {
                linkedGoal.UnlockBeacon();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBossHud(false, 0, maxHealth, string.Empty);
                UIManager.Instance.ShowBanner("MALAKOR DEFEATED! The Sanctum Beacon is unsealed!", 4.5f);
            }
        }

        public void ResetForRetry()
        {
            ClearAllShockwaves();

            if (defeated)
            {
                RefreshBossHud(false);
                return;
            }

            currentHealth = maxHealth;
            hitFlashTimer = 0f;
            attackCooldownTimer = 0.7f;
            armorWarningCooldown = 0f;
            postStompSafeTimer = 0f;
            facingDirection = -1f;
            transform.position = spawnPosition;
            ApplyFacingScale();

            if (bossCollider != null)
            {
                bossCollider.enabled = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = baseColor;
            }

            arenaEngaged = playerTransform != null && playerTransform.position.x >= arenaActivationX;
            SetState(BossState.Wandering);
            RefreshBossHud(arenaEngaged);
        }

        private void SetState(BossState nextState)
        {
            currentState = nextState;
            stateTimer = 0f;
            slamExecuted = false;

            if (animator != null)
            {
                animator.SetInteger(AnimStateHash, (int)currentState);
            }

            if (stunLoopParticles != null)
            {
                if (currentState == BossState.Stunned)
                {
                    stunLoopParticles.Play();
                }
                else
                {
                    stunLoopParticles.Stop();
                }
            }

            RefreshBossHud(arenaEngaged);
        }

        private void RefreshBossHud(bool visible)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBossHud(visible && !defeated, currentHealth, maxHealth, GetStateHudLabel());
            }
        }

        private string GetStateHudLabel()
        {
            return currentState switch
            {
                BossState.Wandering => "ARMORED PATROL — BAIT HALBERD SLAM",
                BossState.Attacking => "ECLIPSE HALBERD SLAM — JUMP OR GLIDE!",
                BossState.Stunned => "STUNNED! STOMP EXPOSED CROWN!",
                _ => string.Empty
            };
        }

        private void ApplyFacingScale()
        {
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * facingDirection, s.y, s.z);
        }

        private static void ClearAllShockwaves()
        {
            BossShockwave[] waves = FindObjectsByType<BossShockwave>(FindObjectsSortMode.None);
            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i] != null)
                {
                    Destroy(waves[i].gameObject);
                }
            }
        }

        // ============================================================
        // EDITOR VERIFICATION PREVIEW SUPPORT (non-runtime pose helper)
        // ============================================================
        public void SetEditorPreviewPose(BossState poseState, float facingDir, Vector3 previewPos)
        {
            facingDirection = facingDir >= 0f ? 1f : -1f;
            transform.position = previewPos;
            ApplyFacingScale();

            if (animator != null)
            {
                animator.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = poseState switch
                {
                    BossState.Attacking => attackPreviewSprite != null ? attackPreviewSprite : spriteRenderer.sprite,
                    BossState.Stunned => stunnedPreviewSprite != null ? stunnedPreviewSprite : spriteRenderer.sprite,
                    _ => wanderPreviewSprite != null ? wanderPreviewSprite : spriteRenderer.sprite
                };
            }
        }

        public void RestoreEditorPreviewPose(Vector3 originalPos)
        {
            spawnPosition = originalPos;
            facingDirection = -1f;
            transform.position = originalPos;
            ApplyFacingScale();

            if (animator != null)
            {
                animator.enabled = true;
                animator.SetInteger(AnimStateHash, (int)BossState.Wandering);
            }

            if (spriteRenderer != null && wanderPreviewSprite != null)
            {
                spriteRenderer.sprite = wanderPreviewSprite;
                spriteRenderer.enabled = true;
            }
        }
    }
}
