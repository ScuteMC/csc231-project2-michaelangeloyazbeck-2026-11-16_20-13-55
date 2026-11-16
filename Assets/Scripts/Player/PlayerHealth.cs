using System.Collections;
using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Tracks Vesper's health, handles invulnerability frames, crimson hit-flash feedback,
    /// hazard recoil, checkpoint respawning, and Game Over triggers.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public int maxHealth = 3;
        public float invulnerabilityDuration = 1.2f;
        public float hurtStateDuration = 0.25f;

        [Header("Feedback")]
        public Color hurtFlashColor = new Color(1f, 0.35f, 0.45f, 1f);
        public ParticleSystem hurtBurstParticles;

        public int CurrentHealth { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsHurting { get; private set; }
        public bool IsDead { get; private set; }

        private SpriteRenderer spriteRenderer;
        private PlayerController playerController;
        private Coroutine invulnRoutine;
        private Color baseColor = Color.white;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerController = GetComponent<PlayerController>();
            baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(CurrentHealth, maxHealth);
            }
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (IsInvulnerable || IsDead)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(1, amount));

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(CurrentHealth, maxHealth);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHurt();
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.24f, 0.36f);
            }

            if (hurtBurstParticles != null)
            {
                hurtBurstParticles.Play();
            }

            if (CurrentHealth <= 0)
            {
                Die();
                return;
            }

            float pushDir = transform.position.x >= sourcePosition.x ? 1f : -1f;
            playerController.ApplyKnockback(new Vector2(pushDir * 6.5f, 9f), 0.22f);

            if (invulnRoutine != null)
            {
                StopCoroutine(invulnRoutine);
            }
            invulnRoutine = StartCoroutine(InvulnerabilityFlashRoutine());
        }

        /// <summary>
        /// Triggered when falling into an abyss pit between rooftops.
        /// Deals 1 damage and teleports Vesper back to the last safe Watchtower Lantern.
        /// </summary>
        public void HandleAbyssFall()
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - 1);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(CurrentHealth, maxHealth);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHurt();
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.25f, 0.4f);
            }

            if (CurrentHealth <= 0)
            {
                Die();
                return;
            }

            Vector3 respawnPos = GameManager.Instance != null
                ? GameManager.Instance.CurrentRespawnPosition
                : transform.position;

            TeleportTo(respawnPos);

            if (invulnRoutine != null)
            {
                StopCoroutine(invulnRoutine);
            }
            invulnRoutine = StartCoroutine(InvulnerabilityFlashRoutine());
        }

        public void ReviveAtCheckpoint(Vector3 checkpointPosition)
        {
            if (invulnRoutine != null)
            {
                StopCoroutine(invulnRoutine);
            }

            IsDead = false;
            IsHurting = false;
            IsInvulnerable = false;
            CurrentHealth = maxHealth;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            TeleportTo(checkpointPosition);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(CurrentHealth, maxHealth);
            }
        }

        public void TeleportTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            if (playerController != null)
            {
                playerController.ResetMotionAndTimers();
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.SnapToTarget();
            }
        }

        private void Die()
        {
            IsDead = true;
            IsHurting = true;
            IsInvulnerable = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtFlashColor;
            }

            if (playerController != null)
            {
                playerController.ResetMotionAndTimers();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }

        private IEnumerator InvulnerabilityFlashRoutine()
        {
            IsInvulnerable = true;
            IsHurting = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtFlashColor;
            }

            yield return new WaitForSeconds(hurtStateDuration);
            IsHurting = false;

            float remaining = Mathf.Max(0f, invulnerabilityDuration - hurtStateDuration);
            float elapsed = 0f;
            bool visible = true;

            while (elapsed < remaining)
            {
                visible = !visible;
                if (spriteRenderer != null)
                {
                    Color blinkColor = baseColor;
                    blinkColor.a = visible ? 1f : 0.35f;
                    spriteRenderer.color = blinkColor;
                }

                yield return new WaitForSeconds(0.08f);
                elapsed += 0.08f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            IsInvulnerable = false;
            invulnRoutine = null;
        }
    }
}
