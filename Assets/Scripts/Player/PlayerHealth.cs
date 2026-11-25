using System.Collections;
using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Tracks Vesper's Health Points (0..100), handles invulnerability frames,
    /// crimson hit-flash, multi-life checkpoint respawns, and Game Over triggers.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public int maxHealth = 100;
        public int abyssFallDamage = 30;
        public float invulnerabilityDuration = 1.05f;
        public float hurtStateDuration = 0.25f;

        [Header("Feedback")]
        public Color hurtFlashColor = new Color(1f, 0.35f, 0.45f, 1f);
        public ParticleSystem hurtBurstParticles;
        public ParticleSystem aetherShieldParticles;
        public ParticleSystem aetherHealParticles;

        public int CurrentHealth { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsHurting { get; private set; }
        public bool IsDead { get; private set; }
        public bool HasAetherShield { get; private set; }

        private SpriteRenderer spriteRenderer;
        private PlayerController playerController;
        private Coroutine invulnRoutine;
        private Coroutine briefInvulnRoutine;
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

            if (HasAetherShield)
            {
                HasAetherShield = false;
                if (aetherShieldParticles != null)
                {
                    aetherShieldParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                if (aetherHealParticles != null)
                {
                    aetherHealParticles.Play();
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayCollect();
                }
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.TriggerShake(0.18f, 0.22f);
                }

                float shieldPushDir = transform.position.x >= sourcePosition.x ? 1f : -1f;
                if (playerController != null)
                {
                    playerController.ApplyKnockback(new Vector2(shieldPushDir * 5.2f, 7.0f), 0.16f);
                }

                GrantBriefInvulnerability(0.95f);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RefreshAetherLightHud();
                }
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBanner("SHIELD SAVED", 1.6f);
                }
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
                CameraController.Instance.TriggerShake(0.24f, 0.34f);
            }

            if (hurtBurstParticles != null)
            {
                hurtBurstParticles.Play();
            }

            if (CurrentHealth <= 0)
            {
                HandleZeroHealth();
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
        /// Deals abyssFallDamage HP and teleports back to the active checkpoint (or consumes a life).
        /// </summary>
        public void HandleAbyssFall()
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - abyssFallDamage);

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
                CameraController.Instance.TriggerShake(0.25f, 0.38f);
            }

            if (CurrentHealth <= 0)
            {
                HandleZeroHealth();
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

        private void HandleZeroHealth()
        {
            bool triggerGameOver = GameManager.Instance == null || GameManager.Instance.ConsumeLifeAndCheckGameOver();
            if (triggerGameOver)
            {
                Die();
                return;
            }

            if (invulnRoutine != null)
            {
                StopCoroutine(invulnRoutine);
                invulnRoutine = null;
            }

            IsHurting = false;
            IsDead = false;
            CurrentHealth = maxHealth;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            Vector3 respawnPos = GameManager.Instance != null
                ? GameManager.Instance.CurrentRespawnPosition
                : transform.position;
            TeleportTo(respawnPos);
            GrantBriefInvulnerability(1.4f);

            if (aetherHealParticles != null)
            {
                aetherHealParticles.Play();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(CurrentHealth, maxHealth);
                UIManager.Instance.ShowBanner("-1 LIFE", 1.8f);
            }
        }

        public void ReviveAtCheckpoint(Vector3 checkpointPosition)
        {
            if (invulnRoutine != null)
            {
                StopCoroutine(invulnRoutine);
                invulnRoutine = null;
            }
            if (briefInvulnRoutine != null)
            {
                StopCoroutine(briefInvulnRoutine);
                briefInvulnRoutine = null;
            }

            IsDead = false;
            IsHurting = false;
            IsInvulnerable = false;
            HasAetherShield = false;
            if (aetherShieldParticles != null)
            {
                aetherShieldParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RefreshAetherLightHud();
            }
        }

        public bool Heal(int amount)
        {
            if (IsDead || CurrentHealth >= maxHealth)
            {
                return false;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + Mathf.Max(1, amount));
            if (aetherHealParticles != null)
            {
                aetherHealParticles.Play();
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(CurrentHealth, maxHealth);
            }
            return true;
        }

        public void GrantAetherShield()
        {
            if (IsDead)
            {
                return;
            }

            HasAetherShield = true;
            if (aetherShieldParticles != null && !aetherShieldParticles.isPlaying)
            {
                aetherShieldParticles.Play();
            }
            if (aetherHealParticles != null)
            {
                aetherHealParticles.Play();
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RefreshAetherLightHud();
            }
        }

        public void GrantBriefInvulnerability(float duration)
        {
            if (IsDead || invulnRoutine != null)
            {
                return;
            }

            if (briefInvulnRoutine != null)
            {
                StopCoroutine(briefInvulnRoutine);
            }
            briefInvulnRoutine = StartCoroutine(BriefInvulnerabilityRoutine(duration));
        }

        private IEnumerator BriefInvulnerabilityRoutine(float duration)
        {
            IsInvulnerable = true;
            yield return new WaitForSeconds(duration);
            if (invulnRoutine == null && !IsDead)
            {
                IsInvulnerable = false;
            }
            briefInvulnRoutine = null;
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
            if (briefInvulnRoutine != null)
            {
                StopCoroutine(briefInvulnRoutine);
                briefInvulnRoutine = null;
            }

            IsDead = true;
            IsHurting = true;
            IsInvulnerable = true;
            HasAetherShield = false;
            if (aetherShieldParticles != null)
            {
                aetherShieldParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

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
