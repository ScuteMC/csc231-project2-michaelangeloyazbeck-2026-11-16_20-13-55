using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Smooth 2D follow camera implementing Itay Keren's "Scroll Back" principles:
    /// horizontal dead-zone window, look-ahead direction hysteresis, vertical platform snapping,
    /// Boss Arena wide-framing lock, zoom punches, and trauma-based screen shake.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Follow Target")]
        public Transform target;
        public Vector3 baseOffset = new Vector3(2.5f, 1.8f, -10f);
        public float smoothTime = 0.14f;
        public float lookAheadDistance = 2.0f;

        [Header("Scroll-Back Camera Principles")]
        public float deadZoneX = 0.75f;
        public float lookAheadSwitchDelay = 0.16f;
        public float airUpperSnapLimit = 2.5f;
        public float airLowerSnapLimit = 1.5f;

        [Header("Bounds")]
        public float minX = 6f;
        public float maxX = 244f;
        public float minY = -2.5f;

        private Vector3 velocity;
        private float currentLookAhead;
        private float pendingLookDir = 1f;
        private float lookDirHoldTimer;
        private float cameraFocusX;
        private float anchoredGroundY;
        private float shakeTimer;
        private float shakeDuration;
        private float shakeMagnitude;
        private PlayerController cachedPlayer;
        private Camera cam;
        private float baseOrthoSize = 6.5f;
        private float zoomPunchDelta;
        private float zoomPunchTimer;
        private float zoomPunchDuration;
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

        public void TriggerZoomPunch(float deltaOrtho, float duration = 0.25f)
        {
            zoomPunchDelta = deltaOrtho;
            zoomPunchDuration = Mathf.Max(0.05f, duration);
            zoomPunchTimer = zoomPunchDuration;
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

            cameraFocusX = target.position.x;
            anchoredGroundY = target.position.y;
            pendingLookDir = cachedPlayer != null ? cachedPlayer.FacingDirection : 1f;
            currentLookAhead = pendingLookDir * lookAheadDistance;
            lookDirHoldTimer = 0f;

            Vector3 desired = new Vector3(
                Mathf.Clamp(cameraFocusX + currentLookAhead, minX, maxX),
                Mathf.Max(anchoredGroundY + baseOffset.y, minY),
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

            float dt = Time.unscaledDeltaTime;

            // Handle orthographic zoom punch + smooth arena camera size transitions
            if (zoomPunchTimer > 0f)
            {
                zoomPunchTimer -= dt;
                if (zoomPunchTimer <= 0f)
                {
                    zoomPunchDelta = 0f;
                }
            }

            if (cam != null && cam.orthographic && Application.isPlaying)
            {
                float targetOrtho = arenaLocked ? arenaOrthoSize : baseOrthoSize;
                float punch = zoomPunchTimer > 0f ? zoomPunchDelta * (zoomPunchTimer / zoomPunchDuration) : 0f;
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrtho + punch, dt * 4.5f);
            }

            // Look-ahead hysteresis so rapid left-right micro-adjustments never flip the camera
            float facingDir = cachedPlayer != null ? cachedPlayer.FacingDirection : 1f;
            if (!Mathf.Approximately(Mathf.Sign(facingDir), Mathf.Sign(pendingLookDir)))
            {
                pendingLookDir = facingDir;
                lookDirHoldTimer = 0f;
            }
            else
            {
                lookDirHoldTimer += dt;
            }

            if (lookDirHoldTimer >= lookAheadSwitchDelay)
            {
                float targetLookAhead = pendingLookDir * lookAheadDistance;
                currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, dt * 4.2f);
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
                // Horizontal Dead-Zone Window
                float dx = target.position.x - cameraFocusX;
                if (dx > deadZoneX)
                {
                    cameraFocusX = target.position.x - deadZoneX;
                }
                else if (dx < -deadZoneX)
                {
                    cameraFocusX = target.position.x + deadZoneX;
                }

                // Vertical Platform Snapping (avoids jump-height camera jitter)
                if (cachedPlayer == null || cachedPlayer.IsGrounded)
                {
                    anchoredGroundY = Mathf.Lerp(anchoredGroundY, target.position.y, dt * 8.0f);
                }
                else
                {
                    if (target.position.y > anchoredGroundY + airUpperSnapLimit)
                    {
                        anchoredGroundY = target.position.y - airUpperSnapLimit;
                    }
                    else if (target.position.y < anchoredGroundY - airLowerSnapLimit)
                    {
                        anchoredGroundY = target.position.y + airLowerSnapLimit;
                    }
                }

                targetX = Mathf.Clamp(cameraFocusX + currentLookAhead, minX, maxX);
                targetY = Mathf.Max(anchoredGroundY + baseOffset.y, minY);
            }

            Vector3 desiredPosition = new Vector3(targetX, targetY, baseOffset.z);
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime, Mathf.Infinity, dt);

            if (shakeTimer > 0f)
            {
                shakeTimer -= dt;
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
