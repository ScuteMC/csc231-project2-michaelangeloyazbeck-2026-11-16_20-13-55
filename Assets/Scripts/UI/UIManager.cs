using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VesperTwilightCourier
{
    /// <summary>
    /// Manages all five UI screens (Title, HUD, Pause, Level Complete, Game Over)
    /// along with live HUD counters, heart indicators, glide status badge, banner tips,
    /// and volume/mute controls.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels")]
        public GameObject titlePanel;
        public GameObject hudPanel;
        public GameObject pausePanel;
        public GameObject winPanel;
        public GameObject gameOverPanel;

        [Header("HUD Elements")]
        public Image[] heartIcons;
        public Color fullHeartColor = new Color(0.96f, 0.25f, 0.42f, 1f);
        public Color emptyHeartColor = new Color(0.22f, 0.22f, 0.28f, 0.45f);
        public Text shardCounterText;
        public Text timerText;
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
        public Button titleQuitButton;
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
        private bool syncingAudioUi;

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
            if (bannerContainer != null)
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
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });
            BindButton(ref titleQuitButton, "TitleQuitButton", allButtons, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
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

            // Avoid double-invoking if a persistent scene listener is already wired on this button
            if (field.onClick.GetPersistentEventCount() == 0)
            {
                field.onClick.RemoveAllListeners();
                field.onClick.AddListener(action);
            }
        }

        public void ShowOnlyPanel(GameState state)
        {
            if (titlePanel != null) titlePanel.SetActive(state == GameState.Title);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);
            if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
            if (winPanel != null) winPanel.SetActive(state == GameState.Victory);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
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
            if (shardCounterText != null)
            {
                shardCounterText.text = $"MEMORY SHARDS: {collected} / {total}";
            }
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
                    ? new Color(0.18f, 0.72f, 0.85f, 0.9f)
                    : new Color(0.12f, 0.14f, 0.28f, 0.85f);
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
            winSummaryText.text =
                $"Sanctum Beacon Reached!\n\n" +
                $"Memory Shards: {collectedShards} / {totalShards}\n" +
                $"Clear Time: {mins:00}:{secs:00}\n" +
                $"Retries: {deaths}";
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
