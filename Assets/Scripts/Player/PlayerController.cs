using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VesperTwilightCourier
{
    /// <summary>
    /// Controls Vesper's responsive platforming movement (coyote time, jump buffer, variable jump height),
    /// her signature Shadow-Cloak Glide (slow descent + Updraft soaring), animation parameters, and Particle Systems.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Horizontal Movement")]
        public float moveSpeed = 8.5f;
        public float groundAcceleration = 68f;
        public float airAcceleration = 46f;
        public float glideSpeedMultiplier = 1.2f;

        [Header("Jump & Responsiveness")]
        public float jumpForce = 14.5f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;
        public float maxFallSpeed = -20f;

        [Header("Signature Move: Shadow-Cloak Glide")]
        public float glideFallSpeed = -2.2f;
        public float updraftAcceleration = 58f;

        [Header("Ground Detection")]
        public Vector2 groundCheckOffset = new Vector2(0f, -0.02f);
        public Vector2 groundCheckSize = new Vector2(0.56f, 0.16f);

        [Header("Particle Systems")]
        public ParticleSystem jumpDustParticles;
        public ParticleSystem landingDustParticles;
        public ParticleSystem glideTrailParticles;
        public ParticleSystem aetherFlashParticles;

        [Header("Combat")]
        public Sprite lightningBoltSprite;
        public float lightningCooldown = 0.20f;

        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded => isGrounded;
        public bool IsGliding => isGliding;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private PlayerHealth playerHealth;
        private float baseGravityScale = 3.2f;
        private float baseMoveSpeed = 8.5f;
        private float updraftLiftMultiplier = 1f;

        private float horizontalInput;
        private bool jumpPressedThisFrame;
        private bool jumpHeld;
        private bool glideButtonHeld;
        private bool jumpHoldEligibleForGlide;
        private bool aetherFlashPressedThisFrame;
        private float fireCooldownTimer;

        private bool isGrounded;
        private bool wasGrounded;
        private bool isGliding;
        private bool wasGliding;
        private bool wasInUpdraftLift;

        private float coyoteTimer;
        private float jumpBufferTimer;
        private float knockbackLockTimer;
        private float footstepDustTimer;
        private float peakFallSpeed;
        private Vector2 storedLiftVelocity;
        private float liftMomentumTimer;
        private float fireBufferTimer;
        private float skidDustCooldown;

        private readonly List<UpdraftVent> activeUpdrafts = new List<UpdraftVent>();
        private readonly Collider2D[] groundOverlapBuffer = new Collider2D[12];
        private readonly Collider2D[] updraftOverlapBuffer = new Collider2D[8];

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VerticalVelHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsGlidingHash = Animator.StringToHash("IsGliding");
        private static readonly int IsHurtHash = Animator.StringToHash("IsHurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

#if UNITY_EDITOR
        public static bool DemoInputOverrideActive;
        public static float DemoMoveX;
        public static bool DemoJumpDown;
        public static bool DemoJumpHeld;
        public static bool DemoGlideHeld;
        public static bool DemoFlashDown;
#endif

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerHealth = GetComponent<PlayerHealth>();
            baseGravityScale = rb != null && rb.gravityScale > 0.01f ? rb.gravityScale : 3.2f;
            baseMoveSpeed = moveSpeed;
        }

        private void Start()
        {
            if (glideTrailParticles != null)
            {
                glideTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                horizontalInput = 0f;
                jumpHeld = false;
                glideButtonHeld = false;
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.unscaledDeltaTime * 10f);
                UpdateAnimator(0f);
                return;
            }

            if (playerHealth != null && playerHealth.IsDead)
            {
                horizontalInput = 0f;
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);
                UpdateAnimator(0f);
                return;
            }

            ReadInput();
            if (fireCooldownTimer > 0f)
            {
                fireCooldownTimer -= Time.deltaTime;
            }
            if (aetherFlashPressedThisFrame)
            {
                fireBufferTimer = 0.10f;
            }
            if (fireBufferTimer > 0f && fireCooldownTimer <= 0f)
            {
                fireBufferTimer = 0f;
                TriggerAetherFlash();
            }
            UpdateTimers();
            HandleSpriteFacing();
            HandleFootstepDust();
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);
            UpdateAnimator(Mathf.Abs(rb.linearVelocity.x));

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateGlideStatus(isGliding, !isGrounded);
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                rb.gravityScale = baseGravityScale;
                wasInUpdraftLift = false;
                return;
            }

            if (playerHealth != null && playerHealth.IsDead)
            {
                rb.gravityScale = baseGravityScale;
                wasInUpdraftLift = false;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            CheckGround();

            if (knockbackLockTimer > 0f)
            {
                knockbackLockTimer -= Time.fixedDeltaTime;
                rb.gravityScale = baseGravityScale;
                wasInUpdraftLift = false;
                SetGlidingState(false);
                return;
            }

            // Execute buffered / coyote jump
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                ExecuteJump();
            }

            float currentUpdraftSpeed = GetActiveUpdraftSpeed();
            bool inUpdraft = currentUpdraftSpeed > 0f;

            // Variable jump height cut when jump key is released early while ascending
            if (!jumpHeld && !isGliding && rb.linearVelocity.y > 0f && !inUpdraft)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }

            // Determine Shadow-Cloak Glide state (works seamlessly whether entering updraft fresh or already gliding!)
            bool wantsGlide = !isGrounded && (glideButtonHeld || (jumpHeld && (jumpHoldEligibleForGlide || inUpdraft)));
            bool canGlide = wantsGlide && (rb.linearVelocity.y <= 0.35f || inUpdraft || glideButtonHeld);
            SetGlidingState(canGlide);

            // Horizontal acceleration
            float maxSpeed = isGliding ? moveSpeed * glideSpeedMultiplier : moveSpeed;
            float targetVelX = horizontalInput * maxSpeed;
            float accel = isGrounded ? groundAcceleration : airAcceleration;
            float nextVelX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelX, accel * Time.fixedDeltaTime);
            float nextVelY = rb.linearVelocity.y;

            bool isUpdraftLifting = isGliding && inUpdraft;
            if (isUpdraftLifting)
            {
                float boostedUpdraftSpeed = currentUpdraftSpeed * updraftLiftMultiplier;
                // Suspend gravity inside thermal updraft columns so glide lift is never cancelled by gravityScale
                rb.gravityScale = 0f;
                if (nextVelY < 2.8f)
                {
                    nextVelY = Mathf.MoveTowards(nextVelY, Mathf.Max(2.8f, boostedUpdraftSpeed), updraftAcceleration * 2.2f * Time.fixedDeltaTime);
                }
                else
                {
                    nextVelY = Mathf.MoveTowards(nextVelY, boostedUpdraftSpeed, updraftAcceleration * Time.fixedDeltaTime);
                }

                if (!wasInUpdraftLift)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayGlide();
                    }
                    if (jumpDustParticles != null)
                    {
                        jumpDustParticles.Emit(6);
                    }
                }
            }
            else
            {
                // Celeste-style halved gravity near jump apex while holding Jump for smoother air readability
                bool nearApex = !isGrounded && !isGliding && jumpHeld && Mathf.Abs(nextVelY) < 2.25f;
                rb.gravityScale = nearApex ? baseGravityScale * 0.52f : baseGravityScale;
                if (isGliding)
                {
                    if (nextVelY < glideFallSpeed)
                    {
                        nextVelY = glideFallSpeed;
                    }
                }
                else if (nextVelY < maxFallSpeed)
                {
                    nextVelY = maxFallSpeed;
                }
            }

            if (!isGrounded)
            {
                peakFallSpeed = Mathf.Min(peakFallSpeed, nextVelY);
                if (nextVelY > 0.6f)
                {
                    TryJumpCornerCorrection();
                }
            }
            else if (Mathf.Abs(horizontalInput) > 0.1f && horizontalInput * rb.linearVelocity.x < -3.5f && skidDustCooldown <= 0f)
            {
                skidDustCooldown = 0.18f;
                transform.localScale = new Vector3(1.08f, 0.94f, 1f);
                if (landingDustParticles != null)
                {
                    landingDustParticles.Emit(4);
                }
            }

            wasInUpdraftLift = isUpdraftLifting;
            rb.linearVelocity = new Vector2(nextVelX, nextVelY);
        }

        private void ReadInput()
        {
#if UNITY_EDITOR
            if (DemoInputOverrideActive)
            {
                horizontalInput = Mathf.Clamp(DemoMoveX, -1f, 1f);
                jumpPressedThisFrame = DemoJumpDown;
                jumpHeld = DemoJumpHeld;
                glideButtonHeld = DemoGlideHeld;
                aetherFlashPressedThisFrame = DemoFlashDown;
                DemoJumpDown = false;
                DemoFlashDown = false;

                if (jumpPressedThisFrame)
                {
                    jumpBufferTimer = jumpBufferTime;
                }
                if (!jumpHeld && !isGrounded)
                {
                    jumpHoldEligibleForGlide = true;
                }
                return;
            }
#endif

            float moveX = 0f;
            bool jumpDown = false;
            bool jumpCurrentlyHeld = false;
            bool glideShiftHeld = false;
            bool flashDown = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1f;

                jumpDown = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                jumpCurrentlyHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
                glideShiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                flashDown = kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                bool overUi = IsPointerOverUi();
                flashDown |= mouse.rightButton.wasPressedThisFrame || (mouse.leftButton.wasPressedThisFrame && !overUi);
            }

            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                float stickX = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.2f)
                {
                    moveX = Mathf.Clamp(stickX, -1f, 1f);
                }
                jumpDown |= gp.buttonSouth.wasPressedThisFrame;
                jumpCurrentlyHeld |= gp.buttonSouth.isPressed;
                glideShiftHeld |= gp.rightTrigger.isPressed || gp.rightShoulder.isPressed;
                flashDown |= gp.buttonWest.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            moveX = Input.GetAxisRaw("Horizontal");
            jumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            jumpCurrentlyHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            glideShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            flashDown = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) ||
                        Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && !IsPointerOverUi());
