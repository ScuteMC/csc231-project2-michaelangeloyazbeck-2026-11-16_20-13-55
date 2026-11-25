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
        public float maxX = 236f;
        public float minY = -2.5f;

        private Vector3 velocity;
        private float currentLookAhead;
        private float shakeTimer;
        private float shakeDuration;
        private float shakeMagnitude;
        private PlayerController cachedPlayer;
        private Camera cam;
        private float baseOrthoSize = 6.5f;
        private bool arenaLocked;
        private Vector2 arenaFocus;
        private float arenaOrthoSize = 7.7f;

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
            {
                baseOrthoSize = cam.orthographicSize;
            }
        }

        public void SetBossArenaCamera(bool locked, Vector2 focus, float orthoSize = 7.7f)
        {
            arenaLocked = locked;
            arenaFocus = focus;
            arenaOrthoSize = orthoSize;
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

            if (cam != null && cam.orthographic && Application.isPlaying)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, arenaLocked ? arenaOrthoSize : baseOrthoSize, Time.unscaledDeltaTime * 3.8f);
            }

            float targetX;
            float targetY;
            if (arenaLocked)
            {
                targetX = Mathf.Clamp(Mathf.Lerp(target.position.x, arenaFocus.x, 0.62f), arenaFocus.x - 2.2f, arenaFocus.x + 2.2f);
                targetY = Mathf.Max(arenaFocus.y, target.position.y * 0.35f + arenaFocus.y * 0.65f);
            }
            else
            {
                targetX = Mathf.Clamp(target.position.x + currentLookAhead, minX, maxX);
                targetY = Mathf.Max(target.position.y + baseOffset.y, minY);
            }
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
