using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Swinging Cathedral Pendulum Scythe hazard suspended from wrought-iron ceiling anchors.
    /// </summary>
    public class PendulumHazard : MonoBehaviour
    {
        public float maxAngle = 52f;
        public float swingSpeed = 2.35f;
        public float phaseOffset;
        public int damageAmount = 25;

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            float angle = Mathf.Sin(Time.time * swingSpeed + phaseOffset) * maxAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