#endif

            horizontalInput = Mathf.Clamp(moveX, -1f, 1f);
            jumpPressedThisFrame = jumpDown;
            jumpHeld = jumpCurrentlyHeld;
            glideButtonHeld = glideShiftHeld;
            aetherFlashPressedThisFrame = flashDown;

            if (jumpPressedThisFrame)
            {
                jumpBufferTimer = jumpBufferTime;
            }

            // Releasing jump once in the air makes subsequent jump holds immediately engage glide
            if (!jumpHeld && !isGrounded)
            {
                jumpHoldEligibleForGlide = true;
            }
        }

        private void UpdateTimers()
        {
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;
            }
            if (liftMomentumTimer > 0f)
            {
                liftMomentumTimer -= Time.deltaTime;
            }
            if (fireBufferTimer > 0f)
            {
                fireBufferTimer -= Time.deltaTime;
            }
            if (skidDustCooldown > 0f)
            {
                skidDustCooldown -= Time.deltaTime;
            }

            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
                jumpHoldEligibleForGlide = false;
            }
            else if (coyoteTimer > 0f)
            {
                coyoteTimer -= Time.deltaTime;
            }
        }

        private void CheckGround()
        {
            wasGrounded = isGrounded;
            isGrounded = false;

            Vector2 center = (Vector2)transform.position + groundCheckOffset;
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            int count = Physics2D.OverlapBox(center, groundCheckSize, 0f, filter, groundOverlapBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = groundOverlapBuffer[i];
                if (col == null || col.isTrigger || col.gameObject == gameObject)
                {
                    continue;
                }

                if (col.GetComponent<Hazard>() != null ||
                    col.GetComponent<PatrolEnemy>() != null ||
                    col.GetComponent<SwoopEnemy>() != null ||
                    col.GetComponent<AetherShooterEnemy>() != null ||
                    col.GetComponent<FinalBossAI>() != null)
                {
                    continue;
                }

                if (rb.linearVelocity.y <= 0.15f)
                {
                    isGrounded = true;
                    MovingPlatform mp = col.GetComponentInParent<MovingPlatform>();
                    if (mp != null)
                    {
                        storedLiftVelocity = mp.CurrentVelocity;
                        liftMomentumTimer = 0.14f;
                    }
                    break;
                }
            }

            if (isGrounded && !wasGrounded)
            {
                SetGlidingState(false);
                bool hardLanding = peakFallSpeed < -13.5f;
                if (hardLanding)
                {
                    transform.localScale = new Vector3(1.26f, 0.76f, 1f);
                    if (landingDustParticles != null)
                    {
                        landingDustParticles.Emit(16);
                    }
                    if (CameraController.Instance != null)
                    {
                        CameraController.Instance.TriggerShake(0.08f, 0.10f);
                    }
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayHardLand();
                    }
                }
                else
                {
                    transform.localScale = new Vector3(1.14f, 0.86f, 1f);
                    if (landingDustParticles != null)
                    {
                        landingDustParticles.Play();
                    }
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayLand();
                    }
                }
            }

            if (isGrounded)
            {
                peakFallSpeed = 0f;
            }
        }

        private void ExecuteJump()
        {
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            isGrounded = false;
            jumpHoldEligibleForGlide = true;

            Vector2 jumpVel = new Vector2(rb.linearVelocity.x, jumpForce);
            if (liftMomentumTimer > 0f)
            {
                jumpVel.x += Mathf.Clamp(storedLiftVelocity.x * 0.72f, -5.2f, 5.2f);
                jumpVel.y += Mathf.Clamp(storedLiftVelocity.y * 0.45f, 0f, 3.8f);
                liftMomentumTimer = 0f;
            }
            rb.linearVelocity = jumpVel;
            transform.localScale = new Vector3(0.90f, 1.12f, 1f);

            if (jumpDustParticles != null)
            {
                jumpDustParticles.Play();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJump();
            }
        }

        private void TryJumpCornerCorrection()
        {
            float headY = transform.position.y + 1.58f;
            bool leftOuterBlocked = HasSolidAtPoint(new Vector2(transform.position.x - 0.31f, headY));
            bool leftInnerClear = !HasSolidAtPoint(new Vector2(transform.position.x - 0.12f, headY));
            if (leftOuterBlocked && leftInnerClear)
            {
                rb.position += new Vector2(0.28f, 0f);
                return;
            }

            bool rightOuterBlocked = HasSolidAtPoint(new Vector2(transform.position.x + 0.31f, headY));
            bool rightInnerClear = !HasSolidAtPoint(new Vector2(transform.position.x + 0.12f, headY));
            if (rightOuterBlocked && rightInnerClear)
            {
                rb.position -= new Vector2(0.28f, 0f);
            }
        }

        private bool HasSolidAtPoint(Vector2 center)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            int count = Physics2D.OverlapBox(center, new Vector2(0.10f, 0.14f), 0f, filter, groundOverlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = groundOverlapBuffer[i];
                if (col != null && !col.isTrigger && col.gameObject != gameObject)
                {
                    return true;
                }
            }
            return false;
        }

        public void Bounce(float upwardSpeed)
        {
            isGrounded = false;
            coyoteTimer = 0f;
            jumpHoldEligibleForGlide = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, upwardSpeed);
            transform.localScale = new Vector3(0.90f, 1.12f, 1f);

            if (jumpDustParticles != null)
            {
                jumpDustParticles.Play();
            }
        }

        public void ApplyKnockback(Vector2 impulse, float lockDuration = 0.2f)
        {
            knockbackLockTimer = lockDuration;
            SetGlidingState(false);
            rb.linearVelocity = impulse;
        }

        public void ResetMotionAndTimers()
        {
            rb.gravityScale = baseGravityScale;
            rb.linearVelocity = Vector2.zero;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            knockbackLockTimer = 0f;
            footstepDustTimer = 0.08f;
            wasInUpdraftLift = false;
            transform.localScale = Vector3.one;
            activeUpdrafts.Clear();
            SetGlidingState(false);
        }

        public void ApplyLightTier(int tier)
        {
            moveSpeed = tier >= 1 ? baseMoveSpeed * 1.12f : baseMoveSpeed;
            if (tier >= 2)
            {
                glideFallSpeed = -1.55f;
                updraftLiftMultiplier = 1.20f;
            }
            else
            {
                glideFallSpeed = -2.2f;
                updraftLiftMultiplier = 1f;
            }
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void TriggerAetherFlash()
        {
            if (fireCooldownTimer > 0f)
            {
                return;
            }

            if (GameManager.Instance == null || !GameManager.Instance.TrySpendLightningAmmo())
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBanner("NO LIGHTNING", 1.0f);
                }
                return;
            }

            fireCooldownTimer = lightningCooldown;

            if (aetherFlashParticles != null)
            {
                aetherFlashParticles.Play();
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayZap();
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.08f, 0.12f);
            }

            Vector3 spawnPos = transform.position + new Vector3(FacingDirection * 0.72f, 0.96f, 0f);
            Vector2? smartAim = FindSmartLightningAimDirection(spawnPos, FacingDirection);

            GameObject boltGo = new GameObject("PlayerLightningBolt");
            boltGo.transform.position = spawnPos;
            SpriteRenderer sr = boltGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;
            BoxCollider2D col = boltGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.25f, 0.65f);

            Rigidbody2D boltRb = boltGo.AddComponent<Rigidbody2D>();
            boltRb.bodyType = RigidbodyType2D.Kinematic;
            boltRb.gravityScale = 0f;
            boltRb.useFullKinematicContacts = true;

            PlayerLightningBolt bolt = boltGo.AddComponent<PlayerLightningBolt>();
            bolt.Initialize(FacingDirection, lightningBoltSprite, smartAim);
        }

        private static Vector2? FindSmartLightningAimDirection(Vector2 origin, float facingDir)
        {
            Vector2 scanCenter = origin + new Vector2(facingDir * 7.5f, 0f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(scanCenter, new Vector2(15.0f, 8.4f), 0f);
            float bestDistSq = float.MaxValue;
            Vector2? bestDir = null;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col == null)
                {
                    continue;
                }

                bool validTarget = col.GetComponentInParent<FinalBossAI>() != null ||
                                   col.GetComponentInParent<SwoopEnemy>() != null ||
                                   col.GetComponentInParent<AetherShooterEnemy>() != null ||
                                   col.GetComponentInParent<PatrolEnemy>() != null;
                if (!validTarget)
                {
                    continue;
                }

                Vector2 targetPos = col.bounds.center;
                Vector2 delta = targetPos - origin;
                if (delta.x * facingDir <= 0.1f)
                {
                    continue;
                }

                float angleDeg = Mathf.Abs(Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg);
                if (angleDeg <= 20.0f)
                {
                    float dSq = delta.sqrMagnitude;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestDir = delta.normalized;
                    }
                }
            }

            return bestDir;
        }

        private void HandleFootstepDust()
        {
            if (!isGrounded || Mathf.Abs(rb.linearVelocity.x) <= 2.0f)
            {
                footstepDustTimer = 0.08f;
                return;
            }

            footstepDustTimer -= Time.deltaTime;
            if (footstepDustTimer <= 0f)
            {
                footstepDustTimer = 0.26f;
                if (landingDustParticles != null)
                {
                    landingDustParticles.Emit(2);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFootstep();
                }
            }
        }

        public void RegisterUpdraft(UpdraftVent vent)
        {
            if (vent != null && !activeUpdrafts.Contains(vent))
            {
                activeUpdrafts.Add(vent);
            }
        }

        public void UnregisterUpdraft(UpdraftVent vent)
        {
            if (vent != null)
            {
                activeUpdrafts.Remove(vent);
            }
        }

        private float GetActiveUpdraftSpeed()
        {
            float maxSpeed = 0f;
            for (int i = activeUpdrafts.Count - 1; i >= 0; i--)
            {
                if (activeUpdrafts[i] == null)
                {
                    activeUpdrafts.RemoveAt(i);
                    continue;
                }

                if (activeUpdrafts[i].updraftSpeed > maxSpeed)
                {
                    maxSpeed = activeUpdrafts[i].updraftSpeed;
                }
            }

            // Live OverlapBox trigger scan guarantees updraft detection even if entered mid-glide or after spawn
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            Vector2 bodyCenter = (Vector2)transform.position + new Vector2(0f, 0.78f);
            int overlapCount = Physics2D.OverlapBox(bodyCenter, new Vector2(0.76f, 1.56f), 0f, filter, updraftOverlapBuffer);
            for (int i = 0; i < overlapCount; i++)
            {
                Collider2D col = updraftOverlapBuffer[i];
                if (col == null)
                {
                    continue;
                }

                UpdraftVent vent = col.GetComponent<UpdraftVent>();
                if (vent != null && vent.updraftSpeed > maxSpeed)
                {
                    maxSpeed = vent.updraftSpeed;
                }
            }

            return maxSpeed;
        }

        private void SetGlidingState(bool gliding)
        {
            wasGliding = isGliding;
            isGliding = gliding;

            if (isGliding && !wasGliding)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayGlide();
                }

                if (glideTrailParticles != null)
                {
                    glideTrailParticles.Play();
                }
            }
            else if (!isGliding && wasGliding)
            {
                if (glideTrailParticles != null)
                {
                    glideTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void HandleSpriteFacing()
        {
            if (horizontalInput > 0.05f)
            {
                FacingDirection = 1f;
                spriteRenderer.flipX = false;
            }
            else if (horizontalInput < -0.05f)
            {
                FacingDirection = -1f;
                spriteRenderer.flipX = true;
            }
        }

        private void UpdateAnimator(float horizontalSpeed)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedHash, horizontalSpeed);
            animator.SetFloat(VerticalVelHash, rb.linearVelocity.y);
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetBool(IsGlidingHash, isGliding);
            animator.SetBool(IsHurtHash, playerHealth != null && playerHealth.IsHurting);
            animator.SetBool(IsDeadHash, playerHealth != null && playerHealth.IsDead);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);
        }
    }
}
