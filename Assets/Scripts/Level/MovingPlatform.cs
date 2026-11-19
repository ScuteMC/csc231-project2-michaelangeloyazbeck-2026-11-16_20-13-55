using System.Collections.Generic;
using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Clockwork levitating platform that smoothly oscillates between startPosition and endPosition
    /// along a strictly collision-free track, carrying Vesper cleanly in FixedUpdate without parent-scale jitter.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Travel Bounds (World Space)")]
        public Vector3 startPosition;
        public Vector3 endPosition;

        [Header("Motion")]
        [Tooltip("Seconds to travel one full half-trip from startPosition to endPosition.")]
        public float travelDuration = 2.6f;
        [Tooltip("Brief pause at each endpoint so players can time their jump cleanly.")]
        public float endpointPause = 0.45f;
        [Range(0f, 1f)] public float phaseOffset = 0f;

        [Header("VFX")]
        public ParticleSystem thrusterParticles;

        private float elapsed;
        private Vector3 lastPosition;
        private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();

        private void Start()
        {
            float fullPeriod = (travelDuration + endpointPause) * 2f;
            elapsed = phaseOffset * fullPeriod;
            Vector3 initial = EvaluatePosition(elapsed);
            transform.position = initial;
            lastPosition = initial;
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            elapsed += Time.fixedDeltaTime;
            Vector3 target = EvaluatePosition(elapsed);
            Vector2 delta = (Vector2)(target - lastPosition);

            transform.position = target;
            lastPosition = target;

            if (delta.sqrMagnitude > 0.0000001f && riders.Count > 0)
            {
                foreach (Rigidbody2D rider in riders)
                {
                    if (rider != null)
                    {
                        rider.position += delta;
                    }
                }
            }
        }

        private Vector3 EvaluatePosition(float time)
        {
            float leg = Mathf.Max(0.1f, travelDuration);
            float pause = Mathf.Max(0f, endpointPause);
            float halfPeriod = leg + pause;
            float fullPeriod = halfPeriod * 2f;

            float t = Mathf.Repeat(time, fullPeriod);
            float alpha;
            if (t < leg)
            {
                alpha = Mathf.SmoothStep(0f, 1f, t / leg);
            }
            else if (t < halfPeriod)
            {
                alpha = 1f;
            }
            else if (t < halfPeriod + leg)
            {
                alpha = Mathf.SmoothStep(1f, 0f, (t - halfPeriod) / leg);
            }
            else
            {
                alpha = 0f;
            }

            return Vector3.Lerp(startPosition, endPosition, alpha);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryAddRider(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryAddRider(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.rigidbody != null)
            {
                riders.Remove(collision.rigidbody);
            }
        }

        private void TryAddRider(Collision2D collision)
        {
            if (collision.rigidbody == null || !collision.collider.CompareTag("Player"))
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                // Contact normal points from other collider into this platform: negative Y means player is standing on top
                if (collision.GetContact(i).normal.y < -0.45f)
                {
                    riders.Add(collision.rigidbody);
                    return;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.45f, 0.92f, 1f, 0.85f);
            Gizmos.DrawLine(startPosition, endPosition);
            Gizmos.DrawWireSphere(startPosition, 0.25f);
            Gizmos.DrawWireSphere(endPosition, 0.25f);
        }
    }
}
