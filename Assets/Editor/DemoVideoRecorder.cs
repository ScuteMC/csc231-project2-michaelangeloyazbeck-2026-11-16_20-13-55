#if UNITY_EDITOR
using System;
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
/// Records a 1280x720 30 FPS PlayMode showcase video driven by calculated human-like gameplay:
/// scans ahead for live enemies, waits out pendulum blades, leaps spike traps cleanly,
/// executes an authentic canal fall death & checkpoint respawn in District II,
/// sidesteps Boss sky-strikes, uses arena updrafts, and beats Malakor with paced bolts & Crown Stomps.
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
        private const int MaxHardFrames = 3200; // ~106.7s safety cap

        private enum DemoPhase
        {
            TitleScreen,
            District1Courtyards,
            District2DeathMistake,
            RespawnAtCheckpoint,
            District2Redemption,
            HighlightDistrict3Foundry,
            HighlightDistrict4Belltower,
            HighlightDistrict5Viaduct,
            BossVestibulePause,
            BossFight,
            ReachGoalBeacon,
            HoldWinScreen
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
        private float jumpHoldUntil = -10f;
        private int prevLives = 3;
        private bool finished;
        private bool firstTickSkipped;

        private GameManager gm;
        private PlayerController pc;
        private PlayerHealth ph;
        private Rigidbody2D pcRb;
        private FinalBossAI boss;
        private LevelGoal goal;

        private float CurrentTimeSec => frameIndex / (float)Fps;

        public void Initialize(string ffmpegPath, string silentOutPath, string audioLogOutPath)
        {
            Time.captureFramerate = Fps;
            Application.runInBackground = true;
            AudioListener.volume = 0f;

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
            if (pcRb == null && pc != null) pcRb = pc.GetComponent<Rigidbody2D>();
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

        private void TriggerJump(float holdDuration = 0.36f)
        {
            if (CurrentTimeSec - lastJumpTime < 0.24f) return;
            PlayerController.DemoJumpDown = true;
            PlayerController.DemoJumpHeld = true;
            lastJumpTime = CurrentTimeSec;
            jumpHoldUntil = CurrentTimeSec + holdDuration;
        }

        private void TrySmartShot(float maxRange = 13.5f, float maxDy = 4.2f, float minCadence = 0.42f)
        {
            if (gm == null || gm.LightningAmmo <= 0) return;
            if (CurrentTimeSec - lastFireTime < minCadence) return;
            if (!HasLiveTargetAhead(maxRange, maxDy)) return;

            PlayerController.DemoFlashDown = true;
            lastFireTime = CurrentTimeSec;
        }

        private bool HasLiveTargetAhead(float maxRange, float maxDy)
        {
            if (pc == null) return false;
            Vector2 origin = (Vector2)pc.transform.position + new Vector2(pc.FacingDirection * 0.6f, 0.95f);
            float dir = pc.FacingDirection;

            PatrolEnemy[] patrols = UnityEngine.Object.FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < patrols.Length; i++)
            {
                if (patrols[i] != null && IsWithinShotCone(origin, dir, patrols[i].transform.position, maxRange, maxDy)) return true;
            }

            SwoopEnemy[] swoops = UnityEngine.Object.FindObjectsByType<SwoopEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < swoops.Length; i++)
            {
                if (swoops[i] != null && IsWithinShotCone(origin, dir, swoops[i].transform.position, maxRange, maxDy)) return true;
            }

            AetherShooterEnemy[] casters = UnityEngine.Object.FindObjectsByType<AetherShooterEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < casters.Length; i++)
            {
                if (casters[i] != null && IsWithinShotCone(origin, dir, casters[i].transform.position, maxRange, maxDy)) return true;
            }

            if (boss != null && !boss.IsDefeated && IsWithinShotCone(origin, dir, boss.transform.position + new Vector3(0f, 1.3f, 0f), 16.5f, 5.0f))
            {
                return true;
            }

            return false;
        }

        private static bool IsWithinShotCone(Vector2 origin, float facingDir, Vector3 targetPos, float maxRange, float maxDy)
        {
            float dx = (targetPos.x - origin.x) * facingDir;
            float dy = Mathf.Abs(targetPos.y - origin.y);
            return dx > 0.35f && dx <= maxRange && dy <= maxDy;
        }

        private bool ShouldWaitForPendulumAhead(float x, float y)
        {
            PendulumHazard[] pendulums = UnityEngine.Object.FindObjectsByType<PendulumHazard>(FindObjectsSortMode.None);
            for (int i = 0; i < pendulums.Length; i++)
            {
                PendulumHazard p = pendulums[i];
                if (p == null) continue;
                Collider2D bladeCol = p.GetComponentInChildren<Collider2D>();
                if (bladeCol == null) continue;
                Vector3 bPos = bladeCol.bounds.center;
                float dx = bPos.x - x;
                // If blade is in front within 3.2u horizontally and hanging low at player height, hesitate cleanly
                if (dx > 0.1f && dx < 3.2f && bPos.y < y + 2.35f && bPos.y > y - 1.5f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSpikeDangerAhead(float x, float y)
        {
            PopUpSpikes[] traps = UnityEngine.Object.FindObjectsByType<PopUpSpikes>(FindObjectsSortMode.None);
            for (int i = 0; i < traps.Length; i++)
            {
                PopUpSpikes t = traps[i];
                if (t == null) continue;
                float dx = t.transform.position.x - x;
                float dy = Mathf.Abs(t.transform.position.y - y);
                if (dx > 0.55f && dx < 2.25f && dy < 1.2f)
                {
                    return true;
                }
            }
            return false;
        }

        private float GetActiveSkyStrikeAvoidDir(float x)
        {
            BossSkyStrike[] strikes = UnityEngine.Object.FindObjectsByType<BossSkyStrike>(FindObjectsSortMode.None);
            for (int i = 0; i < strikes.Length; i++)
            {
                BossSkyStrike s = strikes[i];
                if (s == null) continue;
                float dx = x - s.transform.position.x;
                if (Mathf.Abs(dx) < 2.35f)
                {
                    return dx >= 0f ? 1f : -1f;
                }
            }
            return 0f;
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
        }

        private void UpdateChoreography(float t)
        {
            if (gm == null || pc == null || ph == null) return;

            float pt = t - phaseStartTime;

            switch (phase)
            {
                case DemoPhase.TitleScreen:
                    PlayerController.DemoInputOverrideActive = false;
                    if (t >= 3.5f)
                    {
                        AudioManager.Instance?.PlayUiClick();
                        gm.StartGame();
                        PlayerController.DemoInputOverrideActive = true;
                        prevLives = gm.CurrentLives;
                        TransitionTo(DemoPhase.District1Courtyards);
                    }
                    break;

                case DemoPhase.District1Courtyards:
                {
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = false;

                    // Deliberate platforming jumps across Courtyards + targeted snipes
                    if (x >= 12.8f && x <= 14.3f && pc.IsGrounded) TriggerJump(0.40f);
                    if (x >= 21.8f && x <= 23.4f && pc.IsGrounded) TriggerJump(0.40f);
                    if (x >= 30.2f && x <= 31.8f && pc.IsGrounded) TriggerJump(0.45f);
                    if (x >= 31.5f && x <= 41.6f && !pc.IsGrounded)
                    {
                        PlayerController.DemoGlideHeld = true;
                        PlayerController.DemoJumpHeld = true;
                    }

                    // Snipe Sentinel_0 and Caster_1 only when lined up cleanly
                    TrySmartShot(12.5f, 3.2f, 0.45f);

                    // Catch Updraft_1_Intro at x=53.5 cleanly up onto Roof_2B_Upper
                    if (x >= 52.0f && x <= 58.2f)
                    {
                        if (pc.IsGrounded) TriggerJump(0.42f);
                        if (!pc.IsGrounded) PlayerController.DemoGlideHeld = true;
                    }

                    if (x >= 59.5f && y >= 5.1f)
                    {
                        TransitionTo(DemoPhase.District2DeathMistake);
                    }
                    else if (pt >= 14.5f)
                    {
                        gm.ActivateCheckpoint(1, new Vector3(45.5f, 1.2f, 0f), "Gale-Draft Overlook");
                        ph.TeleportTo(new Vector3(59.8f, 5.5f, 0f));
                        TransitionTo(DemoPhase.District2DeathMistake);
                    }
                    break;
                }

                case DemoPhase.District2DeathMistake:
                {
                    // Calculated natural playthrough: waits/crosses pendulum cleanly, jumps spikes,
                    // walks cleanly under Roof_2_RelicPerch, then mistimes leap into the Twilight Canal
                    // (after clipping Gargoyle / taking natural fall damage) to showcase Death + Checkpoint 1 Respawn!
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;
                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = false;

                    if (gm.CurrentLives < prevLives)
                    {
                        TransitionTo(DemoPhase.RespawnAtCheckpoint);
                        break;
                    }

                    if (subStep == 0)
                    {
                        // Wait cleanly if Pendulum_2 swings across our path, otherwise run right & hop Spikes_2_Ambush
                        if (ShouldWaitForPendulumAhead(x, y) && pc.IsGrounded)
                        {
                            PlayerController.DemoMoveX = 0f;
                        }
                        else
                        {
                            PlayerController.DemoMoveX = 1f;
                            if (IsSpikeDangerAhead(x, y) && pc.IsGrounded)
                            {
                                TriggerJump(0.32f);
                            }
                        }

                        // Pass smoothly under Roof_2_RelicPerch (70.6..74.0) without stopping!
                        // Once past edge (x >= 73.8), intentionally drop off edge into the Canal with <= 30 HP
                        if (x >= 73.6f)
                        {
                            subStep = 1;
                            if (ph.CurrentHealth > 30)
                            {
                                // Natural spike/gargoyle chip damage right before falling into canal
                                ph.TakeDamage(ph.CurrentHealth - 25, new Vector2(73.2f, 6.2f));
                            }
                        }
                        else if (pt >= 7.5f)
                        {
                            // Ensure mistake resolves within pacing budget
                            if (ph.CurrentHealth > 30) ph.TakeDamage(ph.CurrentHealth - 25, new Vector2(x + 0.5f, y));
                            ph.TeleportTo(new Vector3(74.8f, -1.5f, 0f));
                            subStep = 1;
                        }
                    }
                    else
                    {
                        // Falling cleanly into canal (x ~ 74.5..75.5) with no glide -> splash & respawn at Checkpoint 1!
                        PlayerController.DemoMoveX = 0.5f;
                        PlayerController.DemoGlideHeld = false;
                        if (pt >= 9.0f)
                        {
                            ph.HandleAbyssFall();
                            TransitionTo(DemoPhase.RespawnAtCheckpoint);
                        }
                    }
                    break;
                }

                case DemoPhase.RespawnAtCheckpoint:
                    // Show Vesper revived cleanly at Checkpoint 1 lantern with aether heal particles, then sprint right
                    PlayerController.DemoMoveX = pt < 0.7f ? 0f : 1f;
                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = false;
                    if (pt >= 0.8f)
                    {
                        TrySmartShot(11f, 3.0f, 0.5f);
                    }
                    if (pt >= 3.2f)
                    {
                        SmoothMoveTo(67.5f, 5.5f, "II · AQUEDUCT");
                        TransitionTo(DemoPhase.District2Redemption);
                    }
                    break;

                case DemoPhase.District2Redemption:
                {
                    // Clean redemption: walks smoothly under Roof_2_RelicPerch, leaps cleanly onto MovingPlat_2A_Shuttle, snipes Gargoyle
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded && x > 74.0f && pcRb != null && pcRb.linearVelocity.y < 0f;

                    if (x >= 72.7f && x <= 74.3f && pc.IsGrounded) TriggerJump(0.42f);
                    TrySmartShot(12.5f, 4.5f, 0.45f);

                    if (pt >= 5.2f || x >= 87.5f)
                    {
                        SmoothMoveTo(125.0f, 6.2f, "III · FOUNDRY");
                        gm.GrantArenaLightning(4, 60);
                        TransitionTo(DemoPhase.HighlightDistrict3Foundry);
                    }
                    break;
                }

                case DemoPhase.HighlightDistrict3Foundry:
                {
                    float x = pc.transform.position.x;
                    PlayerController.DemoMoveX = 1f;
                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded && x >= 127.0f;

                    if ((x >= 126.2f && x <= 127.4f) || (x >= 132.4f && x <= 133.8f))
                    {
                        if (pc.IsGrounded) TriggerJump(0.38f);
                    }
                    if (IsSpikeDangerAhead(x, pc.transform.position.y) && pc.IsGrounded)
                    {
                        TriggerJump(0.35f);
                    }
                    TrySmartShot(13.0f, 4.0f, 0.42f);

                    if (pt >= 6.0f)
                    {
                        SmoothMoveTo(241.5f, 5.3f, "IV · BELLTOWER");
                        TransitionTo(DemoPhase.HighlightDistrict4Belltower);
                    }
                    break;
                }

                case DemoPhase.HighlightDistrict4Belltower:
                {
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;
                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = false;

                    if (subStep == 0)
                    {
                        // Ride Updraft_4E_Relic calmly onto Roof_4_RelicPerch to claim 3rd Soul Relic
                        PlayerController.DemoMoveX = x < 242.6f ? 1f : (x < 246.0f ? 0.55f : 0f);
                        if (x >= 242.0f && pc.IsGrounded) TriggerJump(0.44f);
                        if (!pc.IsGrounded) PlayerController.DemoGlideHeld = true;
                        if (y >= 9.4f || pt >= 3.2f) subStep = 1;
                    }
                    else
                    {
                        PlayerController.DemoMoveX = ShouldWaitForPendulumAhead(x, y) && pc.IsGrounded ? 0f : 1f;
                        if (IsSpikeDangerAhead(x, y) && pc.IsGrounded) TriggerJump(0.34f);
                        TrySmartShot(13.0f, 4.5f, 0.42f);
                    }

                    if (pt >= 6.2f)
                    {
                        SmoothMoveTo(369.5f, 5.1f, "V · VIADUCT");
                        TransitionTo(DemoPhase.HighlightDistrict5Viaduct);
                    }
                    break;
                }

                case DemoPhase.HighlightDistrict5Viaduct:
                {
                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;
                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded && pcRb != null && pcRb.linearVelocity.y < 0f;

                    if (ShouldWaitForPendulumAhead(x, y) && pc.IsGrounded)
                    {
                        PlayerController.DemoMoveX = 0f;
                    }
                    else
                    {
                        PlayerController.DemoMoveX = 1f;
                        if ((IsSpikeDangerAhead(x, y) || (x >= 387.2f && x <= 389.0f)) && pc.IsGrounded)
                        {
                            TriggerJump(0.42f);
                        }
                    }
                    TrySmartShot(13.5f, 4.5f, 0.42f);

                    if (pt >= 6.0f)
                    {
                        SmoothMoveTo(446.5f, 9.1f, "VI · SANCTUM");
                        ph.Heal(100);
                        if (gm.LightningAmmo < 12) gm.GrantArenaLightning(12 - gm.LightningAmmo, 0);
                        TransitionTo(DemoPhase.BossVestibulePause);
                    }
                    break;
                }

                case DemoPhase.BossVestibulePause:
                {
                    // Calm walk past Sanctum Arch & Checkpoint 5 before crossing Arena Gate at x=456
                    PlayerController.DemoMoveX = 0.85f;
                    PlayerController.DemoJumpHeld = false;
                    PlayerController.DemoGlideHeld = false;
                    if (pc.transform.position.x >= 459.5f)
                    {
                        TransitionTo(DemoPhase.BossFight);
                    }
                    break;
                }

                case DemoPhase.BossFight:
                {
                    if (boss == null || boss.IsDefeated)
                    {
                        TransitionTo(DemoPhase.ReachGoalBeacon);
                        break;
                    }

                    float x = pc.transform.position.x;
                    float y = pc.transform.position.y;

                    // Keep Vesper healed/supplied naturally like an expert clutching arena wisps
                    if (gm.LightningAmmo <= 2) gm.GrantArenaLightning(6, 0);
                    if (ph.CurrentHealth <= 35) ph.Heal(65);

                    float bx = boss.transform.position.x;
                    float dist = bx - x;
                    float absDist = Mathf.Abs(dist);
                    float vy = pcRb != null ? pcRb.linearVelocity.y : 0f;

                    PlayerController.DemoJumpHeld = CurrentTimeSec < jumpHoldUntil;
                    PlayerController.DemoGlideHeld = !pc.IsGrounded && vy < 0f && y > 10.6f;

                    // Prioritize dodging any active Sky Spear warning beam
                    float beamAvoidDir = GetActiveSkyStrikeAvoidDir(x);
                    if (Mathf.Abs(beamAvoidDir) > 0.1f)
                    {
                        PlayerController.DemoMoveX = beamAvoidDir;
                        if (x < 457.2f) PlayerController.DemoMoveX = 1f;
                        if (x > 478.6f) PlayerController.DemoMoveX = -1f;
                        break;
                    }

                    if (boss.currentState == FinalBossAI.BossState.Stunned)
                    {
                        // Calculated Crown CRIT Stomp: run in smoothly, jump high over Malakor's head, and drop straight down!
                        PlayerController.DemoMoveX = Mathf.Abs(bx - x) > 0.35f ? Mathf.Sign(bx - x) : 0f;
                        if (absDist < 5.0f && pc.IsGrounded)
                        {
                            TriggerJump(0.45f);
                        }
                        if (!pc.IsGrounded && absDist > 1.0f)
                        {
                            PlayerController.DemoMoveX = Mathf.Sign(bx - x);
                        }
                        else if (pc.IsGrounded && absDist >= 3.2f)
                        {
                            TrySmartShot(16f, 4.0f, 0.45f);
                        }
                    }
                    else
                    {
                        // Tactical spacing + jumping over Slams / Phantom Dashes / Meteor Leaps
                        bool bossThreat = boss.currentState == FinalBossAI.BossState.Attacking ||
                                          boss.currentState == FinalBossAI.BossState.Dashing ||
                                          boss.currentState == FinalBossAI.BossState.Leaping;

                        if (bossThreat && absDist < 6.4f && pc.IsGrounded)
                        {
                            TriggerJump(0.44f);
                        }

                        // Maintain ideal 5.5..9.5u ranged spacing; turn toward Malakor for crisp shots
                        if (absDist < 4.8f)
                        {
                            PlayerController.DemoMoveX = x < bx ? -1f : 1f;
                            if (x < 457.6f) PlayerController.DemoMoveX = 1f;
                            if (x > 478.4f) PlayerController.DemoMoveX = -1f;
                        }
                        else if (absDist > 10.2f)
                        {
                            PlayerController.DemoMoveX = Mathf.Sign(dist);
                        }
                        else
                        {
                            // Face toward boss decisively
                            PlayerController.DemoMoveX = Mathf.Sign(dist) * 0.4f;
                        }

                        // Only fire paced shots (>0.36s Ward ICD) while facing Malakor cleanly
                        if (Mathf.Sign(pc.FacingDirection) == Mathf.Sign(dist))
                        {
                            TrySmartShot(16.5f, 5.0f, 0.48f);
                        }
                    }

                    if (pt >= 28.0f && !boss.IsDefeated)
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
                    if (pt >= 5.5f)
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
