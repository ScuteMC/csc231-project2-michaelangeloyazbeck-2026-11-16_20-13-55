using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Level hazard supporting both Electrified Roof Spikes (damage + recoil)
    /// and Abyss Pit Killzones below rooftops (damage + respawn at last checkpoint).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [Header("Hazard Mode")]
        public bool isKillzone = false;
        public int damageAmount = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!isKillzone)
            {
                TryDamagePlayer(other);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (other == null || !other.CompareTag("Player"))
            {
                return;
            }

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null)
            {
                return;
            }

            if (isKillzone)
            {
                health.HandleAbyssFall();
            }
            else
            {
                health.TakeDamage(damageAmount, transform.position);
            }
        }
    }
}
