using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Multi-phase Final Boss AI controller for Malakor, The Eclipse Warden.
    /// States: 0=Wandering, 1=Attacking (Halberd Slam), 2=Stunned, 3=Dashing (Phantom Lunge), 4=Casting (Sky Spears + Orbs), 5=Intro.
    /// Escalates across Phase I (100-67 HP), Phase II Enraged (66-34 HP), and Phase III Berserk (33-1 HP).
    /// Vesper can defeat Malakor by shattering his Eclipse Ward with Storm-Lightning bolts (or helm bounces / dodges) and stomping his Crown or zapping him down!
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FinalBossAI : MonoBehaviour
    {
        public enum BossState
        {
            Wandering = 0,
            Attacking = 1,
            Stunned = 2,
            Dashing = 3,
            Casting = 4,
            Intro = 5
        }

        private enum AttackPattern
        {
            HalberdSlam,
            PhantomDash,
            SkySpearBarrage
        }

        [Header("Identity & Vitals")]
        public string bossTitle = "MALAKOR";
        public int maxHealth = 100;
        public BossState currentState = BossState.Wandering;

        [Header("Arena Bounds & Movement")]
        public float arenaActivationX = 219.0f;
        public float minPatrolX = 219.2f;
        public float maxPatrolX = 240.8f;
        public float wanderSpeed = 3.1f;
        public float attackTriggerRange = 13.5f;

        [Header("Combat & Damage (100 HP Scale)")]
        public int meleeDamage = 25;
        public int stompCrownDamage = 26;
        public int lightningWardChipDamage = 9;
        public int lightningWardShatterBonus = 5;
        public int lightningStunCritDamage = 12;
        public int helmBounceDamage = 4;
        public float stompBounceSpeed = 14.8f;
        public float armorDeflectBounceSpeed = 11.2f;

        [Header("References")]
        public Animator animator;
        public SpriteRenderer spriteRenderer;
        public LevelGoal linkedGoal;
        public GameObject wardRingVisual;
        public GameObject arenaGateObject;
        public Sprite shockwaveSprite;
        public Sprite skySpearSprite;
        public Sprite skyBeamWhiteSprite;
        public Sprite eclipseOrbSprite;
        public Sprite wanderPreviewSprite;
        public Sprite attackPreviewSprite;
        public Sprite dashPreviewSprite;
        public Sprite castPreviewSprite;
        public Sprite stunnedPreviewSprite;

        [Header("Particle Systems")]
        public ParticleSystem slamParticles;
        public ParticleSystem hitSparkParticles;
        public ParticleSystem stunLoopParticles;
        public ParticleSystem rageAuraParticles;
        public ParticleSystem defeatExplosionParticles;

        private static readonly int AnimStateHash = Animator.StringToHash("State");

        private BoxCollider2D bossCollider;
        private Transform playerTransform;
        private int currentHealth;
        private int currentPhase = 1;
        private int currentWardPips = 3;
        private float facingDirection = -1f;
        private float stateTimer;
        private float attackCooldownTimer = 0.85f;
        private float armorWarningCooldown;
        private float postStompSafeTimer;
        private float hitFlashTimer;
        private bool actionExecuted;
        private bool arenaEngaged;
        private bool defeated;
        private bool playerDamagedDuringAttack;
        private int comboChainRemaining;
        private int attackRotationIndex;
        private float dashStartX;
        private float dashTargetX;
        private Vector3 spawnPosition;
        private Color baseColor = Color.white;
        private SpriteRenderer dashTelegraphSr;
        private SpriteRenderer slamTelegraphSr;

        public int CurrentPhase => currentPhase;
        public int CurrentWardPips => currentWardPips;
        public int MaxWardPips => GetMaxWardPipsForPhase(currentPhase);
        public int CurrentHealth => currentHealth;
        public bool IsDefeated => defeated;

        private void Awake()
        {
            bossCollider = GetComponent<BoxCollider2D>();
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
            currentPhase = 1;
            currentWardPips = GetMaxWardPipsForPhase(1);
            EnsureTelegraphVisuals();
        }

        private void EnsureTelegraphVisuals()
        {
            if (skyBeamWhiteSprite == null)
            {
                return;
            }

            if (dashTelegraphSr == null)
            {
                GameObject dashTelGo = new GameObject("DashFloorTelegraph");
                dashTelGo.transform.position = transform.position;
                dashTelegraphSr = dashTelGo.AddComponent<SpriteRenderer>();
                dashTelegraphSr.sprite = skyBeamWhiteSprite;
                dashTelegraphSr.sortingOrder = 5;
                dashTelegraphSr.enabled = false;
            }

            if (slamTelegraphSr == null)
            {
                GameObject slamTelGo = new GameObject("SlamZoneTelegraph");
                slamTelGo.transform.position = transform.position;
                slamTelegraphSr = slamTelGo.AddComponent<SpriteRenderer>();
                slamTelegraphSr.sprite = skyBeamWhiteSprite;
                slamTelegraphSr.sortingOrder = 5;
                slamTelegraphSr.enabled = false;
            }
        }

        private void HideAllTelegraphs()
        {
            if (dashTelegraphSr != null) dashTelegraphSr.enabled = false;
            if (slamTelegraphSr != null) slamTelegraphSr.enabled = false;
        }

        public void InitializeForEditorVerification()
        {
            Awake();
            if (arenaGateObject != null)
            {
                arenaGateObject.SetActive(false);
            }
            SetState(BossState.Wandering);
            UpdateWardVisual();
        }

        private void Start()
        {
            FindPlayer();
            if (arenaGateObject != null)
            {
                arenaGateObject.SetActive(false);
            }
            SetState(BossState.Wandering);
            UpdateWardVisual();
            RefreshBossHud(false);
        }

        private void Update()
        {
            if (defeated)
            {
                return;
            }

            if (wardRingVisual != null && wardRingVisual.activeSelf)
            {
                wardRingVisual.transform.Rotate(0f, 0f, -95f * Time.deltaTime);
            }

            if (hitFlashTimer > 0f && spriteRenderer != null)
            {
                hitFlashTimer -= Time.deltaTime;
                spriteRenderer.color = hitFlashTimer > 0f
                    ? new Color(1f, 0.96f, 0.72f, 1f)
                    : GetPhaseTint();
            }

            if (armorWarningCooldown > 0f) armorWarningCooldown -= Time.deltaTime;
            if (postStompSafeTimer > 0f) postStompSafeTimer -= Time.deltaTime;

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            FindPlayer();
            if (playerTransform == null)
            {
                return;
            }

            if (!arenaEngaged)
            {
                // Stay completely still on throne side until Vesper steps past the Vestibule threshold!
                facingDirection = -1f;
                ApplyFacingScale();
                if (playerTransform.position.x >= arenaActivationX)
                {
                    EngageArena(playIntroState: true);
                }
                return;
            }

            float speedMult = GetPhaseSpeedMultiplier();
            switch (currentState)
            {
                case BossState.Intro:
                    TickIntro();
                    break;
                case BossState.Wandering:
                    TickWandering(speedMult);
                    break;
                case BossState.Attacking:
                    TickAttacking(speedMult);
                    break;
                case BossState.Dashing:
                    TickDashing(speedMult);
                    break;
                case BossState.Casting:
                    TickCasting(speedMult);
                    break;
                case BossState.Stunned:
                    TickStunned();
                    break;
            }
        }

        private void EngageArena(bool playIntroState)
        {
            if (arenaEngaged)
            {
                return;
            }

            arenaEngaged = true;
            if (arenaGateObject != null)
            {
                arenaGateObject.SetActive(true);
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetBossArenaCamera(true, new Vector2(230.0f, 11.8f), 7.7f);
                CameraController.Instance.TriggerShake(0.30f, 0.32f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBossMusic(currentPhase);
                AudioManager.Instance.PlayBossRoar();
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowBanner("MALAKOR AWAKENS", 2.0f);
            }

            if (playIntroState)
            {
                SetState(BossState.Intro);
            }
            else
            {
                RefreshBossHud(true);
            }
        }

        private void TickIntro()
        {
            stateTimer += Time.deltaTime;
            if (playerTransform != null)
            {
                float dx = playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.25f)
                {
                    facingDirection = Mathf.Sign(dx);
                    ApplyFacingScale();
                }
            }

            if (stateTimer >= 1.30f)
            {
                attackCooldownTimer = 0.85f;
                SetState(BossState.Wandering);
            }
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        private int GetMaxWardPipsForPhase(int phase) => 3;

        private float GetPhaseSpeedMultiplier() => currentPhase switch
        {
            1 => 1.0f,
            2 => 1.24f,
            _ => 1.42f
        };

        private float GetStunDurationForPhase() => currentPhase switch
        {
            1 => 3.30f,
            2 => 2.80f,
            _ => 2.40f
        };

        private Color GetPhaseTint() => currentPhase switch
        {
            2 => new Color(1f, 0.84f, 0.82f, 1f),
            3 => new Color(1f, 0.72f, 0.76f, 1f),
            _ => baseColor
        };

        private void TickWandering(float speedMult)
        {
            float dxToPlayer = playerTransform.position.x - transform.position.x;
            if (Mathf.Abs(dxToPlayer) > 0.65f)
            {
                facingDirection = Mathf.Sign(dxToPlayer);
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

            attackCooldownTimer -= Time.deltaTime * speedMult;
            if (attackCooldownTimer <= 0f && Mathf.Abs(dxToPlayer) <= attackTriggerRange)
            {
                comboChainRemaining = currentPhase >= 3 ? 1 : 0;
                LaunchNextAttack(dxToPlayer);
            }
        }

        private void LaunchNextAttack(float dxToPlayer)
        {
            facingDirection = Mathf.Sign(dxToPlayer != 0f ? dxToPlayer : facingDirection);
            ApplyFacingScale();

            AttackPattern chosen = ChooseAttackPattern(Mathf.Abs(dxToPlayer));
            switch (chosen)
            {
                case AttackPattern.HalberdSlam:
                    SetState(BossState.Attacking);
                    break;
                case AttackPattern.PhantomDash:
                    dashStartX = transform.position.x;
                    dashTargetX = Mathf.Clamp(dashStartX + facingDirection * 8.8f, minPatrolX, maxPatrolX);
                    SetState(BossState.Dashing);
                    break;
                case AttackPattern.SkySpearBarrage:
                    SetState(BossState.Casting);
                    break;
            }
        }

        private AttackPattern ChooseAttackPattern(float absDist)
        {
            attackRotationIndex++;
            if (currentPhase == 1)
            {
                return (absDist > 7.5f || attackRotationIndex % 2 == 0)
                    ? AttackPattern.PhantomDash
                    : AttackPattern.HalberdSlam;
            }

            int mod = attackRotationIndex % 3;
            return mod switch
            {
                0 => AttackPattern.SkySpearBarrage,
                1 => AttackPattern.HalberdSlam,
                _ => AttackPattern.PhantomDash
            };
        }

        private void TickAttacking(float speedMult)
        {
            stateTimer += Time.deltaTime * speedMult;

            if (!actionExecuted && slamTelegraphSr != null)
            {
                float pulse = 0.25f + 0.50f * Mathf.Abs(Mathf.Sin(stateTimer * 18f));
                slamTelegraphSr.enabled = true;
                slamTelegraphSr.color = new Color(1f, 0.24f, 0.36f, pulse);
                slamTelegraphSr.transform.position = transform.position + new Vector3(facingDirection * 1.95f, 0.32f, 0f);
                slamTelegraphSr.transform.localScale = new Vector3(3.2f, 0.45f, 1f);
            }

            if (!actionExecuted && stateTimer >= 0.68f)
            {
                actionExecuted = true;
                if (slamTelegraphSr != null) slamTelegraphSr.enabled = false;
                ExecuteHalberdSlam();
            }

            if (stateTimer >= 1.15f)
            {
                MaybeCrackWardFromExertion();
                FinishAttackCycle();
            }
        }

        private void ExecuteHalberdSlam()
        {
            if (slamParticles != null)
            {
                slamParticles.transform.position = transform.position + new Vector3(facingDirection * 1.45f, 0.15f, 0f);
                slamParticles.Play();
            }

            if (CameraController.Instance != null) CameraController.Instance.TriggerShake(0.24f, 0.28f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();

            if (playerTransform != null)
            {
                Vector2 cleaveCenter = (Vector2)transform.position + new Vector2(facingDirection * 2.0f, 1.15f);
                Vector2 delta = (Vector2)playerTransform.position - cleaveCenter;
                if (Mathf.Abs(delta.x) <= 1.75f && Mathf.Abs(delta.y) <= 1.45f)
                {
                    PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        playerDamagedDuringAttack = true;
                        health.TakeDamage(meleeDamage, transform.position);
                    }
                }
            }

            SpawnShockwave(facingDirection);
            if (currentPhase >= 2)
            {
                SpawnShockwave(-facingDirection);
            }
        }

        private void SpawnShockwave(float dir)
        {
            GameObject waveObj = new GameObject("BossShockwave");
            waveObj.transform.position = transform.position + new Vector3(dir * 1.75f, 0.05f, 0f);

            SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;
            sr.sprite = shockwaveSprite;

            BoxCollider2D col = waveObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.05f, 0.85f);
            col.offset = new Vector2(0f, 0.44f);

            BossShockwave wave = waveObj.AddComponent<BossShockwave>();
            wave.damageAmount = currentPhase >= 3 ? 25 : 22;
            wave.moveSpeed = currentPhase >= 2 ? 10.8f : 9.5f;
            wave.Initialize(dir, shockwaveSprite, minPatrolX - 0.8f, maxPatrolX + 0.8f);
        }

        private void TickDashing(float speedMult)
        {
            stateTimer += Time.deltaTime * speedMult;
            const float windup = 0.62f;
            const float dashDur = 0.54f;

            if (stateTimer < windup)
            {
                if (dashTelegraphSr != null)
                {
                    float pulse = 0.28f + 0.52f * Mathf.Abs(Mathf.Sin(stateTimer * 20f));
                    dashTelegraphSr.enabled = true;
                    dashTelegraphSr.color = new Color(1f, 0.22f, 0.42f, pulse);
                    float midX = (dashStartX + dashTargetX) * 0.5f;
                    dashTelegraphSr.transform.position = new Vector3(midX, transform.position.y + 0.35f, 0f);
                    dashTelegraphSr.transform.localScale = new Vector3(Mathf.Max(1.8f, Mathf.Abs(dashTargetX - dashStartX)), 0.36f, 1f);
                }
                return;
            }

            if (!actionExecuted)
            {
                actionExecuted = true;
                if (dashTelegraphSr != null) dashTelegraphSr.enabled = false;
                if (slamParticles != null)
                {
                    slamParticles.transform.position = transform.position + new Vector3(0f, 0.25f, 0f);
                    slamParticles.Play();
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();
            }

            float u = Mathf.Clamp01((stateTimer - windup) / dashDur);
            float smooth = u * (2f - u);
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(dashStartX, dashTargetX, smooth);
            transform.position = pos;

            if (u >= 1f)
            {
                MaybeCrackWardFromExertion();
                FinishAttackCycle();
            }
        }

        private void MaybeCrackWardFromExertion()
        {
            if (!playerDamagedDuringAttack && currentWardPips > 1)
            {
                currentWardPips--;
                UpdateWardVisual();
                RefreshBossHud(true);
            }
        }

        private void TickCasting(float speedMult)
        {
            stateTimer += Time.deltaTime * speedMult;
            if (!actionExecuted && stateTimer >= 0.45f)
            {
                actionExecuted = true;
                ExecuteSkySpearBarrage();
            }

            if (stateTimer >= 1.15f)
            {
                FinishAttackCycle();
            }
        }

        private void ExecuteSkySpearBarrage()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
            if (CameraController.Instance != null) CameraController.Instance.TriggerShake(0.20f, 0.22f);

            int spearCount = currentPhase >= 3 ? 5 : 4;
            for (int i = 0; i < spearCount; i++)
            {
                float norm = (i + 0.5f) / spearCount;
                float x = Mathf.Lerp(minPatrolX, maxPatrolX, norm);
                if (i == spearCount / 2 && playerTransform != null)
                {
                    x = Mathf.Clamp(playerTransform.position.x, minPatrolX, maxPatrolX);
                }
                SpawnSkyStrike(x);
            }

            // Phase III Berserk also fires a 3-way Eclipse Orb spread toward Vesper
            if (currentPhase >= 3 && playerTransform != null)
            {
                Vector2 origin = (Vector2)transform.position + new Vector2(facingDirection * 0.9f, 1.8f);
                Vector2 toPlayer = ((Vector2)playerTransform.position + new Vector2(0f, 0.7f) - origin).normalized;
                float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);
                for (int a = -1; a <= 1; a++)
                {
                    float ang = baseAngle + a * 0.22f;
                    Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 8.2f;
                    SpawnEclipseOrb(origin, vel);
                }
            }
        }

        private void SpawnSkyStrike(float targetX)
        {
            GameObject go = new GameObject("BossSkyStrike");
            go.transform.position = new Vector3(targetX, transform.position.y + 3.0f, 0f);
            BossSkyStrike strike = go.AddComponent<BossSkyStrike>();
            strike.Initialize(skyBeamWhiteSprite, skySpearSprite, currentPhase >= 3 ? 25 : 22, 0.85f);
        }

        private void SpawnEclipseOrb(Vector2 origin, Vector2 velocity)
        {
            GameObject go = new GameObject("BossEclipseOrb");
            go.transform.position = origin;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.28f;
            EnemyProjectile proj = go.AddComponent<EnemyProjectile>();
            proj.Initialize(velocity, eclipseOrbSprite, 20, 2.8f);
        }

        private void FinishAttackCycle()
        {
            if (comboChainRemaining > 0 && playerTransform != null && !defeated)
            {
                comboChainRemaining--;
                LaunchNextAttack(playerTransform.position.x - transform.position.x);
                return;
            }

            attackCooldownTimer = currentPhase switch
            {
                1 => 1.35f,
                2 => 0.95f,
                _ => 0.72f
            };
            SetState(BossState.Wandering);
        }

        private void TickStunned()
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= GetStunDurationForPhase())
            {
                currentWardPips = GetMaxWardPipsForPhase(currentPhase);
                UpdateWardVisual();
                attackCooldownTimer = 0.75f;
                SetState(BossState.Wandering);
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => HandlePlayerContact(other);
        private void OnTriggerStay2D(Collider2D other) => HandlePlayerContact(other);

        private void HandlePlayerContact(Collider2D other)
        {
            if (defeated || !arenaEngaged || postStompSafeTimer > 0f || other == null || !other.CompareTag("Player"))
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
                bool stunnedStomp = playerRb != null && playerRb.linearVelocity.y <= 1.2f &&
                                    other.bounds.min.y >= transform.position.y + 0.55f;
                if (stunnedStomp)
                {
                    StompCrown(player, health);
                }
                return;
            }

            // While Ward is active or patrolling: landing atop Malakor deflects safely, chips HP & strips 1 Ward Pip!
            bool comingFromAbove = playerRb != null && playerRb.linearVelocity.y <= 0.8f &&
                                   other.bounds.min.y >= transform.position.y + 1.15f;
            if (comingFromAbove && player != null)
            {
                postStompSafeTimer = 0.55f;
                if (health != null) health.GrantBriefInvulnerability(0.50f);
                player.Bounce(armorDeflectBounceSpeed);

                if (hitSparkParticles != null)
                {
                    hitSparkParticles.transform.position = other.bounds.min + new Vector3(0f, 0.1f, 0f);
                    hitSparkParticles.Play();
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
                if (GameFeelManager.Instance != null)
                {
                    GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 2.75f, 0f), $"-{helmBounceDamage}", new Color(0.75f, 0.96f, 1f, 1f), 0.9f);
                }

                ApplyBossDamage(helmBounceDamage);
                if (defeated)
                {
                    return;
                }

                if (currentWardPips > 0)
                {
                    currentWardPips--;
                    UpdateWardVisual();
                    if (currentWardPips <= 0)
                    {
                        ClearArenaHazards();
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayWardShatter();
                        if (CameraController.Instance != null)
                        {
                            CameraController.Instance.TriggerShake(0.28f, 0.34f);
                            CameraController.Instance.TriggerZoomPunch(-0.55f, 0.25f);
                        }
                        if (GameFeelManager.Instance != null)
                        {
                            GameFeelManager.Instance.TriggerHitStop(0.08f);
                            GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 3.1f, 0f), "WARD SHATTER!", new Color(1f, 0.88f, 0.38f, 1f), 1.35f);
                        }
                        SetState(BossState.Stunned);
                        if (UIManager.Instance != null) UIManager.Instance.ShowBanner("WARD BROKEN — STOMP!", 2.0f);
                        return;
                    }
                }

                RefreshBossHud(true);
                if (armorWarningCooldown <= 0f)
                {
                    armorWarningCooldown = 2.0f;
                    if (UIManager.Instance != null) UIManager.Instance.ShowBanner("ZAP WARD [CLICK / E]", 1.6f);
                }
                return;
            }

            if (currentState == BossState.Intro)
            {
                return;
            }

            if (health != null)
            {
                playerDamagedDuringAttack = true;
                health.TakeDamage(meleeDamage, transform.position);
            }
        }

        public void TakeLightningHit(Vector2 hitPos)
        {
            if (defeated)
            {
                return;
            }

            if (!arenaEngaged)
            {
                EngageArena(playIntroState: false);
            }

            if (hitSparkParticles != null)
            {
                hitSparkParticles.transform.position = hitPos;
                hitSparkParticles.Play();
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();

            if (currentState == BossState.Stunned)
            {
                if (GameFeelManager.Instance != null)
                {
                    GameFeelManager.Instance.TriggerHitStop(0.032f);
                    GameFeelManager.Instance.SpawnFloatingText(hitPos + new Vector2(0f, 0.45f), $"-{lightningStunCritDamage}", new Color(1f, 0.90f, 0.42f, 1f), 1.05f);
                }
                ApplyBossDamage(lightningStunCritDamage);
                return;
            }

            bool willBreak = currentWardPips == 1;
            int totalBoltDmg = lightningWardChipDamage + (willBreak ? lightningWardShatterBonus : 0);
            if (GameFeelManager.Instance != null)
            {
                GameFeelManager.Instance.TriggerHitStop(willBreak ? 0.08f : 0.032f);
                GameFeelManager.Instance.SpawnFloatingText(
                    hitPos + new Vector2(0f, 0.45f),
                    $"-{totalBoltDmg}",
                    willBreak ? new Color(1f, 0.90f, 0.42f, 1f) : new Color(0.45f, 0.98f, 1f, 1f),
                    willBreak ? 1.15f : 0.95f
                );
            }
            ApplyBossDamage(totalBoltDmg);
            if (defeated)
            {
                return;
            }

            if (currentWardPips > 0)
            {
                currentWardPips--;
                UpdateWardVisual();

                if (currentWardPips <= 0)
                {
                    ClearArenaHazards();
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayWardShatter();
                    if (CameraController.Instance != null)
                    {
                        CameraController.Instance.TriggerShake(0.28f, 0.34f);
                        CameraController.Instance.TriggerZoomPunch(-0.55f, 0.25f);
                    }
                    if (GameFeelManager.Instance != null)
                    {
                        GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 2.9f, 0f), "WARD SHATTER!", new Color(1f, 0.88f, 0.38f, 1f), 1.35f);
                    }
                    SetState(BossState.Stunned);
                    if (UIManager.Instance != null) UIManager.Instance.ShowBanner("WARD BROKEN — STOMP!", 2.0f);
                    return;
                }
            }

            RefreshBossHud(true);
        }

        private void StompCrown(PlayerController player, PlayerHealth health)
        {
            postStompSafeTimer = 0.75f;
            if (health != null) health.GrantBriefInvulnerability(0.65f);
            if (player != null) player.Bounce(stompBounceSpeed);

            if (hitSparkParticles != null)
            {
                hitSparkParticles.transform.position = transform.position + new Vector3(0f, 2.0f, 0f);
                hitSparkParticles.Play();
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.26f, 0.32f);
                CameraController.Instance.TriggerZoomPunch(-0.6f, 0.24f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWardShatter();

            int bonusDmg = (GameManager.Instance != null && GameManager.Instance.HasCrownBreakerPower) ? 6 : 0;
            int totalDmg = stompCrownDamage + bonusDmg;
            if (GameFeelManager.Instance != null)
            {
                GameFeelManager.Instance.TriggerHitStop(0.085f);
                GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 2.9f, 0f), $"-{totalDmg} CRIT!", new Color(1f, 0.90f, 0.36f, 1f), 1.4f);
            }
            ApplyBossDamage(totalDmg);
            if (defeated)
            {
                return;
            }

            currentWardPips = GetMaxWardPipsForPhase(currentPhase);
            UpdateWardVisual();
            attackCooldownTimer = 0.65f;
            SetState(BossState.Wandering);
        }

        private void ApplyBossDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, amount));
            hitFlashTimer = 0.22f;

            if (currentHealth <= 0)
            {
                DefeatBoss();
                return;
            }

            int nextPhase = currentHealth <= 33 ? 3 : currentHealth <= 66 ? 2 : 1;
            if (nextPhase > currentPhase)
            {
                EnterEnragePhase(nextPhase);
            }
            else
            {
                RefreshBossHud(true);
            }
        }

        private void EnterEnragePhase(int nextPhase)
        {
            ClearArenaHazards();
            currentPhase = nextPhase;
            currentWardPips = GetMaxWardPipsForPhase(currentPhase);
            UpdateWardVisual();
            attackCooldownTimer = 1.15f;
            comboChainRemaining = 0;

            if (rageAuraParticles != null && !rageAuraParticles.isPlaying)
            {
                rageAuraParticles.Play();
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetPhaseTint();
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBossMusic(currentPhase);
                AudioManager.Instance.PlayBossRoar();
            }
            if (CameraController.Instance != null) CameraController.Instance.TriggerShake(0.35f, 0.42f);

            if (UIManager.Instance != null)
            {
                string msg = currentPhase >= 3 ? "PHASE III — BERSERK!" : "PHASE II — ENRAGED!";
                UIManager.Instance.ShowBanner(msg, 2.4f);
            }
            if (currentState == BossState.Stunned)
            {
                SetState(BossState.Wandering);
            }
            else
            {
                RefreshBossHud(true);
            }
        }

        public bool ForceStunFromAetherFlash()
        {
            if (defeated || currentState == BossState.Stunned)
            {
                return false;
            }

            currentWardPips = 0;
            UpdateWardVisual();
            ClearArenaHazards();
            SetState(BossState.Stunned);
            return true;
        }

        private void DefeatBoss()
        {
            defeated = true;
            HideAllTelegraphs();
            ClearArenaHazards();
            UpdateWardVisual();

            if (arenaGateObject != null) arenaGateObject.SetActive(false);
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetBossArenaCamera(false, Vector2.zero);
                CameraController.Instance.TriggerZoomPunch(-0.85f, 0.45f);
                CameraController.Instance.TriggerShake(0.45f, 0.45f);
            }
            if (GameFeelManager.Instance != null)
            {
                GameFeelManager.Instance.TriggerHitStop(0.22f, 0.08f);
                GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 2.8f, 0f), "ECLIPSE BROKEN!", new Color(1f, 0.90f, 0.40f, 1f), 1.55f);
            }

            if (stunLoopParticles != null) stunLoopParticles.Stop();
            if (rageAuraParticles != null) rageAuraParticles.Stop();
            if (defeatExplosionParticles != null)
            {
                defeatExplosionParticles.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);
                defeatExplosionParticles.Play();
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLevelMusic();
                AudioManager.Instance.PlayWin();
                AudioManager.Instance.PlayBossRoar();
            }
            if (bossCollider != null) bossCollider.enabled = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (linkedGoal != null) linkedGoal.UnlockBeacon();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBossHud(false, 0, maxHealth, 0, 3, currentPhase, string.Empty);
                UIManager.Instance.ShowBanner("MALAKOR FALLS — IGNITE BEACON!", 3.2f);
            }
        }

        public void ResetForRetry()
        {
            HideAllTelegraphs();
            ClearArenaHazards();
            if (arenaGateObject != null) arenaGateObject.SetActive(false);
            if (CameraController.Instance != null) CameraController.Instance.SetBossArenaCamera(false, Vector2.zero);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelMusic();

            if (defeated)
            {
                RefreshBossHud(false);
                return;
            }

            arenaEngaged = false;
            currentHealth = maxHealth;
            currentPhase = 1;
            currentWardPips = GetMaxWardPipsForPhase(1);
            hitFlashTimer = 0f;
            attackCooldownTimer = 0.85f;
            armorWarningCooldown = 0f;
            postStompSafeTimer = 0f;
            comboChainRemaining = 0;
            facingDirection = -1f;
            transform.position = spawnPosition;
            ApplyFacingScale();

            if (bossCollider != null) bossCollider.enabled = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = baseColor;
            }
            if (rageAuraParticles != null)
            {
                rageAuraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            UpdateWardVisual();
            SetState(BossState.Wandering);
            RefreshBossHud(false);
        }

        private void SetState(BossState nextState)
        {
            HideAllTelegraphs();
            currentState = nextState;
            stateTimer = 0f;
            actionExecuted = false;
            playerDamagedDuringAttack = false;

            if (arenaEngaged && Application.isPlaying && AudioManager.Instance != null &&
                (nextState == BossState.Attacking || nextState == BossState.Dashing || nextState == BossState.Casting))
            {
                AudioManager.Instance.PlayBossTelegraph();
            }

            if (bossCollider != null)
            {
                if (nextState == BossState.Dashing)
                {
                    bossCollider.size = new Vector2(1.65f, 1.85f);
                    bossCollider.offset = new Vector2(0f, 0.95f);
                }
                else
                {
                    bossCollider.size = new Vector2(1.75f, 2.75f);
                    bossCollider.offset = new Vector2(0f, 1.38f);
                }
            }

            if (animator != null)
            {
                int animInt = nextState == BossState.Intro ? (int)BossState.Casting : (int)nextState;
                animator.SetInteger(AnimStateHash, animInt);
            }

            if (stunLoopParticles != null)
            {
                if (currentState == BossState.Stunned) stunLoopParticles.Play();
                else stunLoopParticles.Stop();
            }

            RefreshBossHud(arenaEngaged);
        }

        private void UpdateWardVisual()
        {
            if (wardRingVisual != null)
            {
                wardRingVisual.SetActive(!defeated && currentWardPips > 0 && currentState != BossState.Stunned);
            }
        }

        private void RefreshBossHud(bool visible)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBossHud(
                    visible && !defeated,
                    currentHealth,
                    maxHealth,
                    currentWardPips,
                    MaxWardPips,
                    currentPhase,
                    GetStateHudLabel()
                );
            }
        }

        private string GetStateHudLabel()
        {
            return currentState switch
            {
                BossState.Intro => "PREPARE FOR BATTLE!",
                BossState.Stunned => "STUNNED — STOMP!",
                BossState.Attacking => "HALBERD SLAM",
                BossState.Dashing => "PHANTOM DASH",
                BossState.Casting => "SKY LIGHTNING",
                _ => currentWardPips > 0 ? "WARD UP — ZAP!" : "STUN READY"
            };
        }

        private void ApplyFacingScale()
        {
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * facingDirection, s.y, s.z);
        }

        private static void ClearArenaHazards()
        {
            BossShockwave[] waves = FindObjectsByType<BossShockwave>(FindObjectsSortMode.None);
            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i] != null) Destroy(waves[i].gameObject);
            }

            BossSkyStrike[] strikes = FindObjectsByType<BossSkyStrike>(FindObjectsSortMode.None);
            for (int i = 0; i < strikes.Length; i++)
            {
                if (strikes[i] != null) Destroy(strikes[i].gameObject);
            }

            EnemyProjectile[] orbs = FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
            for (int i = 0; i < orbs.Length; i++)
            {
                if (orbs[i] != null) Destroy(orbs[i].gameObject);
            }
        }

        // ============================================================
        // EDITOR VERIFICATION PREVIEW SUPPORT
        // ============================================================
        public void SetEditorPreviewPose(BossState poseState, float facingDir, Vector3 previewPos, bool showWard = true)
        {
            HideAllTelegraphs();
            facingDirection = facingDir >= 0f ? 1f : -1f;
            transform.position = previewPos;
            ApplyFacingScale();

            if (animator != null) animator.enabled = false;
            if (wardRingVisual != null) wardRingVisual.SetActive(showWard && poseState != BossState.Stunned);

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = poseState switch
                {
                    BossState.Attacking => attackPreviewSprite != null ? attackPreviewSprite : spriteRenderer.sprite,
                    BossState.Dashing => dashPreviewSprite != null ? dashPreviewSprite : spriteRenderer.sprite,
                    BossState.Casting => castPreviewSprite != null ? castPreviewSprite : spriteRenderer.sprite,
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

            if (wardRingVisual != null) wardRingVisual.SetActive(true);

            if (spriteRenderer != null && wanderPreviewSprite != null)
            {
                spriteRenderer.sprite = wanderPreviewSprite;
                spriteRenderer.enabled = true;
            }
        }
    }
}
