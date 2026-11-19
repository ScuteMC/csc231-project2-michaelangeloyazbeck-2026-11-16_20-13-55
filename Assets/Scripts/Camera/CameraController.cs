using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Smooth 2D follow camera with horizontal look-ahead, minimum altitude clamp,
    /// and trauma-based screen shake for hits and sentinel stomps.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Follow Target")]
        public Transform target;
        public Vector3 baseOffset = new Vector3(2.5f, 1.8f, -10f);
        public float smoothTime = 0.14f;
        public float lookAheadDistance = 2.0f;

        [Header("Bounds")]
        public float minX = 6f;
        public float maxX = 221f;
        public float minY = -2.5f;

        private Vector3 velocity;
        private float currentLookAhead;
        private float shakeTimer;
        private float shakeDuration;
        private float shakeMagnitude;
        private PlayerController cachedPlayer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (target != null)
            {
                cachedPlayer = target.GetComponent<PlayerController>();
                SnapToTarget();
            }
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(
                Mathf.Clamp(target.position.x + baseOffset.x, minX, maxX),
                Mathf.Max(target.position.y + baseOffset.y, minY),
                baseOffset.z
            );
            transform.position = desired;
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float facingDir = 1f;
            if (cachedPlayer != null)
            {
                facingDir = cachedPlayer.FacingDirection;
            }

            float targetLookAhead = facingDir * lookAheadDistance;
            currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, Time.unscaledDeltaTime * 4f);

            float targetX = Mathf.Clamp(target.position.x + currentLookAhead, minX, maxX);
            float targetY = Mathf.Max(target.position.y + baseOffset.y, minY);
            Vector3 desiredPosition = new Vector3(targetX, targetY, baseOffset.z);

            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float normalized = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
                Vector2 offset2D = Random.insideUnitCircle * (shakeMagnitude * normalized);
                smoothed.x += offset2D.x;
                smoothed.y += offset2D.y;
            }

            transform.position = smoothed;
        }

        public void TriggerShake(float duration, float magnitude)
        {
            shakeDuration = Mathf.Max(duration, 0.01f);
            shakeTimer = Mathf.Max(shakeTimer, shakeDuration);
            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
        }
    }
}
