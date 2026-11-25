using UnityEngine;
using UnityEngine.UI;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Animates UI Image sprites and subtle anchored motion using unscaled time
    /// so Tutorial cards animate smoothly even while gameplay is paused.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class AnimatedUiSprite : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;
        public float bobAmplitudeY;
        public float bobFrequency = 3.2f;
        public float slideAmplitudeX;
        public float slideFrequency = 2.0f;

        private Image targetImage;
        private RectTransform rectTransform;
        private Vector2 baseAnchoredPos;
        private float elapsed;
        private bool initialized;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            elapsed = 0f;
            ApplyFrame(0);
        }

        private void CacheComponents()
        {
            if (initialized)
            {
                return;
            }
            targetImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                baseAnchoredPos = rectTransform.anchoredPosition;
            }
            initialized = true;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;

            if (frames != null && frames.Length > 1 && fps > 0.01f)
            {
                int idx = Mathf.FloorToInt(elapsed * fps) % frames.Length;
                ApplyFrame(idx);
            }

            if (rectTransform != null && (bobAmplitudeY > 0.01f || slideAmplitudeX > 0.01f))
            {
                float ox = slideAmplitudeX > 0.01f ? Mathf.Sin(elapsed * slideFrequency) * slideAmplitudeX : 0f;
                float oy = bobAmplitudeY > 0.01f ? Mathf.Sin(elapsed * bobFrequency) * bobAmplitudeY : 0f;
                rectTransform.anchoredPosition = baseAnchoredPos + new Vector2(ox, oy);
            }
        }

        private void ApplyFrame(int index)
        {
            if (targetImage != null && frames != null && index >= 0 && index < frames.Length && frames[index] != null)
            {
                targetImage.sprite = frames[index];
            }
        }
    }
}
