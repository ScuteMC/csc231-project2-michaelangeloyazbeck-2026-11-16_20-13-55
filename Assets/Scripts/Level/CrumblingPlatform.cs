using System.Collections;
using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Fragile cracked stone ledge that shudders with falling debris when Vesper steps on it,
    /// collapses into the canal below, and reforms after a short delay.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrumblingPlatform : MonoBehaviour
    {
        [Header("Timing")]
        public float shakeDuration = 0.65f;
        public float respawnDelay = 3.2f;
        public float shakeAmplitude = 0.08f;

        [Header("References")]
        public Transform visualTransform;
        public SpriteRenderer visualRenderer;
        public ParticleSystem debrisParticles;

        private BoxCollider2D solidCollider;
        private Vector3 restVisualLocalPos;
        private Vector3 restVisualLocalScale;
        private Color restColor = Color.white;
        private bool isTriggered;

        private void Awake()
        {
            solidCollider = GetComponent<BoxCollider2D>();
            if (visualTransform == null)
            {
                visualTransform = transform;
            }
            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            restVisualLocalPos = visualTransform.localPosition;
            restVisualLocalScale = visualTransform.localScale;
            if (visualRenderer != null)
            {
                restColor = visualRenderer.color;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isTriggered || !collision.collider.CompareTag("Player"))
            {
                return;
            }

            // Trigger only when landing on top of the stone ledge
            bool landedOnTop = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < -0.4f)
                {
                    landedOnTop = true;
                    break;
                }
            }

            if (landedOnTop)
            {
                StartCoroutine(CrumbleCycleRoutine());
            }
        }

        private IEnumerator CrumbleCycleRoutine()
        {
            isTriggered = true;

            if (debrisParticles != null)
            {
                debrisParticles.Play();
            }

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, shakeDuration));
                float shakeX = Mathf.Sin(elapsed * 72f) * shakeAmplitude * (0.4f + 0.6f * t);
                float shakeY = Mathf.Cos(elapsed * 55f) * shakeAmplitude * 0.35f;
                visualTransform.localPosition = restVisualLocalPos + new Vector3(shakeX, shakeY, 0f);

                if (visualRenderer != null)
                {
                    visualRenderer.color = Color.Lerp(restColor, new Color(1f, 0.72f, 0.45f, 1f), t);
                }
                yield return null;
            }

            // Collapse
            visualTransform.localPosition = restVisualLocalPos;
            solidCollider.enabled = false;
            if (visualRenderer != null)
            {
                visualRenderer.enabled = false;
            }

            if (debrisParticles != null)
            {
                debrisParticles.Emit(24);
                debrisParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLand();
            }

            yield return new WaitForSeconds(respawnDelay);

            // Reform with a quick pop-in scale
            if (visualRenderer != null)
            {
                visualRenderer.color = restColor;
                visualRenderer.enabled = true;
            }
            solidCollider.enabled = true;

            float reformTime = 0.22f;
            float rElapsed = 0f;
            while (rElapsed < reformTime)
            {
                rElapsed += Time.deltaTime;
                float s = Mathf.SmoothStep(0.25f, 1f, rElapsed / reformTime);
                visualTransform.localScale = new Vector3(restVisualLocalScale.x, restVisualLocalScale.y * s, restVisualLocalScale.z);
                yield return null;
            }

            visualTransform.localScale = restVisualLocalScale;
            isTriggered = false;
        }
    }
}
