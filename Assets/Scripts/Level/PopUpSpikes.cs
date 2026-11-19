using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Ground-embedded retractable spike trap concealed inside a recessed floor grate
    /// using a Unity SpriteMask. Supports ProximityAmbush (pops up with a brief spark
    /// telegraph when Vesper approaches/crosses over) and TimedCycle (staggered surges)
    /// to make rooftop traversal tense and unpredictable.
    /// </summary>
    public class PopUpSpikes : MonoBehaviour
    {
        public enum TrapMode
        {
            ProximityAmbush,
            TimedCycle
        }

        private enum TrapState
        {
            Retracted,
            Telegraph,
            Extending,
            Extended,
            Retracting,
            Cooldown
        }

        [Header("Trap Behavior")]
        public TrapMode mode = TrapMode.ProximityAmbush;
        public int damageAmount = 1;
        public float triggerHorizontalRange = 2.6f;
        public float triggerVerticalRange = 2.4f;

        [Header("Timing")]
        public float telegraphDuration = 0.24f;
        public float extendDuration = 0.08f;
        public float holdExtendedDuration = 1.05f;
        public float retractDuration = 0.34f;
        public float cooldownDuration = 1.35f;
        [Tooltip("Used by TimedCycle mode to stagger adjacent traps out of phase.")]
        public float timedRetractedDuration = 1.65f;
        public float cycleOffset = 0f;

        [Header("Visual & Mask Alignment")]
        public Transform spikeVisual;
        public float retractedLocalY = -0.92f;
        public float telegraphLocalY = -0.68f;
        public float extendedLocalY = -0.14f;

        [Header("Lethal Hitbox (Local Space)")]
        public Vector2 lethalBoxOffset = new Vector2(0f, 0.34f);
        public Vector2 lethalBoxSize = new Vector2(1.42f, 0.56f);

        [Header("VFX")]
        public ParticleSystem sparkParticles;

        private TrapState state = TrapState.Retracted;
        private float stateTimer;
        private Transform playerTransform;
        private readonly Collider2D[] overlapBuffer = new Collider2D[6];

        private void Start()
        {
            FindPlayer();
            if (mode == TrapMode.TimedCycle)
            {
                AdvanceTimedCycle(cycleOffset);
            }
            else
            {
                state = TrapState.Retracted;
                stateTimer = 0f;
                ApplySpikeOffset(retractedLocalY);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (playerTransform == null)
            {
                FindPlayer();
            }

            float dt = Time.deltaTime;
            stateTimer += dt;

            switch (state)
            {
                case TrapState.Retracted:
                    ApplySpikeOffset(retractedLocalY);
                    if (mode == TrapMode.ProximityAmbush)
                    {
                        if (IsPlayerInAmbushZone())
                        {
                            EnterTelegraph();
                        }
                    }
                    else if (stateTimer >= timedRetractedDuration)
                    {
                        EnterTelegraph();
                    }
                    break;

                case TrapState.Telegraph:
                    {
                        float t = Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, telegraphDuration));
                        float jitterX = Mathf.Sin(stateTimer * 85f) * 0.035f;
                        float y = Mathf.Lerp(retractedLocalY, telegraphLocalY, t);
                        ApplySpikeOffset(y, jitterX);

                        if (stateTimer >= telegraphDuration)
                        {
                            EnterExtending();
                        }
                    }
                    break;

                case TrapState.Extending:
                    {
                        float t = Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, extendDuration));
                        // Snappy ease-out eruption
                        float eased = 1f - (1f - t) * (1f - t);
                        ApplySpikeOffset(Mathf.Lerp(telegraphLocalY, extendedLocalY, eased));

                        if (t > 0.35f)
                        {
                            CheckLethalOverlap();
                        }

                        if (stateTimer >= extendDuration)
                        {
                            state = TrapState.Extended;
                            stateTimer = 0f;
                            ApplySpikeOffset(extendedLocalY);
                        }
                    }
                    break;

                case TrapState.Extended:
                    ApplySpikeOffset(extendedLocalY);
                    CheckLethalOverlap();
                    if (stateTimer >= holdExtendedDuration)
                    {
                        state = TrapState.Retracting;
                        stateTimer = 0f;
                    }
                    break;

                case TrapState.Retracting:
                    {
                        float t = Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, retractDuration));
                        ApplySpikeOffset(Mathf.Lerp(extendedLocalY, retractedLocalY, t));
                        if (t < 0.35f)
                        {
                            CheckLethalOverlap();
                        }

                        if (stateTimer >= retractDuration)
                        {
                            state = mode == TrapMode.ProximityAmbush ? TrapState.Cooldown : TrapState.Retracted;
                            stateTimer = 0f;
                            ApplySpikeOffset(retractedLocalY);
                        }
                    }
                    break;

                case TrapState.Cooldown:
                    ApplySpikeOffset(retractedLocalY);
                    if (stateTimer >= cooldownDuration)
                    {
                        state = TrapState.Retracted;
                        stateTimer = 0f;
                    }
                    break;
            }
        }

        private void EnterTelegraph()
        {
            state = TrapState.Telegraph;
            stateTimer = 0f;
            if (sparkParticles != null)
            {
                sparkParticles.Emit(10);
                sparkParticles.Play();
            }
        }

        private void EnterExtending()
        {
            state = TrapState.Extending;
            stateTimer = 0f;
            if (sparkParticles != null)
            {
                sparkParticles.Emit(16);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLand();
            }
        }

        private bool IsPlayerInAmbushZone()
        {
            if (playerTransform == null)
            {
                return false;
            }

            Vector3 delta = playerTransform.position - transform.position;
            return Mathf.Abs(delta.x) <= triggerHorizontalRange
                && delta.y >= -0.6f
                && delta.y <= triggerVerticalRange;
        }

        private void CheckLethalOverlap()
        {
            Vector2 center = (Vector2)transform.position + lethalBoxOffset;
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            int count = Physics2D.OverlapBox(center, lethalBoxSize, 0f, filter, overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null || !col.CompareTag("Player"))
                {
                    continue;
                }

                PlayerHealth health = col.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damageAmount, transform.position);
                    break;
                }
            }
        }

        private void AdvanceTimedCycle(float offset)
        {
            float fullCycle = timedRetractedDuration + telegraphDuration + extendDuration + holdExtendedDuration + retractDuration;
            if (fullCycle <= 0.01f)
            {
                return;
            }

            float t = Mathf.Repeat(offset, fullCycle);
            if (t < timedRetractedDuration)
            {
                state = TrapState.Retracted;
                stateTimer = t;
                ApplySpikeOffset(retractedLocalY);
                return;
            }
            t -= timedRetractedDuration;

            if (t < telegraphDuration)
            {
                state = TrapState.Telegraph;
                stateTimer = t;
                ApplySpikeOffset(Mathf.Lerp(retractedLocalY, telegraphLocalY, t / Mathf.Max(0.01f, telegraphDuration)));
                return;
            }
            t -= telegraphDuration;

            if (t < extendDuration)
            {
                state = TrapState.Extending;
                stateTimer = t;
                ApplySpikeOffset(Mathf.Lerp(telegraphLocalY, extendedLocalY, t / Mathf.Max(0.01f, extendDuration)));
                return;
            }
            t -= extendDuration;

            if (t < holdExtendedDuration)
            {
                state = TrapState.Extended;
                stateTimer = t;
                ApplySpikeOffset(extendedLocalY);
                return;
            }
            t -= holdExtendedDuration;

            state = TrapState.Retracting;
            stateTimer = t;
            ApplySpikeOffset(Mathf.Lerp(extendedLocalY, retractedLocalY, t / Mathf.Max(0.01f, retractDuration)));
        }

        private void ApplySpikeOffset(float localY, float localX = 0f)
        {
            if (spikeVisual != null)
            {
                spikeVisual.localPosition = new Vector3(localX, localY, 0f);
            }
        }

        /// <summary>
        /// Sets the visual extension directly for editor previews / screenshots (0 = hidden, 1 = fully popped up).
        /// </summary>
        public void SetPreviewExtension(float normalized)
        {
            ApplySpikeOffset(Mathf.Lerp(retractedLocalY, extendedLocalY, Mathf.Clamp01(normalized)));
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.55f, 0.85f);
            Gizmos.DrawWireCube((Vector2)transform.position + lethalBoxOffset, lethalBoxSize);

            if (mode == TrapMode.ProximityAmbush)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.55f);
                Vector3 sensorCenter = transform.position + new Vector3(0f, (triggerVerticalRange - 0.6f) * 0.5f, 0f);
                Vector3 sensorSize = new Vector3(triggerHorizontalRange * 2f, triggerVerticalRange + 0.6f, 0.1f);
                Gizmos.DrawWireCube(sensorCenter, sensorSize);
            }
        }
    }
}
