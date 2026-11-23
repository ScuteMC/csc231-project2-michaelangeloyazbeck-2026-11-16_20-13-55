using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Manages all eight UI screens (Title, Story Intro Prologue, Lore Briefing, Visual Tutorial,
    /// HUD, Pause, Level Complete Epilogue, Game Over) along with live HUD counters, mission objective,
    /// Sanctum progress gauge, heart indicators, glide status badge, banner tips, and audio controls.
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

        [Header("HUD Elements")]
        public Image[] heartIcons;
        public Color fullHeartColor = new Color(0.96f, 0.25f, 0.42f, 1f);
        public Color emptyHeartColor = new Color(0.22f, 0.22f, 0.28f, 0.45f);
        public Text shardCounterText;
        public Text timerText;
        public Text districtObjectiveText;
        public Image sanctumProgressFill;
        public Text sanctumProgressPercentText;
        public Text glideBadgeText;
        public Image glideBadgeBackground;
        public GameObject bannerContainer;
        public Text bannerText;

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
        private Coroutine shardPopRoutine;
        private bool syncingAudioUi;
        private int currentStorySlide;

        private static readonly string[] StorySlideTitles =
        {
            "I. THE FALLING ECLIPSE",
            "II. SHATTERED LIGHT",
            "III. THE COURIER'S OATH"
        };

        private static readonly string[] StorySlideBodies =
        {
            "A crimson eclipse hangs frozen over Noctis Reach, draining every district lantern toward eternal frost.\nShould darkness claim the upper spires, the canal city beneath will never see another dawn.",
            "The high Sanctum Beacon has shattered its Memory Shards across four perilous rooftop districts.\nCorrupted Clockwork Sentinels and concealed spike grates now guard every fractured crossing.",
            "You are Vesper, a Shadow-Stitcher Courier sworn to restore the fallen light.\nSpread your dusk-woven cloak, ride the thermal chimneys, reclaim the shards, and reignite the Beacon!"
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

        private void RegisterAudioListeners()
        {
            if (hudVolumeSlider != null)
            {
                hudVolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            }
            if (pauseVolumeSlider != null)
            {
                pauseVolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            }
        }

        private void RegisterButtonListeners()
        {
            Button[] allButtons = GetComponentsInChildren<Button>(true);

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

            // Avoid double-invoking when a persistent scene listener is already wired on this button
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
                    ? "BEGIN DELIVERY"
                    : "NEXT PAGE";
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
                districtObjectiveText.text = $"{district}\n{objective}";
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
            if (heartIcons == null)
            {
                return;
            }

            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    heartIcons[i].color = i < current ? fullHeartColor : emptyHeartColor;
                }
            }
        }

        public void UpdateShards(int collected, int total)
        {
            if (shardCounterText == null)
            {
                return;
            }

            shardCounterText.text = $"MEMORY SHARDS: {collected} / {total}";

            if (Application.isPlaying && collected > 0 && gameObject.activeInHierarchy)
            {
                if (shardPopRoutine != null)
                {
                    StopCoroutine(shardPopRoutine);
                }
                shardPopRoutine = StartCoroutine(PopShardCounterRoutine());
            }
        }

        private IEnumerator PopShardCounterRoutine()
        {
            Transform rt = shardCounterText.transform;
            rt.localScale = Vector3.one;
            const float halfDuration = 0.09f;

            for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
            {
                float s = Mathf.Lerp(1.0f, 1.22f, t / halfDuration);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
            {
                float s = Mathf.Lerp(1.22f, 1.0f, t / halfDuration);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            rt.localScale = Vector3.one;
            shardPopRoutine = null;
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText == null)
            {
                return;
            }

            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"TIME: {mins:00}:{secs:00}";
        }

        public void UpdateGlideStatus(bool isGliding, bool isAirborne)
        {
            if (glideBadgeText != null)
            {
                if (isGliding)
                {
                    glideBadgeText.text = "CLOAK: GLIDING";
                }
                else if (isAirborne)
                {
                    glideBadgeText.text = "HOLD SPACE/SHIFT: GLIDE";
                }
                else
                {
                    glideBadgeText.text = "CLOAK: READY";
                }
            }

            if (glideBadgeBackground != null)
            {
                glideBadgeBackground.color = isGliding
                    ? new Color(0.55f, 0.96f, 1f, 0.96f)
                    : Color.white;
            }
        }

        public void ShowBanner(string message, float duration = 4f)
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
            string rank;
            if (collectedShards >= 22 && deaths <= 1)
            {
                rank = "S  -  MASTER OF THE TWILIGHT";
            }
            else if (collectedShards >= 15)
            {
                rank = "A  -  SANCTUM VANGUARD";
            }
            else
            {
                rank = "B  -  SHADOW COURIER";
            }

            winSummaryText.text =
                "The Sanctum Beacon blazes across Noctis Reach, shattering the Eternal Eclipse!\n" +
                "Dawn breaks over the canal spires thanks to Vesper's delivery.\n\n" +
                $"COURIER RANK:  {rank}\n" +
                $"Memory Shards Restored:  {collectedShards} / {totalShards}    |    " +
                $"Clear Time:  {mins:00}:{secs:00}    |    Retries:  {deaths}";
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

            string muteLabel = muted ? "AUDIO: MUTED" : "MUTE AUDIO";
            if (hudMuteButtonText != null) hudMuteButtonText.text = muteLabel;
            if (pauseMuteButtonText != null) pauseMuteButtonText.text = muteLabel;

            syncingAudioUi = false;
        }
    }
}
