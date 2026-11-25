using System.Collections;
using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Multi-phase Final Boss AI controller for Malakor, The Eclipse Warden (220 HP).
    /// States: 0=Wandering, 1=Attacking (Halberd Slam), 2=Stunned, 3=Dashing (Shadow Cross-Dash),
    ///         4=Casting (Rolling Sky-Spears / Eclipse Orb Fan), 5=Intro, 6=Leaping (Meteor Leap Slam).
    /// Escalates across Phase I (220-150 HP, 3 Ward), Phase II ENRAGED (149-75 HP, 4 Ward),
    /// and Phase III NIGHTMARE (74-1 HP, 5 Ward).
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
            Intro = 5,
            Leaping = 6
        }

        private enum AttackPattern
        {
            HalberdSlam,
            PhantomDash,
            MeteorLeapSlam,
            SkySpearStorm,
            EclipseOrbFan
        }

        [Header("Identity & Vitals")]
        public string bossTitle = "MALAKOR";
        public int maxHealth = 220;
        public BossState currentState = BossState.Wandering;

        [Header("Arena Bounds & Movement")]
        public float arenaActivationX = 456.0f;
        public float minPatrolX = 456.2f;
        public float maxPatrolX = 479.8f;
        public float wanderSpeed = 4.8f;
        public float attackTriggerRange = 17.5f;

        [Header("Combat & Damage (100 HP Scale)")]
        public int meleeDamage = 26;
        public int stompCrownDamage = 38;
        public int lightningWardChipDamage = 4;
        public int lightningWardShatterBonus = 8;
        public int lightningStunCritDamage = 10;
        public int helmBounceDamage = 6;
        public float wardHitIcd = 0.36f;
        public float stunBoltIcd = 0.30f;
        public float stompBounceSpeed = 15.2f;
        public float armorDeflectBounceSpeed = 11.4f;

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
        private float attackCooldownTimer = 0.62f;
        private float wardHitCooldownTimer;
        private float stunBoltCooldownTimer;
        private float armorWarningCooldown;
        private float postStompSafeTimer;
        private float hitFlashTimer;
        private bool actionExecuted;
        private bool secondaryCuePlayed;
        private bool arenaEngaged;
        private bool defeated;
        private bool playerDamagedDuringAttack;
        private int comboChainRemaining;
        private int attackRotationIndex;
        private float dashStartX;
        private float dashTargetX;
        private int dashSubCount;
        private float nextAfterimageTime;
        private float leapStartX;
        private float leapTargetX;
        private AttackPattern activeCastPattern = AttackPattern.SkySpearStorm;
        private Coroutine spearStormRoutine;
        private Vector3 spawnPosition;
        private Color baseColor = Color.white;
        private SpriteRenderer dashTelegraphSr;
        private SpriteRenderer slamTelegraphSr;

        public bool EditorBypassHitIcd { get; set; }
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
                wardRingVisual.transform.Rotate(0f, 0f, -110f * Time.deltaTime);
            }

            if (hitFlashTimer > 0f && spriteRenderer != null)
            {
                hitFlashTimer -= Time.deltaTime;
                spriteRenderer.color = hitFlashTimer > 0f
                    ? new Color(1f, 0.96f, 0.72f, 1f)
                    : GetPhaseTint();
            }

            if (wardHitCooldownTimer > 0f) wardHitCooldownTimer -= Time.deltaTime;
            if (stunBoltCooldownTimer > 0f) stunBoltCooldownTimer -= Time.deltaTime;
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
                case BossState.Leaping:
                    TickLeaping(speedMult);
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
                Vector2 arenaCenter = new Vector2((minPatrolX + maxPatrolX) * 0.5f, spawnPosition.y + 2.8f);
                CameraController.Instance.SetBossArenaCamera(true, arenaCenter, 7.8f);
                CameraController.Instance.TriggerShake(0.32f, 0.34f);
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

            if (stateTimer >= 1.15f)
            {
                attackCooldownTimer = 0.50f;
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

        private int GetMaxWardPipsForPhase(int phase) => phase == 1 ? 3 : (phase == 2 ? 4 : 5);

        private float GetPhaseSpeedMultiplier() => currentPhase switch
        {
            1 => 1.0f,
            2 => 1.25f,
            _ => 1.48f
        };

        private float GetStunDurationForPhase() => currentPhase switch
        {
            1 => 2.65f,
            2 => 2.30f,
            _ => 2.00f
        };

        private Color GetPhaseTint() => currentPhase switch
        {
            2 => new Color(1f, 0.80f, 0.78f, 1f),
            3 => new Color(1f, 0.62f, 0.70f, 1f),
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
                comboChainRemaining = currentPhase >= 3 ? (attackRotationIndex % 2 == 0 ? 2 : 1) : (currentPhase == 2 ? 1 : 0);
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
                    dashSubCount = currentPhase >= 3 ? 1 : 0;
                    BeginDashLeg(dxToPlayer);
                    break;
                case AttackPattern.MeteorLeapSlam:
                    leapStartX = transform.position.x;
                    leapTargetX = playerTransform != null
                        ? Mathf.Clamp(playerTransform.position.x, minPatrolX, maxPatrolX)
                        : transform.position.x;
                    SetState(BossState.Leaping);
                    break;
                case AttackPattern.SkySpearStorm:
                    activeCastPattern = AttackPattern.SkySpearStorm;
                    SetState(BossState.Casting);
                    break;
                case AttackPattern.EclipseOrbFan:
                    activeCastPattern = AttackPattern.EclipseOrbFan;
                    SetState(BossState.Casting);
                    break;
            }
        }

        private void BeginDashLeg(float dxToPlayer)
        {
            float dir = Mathf.Sign(dxToPlayer != 0f ? dxToPlayer : facingDirection);
            facingDirection = dir;
            ApplyFacingScale();
            dashStartX = transform.position.x;
            float dashDist = currentPhase >= 3 ? 11.2f : 9.6f;
            dashTargetX = Mathf.Clamp(dashStartX + dir * dashDist, minPatrolX, maxPatrolX);
            if (Mathf.Abs(dashTargetX - dashStartX) < 3.0f)
            {
                facingDirection = -dir;
                ApplyFacingScale();
                dashTargetX = Mathf.Clamp(dashStartX + facingDirection * dashDist, minPatrolX, maxPatrolX);
            }
            nextAfterimageTime = 0.06f;
            SetState(BossState.Dashing);
        }

        private AttackPattern ChooseAttackPattern(float absDist)
        {
            attackRotationIndex++;
            if (currentPhase == 1)
            {
                if (absDist > 8.0f) return attackRotationIndex % 2 == 0 ? AttackPattern.PhantomDash : AttackPattern.MeteorLeapSlam;
                return (attackRotationIndex % 3) switch
                {
                    0 => AttackPattern.HalberdSlam,
                    1 => AttackPattern.PhantomDash,
                    _ => AttackPattern.MeteorLeapSlam
                };
            }

            if (currentPhase == 2)
            {
                return (attackRotationIndex % 5) switch
                {
                    0 => AttackPattern.SkySpearStorm,
                    1 => AttackPattern.PhantomDash,
                    2 => AttackPattern.MeteorLeapSlam,
                    3 => AttackPattern.EclipseOrbFan,
                    _ => AttackPattern.HalberdSlam
                };
            }

            return (attackRotationIndex % 5) switch
            {
                0 => AttackPattern.PhantomDash,
                1 => AttackPattern.SkySpearStorm,
                2 => AttackPattern.MeteorLeapSlam,
                3 => AttackPattern.EclipseOrbFan,
                _ => AttackPattern.HalberdSlam
            };
        }

        private void TickAttacking(float speedMult)
        {
            stateTimer += Time.deltaTime * speedMult;

            if (!actionExecuted && slamTelegraphSr != null)
            {
                float pulse = 0.30f + 0.52f * Mathf.Abs(Mathf.Sin(stateTimer * 22f));
                slamTelegraphSr.enabled = true;
                slamTelegraphSr.color = new Color(1f, 0.22f, 0.36f, pulse);
                slamTelegraphSr.transform.position = transform.position + new Vector3(facingDirection * 1.95f, 0.32f, 0f);
                slamTelegraphSr.transform.localScale = new Vector3(3.4f, 0.48f, 1f);
            }

            if (!actionExecuted && stateTimer >= 0.50f)
            {
                actionExecuted = true;
                if (slamTelegraphSr != null) slamTelegraphSr.enabled = false;
                ExecuteHalberdSlam();
            }

            if (stateTimer >= 0.92f)
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

            if (CameraController.Instance != null) CameraController.Instance.TriggerShake(0.26f, 0.32f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHardLand();

            if (playerTransform != null)
            {
                Vector2 cleaveCenter = (Vector2)transform.position + new Vector2(facingDirection * 2.0f, 1.15f);
                Vector2 delta = (Vector2)playerTransform.position - cleaveCenter;
                if (Mathf.Abs(delta.x) <= 1.85f && Mathf.Abs(delta.y) <= 1.50f)
                {
                    PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        playerDamagedDuringAttack = true;
                        health.TakeDamage(meleeDamage, transform.position);
                    }
                }
            }

            SpawnShockwave(facingDirection, currentPhase >= 2 ? 15.2f : 13.5f);
            if (currentPhase >= 2)
            {
                SpawnShockwave(-facingDirection, 15.2f);
            }
        }

        private void SpawnShockwave(float dir, float waveSpeed)
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
            wave.damageAmount = currentPhase >= 3 ? 28 : 24;
            wave.moveSpeed = waveSpeed;
            wave.Initialize(dir, shockwaveSprite, minPatrolX - 0.8f, maxPatrolX + 0.8f);
        }

        private void TickDashing(float speedMult)
        {
            stateTimer += Time.deltaTime * speedMult;
            float windup = dashSubCount == 0 && currentPhase >= 3 ? 0.34f : 0.46f;
            float dashDur = Mathf.Max(0.28f, Mathf.Abs(dashTargetX - dashStartX) / 16.5f);

            if (stateTimer < windup)
            {
                if (dashTelegraphSr != null)
                {
                    float pulse = 0.32f + 0.55f * Mathf.Abs(Mathf.Sin(stateTimer * 24f));
                    dashTelegraphSr.enabled = true;
                    dashTelegraphSr.color = new Color(1f, 0.22f, 0.42f, pulse);
                    float midX = (dashStartX + dashTargetX) * 0.5f;
                    dashTelegraphSr.transform.position = new Vector3(midX, spawnPosition.y + 0.35f, 0f);
                    dashTelegraphSr.transform.localScale = new Vector3(Mathf.Max(1.8f, Mathf.Abs(dashTargetX - dashStartX)), 0.38f, 1f);
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
                if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
            }

            float dashElapsed = stateTimer - windup;
            float u = Mathf.Clamp01(dashElapsed / dashDur);
            float smooth = u * (2f - u);
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(dashStartX, dashTargetX, smooth);
            transform.position = pos;

            if (dashElapsed >= nextAfterimageTime)
            {
                nextAfterimageTime += dashDur * 0.24f;
                SpawnAfterimage();
            }

            if (u >= 1f)
            {
                if (dashSubCount > 0 && playerTransform != null && !defeated)
                {
                    dashSubCount--;
                    BeginDashLeg(playerTransform.position.x - transform.position.x);
                    return;
                }

                MaybeCrackWardFromExertion();
                FinishAttackCycle();
            }
        }

        private void TickLeaping(float speedMult)
        {
            stateTimer += Time.deltaTime * speedMult;
            const float riseDur = 0.36f;
            const float hoverEnd = 0.78f;
            const float slamEnd = 0.96f;

            if (stateTimer < riseDur)
            {
                float u = stateTimer / riseDur;
                float ease = 1f - (1f - u) * (1f - u);
                if (playerTransform != null)
                {
                    leapTargetX = Mathf.Lerp(leapTargetX, Mathf.Clamp(playerTransform.position.x, minPatrolX, maxPatrolX), Time.deltaTime * 6f);
                }
                Vector3 pos = transform.position;
                pos.x = Mathf.Lerp(leapStartX, leapTargetX, ease * 0.85f);
                pos.y = spawnPosition.y + 6.8f * ease;
                transform.position = pos;
                return;
            }

            if (stateTimer < hoverEnd)
            {
                if (!secondaryCuePlayed)
                {
                    secondaryCuePlayed = true;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayBossTelegraph();
                }

                if (stateTimer < hoverEnd - 0.14f && playerTransform != null)
                {
                    leapTargetX = Mathf.Lerp(leapTargetX, Mathf.Clamp(playerTransform.position.x, minPatrolX, maxPatrolX), Time.deltaTime * 5f);
                }

                Vector3 pos = transform.position;
                pos.x = Mathf.Lerp(pos.x, leapTargetX, Time.deltaTime * 10f);
                pos.y = spawnPosition.y + 6.8f;
                transform.position = pos;

                if (slamTelegraphSr != null)
                {
                    float pulse = 0.35f + 0.55f * Mathf.Abs(Mathf.Sin(stateTimer * 26f));
                    slamTelegraphSr.enabled = true;
                    slamTelegraphSr.color = new Color(1f, 0.18f, 0.35f, pulse);
                    slamTelegraphSr.transform.position = new Vector3(transform.position.x, spawnPosition.y + 0.32f, 0f);
                    slamTelegraphSr.transform.localScale = new Vector3(4.4f, 0.55f, 1f);
                }
                return;
            }

            if (stateTimer < slamEnd)
            {
                if (animator != null) animator.SetInteger(AnimStateHash, (int)BossState.Attacking);
                float u = (stateTimer - hoverEnd) / (slamEnd - hoverEnd);
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(spawnPosition.y + 6.8f, spawnPosition.y, u * u);
                transform.position = pos;
                return;
            }

            transform.position = new Vector3(transform.position.x, spawnPosition.y, transform.position.z);
            if (!actionExecuted)
            {
                actionExecuted = true;
                if (slamTelegraphSr != null) slamTelegraphSr.enabled = false;
                ExecuteMeteorImpact();
            }

            if (stateTimer >= slamEnd + 0.22f)
            {
                MaybeCrackWardFromExertion();
                FinishAttackCycle();
            }
        }

        private void ExecuteMeteorImpact()
        {
            if (slamParticles != null)
            {
                slamParticles.transform.position = transform.position + new Vector3(0f, 0.15f, 0f);
                slamParticles.Play();
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.32f, 0.42f);
                CameraController.Instance.TriggerZoomPunch(-0.35f, 0.18f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHardLand();
                AudioManager.Instance.PlayBossRoar();
            }

            if (playerTransform != null)
            {
                float dx = Mathf.Abs(playerTransform.position.x - transform.position.x);
                float dy = playerTransform.position.y - spawnPosition.y;
                if (dx <= 2.35f && dy >= -0.4f && dy <= 2.1f)
                {
                    PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        playerDamagedDuringAttack = true;
                        health.TakeDamage(meleeDamage + 2, transform.position);
                    }
                }
            }

            SpawnShockwave(1f, 14.5f);
            SpawnShockwave(-1f, 14.5f);
        }

        private void SpawnAfterimage()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            GameObject ghost = new GameObject("BossDashGhost");
            ghost.transform.position = transform.position;
            ghost.transform.localScale = transform.localScale;
            SpriteRenderer gSr = ghost.AddComponent<SpriteRenderer>();
            gSr.sprite = spriteRenderer.sprite;
            gSr.sortingOrder = spriteRenderer.sortingOrder - 1;
            gSr.color = new Color(1f, 0.22f, 0.45f, 0.58f);
            ghost.AddComponent<BossGhostFade>();
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
            if (!actionExecuted && stateTimer >= 0.38f)
            {
                actionExecuted = true;
                if (activeCastPattern == AttackPattern.EclipseOrbFan)
                {
                    ExecuteEclipseOrbFan();
                }
                else
                {
                    if (spearStormRoutine != null) StopCoroutine(spearStormRoutine);
                    spearStormRoutine = StartCoroutine(RollingSkySpearStormRoutine());
                }
            }

            if (stateTimer >= 0.96f)
            {
                FinishAttackCycle();
            }
        }

        private IEnumerator RollingSkySpearStormRoutine()
        {
            int spearCount = currentPhase >= 3 ? 7 : 5;
            bool leftToRight = (attackRotationIndex % 2) == 0;
            for (int i = 0; i < spearCount; i++)
            {
                if (defeated || !arenaEngaged) yield break;

                float norm = (i + 0.5f) / spearCount;
                if (!leftToRight) norm = 1f - norm;
                float x = Mathf.Lerp(minPatrolX, maxPatrolX, norm);
                SpawnSkyStrike(x);

                if (AudioManager.Instance != null && i % 2 == 0)
                {
                    AudioManager.Instance.PlayZap();
                }
                yield return new WaitForSeconds(0.14f);
            }
            spearStormRoutine = null;
        }

        private void ExecuteEclipseOrbFan()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayZap();
            if (CameraController.Instance != null) CameraController.Instance.TriggerShake(0.18f, 0.22f);

            int orbCount = currentPhase >= 3 ? 5 : 4;
            Vector2 origin = (Vector2)transform.position + new Vector2(facingDirection * 0.95f, 1.85f);
            Vector2 aimDir = playerTransform != null
                ? ((Vector2)playerTransform.position + new Vector2(0f, 0.7f) - origin).normalized
                : new Vector2(facingDirection, 0f);
            float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x);

            for (int i = 0; i < orbCount; i++)
            {
                float offsetIdx = i - (orbCount - 1) * 0.5f;
                float ang = baseAngle + offsetIdx * 0.19f;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (currentPhase >= 3 ? 10.6f : 9.4f);
                SpawnEclipseOrb(origin, vel);
            }
        }

        private void SpawnSkyStrike(float targetX)
        {
            GameObject go = new GameObject("BossSkyStrike");
            go.transform.position = new Vector3(targetX, spawnPosition.y + 3.0f, 0f);
            BossSkyStrike strike = go.AddComponent<BossSkyStrike>();
            strike.Initialize(skyBeamWhiteSprite, skySpearSprite, currentPhase >= 3 ? 25 : 22, currentPhase >= 3 ? 0.62f : 0.72f);
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
                1 => 0.62f,
                2 => 0.42f,
                _ => 0.26f
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
                attackCooldownTimer = 0.45f;
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

            bool comingFromAbove = playerRb != null && playerRb.linearVelocity.y <= 0.8f &&
                                   other.bounds.min.y >= transform.position.y + 1.15f;
            if (comingFromAbove && player != null && currentState != BossState.Leaping)
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
                        TriggerWardShatterStun();
                        return;
                    }
                }

                RefreshBossHud(true);
                if (armorWarningCooldown <= 0f)
                {
                    armorWarningCooldown = 1.8f;
                    if (UIManager.Instance != null) UIManager.Instance.ShowBanner("ZAP WARD [CLICK / E]", 1.5f);
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

        private void TriggerWardShatterStun()
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
            if (UIManager.Instance != null) UIManager.Instance.ShowBanner("WARD BROKEN — STOMP!", 1.8f);
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
                if (!EditorBypassHitIcd && stunBoltCooldownTimer > 0f)
                {
                    ApplyBossDamage(1);
                    return;
                }
                stunBoltCooldownTimer = stunBoltIcd;

                if (GameFeelManager.Instance != null)
                {
                    GameFeelManager.Instance.TriggerHitStop(0.032f);
                    GameFeelManager.Instance.SpawnFloatingText(hitPos + new Vector2(0f, 0.45f), $"-{lightningStunCritDamage}", new Color(1f, 0.90f, 0.42f, 1f), 1.05f);
                }
                ApplyBossDamage(lightningStunCritDamage);
                return;
            }

            // Anti click-spam Ward ICD: excess rapid clicks chip 1 HP cleanly without stripping extra Ward pips in 0.36s
            if (!EditorBypassHitIcd && wardHitCooldownTimer > 0f)
            {
                ApplyBossDamage(1);
                return;
            }
            wardHitCooldownTimer = wardHitIcd;

            bool willBreak = currentWardPips == 1;
            int totalBoltDmg = lightningWardChipDamage + (willBreak ? lightningWardShatterBonus : 0);
            if (GameFeelManager.Instance != null)
            {
                GameFeelManager.Instance.TriggerHitStop(willBreak ? 0.08f : 0.025f);
                GameFeelManager.Instance.SpawnFloatingText(
                    hitPos + new Vector2(0f, 0.45f),
                    $"-{totalBoltDmg}",
                    willBreak ? new Color(1f, 0.90f, 0.42f, 1f) : new Color(0.45f, 0.98f, 1f, 1f),
                    willBreak ? 1.15f : 0.90f
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
                    TriggerWardShatterStun();
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
                CameraController.Instance.TriggerShake(0.28f, 0.36f);
                CameraController.Instance.TriggerZoomPunch(-0.6f, 0.24f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWardShatter();

            int bonusDmg = (GameManager.Instance != null && GameManager.Instance.HasCrownBreakerPower) ? 8 : 0;
            int totalDmg = stompCrownDamage + bonusDmg;
            if (GameFeelManager.Instance != null)
            {
                GameFeelManager.Instance.TriggerHitStop(0.085f);
                GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 2.9f, 0f), $"-{totalDmg} CRIT!", new Color(1f, 0.90f, 0.36f, 1f), 1.45f);
            }
            ApplyBossDamage(totalDmg);
            if (defeated)
            {
                return;
            }

            currentWardPips = GetMaxWardPipsForPhase(currentPhase);
            UpdateWardVisual();
            attackCooldownTimer = 0.45f;
            SetState(BossState.Wandering);
        }

        private void ApplyBossDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, amount));
            hitFlashTimer = 0.20f;

            if (currentHealth <= 0)
            {
                DefeatBoss();
                return;
            }

            int nextPhase = currentHealth <= 74 ? 3 : currentHealth <= 149 ? 2 : 1;
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
            attackCooldownTimer = 0.72f;
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
            if (CameraController.Instance != null) CameraController.Instance.TriggerShake(0.38f, 0.44f);

            if (UIManager.Instance != null)
            {
                if (currentPhase >= 3)
                {
                    UIManager.Instance.FlashDamageVignette(0.45f);
                }
                string msg = currentPhase >= 3 ? "PHASE III — NIGHTMARE!" : "PHASE II — ENRAGED!";
                UIManager.Instance.ShowBanner(msg, 2.2f);
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
            StopAllBossCoroutines();
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
                GameFeelManager.Instance.TriggerHitStop(0.24f, 0.08f);
                GameFeelManager.Instance.SpawnFloatingText(transform.position + new Vector3(0f, 2.8f, 0f), "ECLIPSE BROKEN!", new Color(1f, 0.90f, 0.40f, 1f), 1.6f);
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
                UIManager.Instance.UpdateBossHud(false, 0, maxHealth, 0, 5, currentPhase, string.Empty);
                UIManager.Instance.ShowBanner("MALAKOR FALLS — IGNITE BEACON!", 3.2f);
            }
        }

        public void ResetForRetry()
        {
            HideAllTelegraphs();
            StopAllBossCoroutines();
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
            attackCooldownTimer = 0.62f;
            wardHitCooldownTimer = 0f;
            stunBoltCooldownTimer = 0f;
            armorWarningCooldown = 0f;
            postStompSafeTimer = 0f;
            comboChainRemaining = 0;
            dashSubCount = 0;
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

        private void StopAllBossCoroutines()
        {
            if (spearStormRoutine != null)
            {
                StopCoroutine(spearStormRoutine);
                spearStormRoutine = null;
            }
        }

        private void SetState(BossState nextState)
        {
            HideAllTelegraphs();
            currentState = nextState;
            stateTimer = 0f;
            actionExecuted = false;
            secondaryCuePlayed = false;
            playerDamagedDuringAttack = false;

            if (arenaEngaged && Application.isPlaying && AudioManager.Instance != null &&
                (nextState == BossState.Attacking || nextState == BossState.Dashing || nextState == BossState.Casting || nextState == BossState.Leaping))
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
                int animInt = nextState switch
                {
                    BossState.Intro => (int)BossState.Casting,
                    BossState.Leaping => (int)BossState.Dashing,
                    _ => (int)nextState
                };
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
                BossState.Stunned => "STUNNED — STOMP CROWN!",
                BossState.Attacking => "HALBERD SLAM",
                BossState.Dashing => "PHANTOM CROSS-DASH",
                BossState.Leaping => "METEOR LEAP SLAM",
                BossState.Casting => activeCastPattern == AttackPattern.EclipseOrbFan ? "ECLIPSE ORB BARRAGE" : "SKY-SPEAR STORM",
                _ => currentWardPips > 0 ? "BREAK WARD WITH ⚡" : "STUN READY"
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
                    BossState.Leaping => attackPreviewSprite != null ? attackPreviewSprite : spriteRenderer.sprite,
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

    public class BossGhostFade : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Color baseColor;
        private float elapsed;
        private const float Duration = 0.24f;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) baseColor = sr.color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / Duration);
            if (sr != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, u);
                sr.color = c;
            }
            if (u >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
