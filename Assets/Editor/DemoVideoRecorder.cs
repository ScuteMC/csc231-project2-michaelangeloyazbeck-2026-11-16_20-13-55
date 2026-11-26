#if UNITY_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VesperTwilightCourier;
using Debug = UnityEngine.Debug;

/// <summary>
/// Records a 1280x720 30 FPS PlayMode showcase video (Title Screen, Courtyards Glide,
/// Aqueduct Death & Checkpoint Respawn, Belltower & Viaduct Highlights, 5-Attack Malakor Boss Fight,
/// Beacon Ignition & Dawn Restored Win Screen) and logs sample-accurate audio events.
/// </summary>
public static class DemoVideoRecorder
{
    private const string ArmedFlagPath = "Logs/demo_record_armed.flag";
    private const string AudioLogPath = "DemoVideo/.tmp_audio.csv";
    private const string SilentVideoPath = "DemoVideo/.tmp_silent.mp4";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Vesper/Demo/Record Gameplay Demo Video (Batchmode)")]
    public static void RecordDemoVideo()
    {
        Directory.CreateDirectory("Logs");
        Directory.CreateDirectory("DemoVideo");

        if (File.Exists(AudioLogPath)) File.Delete(AudioLogPath);
        if (File.Exists(SilentVideoPath)) File.Delete(SilentVideoPath);

        string ffmpegPath = Environment.GetEnvironmentVariable("DEMO_FFMPEG_PATH");
        if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
        {
            throw new Exception($"[DemoVideoRecorder] Valid DEMO_FFMPEG_PATH env var is required. Got '{ffmpegPath}'.");
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        File.WriteAllText(ArmedFlagPath, ffmpegPath);
        Debug.Log("[DemoVideoRecorder] Scene opened, arm flag written, entering PlayMode...");
        EditorApplication.isPlaying = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        if (!File.Exists(ArmedFlagPath))
        {
            return;
        }

        string ffmpegPath = File.ReadAllText(ArmedFlagPath).Trim();
        File.Delete(ArmedFlagPath);

        GameObject driverGo = new GameObject("DemoPlayModeDriver");
        UnityEngine.Object.DontDestroyOnLoad(driverGo);
        DemoPlayModeDriver driver = driverGo.AddComponent<DemoPlayModeDriver>();
        driver.Initialize(ffmpegPath, SilentVideoPath, AudioLogPath);
    }

    private sealed class DemoPlayModeDriver : MonoBehaviour
    {
        private const int Width = 1280;
        private const int Height = 720;
        private const int Fps = 30;
        private const int MaxHardFrames = 3050; // ~101.6s safety cap

        private enum DemoPhase
        {
            TitleScreen,
            District1RunAndGlide,
            District2DeathAndRespawn,
            RespawnRecovery,
            HighlightDistrict2Ledge,
            HighlightDistrict3Foundry,
            HighlightDistrict4Belltower,
            HighlightDistrict5Viaduct,
            BossApproachAndFight,
            ReachGoalBeacon,
            HoldWinScreen,
            Finishing
        }

        private Process ffmpegProc;
        private Stream ffmpegStdin;
        private StreamWriter audioCsv;
        private RenderTexture rt;
        private Texture2D readTex;
        private byte[] rgbBuffer;
        private Camera mainCam;

        private DemoPhase phase = DemoPhase.TitleScreen;
        private int frameIndex;
        private float phaseStartTime;
        private int subStep;
        private float lastFireTime = -10f;
        private float lastJumpTime = -10f;
        private float lastTeleportTime = -10f;
        private int prevLives = 3;
        private bool bossStarted;
        private bool finished;
        private bool firstTickSkipped;

        private GameManager gm;
        private PlayerController pc;
        private PlayerHealth ph;
        private FinalBossAI boss;
        private LevelGoal goal;

        private float CurrentTimeSec => frameIndex / (float)Fps;

        public void Initialize(string ffmpegPath, string silentOutPath, string audioLogOutPath)
        {
            Time.captureFramerate = Fps;
            Application.runInBackground = true;
            AudioListener.volume = 0f; // Real mix is rendered cleanly in post via logged events

            rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            readTex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            rgbBuffer = new byte[Width * Height * 3];

            audioCsv = new StreamWriter(audioLogOutPath, false) { AutoFlush = true };
            audioCsv.WriteLine("type,time,name,volume,pitch");
            audioCsv.WriteLine("BGM,0.0000,bgm_twilight_rooftops,1.000,1.000");

            AudioManager.OnEditorSfxEvent += HandleSfxEvent;
            AudioManager.OnEditorBgmEvent += HandleBgmEvent;

            string args = $"-y -f rawvideo -pix_fmt rgb24 -s {Width}x{Height} -r {Fps} -i - -vf vflip " +
                          $"-c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p \"{silentOutPath}\"";
            ffmpegProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            ffmpegProc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("Error"))
                {
                    Debug.LogWarning($"[ffmpeg] {e.Data}");
                }
            };
            ffmpegProc.Start();
            ffmpegProc.BeginErrorReadLine();
            ffmpegStdin = ffmpegProc.StandardInput.BaseStream;
            Debug.Log("[DemoVideoRecorder] DemoPlayModeDriver initialized, ffmpeg pipe open.");
        }

