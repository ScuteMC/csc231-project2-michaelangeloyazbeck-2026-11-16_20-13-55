using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Safe-zone tutorial trigger introducing each mechanic on the HUD
    /// before the player encounters full hazards using that mechanic.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TutorialTrigger : MonoBehaviour
    {
        [TextArea(2, 4)]
        public string tutorialMessage = "Move with A/D or Arrows. Press SPACE to Jump!";
        public float displayDuration = 5.0f;
        public bool triggerOnce = true;

        private bool hasTriggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (triggerOnce && hasTriggered)
            {
                return;
            }

            hasTriggered = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowBanner(tutorialMessage, displayDuration);
            }
        }
    }
}
