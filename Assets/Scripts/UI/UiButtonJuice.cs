using UnityEngine;
using UnityEngine.EventSystems;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Adds responsive unscaled-time hover & press spring scaling plus crisp UI audio
    /// to ornate menu and HUD buttons.
    /// </summary>
    public class UiButtonJuice : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        public float hoverScale = 1.06f;
        public float pressedScale = 0.94f;
        public float lerpSpeed = 18f;

        private Vector3 baseScale = Vector3.one;
        private float targetMultiplier = 1f;
        private bool isHovered;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            isHovered = false;
            targetMultiplier = 1f;
            transform.localScale = baseScale;
        }

        private void Update()
        {
            Vector3 desired = baseScale * targetMultiplier;
            transform.localScale = Vector3.Lerp(transform.localScale, desired, Time.unscaledDeltaTime * lerpSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            targetMultiplier = hoverScale;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            targetMultiplier = 1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetMultiplier = pressedScale;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetMultiplier = isHovered ? hoverScale : 1f;
        }
    }
}
