using UnityEngine;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Vertical crimson-violet Eclipse Sky-Lightning strike summoned by Malakor in Phase II & III.
    /// Shows a pulsing crimson warning beam before erupting into a damaging spear column.
    /// </summary>
    public class BossSkyStrike : MonoBehaviour
    {
        public float telegraphDuration = 0.68f;
        public float strikeActiveDuration = 0.32f;
        public int damageAmount = 25;

        private SpriteRenderer beamSr;
        private BoxCollider2D strikeCol;
        private Sprite spearSprite;
        private Color telegraphColor = new Color(1f, 0.25f, 0.35f, 0.42f);
        private float timer;
        private bool struck;

        public void Initialize(Sprite beamWhiteSprite, Sprite skySpearSprite, int damage = 25, float telegraph = 0.68f)
        {
            spearSprite = skySpearSprite;
            damageAmount = damage;
            telegraphDuration = telegraph;
            timer = 0f;
            struck = false;

            beamSr = gameObject.AddComponent<SpriteRenderer>();
            beamSr.sprite = beamWhiteSprite;
            beamSr.color = telegraphColor;
            beamSr.sortingOrder = 5;
            transform.localScale = new Vector3(0.45f, 6.2f, 1f);

            strikeCol = gameObject.AddComponent<BoxCollider2D>();
            strikeCol.isTrigger = true;
            strikeCol.enabled = false;
            strikeCol.size = new Vector2(1.0f, 5.4f);
            strikeCol.offset = Vector2.zero;
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            timer += Time.deltaTime;
            if (!struck)
            {
                float pulse = 0.3f + 0.55f * Mathf.Abs(Mathf.Sin(timer * 14f));
                if (beamSr != null)
                {
                    beamSr.color = new Color(1f, 0.26f, 0.38f, pulse);
                }

                if (timer >= telegraphDuration)
                {
                    TriggerStrike();
                }
            }
            else if (timer >= telegraphDuration + strikeActiveDuration)
            {
                Destroy(gameObject);
            }
        }

        private void TriggerStrike()
        {
            struck = true;
            if (beamSr != null)
            {
                if (spearSprite != null)
                {
                    beamSr.sprite = spearSprite;
                    transform.localScale = new Vector3(1.15f, 1.35f, 1f);
                }
                else
                {
                    transform.localScale = new Vector3(1.05f, 6.2f, 1f);
                }
                beamSr.color = Color.white;
                beamSr.sortingOrder = 8;
            }

            if (strikeCol != null)
            {
                strikeCol.enabled = true;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayZap();
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.14f, 0.18f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!struck || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damageAmount, transform.position);
            }
        }
    }
}
