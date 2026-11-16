using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Rising twilight wind vent. Holding Shadow-Cloak Glide inside this trigger
    /// lifts Vesper skyward across tall rooftops and wide gaps.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UpdraftVent : MonoBehaviour
    {
        [Header("Wind Strength")]
        public float updraftSpeed = 9.5f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.RegisterUpdraft(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.UnregisterUpdraft(this);
            }
        }
    }
}
