# Vesper: Twilight Courier (CSC 231 Project 2 — Platformer MVP)

A 2D atmospheric platformer built in **Unity 6000.5.1f1** (URP 2D, Unity Input System, uGUI) for **CSC 231 Project 2: Platformer MVP (Level 1)**.

---

## Character Pitch & Backstory

**Vesper** is a *shadow-stitcher courier* working the high skylines of **Noctis Reach**, a dusk-shrouded clockwork city whose district lanterns rely on crystallized **Memory Shards**. Wrapped in an enchanted dusk-woven cloak trimmed with luminescent cyan threads, Vesper runs rooftops, rides rising thermal chimneys, and delivers lost shards to reignite the **Sanctum Beacon** before nightfall freezes the district.

- **Signature Move — Shadow-Cloak Glide**: Holding **Space** or **Shift** in mid-air unfurls Vesper's shadow-stitched cloak to slow her descent (`-2.2 u/s`), boost horizontal air control (`1.2x`), and catch rising **Updraft Vents** to soar across wide chasms and climb towering spires.
- **Animations**: `Idle`, `Run` (2-frame cycle), `Jump`, `Fall`, `Glide`, and `Hurt` driven cleanly via `VesperAnimator.controller`.

---

## Controls

| Action | Keyboard | Gamepad |
| :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` or `Left` / `Right` Arrow | Left Stick |
| **Jump** | `Space`, `W`, or `Up` Arrow (variable height) | South Button (`A` / Cross) |
| **Shadow-Cloak Glide** | Hold `Space` (after apex) or `Left/Right Shift` in air | Hold South Button or Right Trigger/Shoulder |
| **Stomp Sentinel Drone** | Land on top of a patrolling drone | Land on top of a patrolling drone |
| **Pause / Resume** | `Esc` or `P` | HUD Pause Button |
| **Audio Volume & Mute** | HUD top-right slider & Mute button, or Pause menu | — |

---

## Level 1 Design & Progression (2–5 Minute Run)

Level 1 (**Noctis Reach Rooftops**, ~214 world units across) is split into four paced zones that safely introduce every mechanic before testing the player:

1. **Zone 1 — Courtyard of Whispers**: Safe starting plaza teaching horizontal movement, jumping, collecting floating **Memory Shards**, and a risk-free **Shadow-Cloak Glide** training gap (`Roof_1C_Launch` to `Roof_1D_Landing`) with a safety roof floor (`Roof_1_SafetyFloor`) beneath it.
2. **Zone 2 — Gale-Draft Chimneys**: Introduces **Checkpoint 1 (`Gale-Draft Overlook`)**, a low-risk **Updraft Vent** demonstration (`Updraft_1_Intro`) with rising cyan wind particles, and **Hazard Type #1 (Electrified Roof Spikes)** on a long safe rooftop runway before combining them over real gaps.
3. **Zone 3 — Clockwork Foundry Roofs**: Introduces **Checkpoint 2 (`Clockwork Foundry Watch`)** and **Hazard Type #2 (Clockwork Sentinel Drones)** on a flat hazard-free plaza so players can safely practice stomping drones from above (`stompBounceVelocity = 12.5`), followed by dual high/low updraft routes leading to **Checkpoint 3 (`Sanctum Stair Gate`)**.
4. **Zone 4 — Twilight Sanctum Ascent**: Combines patrolling sentinels, dual updrafts (`Updraft_4_AscentA` & `Updraft_5_AscentB`), mid-air glides, and final rooftop spikes culminating at the **Sanctum Beacon (`SanctumBeacon_Goal`)** and the **Level Complete** screen.

---

## Menus, Audio & Visual Polish

- **5 UI Screens (`UIManager.cs`)**: Title Screen (*Start Run* / *Quit*), Live HUD (3 Heart icons, `MEMORY SHARDS: X / 25` counter, level timer, Shadow-Cloak status badge, tutorial/checkpoint banners, volume slider, mute toggle, Pause button), Pause Menu (*Resume*, *Restart Level*, Volume/Mute, *Main Menu*, *Quit*), Level Complete Win Screen (shards, clear time, retries), and Game Over Screen (*Retry Checkpoint*, *Restart Level*, *Main Menu*).
- **Unity Particle Systems (6+ effects)**: Jump dust burst, landing dust burst, looping Shadow-Cloak glide trail, crimson hurt sparks, collectible pickup bursts, checkpoint ignition sparks & ember flame, visible Updraft rising wind streams, sentinel defeat sparks, camera ambient twilight motes, and Sanctum Beacon celebration particles.
- **Audio & Feedback**: Original synthesized `.wav` SFX (`jump`, `land`, `collect`, `hurt`, `checkpoint`, `glide`, `win`) and looping ambient twilight music track (`bgm_twilight_rooftops.wav`), paired with camera look-ahead, trauma screen shake, and invulnerability hit-flash.
- **3-Layer Parallax Background**: Distant crescent-moon sky (`0.86x`), mid-distance Gothic spires & clocktowers (`0.58x`), and foreground spire silhouettes (`0.32x`) anchored smoothly via `ParallaxLayer.cs`.

---

## Code Walkthrough (Study Reference for Check-Ins)

- `Assets/Scripts/Player/PlayerController.cs`: Reads Unity `InputSystem` input (`Keyboard.current` / `Gamepad.current`) with Legacy fallback; applies coyote time (`0.12s`), jump buffering (`0.12s`), variable jump cut (`0.5x`), `Shadow-Cloak Glide` vertical clamp (`-2.2f`), updraft lifting (`UpdraftVent`), ground `Physics2D.OverlapBox` checks, sprite facing, animator parameters (`Speed`, `VerticalVelocity`, `IsGrounded`, `IsGliding`, `IsHurt`, `IsDead`), and movement particles.
- `Assets/Scripts/Player/PlayerHealth.cs`: Tracks 3 Hearts, handles invulnerability frames (`1.2s`), crimson/alpha hit flash coroutine, knockback recoil, abyss fall respawn (`HandleAbyssFall`), checkpoint revival (`ReviveAtCheckpoint`), and Game Over triggering.
- `Assets/Scripts/Core/GameManager.cs`: Singleton state machine (`Title`, `Playing`, `Paused`, `GameOver`, `Victory`) tracking collected Memory Shards, elapsed time, death retries, active checkpoint position, and scene reload/retry lifecycle.
- `Assets/Scripts/UI/UIManager.cs`: Controls visibility across all 5 UI panels and synchronizes heart colors, shard counter, timer, glide badge, timed tutorial/checkpoint banners, and volume/mute controls.
- `Assets/Scripts/Audio/AudioManager.cs`: Persistent audio singleton managing `bgmSource` (looping music) and `sfxSource` (`PlayOneShot` SFX), persisting volume and mute preferences via `PlayerPrefs`.
- `Assets/Scripts/Camera/CameraController.cs` & `ParallaxLayer.cs`: `Vector3.SmoothDamp` camera follow with horizontal look-ahead, minimum Y clamp, decaying random screen shake (`TriggerShake`), and multi-layer parallax scrolling.
- `Assets/Scripts/Level/`: Modular level components for `CollectibleShard.cs`, `Checkpoint.cs`, `Hazard.cs` (spikes & abyss killzone), `PatrolEnemy.cs` (drone patrol + top stomp bounce), `UpdraftVent.cs`, `TutorialTrigger.cs`, and `LevelGoal.cs`.
- `Assets/Editor/LevelBuilder.cs`: Deterministic Editor pipeline that configures sprite PPU/pivots, builds `VesperAnimator.controller` + `.anim` clips, generates UI/particle assets, assembles `Assets/Scenes/SampleScene.unity`, renders verification screenshots (`Screenshots/`), and builds the Windows standalone executable.

---

## Standalone Build

- **Executable**: `Builds/Windows/VesperTwilightCourier.exe`
- **Archived Distribution**: `Builds/VesperTwilightCourier_WindowsBuild.zip`
- Built via `BuildPipeline.BuildPlayer` for `StandaloneWindows64`; extract the zip and run `VesperTwilightCourier.exe` directly without Unity installed.

---

## Tools & Credits

- **Game Engine & Packages**: Unity `6000.5.1f1` (Universal Render Pipeline 2D `17.5.0`, Input System `1.19.0`, uGUI `2.5.0`).
- **AI & Automation Disclosure**:
  - **Google Antigravity**: Used to author C# gameplay/UI scripts, the deterministic Unity Editor scene builder (`LevelBuilder.cs`), documentation, and nightly commit automation.
  - **Antigravity Image Generator (`image-generator`) + Python PIL Post-Processing**: Used to generate cohesive original 2D sprites (`Vesper`, `Clockwork Sentinel`, `Electrified Spikes`, `Memory Shard`, `Watchtower Lantern`, `Sanctum Beacon`, `Rooftop Masonry Tile`, and `Parallax Skylines`) chroma-keyed to clean RGBA transparency (`Assets/Art/Sprites/`).
  - **Procedural Audio Synthesis**: All sound effects and the looping background track in `Assets/Audio/` were synthesized programmatically via Python `wave`/DSP math with zero third-party copyrighted audio samples.
- **Asset & Code License**: All scripts, sprites, and synthesized audio assets in this repository are original project assets created for CSC 231 academic coursework (MIT / CC0-compatible for educational review).
