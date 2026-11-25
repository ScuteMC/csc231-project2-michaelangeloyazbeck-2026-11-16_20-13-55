using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Centralized tactile game-feel engine implementing hit-stop freeze frames
    /// and eased world-space floating combat/pickup popups.
    /// </summary>
    public class GameFeelManager : MonoBehaviour
    {
        public static GameFeelManager Instance { get; private set; }

        private Coroutine hitStopRoutine;
        private bool hitStopActive;
        private Font cachedFont;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDisable()
        {
            if (hitStopActive && (GameManager.Instance == null || GameManager.Instance.IsPlaying))
            {
                Time.timeScale = 1f;
            }
            hitStopActive = false;
        }

        public void TriggerHitStop(float duration, float slowScale = 0.04f)
        {
            if (!Application.isPlaying || duration <= 0.001f)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
            }
            hitStopRoutine = StartCoroutine(HitStopRoutine(duration, slowScale));
        }

        private IEnumerator HitStopRoutine(float duration, float slowScale)
        {
            hitStopActive = true;
            Time.timeScale = Mathf.Clamp(slowScale, 0.01f, 0.5f);
            yield return new WaitForSecondsRealtime(duration);
            hitStopActive = false;

            // Never override Pause / GameOver / Title freeze!
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
            {
                Time.timeScale = 1f;
            }
            hitStopRoutine = null;
        }

        public void SpawnFloatingText(Vector3 worldPos, string text, Color color, float scale = 1f)
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(text))
            {
                return;
            }

            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cachedFont == null)
                {
                    cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 26);
                }
            }

            GameObject go = new GameObject("FloatingCombatText");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.18f, 0.18f), 0f, 0f);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 40;

            RectTransform canvasRt = go.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(320f, 90f);
            float baseScale = 0.014f * Mathf.Max(0.5f, scale);
            go.transform.localScale = new Vector3(baseScale, baseScale, 1f);

            GameObject txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            Text txt = txtGo.AddComponent<Text>();
            txt.font = cachedFont;
            txt.text = text;
            txt.fontSize = 30;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Outline outline = txtGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.03f, 0.08f, 0.96f);
            outline.effectDistance = new Vector2(2f, -2f);

            FloatingTextAnim anim = go.AddComponent<FloatingTextAnim>();
            anim.Initialize(txt, color, baseScale);
        }
    }

    public class FloatingTextAnim : MonoBehaviour
    {
        private Text label;
        private Color baseColor;
        private float baseScale;
        private Vector3 startPos;
        private float elapsed;
        private const float Duration = 0.72f;

        public void Initialize(Text txt, Color color, float scale)
        {
            label = txt;
            baseColor = color;
            baseScale = scale;
            startPos = transform.position;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(elapsed / Duration);
            float easeOut = 1f - Mathf.Pow(1f - u, 3f);

            transform.position = startPos + Vector3.up * (1.15f * easeOut);

            float pop = u < 0.16f ? Mathf.Lerp(1.32f, 1.0f, u / 0.16f) : 1.0f;
            float s = baseScale * pop;
            transform.localScale = new Vector3(s, s, 1f);

            if (label != null)
            {
                Color c = baseColor;
                c.a = u > 0.55f ? Mathf.Lerp(1f, 0f, (u - 0.55f) / 0.45f) : 1f;
                label.color = c;
            }

            if (u >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
