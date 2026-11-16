using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VesperTwilightCourier
{
    public enum GameState
    {
        Title,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    /// <summary>
    /// Coordinates game states (Title, Playing, Paused, GameOver, Victory),
    /// shard progression, checkpoints, level timer, and scene flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private static bool skipTitleOnNextLoad;

        [Header("Player References")]
        public PlayerController playerController;
        public PlayerHealth playerHealth;

        [Header("Level Progression")]
        public int totalShards = 25;

        public GameState CurrentState { get; private set; } = GameState.Title;
        public bool IsPlaying => CurrentState == GameState.Playing;
        public int CollectedShards { get; private set; }
        public float ElapsedTime { get; private set; }
        public int DeathsCount { get; private set; }
        public int HighestCheckpointIndex { get; private set; }
        public Vector3 CurrentRespawnPosition { get; private set; }

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
            if (playerHealth == null)
            {
                playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
            }
            if (playerController == null && playerHealth != null)
            {
                playerController = playerHealth.GetComponent<PlayerController>();
            }

            if (playerController != null)
            {
                CurrentRespawnPosition = playerController.transform.position;
            }

            CollectibleShard[] shardsInScene = Object.FindObjectsByType<CollectibleShard>(FindObjectsSortMode.None);
            if (shardsInScene != null && shardsInScene.Length > 0)
            {
                totalShards = shardsInScene.Length;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateShards(CollectedShards, totalShards);
            }

            if (skipTitleOnNextLoad)
            {
                skipTitleOnNextLoad = false;
                StartGame();
            }
            else
            {
                ShowTitleScreen();
            }
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                ElapsedTime += Time.deltaTime;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateTimer(ElapsedTime);
                }
            }

            bool pausePressed = false;
            bool confirmPressed = false;
            bool retryPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                pausePressed = Keyboard.current.escapeKey.wasPressedThisFrame ||
                               Keyboard.current.pKey.wasPressedThisFrame;
                confirmPressed = Keyboard.current.enterKey.wasPressedThisFrame ||
                                 Keyboard.current.numpadEnterKey.wasPressedThisFrame;
                retryPressed = Keyboard.current.rKey.wasPressedThisFrame;
            }

            if (Gamepad.current != null)
            {
                pausePressed |= Gamepad.current.startButton.wasPressedThisFrame;
                confirmPressed |= Gamepad.current.startButton.wasPressedThisFrame ||
                                  Gamepad.current.buttonSouth.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            pausePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            confirmPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            retryPressed = Input.GetKeyDown(KeyCode.R);
#endif

            if (CurrentState == GameState.Title && confirmPressed)
            {
                StartGame();
                return;
            }

            if (CurrentState == GameState.GameOver)
            {
                if (confirmPressed || retryPressed)
                {
                    RetryFromCheckpoint();
                    return;
                }
            }

            if (CurrentState == GameState.Victory && confirmPressed)
            {
                RestartLevel();
                return;
            }

            if (pausePressed && (CurrentState == GameState.Playing || CurrentState == GameState.Paused))
            {
                TogglePause();
            }
        }

        public void ShowTitleScreen()
        {
            CurrentState = GameState.Title;
            Time.timeScale = 0f;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnlyPanel(GameState.Title);
                UIManager.Instance.SyncAudioControls();
            }
        }

        public void StartGame()
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnlyPanel(GameState.Playing);
                UIManager.Instance.SyncAudioControls();
                UIManager.Instance.UpdateShards(CollectedShards, totalShards);
                if (playerHealth != null)
                {
                    UIManager.Instance.UpdateHealth(playerHealth.CurrentHealth, playerHealth.maxHealth);
                }
                UIManager.Instance.ShowBanner("Move: A/D or Arrows | Jump: Space/W | Reach the Sanctum Beacon!", 4.5f);
            }
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowOnlyPanel(GameState.Paused);
                    UIManager.Instance.SyncAudioControls();
                }
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnlyPanel(GameState.Playing);
                UIManager.Instance.SyncAudioControls();
            }
        }

        public void CollectShard()
        {
            CollectedShards++;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateShards(CollectedShards, totalShards);
            }
        }

        public void ActivateCheckpoint(int index, Vector3 respawnPos, string checkpointName)
        {
            if (index >= HighestCheckpointIndex)
            {
                HighestCheckpointIndex = index;
                CurrentRespawnPosition = respawnPos;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowBanner($"CHECKPOINT ACTIVATED: {checkpointName}", 3.5f);
            }
        }

        public void TriggerGameOver()
        {
            DeathsCount++;
            CurrentState = GameState.GameOver;
            Time.timeScale = 0f;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnlyPanel(GameState.GameOver);
            }
        }

        public void RetryFromCheckpoint()
        {
            Time.timeScale = 1f;
            CurrentState = GameState.Playing;

            if (playerHealth != null)
            {
                playerHealth.ReviveAtCheckpoint(CurrentRespawnPosition);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnlyPanel(GameState.Playing);
                UIManager.Instance.ShowBanner("Respawned at Watchtower Lantern", 2.5f);
            }
        }

        public void RestartLevel()
        {
            skipTitleOnNextLoad = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToTitle()
        {
            skipTitleOnNextLoad = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void CompleteLevel()
        {
            if (CurrentState == GameState.Victory)
            {
                return;
            }

            CurrentState = GameState.Victory;
            Time.timeScale = 1f;

            if (playerController != null)
            {
                playerController.ResetMotionAndTimers();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.PopulateWinStats(CollectedShards, totalShards, ElapsedTime, DeathsCount);
                UIManager.Instance.ShowOnlyPanel(GameState.Victory);
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
