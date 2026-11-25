using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Minimal-text Gothic HUD & UI state manager with delayed-drain HP bars,
    /// Life Soul emblems, Storm-Lightning ammo counter, compact zone progress gauge,
    /// Boss Phase + Ward Diamond shield pips, and concise story/rank screens.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels")]
        public GameObject titlePanel;
        public GameObject storyIntroPanel;
        public GameObject lorePanel;
        public GameObject tutorialPanel;
        public GameObject hudPanel;
        public GameObject pausePanel;
        public GameObject winPanel;
        public GameObject gameOverPanel;

        [Header("Story Prologue Elements")]
        public Image storyCardImage;
        public Text storyStepTitleText;
        public Text storyStepBodyText;
        public Text storyPageIndicatorText;
        public Text storyNextButtonText;
        public Sprite[] storyCardSprites;

        [Header("Player Crest HUD (HP, Lives, Lightning)")]
        public Image hpFillImage;
        public Image hpTrailFillImage;
        public Text hpValueText;
        public Image[] lifeIcons;
        public Color fullLifeColor = new Color(0.98f, 0.92f, 0.72f, 1f);
        public Color emptyLifeColor = new Color(0.18f, 0.20f, 0.30f, 0.35f);
        public Text lightningAmmoText;
        public Image lightningBadgeBackground;
        public Text shardCounterText;
        public Text timerText;

        [Header("Mission & Cloak HUD")]
        public Text districtObjectiveText;
        public Image sanctumProgressFill;
        public Text sanctumProgressPercentText;
        public Text glideBadgeText;
        public Image glideBadgeBackground;
        public GameObject bannerContainer;
        public Text bannerText;

        [Header("Legacy Aliases")]
        public Image[] heartIcons;
        public Color fullHeartColor = new Color(0.96f, 0.25f, 0.42f, 1f);
        public Color emptyHeartColor = new Color(0.22f, 0.22f, 0.28f, 0.45f);
        public Text aetherLightBadgeText;
        public Image aetherLightBadgeBackground;

        [Header("Boss HUD Elements")]
        public GameObject bossHudContainer;
        public Text bossNameText;
        public Text bossPhaseText;
        public Text bossStateText;
        public Image bossHealthFill;
        public Image bossHealthTrailFill;
        public Image[] bossWardIcons;
        public Color activeWardColor = new Color(1f, 0.35f, 0.48f, 1f);
        public Color brokenWardColor = new Color(0.22f, 0.16f, 0.24f, 0.32f);

        [Header("Audio Controls")]
        public Slider hudVolumeSlider;
        public Slider pauseVolumeSlider;
        public Text hudMuteButtonText;
        public Text pauseMuteButtonText;

        [Header("Win Screen Stats")]
        public Text winSummaryText;

        [Header("Menu & HUD Buttons")]
        public Button startRunButton;
        public Button titleStoryButton;
        public Button titleTutorialButton;
        public Button titleQuitButton;
        public Button storyNextButton;
        public Button storyBackButton;
        public Button storySkipButton;
        public Button loreBackButton;
        public Button loreStartButton;
        public Button tutorialBackButton;
        public Button pauseTutorialButton;
        public Button hudMuteButton;
        public Button hudPauseButton;
        public Button pauseMuteButton;
        public Button resumeButton;
        public Button pauseRestartButton;
        public Button pauseTitleButton;
        public Button pauseQuitButton;
        public Button winPlayAgainButton;
        public Button winMenuButton;
        public Button retryCheckpointButton;
        public Button gameOverRestartButton;
        public Button gameOverTitleButton;

        private Coroutine bannerRoutine;
        private Coroutine ammoPopRoutine;
        private bool syncingAudioUi;
        private int currentStorySlide;

        private float targetHpNorm = 1f;
        private float trailHpNorm = 1f;
        private float targetBossHpNorm = 1f;
        private float trailBossHpNorm = 1f;

        private static readonly string[] StorySlideTitles =
        {
            "I · ECLIPSE",
            "II · MALAKOR",
            "III · STORM OATH"
        };

        private static readonly string[] StorySlideBodies =
        {
            "Endless night froze Noctis Reach.",
            "The Eclipse Warden seals the Beacon.",
            "Shoot Lightning. Shatter his dark."
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            RegisterAudioListeners();
            RegisterButtonListeners();
            SyncAudioControls();
            if (bannerContainer != null && bannerRoutine == null)
            {
                bannerContainer.SetActive(false);
            }
        }

        private void Update()
        {
            // Delayed white/gold damage drain bars
            if (hpTrailFillImage != null)
            {
                trailHpNorm = trailHpNorm > targetHpNorm
                    ? Mathf.MoveTowards(trailHpNorm, targetHpNorm, Time.unscaledDeltaTime * 0.48f)
                    : targetHpNorm;
                hpTrailFillImage.fillAmount = trailHpNorm;
            }

            if (bossHealthTrailFill != null)
            {
                trailBossHpNorm = trailBossHpNorm > targetBossHpNorm
                    ? Mathf.MoveTowards(trailBossHpNorm, targetBossHpNorm, Time.unscaledDeltaTime * 0.42f)
                    : targetBossHpNorm;
                bossHealthTrailFill.fillAmount = trailBossHpNorm;
            }
        }

        private void RegisterAudioListeners()
        {
            if (hudVolumeSlider != null)
            {
                hudVolumeSlider.onValueChanged.RemoveAllListeners();
                hudVolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            }

            if (pauseVolumeSlider != null)
            {
                pauseVolumeSlider.onValueChanged.RemoveAllListeners();
                pauseVolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            }
        }

        private void RegisterButtonListeners()
        {
            Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

            BindButton(ref startRunButton, "StartRunButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.BeginDeliveryFromTitle();
            });
            BindButton(ref titleStoryButton, "TitleStoryButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ShowLoreScreen();
            });
            BindButton(ref titleTutorialButton, "TitleTutorialButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.OpenTutorialFromTitle();
            });
            BindButton(ref titleQuitButton, "TitleQuitButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            });
            BindButton(ref storyNextButton, "StoryNextButton", allButtons, AdvanceStoryIntro);
            BindButton(ref storyBackButton, "StoryBackButton", allButtons, PreviousStoryIntro);
            BindButton(ref storySkipButton, "StorySkipButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });
            BindButton(ref loreBackButton, "LoreBackButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.CloseSubScreen();
            });
            BindButton(ref loreStartButton, "LoreStartButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });
            BindButton(ref tutorialBackButton, "TutorialBackButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.CloseSubScreen();
            });
            BindButton(ref pauseTutorialButton, "PauseTutorialButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.OpenTutorialFromPause();
            });
            BindButton(ref hudMuteButton, "HudMuteButton", allButtons, OnToggleMuteClicked);
            BindButton(ref hudPauseButton, "HudPauseButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TogglePause();
            });
            BindButton(ref pauseMuteButton, "PauseMuteButton", allButtons, OnToggleMuteClicked);
            BindButton(ref resumeButton, "ResumeButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
            });
            BindButton(ref pauseRestartButton, "PauseRestartButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
            });
            BindButton(ref pauseTitleButton, "PauseTitleButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
            });
            BindButton(ref pauseQuitButton, "PauseQuitButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            });
            BindButton(ref winPlayAgainButton, "WinPlayAgainButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
            });
            BindButton(ref winMenuButton, "WinMenuButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
            });
            BindButton(ref retryCheckpointButton, "RetryCheckpointButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.RetryFromCheckpoint();
            });
            BindButton(ref gameOverRestartButton, "GameOverRestartButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
            });
            BindButton(ref gameOverTitleButton, "GameOverTitleButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
            });
        }

        private static void BindButton(ref Button field, string objectName, Button[] allButtons, UnityEngine.Events.UnityAction action)
        {
            if (field == null && allButtons != null)
            {
                for (int i = 0; i < allButtons.Length; i++)
                {
                    if (allButtons[i] != null && allButtons[i].name == objectName)
                    {
                        field = allButtons[i];
                        break;
                    }
                }
            }

            if (field == null || action == null)
            {
                return;
            }

            if (field.onClick.GetPersistentEventCount() == 0)
            {
                field.onClick.RemoveAllListeners();
                field.onClick.AddListener(action);
            }
        }

        public void ShowOnlyPanel(GameState state)
        {
            if (titlePanel != null) titlePanel.SetActive(state == GameState.Title);
            if (storyIntroPanel != null) storyIntroPanel.SetActive(state == GameState.StoryIntro);
            if (lorePanel != null) lorePanel.SetActive(state == GameState.Lore);
            if (tutorialPanel != null) tutorialPanel.SetActive(state == GameState.Tutorial);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);
            if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
            if (winPanel != null) winPanel.SetActive(state == GameState.Victory);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
        }

        public void OpenStoryIntro()
        {
            ShowOnlyPanel(GameState.StoryIntro);
            SetStorySlide(0);
        }

        public void AdvanceStoryIntro()
        {
            if (currentStorySlide + 1 >= StorySlideTitles.Length)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartGame();
                }
                return;
            }

            SetStorySlide(currentStorySlide + 1);
        }

        public void PreviousStoryIntro()
        {
            if (currentStorySlide > 0)
            {
                SetStorySlide(currentStorySlide - 1);
            }
        }

        public void SetStorySlide(int index)
        {
            currentStorySlide = Mathf.Clamp(index, 0, StorySlideTitles.Length - 1);

            if (storyStepTitleText != null)
            {
                storyStepTitleText.text = StorySlideTitles[currentStorySlide];
            }

            if (storyStepBodyText != null)
            {
                storyStepBodyText.text = StorySlideBodies[currentStorySlide];
            }

            if (storyPageIndicatorText != null)
            {
                storyPageIndicatorText.text = $"{currentStorySlide + 1} / {StorySlideTitles.Length}";
            }

            if (storyCardImage != null && storyCardSprites != null && currentStorySlide < storyCardSprites.Length && storyCardSprites[currentStorySlide] != null)
            {
                storyCardImage.sprite = storyCardSprites[currentStorySlide];
            }

            if (storyNextButtonText != null)
            {
                storyNextButtonText.text = currentStorySlide == StorySlideTitles.Length - 1
                    ? "PLAY"
                    : "NEXT";
            }

            if (storyBackButton != null)
            {
                storyBackButton.interactable = currentStorySlide > 0;
            }
        }

        public void UpdateObjective(string district, string objective)
        {
            if (districtObjectiveText == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(district) && !string.IsNullOrEmpty(objective))
            {
                districtObjectiveText.text = $"{district} · {objective}";
            }
            else if (!string.IsNullOrEmpty(district))
            {
                districtObjectiveText.text = district;
            }
            else if (!string.IsNullOrEmpty(objective))
            {
                districtObjectiveText.text = objective;
            }
        }

        public void UpdateSanctumProgress(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            if (sanctumProgressFill != null)
            {
                RectTransform fillRt = sanctumProgressFill.rectTransform;
                fillRt.anchorMax = new Vector2(clamped, 1f);
            }

            if (sanctumProgressPercentText != null)
            {
                sanctumProgressPercentText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
            }
        }

        public void UpdateHealth(int current, int max)
        {
            targetHpNorm = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (!Application.isPlaying)
            {
                trailHpNorm = targetHpNorm;
            }

            if (hpFillImage != null)
            {
                hpFillImage.fillAmount = targetHpNorm;
                if (targetHpNorm > 0.55f)
                {
                    hpFillImage.color = new Color(0.28f, 0.92f, 1f, 1f);
                }
                else if (targetHpNorm > 0.25f)
                {
                    hpFillImage.color = new Color(1f, 0.72f, 0.26f, 1f);
                }
                else
                {
                    hpFillImage.color = new Color(1f, 0.24f, 0.38f, 1f);
                }
            }

            if (hpTrailFillImage != null && !Application.isPlaying)
            {
                hpTrailFillImage.fillAmount = targetHpNorm;
            }

            if (hpValueText != null)
            {
                hpValueText.text = $"{current} / {max}";
            }
        }

        public void UpdateLives(int currentLives, int maxLives)
        {
            Image[] slots = lifeIcons != null && lifeIcons.Length > 0 ? lifeIcons : heartIcons;
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                bool active = i < currentLives;
                slots[i].color = active ? fullLifeColor : emptyLifeColor;
                slots[i].transform.localScale = active ? Vector3.one : new Vector3(0.82f, 0.82f, 1f);
            }
        }

        public void UpdateLightningAmmo(int ammo, int collectedShards, int totalShards)
        {
            if (lightningAmmoText != null)
            {
                lightningAmmoText.text = $"{ammo}  [E / CLICK]";
                lightningAmmoText.color = ammo > 0
                    ? new Color(0.55f, 0.98f, 1f, 1f)
                    : new Color(0.75f, 0.50f, 0.58f, 1f);
            }

            if (lightningBadgeBackground != null)
            {
                lightningBadgeBackground.color = ammo > 0
                    ? new Color(0.95f, 0.92f, 0.75f, 1f)
                    : new Color(0.70f, 0.60f, 0.65f, 0.9f);
            }

            if (aetherLightBadgeText != null)
            {
                aetherLightBadgeText.text = $"⚡ {ammo}  [CLICK / E]";
            }

            if (Application.isPlaying && ammo > 0 && lightningAmmoText != null && gameObject.activeInHierarchy)
            {
                if (ammoPopRoutine != null) StopCoroutine(ammoPopRoutine);
                ammoPopRoutine = StartCoroutine(PopTransformRoutine(lightningAmmoText.transform));
            }

            UpdateShards(collectedShards, totalShards);
        }

        public void UpdateShards(int collected, int total)
        {
            if (shardCounterText == null)
            {
                return;
            }

            shardCounterText.text = $"{collected}/{total}";
        }

        private IEnumerator PopTransformRoutine(Transform target)
        {
            target.localScale = Vector3.one;
            const float half = 0.08f;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                float s = Mathf.Lerp(1f, 1.25f, t / half);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                float s = Mathf.Lerp(1.25f, 1f, t / half);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
            ammoPopRoutine = null;
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText == null)
            {
                return;
            }

            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"{mins:00}:{secs:00}";
        }

        public void UpdateGlideStatus(bool isGliding, bool isAirborne)
        {
            if (glideBadgeText != null)
            {
                if (isGliding)
                {
                    glideBadgeText.text = "GLIDING";
                }
                else if (isAirborne)
                {
                    glideBadgeText.text = "SHIFT: GLIDE";
                }
                else
                {
                    glideBadgeText.text = "CLOAK OK";
                }
            }

            if (glideBadgeBackground != null)
            {
                glideBadgeBackground.color = isGliding
                    ? new Color(0.55f, 0.96f, 1f, 0.96f)
                    : Color.white;
            }
        }

        public void UpdateBossHud(bool visible, int currentHp, int maxHp, int currentWard, int maxWard, int phase, string stateLabel)
        {
            if (bossHudContainer == null)
            {
                return;
            }

            bossHudContainer.SetActive(visible);
            if (!visible)
            {
                return;
            }

            if (bossNameText != null)
            {
                bossNameText.text = $"MALAKOR   {currentHp}/{maxHp}";
            }

            if (bossPhaseText != null)
            {
                bossPhaseText.text = phase switch
                {
                    3 => "PHASE III · FURY",
                    2 => "PHASE II · RAGE",
                    _ => "PHASE I"
                };
                bossPhaseText.color = phase switch
                {
                    3 => new Color(1f, 0.36f, 0.65f, 1f),
                    2 => new Color(1f, 0.60f, 0.25f, 1f),
                    _ => new Color(1f, 0.86f, 0.55f, 1f)
                };
            }

            targetBossHpNorm = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            if (!Application.isPlaying)
            {
                trailBossHpNorm = targetBossHpNorm;
            }

            if (bossHealthFill != null)
            {
                bossHealthFill.fillAmount = targetBossHpNorm;
                bossHealthFill.color = stateLabel.Contains("STUNNED")
                    ? new Color(0.36f, 0.96f, 1f, 1f)
                    : phase >= 3
                        ? new Color(0.95f, 0.14f, 0.45f, 1f)
                        : phase == 2
                            ? new Color(1f, 0.36f, 0.18f, 1f)
                            : new Color(0.86f, 0.16f, 0.28f, 1f);
            }

            if (bossHealthTrailFill != null && !Application.isPlaying)
            {
                bossHealthTrailFill.fillAmount = targetBossHpNorm;
            }

            if (bossWardIcons != null)
            {
                for (int i = 0; i < bossWardIcons.Length; i++)
                {
                    if (bossWardIcons[i] == null) continue;
                    bool showSlot = i < maxWard;
                    bossWardIcons[i].gameObject.SetActive(showSlot);
                    bossWardIcons[i].color = i < currentWard ? activeWardColor : brokenWardColor;
                }
            }

            if (bossStateText != null)
            {
                bossStateText.text = stateLabel;
                bossStateText.color = stateLabel.Contains("STUNNED")
                    ? new Color(0.52f, 0.98f, 1f, 1f)
                    : stateLabel.Contains("DASH") || stateLabel.Contains("SLAM") || stateLabel.Contains("SKY")
                        ? new Color(1f, 0.72f, 0.36f, 1f)
                        : new Color(0.95f, 0.88f, 0.80f, 1f);
            }
        }

        public void ShowBanner(string message, float duration = 2.0f)
        {
            if (bannerContainer == null || bannerText == null)
            {
                return;
            }

            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            bannerRoutine = StartCoroutine(ShowBannerRoutine(message, duration));
        }

        private IEnumerator ShowBannerRoutine(string message, float duration)
        {
            bannerText.text = message;
            bannerContainer.SetActive(true);
            yield return new WaitForSecondsRealtime(duration);
            bannerContainer.SetActive(false);
            bannerRoutine = null;
        }

        public void PopulateWinStats(int collectedShards, int totalShards, float elapsedSeconds, int deaths)
        {
            if (winSummaryText == null)
            {
                return;
            }

            int mins = Mathf.FloorToInt(elapsedSeconds / 60f);
            int secs = Mathf.FloorToInt(elapsedSeconds % 60f);
            string rank = (collectedShards >= 24 && deaths == 0)
                ? "RANK S · TWILIGHT LEGEND"
                : collectedShards >= 16
                    ? "RANK A · SANCTUM VANGUARD"
                    : "RANK B · SHADOW COURIER";

            winSummaryText.text =
                $"{rank}\n\n" +
                $"⚡ {collectedShards}/{totalShards}    ·    ⏱ {mins:00}:{secs:00}    ·    ♥ ×{Mathf.Max(0, deaths)} RETRIES";
        }

        public void OnVolumeSliderChanged(float value)
        {
            if (syncingAudioUi || AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.SetMasterVolume(value);
            SyncAudioControls();
        }

        public void OnToggleMuteClicked()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.ToggleMute();
            SyncAudioControls();
        }

        public void SyncAudioControls()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            syncingAudioUi = true;
            float vol = AudioManager.Instance.masterVolume;
            bool muted = AudioManager.Instance.isMuted;

            if (hudVolumeSlider != null) hudVolumeSlider.value = vol;
            if (pauseVolumeSlider != null) pauseVolumeSlider.value = vol;

            string muteLabel = muted ? "MUTED" : "AUDIO";
            if (hudMuteButtonText != null) hudMuteButtonText.text = muteLabel;
            if (pauseMuteButtonText != null) pauseMuteButtonText.text = muteLabel;

            syncingAudioUi = false;
        }
    }
}