        private void HandleSfxEvent(string clipName, float volume, float pitch)
        {
            if (audioCsv == null || finished) return;
            audioCsv.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "SFX,{0:F4},{1},{2:F3},{3:F3}", CurrentTimeSec, clipName, volume, pitch));
        }

        private void HandleBgmEvent(string trackName, float pitch)
        {
            if (audioCsv == null || finished) return;
            audioCsv.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "BGM,{0:F4},{1},1.000,{2:F3}", CurrentTimeSec, trackName, pitch));
        }

        private void LateUpdate()
        {
            if (finished) return;

            // Allow one initial scene Start() pass before capturing frame 0
            if (!firstTickSkipped)
            {
                firstTickSkipped = true;
                return;
            }

            ResolveReferences();
            UpdateChoreography(CurrentTimeSec);
            CaptureAndWriteFrame();
            frameIndex++;

            if (frameIndex % 300 == 0)
            {
                Debug.Log($"[DemoVideoRecorder] Captured {frameIndex} frames ({CurrentTimeSec:F1}s), phase={phase}.");
            }

            if (frameIndex >= MaxHardFrames && !finished)
            {
                Debug.LogWarning("[DemoVideoRecorder] Hit hard frame cap; completing video cleanly.");
                FinishAndExit();
            }
        }

        private void ResolveReferences()
        {
            if (gm == null) gm = GameManager.Instance;
            if (pc == null && gm != null) pc = gm.playerController;
            if (pc == null) pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (ph == null && pc != null) ph = pc.GetComponent<PlayerHealth>();
            if (boss == null) boss = UnityEngine.Object.FindAnyObjectByType<FinalBossAI>();
            if (goal == null) goal = UnityEngine.Object.FindAnyObjectByType<LevelGoal>();
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null && Mathf.Abs(mainCam.aspect - (Width / (float)Height)) > 0.01f)
            {
                mainCam.aspect = Width / (float)Height;
            }
        }

        private void TransitionTo(DemoPhase next)
        {
            phase = next;
            phaseStartTime = CurrentTimeSec;
            subStep = 0;
            Debug.Log($"[DemoVideoRecorder] t={CurrentTimeSec:F2}s -> {next}");
        }

        private void ClearDemoInputs()
        {
            PlayerController.DemoMoveX = 0f;
            PlayerController.DemoJumpDown = false;
            PlayerController.DemoJumpHeld = false;
            PlayerController.DemoGlideHeld = false;
            PlayerController.DemoFlashDown = false;
        }

        private void TriggerJump(float holdCheck = 0f)
        {
            if (CurrentTimeSec - lastJumpTime < 0.24f) return;
            PlayerController.DemoJumpDown = true;
            PlayerController.DemoJumpHeld = true;
            lastJumpTime = CurrentTimeSec;
        }

        private void TriggerFire(float cooldown = 0.28f)
        {
            if (CurrentTimeSec - lastFireTime < cooldown) return;
            PlayerController.DemoFlashDown = true;
            lastFireTime = CurrentTimeSec;
        }

        private void SmoothMoveTo(float x, float y, string zoneLabel = null)
        {
            if (ph == null) return;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayScreenTransitionFlash(0.24f);
                if (!string.IsNullOrEmpty(zoneLabel))
                {
                    UIManager.Instance.UpdateObjective(zoneLabel, string.Empty);
                }
            }
            ph.TeleportTo(new Vector3(x, y, 0f));
            lastTeleportTime = CurrentTimeSec;
        }

        private void UpdateChoreography(float t)
        {
            if (gm == null || pc == null || ph == null) return;

            float pt = t - phaseStartTime;

            switch (phase)
            {
                case DemoPhase.TitleScreen:
                    PlayerController.DemoInputOverrideActive = false;
                    if (t >= 3.3f)
                    {
                        AudioManager.Instance?.PlayUiClick();
                        gm.StartGame();
                        PlayerController.DemoInputOverrideActive = true;
                        prevLives = gm.CurrentLives;
                        TransitionTo(DemoPhase.District1RunAndGlide);
                    }
                    break;

                case DemoPhase.District1RunAndGlide:
                {
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoJumpHeld = (CurrentTimeSec - lastJumpTime) < 0.42f;
                    PlayerController.DemoGlideHeld = false;

                    // Jump gap 1 -> Roof_1B_Step
                    if (x >= 12.6f && x <= 14.5f && pc.IsGrounded) TriggerJump();
                    // Shoot Sentinel_0_Courtyard on Roof_1B
                    if (x >= 14.5f && x <= 19.0f) TriggerFire(0.45f);
                    // Jump gap 2 -> Roof_1C_Launch
                    if (x >= 21.8f && x <= 23.8f && pc.IsGrounded) TriggerJump();
                    // Leap off Roof_1C and engage Shadow-Cloak Glide across chasm
                    if (x >= 29.8f && x <= 32.0f && pc.IsGrounded) TriggerJump();
                    if (x >= 31.2f && x <= 41.8f && !pc.IsGrounded)
                    {
                        PlayerController.DemoGlideHeld = true;
                        PlayerController.DemoJumpHeld = true;
                    }
                    // Zap Caster_1_Landing on Roof_1D
                    if (x >= 43.0f && x <= 48.0f) TriggerFire(0.45f);
                    // Catch Updraft_1_Intro at x=53.5
                    if (x >= 51.8f && x <= 57.5f)
                    {
                        if (pc.IsGrounded) TriggerJump();
                        if (!pc.IsGrounded) PlayerController.DemoGlideHeld = true;
                    }
                    if (x >= 60.0f && y >= 5.0f)
                    {
                        TransitionTo(DemoPhase.District2DeathAndRespawn);
                    }
                    else if (pt >= 18.5f)
                    {
                        // Safety jump directly to District II ledge if timing slipped
                        gm.ActivateCheckpoint(1, new Vector3(45.5f, 1.2f, 0f), "Gale-Draft Overlook");
                        ph.TeleportTo(new Vector3(60.5f, 5.5f, 0f));
                        TransitionTo(DemoPhase.District2DeathAndRespawn);
                    }
                    break;
                }

                case DemoPhase.District2DeathAndRespawn:
                {
                    float x = pc.transform.position.x;
                    ClearDemoInputs();

                    if (gm.CurrentLives < prevLives)
                    {
                        // Respawn just happened!
                        TransitionTo(DemoPhase.RespawnRecovery);
                        break;
                    }

                    if (subStep == 0)
                    {
                        // Walk right toward Pendulum / Spikes on Roof_2B_Upper and take an initial hit
                        PlayerController.DemoMoveX = x < 66.2f ? 1f : 0f;
                        if (x >= 64.8f && ph.CurrentHealth > 30 && !ph.IsInvulnerable)
                        {
                            ph.TakeDamage(75, new Vector2(66.2f, 5.4f));
                            subStep = 1;
                        }
                        else if (pt >= 3.2f && ph.CurrentHealth > 30 && !ph.IsInvulnerable)
                        {
                            ph.TakeDamage(75, new Vector2(x + 1f, 5.4f));
                            subStep = 1;
                        }
                    }
                    else if (subStep == 1)
                    {
                        // After invulnerability fades, take lethal hit to trigger real 0-HP Death & Checkpoint Respawn
                        PlayerController.DemoMoveX = Mathf.Sin(pt * 3f) > 0f ? 0.4f : -0.4f;
                        if (!ph.IsInvulnerable && pt >= 2.2f)
                        {
                            prevLives = gm.CurrentLives;
                            ph.TakeDamage(ph.CurrentHealth + 10, new Vector2(x + 0.6f, pc.transform.position.y));
                            subStep = 2;
                        }
                    }

                    if (pt >= 8.5f)
                    {
                        TransitionTo(DemoPhase.RespawnRecovery);
                    }
                    break;
                }

                case DemoPhase.RespawnRecovery:
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoGlideHeld = false;
                    PlayerController.DemoJumpHeld = false;
                    if (pt >= 2.6f)
                    {
                        SmoothMoveTo(66.5f, 5.5f, "II · AQUEDUCT");
                        TransitionTo(DemoPhase.HighlightDistrict2Ledge);
                    }
                    break;

                case DemoPhase.HighlightDistrict2Ledge:
                {
                    // Demonstrates smooth walking underneath Roof_2_RelicPerch & jumping onto shuttle
                    float x = pc.transform.position.x;
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded && x > 74.0f;
                    PlayerController.DemoJumpHeld = (CurrentTimeSec - lastJumpTime) < 0.4f;
                    if (x >= 72.8f && x <= 74.5f && pc.IsGrounded) TriggerJump();
                    if (x >= 74.2f) TriggerFire(0.4f);

                    if (pt >= 5.5f)
                    {
                        SmoothMoveTo(125.5f, 6.2f, "III · FOUNDRY");
                        TransitionTo(DemoPhase.HighlightDistrict3Foundry);
                    }
                    break;
                }

                case DemoPhase.HighlightDistrict3Foundry:
                {
                    float x = pc.transform.position.x;
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoJumpHeld = (CurrentTimeSec - lastJumpTime) < 0.42f;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded && x >= 127.0f;
                    if ((x >= 126.2f && x <= 127.6f) || (x >= 132.4f && x <= 134.0f))
                    {
                        if (pc.IsGrounded) TriggerJump();
                    }
                    TriggerFire(0.40f);

                    if (pt >= 6.0f)
                    {
                        SmoothMoveTo(241.5f, 5.3f, "IV · BELLTOWER");
                        gm.GrantArenaLightning(4, 50);
                        TransitionTo(DemoPhase.HighlightDistrict4Belltower);
                    }
                    break;
                }

                case DemoPhase.HighlightDistrict4Belltower:
                {
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;
                    PlayerController.DemoGlideHeld = false;
                    PlayerController.DemoJumpHeld = (CurrentTimeSec - lastJumpTime) < 0.45f;

                    if (subStep == 0)
                    {
                        // Ride Updraft_4E_Relic up onto Roof_4_RelicPerch for 3rd Soul Relic
                        PlayerController.DemoMoveX = x < 242.6f ? 1f : (x < 246.2f ? 0.55f : 0f);
                        if (x >= 242.0f && pc.IsGrounded) TriggerJump();
                        if (!pc.IsGrounded) PlayerController.DemoGlideHeld = true;
                        if (y >= 9.4f || pt >= 3.3f) subStep = 1;
                    }
                    else
                    {
                        PlayerController.DemoMoveX = 1f;
                        TriggerFire(0.35f);
                    }

                    if (pt >= 5.5f)
                    {
                        SmoothMoveTo(371.5f, 5.1f, "V · VIADUCT");
                        TransitionTo(DemoPhase.HighlightDistrict5Viaduct);
                    }
                    break;
                }

                case DemoPhase.HighlightDistrict5Viaduct:
                {
                    float x = pc.transform.position.x;
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoJumpHeld = (CurrentTimeSec - lastJumpTime) < 0.35f;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded;
                    if ((x >= 371.8f && x <= 373.2f) || (x >= 380.8f && x <= 382.2f) || x >= 388.0f)
                    {
                        if (pc.IsGrounded) TriggerJump();
                    }
                    TriggerFire(0.38f);

                    if (pt >= 5.0f)
                    {
                        SmoothMoveTo(446.5f, 9.1f, "VI · SANCTUM");
                        ph.Heal(100);
                        if (gm.LightningAmmo < 14) gm.GrantArenaLightning(14 - gm.LightningAmmo, 0);
                        bossStarted = false;
                        TransitionTo(DemoPhase.BossApproachAndFight);
                    }
                    break;
                }

                case DemoPhase.BossApproachAndFight:
                {
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;

                    if (boss == null || boss.IsDefeated)
                    {
                        TransitionTo(DemoPhase.ReachGoalBeacon);
                        break;
                    }

                    // Step into boss arena (x >= 456.0)
                    if (!bossStarted)
                    {
                        PlayerController.DemoMoveX = 1f;
                        PlayerController.DemoJumpHeld = false;
                        PlayerController.DemoGlideHeld = false;
                        if (x >= 459.5f)
                        {
                            bossStarted = true;
                        }
                        break;
                    }

                    // Keep Vesper stocked in long demo clash & prevent accidental death right before victory
                    if (gm.LightningAmmo <= 2)
                    {
                        gm.GrantArenaLightning(6, 0);
                    }
                    if (ph.CurrentHealth <= 25)
                    {
                        ph.Heal(75);
                    }

                    float bx = boss.transform.position.x;
                    float dist = bx - x;
                    float absDist = Mathf.Abs(dist);

                    PlayerController.DemoJumpHeld = (CurrentTimeSec - lastJumpTime) < 0.36f;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded && pc.GetComponent<Rigidbody2D>().linearVelocity.y < 0f && y > 10.8f;

                    if (boss.currentState == FinalBossAI.BossState.Stunned)
                    {
                        // Move toward stunned Malakor, leap overhead and land Crown Stomp!
                        PlayerController.DemoMoveX = Mathf.Abs(bx - x) > 0.45f ? Mathf.Sign(bx - x) : 0f;
                        if (absDist < 5.5f && pc.IsGrounded)
                        {
                            TriggerJump();
                        }
                        else if (absDist >= 3.2f && y < 9.5f)
                        {
                            TriggerFire(0.35f);
                        }
                    }
                    else
                    {
                        // Dodge incoming attacks & maintain clean firing distance (4.5..9.5u away)
                        bool dangerClose = boss.currentState == FinalBossAI.BossState.Attacking ||
                                           boss.currentState == FinalBossAI.BossState.Dashing ||
                                           boss.currentState == FinalBossAI.BossState.Leaping;

                        if (dangerClose && absDist < 6.0f && pc.IsGrounded)
                        {
                            TriggerJump();
                        }

                        // Steer away if cornered or too close, face Malakor briefly when shooting
                        float preferredSign = x < 467.5f ? 1f : -1f;
                        if (absDist < 4.4f)
                        {
                            // Dash toward center updraft/open half of arena
                            PlayerController.DemoMoveX = x < bx ? -1f : 1f;
                            if (x < 457.5f) PlayerController.DemoMoveX = 1f;
                            if (x > 478.2f) PlayerController.DemoMoveX = -1f;
                        }
                        else if (absDist > 9.8f)
                        {
                            PlayerController.DemoMoveX = Mathf.Sign(dist);
                        }
                        else
                        {
                            // Step subtly toward boss so Vesper faces him cleanly when firing
                            PlayerController.DemoMoveX = Mathf.Sign(dist) * 0.35f;
                        }

                        if (Mathf.Sign(pc.FacingDirection) == Mathf.Sign(dist) && absDist < 14f)
                        {
                            TriggerFire(0.40f);
                        }
                        else if (absDist > 1.0f)
                        {
                            // Turn toward boss to fire
                            PlayerController.DemoMoveX = Mathf.Sign(dist);
                            TriggerFire(0.40f);
                        }
                    }

                    // Keep within arena bounds and cleanly accelerate ending if reaching 78.5s mark
                    if (pt >= 25.5f && !boss.IsDefeated)
                    {
                        boss.EditorBypassHitIcd = true;
                        boss.TakeLightningHit(boss.transform.position);
                        boss.EditorBypassHitIcd = false;
                    }
                    break;
                }

                case DemoPhase.ReachGoalBeacon:
                {
                    ClearDemoInputs();
                    if (gm.CurrentState == GameState.Victory)
                    {
                        TransitionTo(DemoPhase.HoldWinScreen);
                        break;
                    }

                    float goalX = goal != null ? goal.transform.position.x : 490.5f;
                    float x = pc.transform.position.x;
                    PlayerController.DemoMoveX = x < goalX ? 1f : -1f;
                    if (pt >= 5.5f)
                    {
                        ph.TeleportTo(new Vector3(goalX, 9.05f, 0f));
                        gm.CompleteLevel();
                        TransitionTo(DemoPhase.HoldWinScreen);
                    }
                    break;
                }

                case DemoPhase.HoldWinScreen:
                    ClearDemoInputs();
                    PlayerController.DemoInputOverrideActive = false;
                    if (pt >= 6.0f)
                    {
                        FinishAndExit();
                    }
                    break;
            }
        }

        private void CaptureAndWriteFrame()
        {
            if (mainCam == null || ffmpegStdin == null || finished) return;

            RenderTexture prevTarget = mainCam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            mainCam.targetTexture = rt;
            mainCam.Render();
            RenderTexture.active = rt;
            readTex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            mainCam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            NativeArray<byte> raw = readTex.GetRawTextureData<byte>();
            raw.CopyTo(rgbBuffer);
            ffmpegStdin.Write(rgbBuffer, 0, rgbBuffer.Length);
        }

        private void FinishAndExit()
        {
            if (finished) return;
            finished = true;
            PlayerController.DemoInputOverrideActive = false;
            AudioManager.OnEditorSfxEvent -= HandleSfxEvent;
            AudioManager.OnEditorBgmEvent -= HandleBgmEvent;

            try
            {
                audioCsv?.Flush();
                audioCsv?.Close();
            }
            catch { }

            try
            {
                ffmpegStdin?.Flush();
                ffmpegStdin?.Close();
                if (ffmpegProc != null && !ffmpegProc.HasExited)
                {
                    ffmpegProc.WaitForExit(15000);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DemoVideoRecorder] Error finalizing ffmpeg: {ex.Message}");
            }

            if (rt != null) rt.Release();
            Debug.Log($"[DemoVideoRecorder] Completed recording {frameIndex} frames ({CurrentTimeSec:F2}s).");
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        private void OnDestroy()
        {
            AudioManager.OnEditorSfxEvent -= HandleSfxEvent;
            AudioManager.OnEditorBgmEvent -= HandleBgmEvent;
            PlayerController.DemoInputOverrideActive = false;
        }
    }
}
#endif
