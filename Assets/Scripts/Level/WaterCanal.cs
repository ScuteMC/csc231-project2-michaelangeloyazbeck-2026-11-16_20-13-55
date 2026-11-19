using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Animates the Twilight Canal water surface beneath the rooftops with subtle horizontal
    /// wave sway and emits a luminous water splash burst when Vesper falls into the canal.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WaterCanal : MonoBehaviour
    {
        [Header("Wave Motion")]
        public Transform frontWaveLayer;
        public Transform backWaveLayer;
        public float frontSwayAmplitude = 0.45f;
        public float frontSwaySpeed = 1.25f;
        public float backSwayAmplitude = 0.65f;
        public float backSwaySpeed = 0.85f;
        public float surfaceWorldY = -5.4f;

        [Header("VFX")]
        public ParticleSystem splashParticles;

        private Vector3 frontBasePos;
        private Vector3 backBasePos;

        private void Awake()
        {
            if (frontWaveLayer != null)
            {
                frontBasePos = frontWaveLayer.localPosition;
            }
            if (backWaveLayer != null)
            {
                backBasePos = backWaveLayer.localPosition;
            }
        }

        private void Update()
        {
            float t = Time.time;
            if (frontWaveLayer != null)
            {
                float dx = Mathf.Sin(t * frontSwaySpeed) * frontSwayAmplitude;
                float dy = Mathf.Cos(t * frontSwaySpeed * 1.4f) * 0.06f;
                frontWaveLayer.localPosition = frontBasePos + new Vector3(dx, dy, 0f);
            }

            if (backWaveLayer != null)
            {
                float dx = Mathf.Cos(t * backSwaySpeed) * backSwayAmplitude;
                float dy = Mathf.Sin(t * backSwaySpeed * 1.2f) * 0.05f;
                backWaveLayer.localPosition = backBasePos + new Vector3(dx, dy, 0f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (splashParticles != null)
            {
                splashParticles.transform.position = new Vector3(other.transform.position.x, surfaceWorldY, 0f);
                splashParticles.Emit(30);
                splashParticles.Play();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLand();
            }

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.HandleAbyssFall();
            }
        }
    }
}
