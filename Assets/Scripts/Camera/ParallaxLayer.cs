using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Moves background layers proportionally to camera displacement to create twilight rooftop depth.
    /// Higher parallaxFactor keeps distant layers slower relative to foreground rooftops.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("0 = locked in world space, 1 = moves at camera speed (infinitely far).")]
        public float parallaxFactorX = 0.5f;

        [Range(0f, 1f)]
        public float parallaxFactorY = 0.25f;

        private Transform cameraTransform;
        private Vector3 startPosition;
        private Vector3 cameraStartPosition;
        private bool initialized;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                return;
            }

            cameraTransform = mainCam.transform;
            startPosition = transform.position;
            cameraStartPosition = cameraTransform.position;
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                Initialize();
                if (!initialized)
                {
                    return;
                }
            }

            Vector3 camDelta = cameraTransform.position - cameraStartPosition;
            transform.position = new Vector3(
                startPosition.x + camDelta.x * parallaxFactorX,
                startPosition.y + camDelta.y * parallaxFactorY,
                startPosition.z
            );
        }
    }
}
