using System.Collections.Generic;
using UnityEngine;
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

        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded => isGrounded;
        public bool IsGliding => isGliding;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private PlayerHealth playerHealth;
        private float baseGravityScale = 3.2f;

        private float horizontalInput;
        private bool jumpPressedThisFrame;
        private bool jumpHeld;
        private bool glideButtonHeld;
        private bool jumpHoldEligibleForGlide;

        private bool isGrounded;
        private bool wasGrounded;
        private bool isGliding;
        private bool wasGliding;
        private bool wasInUpdraftLift;

        private float coyoteTimer;
        private float jumpBufferTimer;
        private float knockbackLockTimer;
        private float footstepDustTimer;

        private readonly List<UpdraftVent> activeUpdrafts = new List<UpdraftVent>();
        private readonly Collider2D[] groundOverlapBuffer = new Collider2D[12];
        private readonly Collider2D[] updraftOverlapBuffer = new Collider2D[8];

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VerticalVelHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsGlidingHash = Animator.StringToHash("IsGliding");
        private static readonly int IsHurtHash = Animator.StringToHash("IsHurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerHealth = GetComponent<PlayerHealth>();
            baseGravityScale = rb != null && rb.gravityScale > 0.01f ? rb.gravityScale : 3.2f;
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
                // Suspend gravity inside thermal updraft columns so glide lift is never cancelled by gravityScale
                rb.gravityScale = 0f;
                if (nextVelY < 2.8f)
                {
                    nextVelY = Mathf.MoveTowards(nextVelY, Mathf.Max(2.8f, currentUpdraftSpeed), updraftAcceleration * 2.2f * Time.fixedDeltaTime);
                }
                else
                {
                    nextVelY = Mathf.MoveTowards(nextVelY, currentUpdraftSpeed, updraftAcceleration * Time.fixedDeltaTime);
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
                rb.gravityScale = baseGravityScale;
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

            wasInUpdraftLift = isUpdraftLifting;
            rb.linearVelocity = new Vector2(nextVelX, nextVelY);
        }

        private void ReadInput()
        {
            float moveX = 0f;
            bool jumpDown = false;
            bool jumpCurrentlyHeld = false;
            bool glideShiftHeld = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1f;

                jumpDown = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                jumpCurrentlyHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
                glideShiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
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
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            moveX = Input.GetAxisRaw("Horizontal");
            jumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            jumpCurrentlyHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            glideShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

            horizontalInput = Mathf.Clamp(moveX, -1f, 1f);
            jumpPressedThisFrame = jumpDown;
            jumpHeld = jumpCurrentlyHeld;
            glideButtonHeld = glideShiftHeld;

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
                    col.GetComponent<FinalBossAI>() != null)
                {
                    continue;
                }

                if (rb.linearVelocity.y <= 0.15f)
                {
                    isGrounded = true;
                    break;
                }
            }

            if (isGrounded && !wasGrounded)
            {
                SetGlidingState(false);
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

        private void ExecuteJump()
        {
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            isGrounded = false;
            jumpHoldEligibleForGlide = true;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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
