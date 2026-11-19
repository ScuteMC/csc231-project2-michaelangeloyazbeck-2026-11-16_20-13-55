# Vesper: Twilight Courier (CSC 231 Project 2 — Platformer MVP)

A 2D atmospheric platformer built in **Unity 6000.5.1f1** (URP 2D, Unity Input System, uGUI) for **CSC 231 Project 2: Platformer MVP (Level 1)**.

---

## Character Pitch & Backstory

**Vesper** is a *shadow-stitcher courier* working the high skylines of **Noctis Reach**, a dusk-shrouded clockwork city whose district lanterns rely on crystallized **Memory Shards**. Wrapped in an enchanted dusk-woven cloak trimmed with luminescent cyan threads, Vesper runs rooftops above the moonlit **Twilight Canal**, rides rising thermal chimneys and clockwork lifts, dodges ambush traps, and delivers lost shards to reignite the **Sanctum Beacon** before nightfall freezes the district.

- **Signature Move — Shadow-Cloak Glide**: Holding **Space** or **Shift** in mid-air unfurls Vesper's shadow-stitched cloak to slow her descent (`-2.2 u/s`), boost horizontal air control (`1.2x`), stream shadow-ribbon trail particles, and catch rising **Updraft Vents** to soar across wide chasms and climb towering spires.
- **Multi-Keyframe Animations (Real Unity `AnimationClip`s + `AnimatorController`s)**:
  - **Vesper (`VesperAnimator.controller`)**: `Idle` (6 frames), `Run` (6 frames), `Jump` (4 frames), `Fall` (4 frames), `Glide` (4 frames), and `Hurt` (4 frames).
  - **World & Entities**: `SentinelAnimator` (6-frame wingbeat & scanning eye), `ShardAnimator` (6-frame prism rotation & glint), `CheckpointAnimator` (4-frame lantern flicker), `GoalBeaconAnimator` (6-frame sanctum pulse), and `WaterCanalAnimator` (6-frame seamless moonlit wave animation).

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

Level 1 (**Noctis Reach Rooftops**, ~230 world units across) is built with strictly disjoint, zero-overlap architecture (automatically verified at build time by `LevelBuilder.ValidateNoPlatformOverlaps()` across all 18 static, crumbling, and swept moving-platform footprints) over a continuous animated **Twilight Canal**:

1. **Zone 1 — Courtyard of Whispers**: Safe starting plaza grounded straight into the Twilight Canal, teaching horizontal movement, jumping, collecting **Memory Shards**, and a risk-free **Shadow-Cloak Glide** training chasm (`Roof_1C_Launch` to `Roof_1D_Landing`) with a recessed, non-overlapping safety floor and step-up block below.
2. **Zone 2 — Gale-Draft Chimneys**: Introduces **Checkpoint 1 (`Gale-Draft Overlook`)**, a swirling **Updraft Vent** (`Updraft_1_Intro`), an elevated aqueduct (`Roof_2B_Upper`) supported by grounded **Gothic Stone Pillars**, **Ground-Embedded Pop-Up Spikes** (`PopUpSpikes.cs` hidden inside recessed floor grates via Unity `SpriteMask`, sparking briefly before erupting when crossed or on staggered timed cycles), and a non-colliding **Clockwork Moving Platform (`MovingPlat_2A_Shuttle`)** with glowing guide rail and thruster jets.
3. **Zone 3 — Clockwork Foundry Roofs**: Introduces **Checkpoint 2 (`Clockwork Foundry Watch`)**, patrolling **Clockwork Sentinel Drones** (`PatrolEnemy.cs`, stomp-bounceable from above), a **Crumbling Stone Span (`Crumble_3A_Span`)** that shudders and collapses with tumbling debris chunks, dual non-colliding **Moving Platforms (`MovingPlat_3A_VerticalLift` & `MovingPlat_3B_Shuttle`)** with ≥1.6u swept clearance, and a twin staggered pop-up spike gauntlet leading to **Checkpoint 3 (`Sanctum Stair Gate`)**.
4. **Zone 4 — Twilight Sanctum Ascent**: Combines patrolling sentinels, dual chasm updrafts (`Updraft_4_AscentA` & `Updraft_5_AscentB`), a fragile mid-air **Crumbling Perch (`Crumble_4B_Perch`)**, a final concealed ambush spike grate, and the **Sanctum Beacon (`SanctumBeacon_Goal`)** culminating in the **Level Complete** screen.

---

## Menus, Audio & Visual Polish

- **5 UI Screens (`UIManager.cs`)**: Title Screen (*Start Run* / *Quit*), Live HUD (3 Heart icons, `MEMORY SHARDS: X / 25` counter with pickup scale-pop, level timer, Shadow-Cloak status badge, tutorial/checkpoint banners, volume slider, mute toggle, Pause button), Pause Menu (*Resume*, *Restart Level*, Volume/Mute, *Main Menu*, *Quit*), Level Complete Win Screen (shards, clear time, retries), and Game Over Screen (*Retry Checkpoint*, *Restart Level*, *Main Menu*).
- **Unity Particle Systems (Full Module Showcase)**:
  - **TrailsModule**: Shadow-cloak glide ribbons, swirling updraft wind vortices, crackling spike-trap telegraph lightning arcs, water canal splash arcs, shard/checkpoint/hurt/sentinel-defeat bursts, and the Sanctum Beacon rising pillar & victory fireworks.
  - **NoiseModule**: Organic twilight firefly drift (`AmbientTwilightParticles`), river mist rising over the Twilight Canal (`CanalMistParticles`), turbulent updraft vortices, checkpoint ember flicker, and sentinel exhaust wisps.
  - **VelocityOverLifetime & RotationOverLifetime**: Backward shadow-ribbon flow, upward canal mist & beacon plumes, and tumbling stone rubble chunks (`CrumbleDebrisParticles`).
- **Audio & Feedback**: Original synthesized `.wav` SFX (`jump`, `land`, `collect`, `hurt`, `checkpoint`, `glide`, `win`) and looping ambient twilight music track (`bgm_twilight_rooftops.wav`), paired with camera look-ahead, trauma screen shake, squash-and-stretch landing/jumping, and invulnerability hit-flash.
- **3-Layer Parallax Background + Animated Water**: Distant crescent-moon sky (`0.86x`), mid-distance Gothic spires & clocktowers (`0.58x`), foreground spire silhouettes (`0.32x`), and dual-layer animated Twilight Canal water (`WaterBackLayer` behind masonry + translucent `WaterFrontLayer` lapping building bases).

---

## Screenshots

| Title & Courtyard | Glide & Safety Ledge |
| :---: | :---: |
| ![Gameplay Preview](Screenshots/gameplay_preview.png) | ![Glide Preview](Screenshots/glide_and_particles_preview.png) |

| Zone 2: Pillars, Pop-Up Spikes & Moving Lift | Zone 3: Crumbling Stone & Dual Moving Platforms |
| :---: | :---: |
| ![Zone 2 Traps & Moving Platform](Screenshots/zone2_traps_and_moving_preview.png) | ![Zone 3 Moving Platforms](Screenshots/zone3_dynamic_platforms_preview.png) |

| Zone 3: Crumbling Span | Zone 4: Sanctum Beacon Goal |
| :---: | :---: |
| ![Zone 3 Crumble Span](Screenshots/zone3_crumble_span_preview.png) | ![Level Goal Preview](Screenshots/level_goal_preview.png) |

---

## Code Walkthrough (Study Reference for Check-Ins)

- `Assets/Scripts/Player/PlayerController.cs`: Reads Unity `InputSystem` input (`Keyboard.current` / `Gamepad.current`) with Legacy fallback; applies coyote time (`0.12s`), jump buffering (`0.12s`), variable jump cut (`0.5x`), `Shadow-Cloak Glide` vertical clamp (`-2.2f`), updraft lifting (`UpdraftVent`), ground `Physics2D.OverlapBox` checks, sprite facing, animator parameters (`Speed`, `VerticalVelocity`, `IsGrounded`, `IsGliding`, `IsHurt`, `IsDead`), squash/stretch, footstep dust, and movement particles.
- `Assets/Scripts/Player/PlayerHealth.cs`: Tracks 3 Hearts, handles invulnerability frames (`1.2s`), crimson/alpha hit flash coroutine, knockback recoil, canal/abyss respawn (`HandleAbyssFall`), checkpoint revival (`ReviveAtCheckpoint`), and Game Over triggering.
- `Assets/Scripts/Core/GameManager.cs`: Singleton state machine (`Title`, `Playing`, `Paused`, `GameOver`, `Victory`) tracking collected Memory Shards, elapsed time, death retries, active checkpoint position, keyboard shortcuts (`Enter`/`Space`, `R`), and scene reload/retry lifecycle.
- `Assets/Scripts/UI/UIManager.cs`: Controls visibility across all 5 UI panels and synchronizes heart colors, shard counter pop animation, timer, glide badge, timed tutorial/checkpoint banners, persistent + fallback button listeners, and volume/mute controls.
- `Assets/Scripts/Audio/AudioManager.cs`: Persistent audio singleton managing `bgmSource` (looping music) and `sfxSource` (`PlayOneShot` SFX), persisting volume and mute preferences via `PlayerPrefs`.
- `Assets/Scripts/Camera/CameraController.cs` & `ParallaxLayer.cs`: `Vector3.SmoothDamp` camera follow with horizontal look-ahead, world bounds clamp (`maxX = 221f`), decaying random screen shake (`TriggerShake`), and multi-layer parallax scrolling.
- `Assets/Scripts/Level/`:
  - `PopUpSpikes.cs`: Ground-embedded retractable spike trap hidden inside a flush `Tile_SpikeSlot` floor grate via a custom-range `SpriteMask`, supporting `ProximityAmbush` (spark telegraph -> fast eruption when Vesper crosses over) and `TimedCycle` (staggered out-of-phase surges).
  - `MovingPlatform.cs`: Smooth sine-eased ping-pong clockwork platform carrying riding `Rigidbody2D`s cleanly in `FixedUpdate` without parent-scale distortion.
  - `CrumblingPlatform.cs`: Shuddering fragile cracked stone ledge emitting tumbling debris before collapsing and reforming after `3.2s`.
  - `WaterCanal.cs`: Animates dual-layer Twilight Canal wave sway and triggers a cyan-white `WaterSplashParticles` burst before respawning Vesper at the last checkpoint.
  - `CollectibleShard.cs`, `Checkpoint.cs`, `Hazard.cs`, `PatrolEnemy.cs`, `UpdraftVent.cs`, `TutorialTrigger.cs`, `LevelGoal.cs`.
- `Assets/Editor/LevelBuilder.cs`: Deterministic Editor pipeline that imports all sprites & multi-keyframe animation sequences, builds 6 `AnimatorController`s + `.anim` clips, validates zero platform overlaps (`ValidateNoPlatformOverlaps`), assembles `Assets/Scenes/SampleScene.unity`, renders all 7 verification screenshots (`Screenshots/`), and builds the Windows standalone executable.

---

## Standalone Build

- **Executable**: `Builds/Windows/VesperTwilightCourier.exe`
- **Archived Distribution**: `Builds/VesperTwilightCourier_WindowsBuild.zip`
- Built via `BuildPipeline.BuildPlayer` for `StandaloneWindows64`; extract the zip and run `VesperTwilightCourier.exe` directly without Unity installed.

---

## Tools & Credits

- **Game Engine & Packages**: Unity `6000.5.1f1` (Universal Render Pipeline 2D `17.5.0`, Input System `1.19.0`, uGUI `2.5.0`).
- **AI & Automation Disclosure**:
  - **Google Antigravity**: Used to author C# gameplay/UI scripts, the deterministic Unity Editor scene builder (`LevelBuilder.cs`), documentation, and project maintenance.
  - **Antigravity Image Generator (`image-generator`) + Python PIL Multi-Keyframe Synthesis**: Used to generate cohesive original 2D base sprites and multi-frame animation sequences (`Vesper`, `Clockwork Sentinel`, `Electrified Spikes`, `Memory Shard`, `Watchtower Lantern`, `Sanctum Beacon`, `Twilight Canal Water`, `Clockwork Moving Platform`, `Crumbling Stone`, `Gothic Pillar`, `Floor Trap Grate`, `Rooftop Masonry Tile`, and `Parallax Skylines`) in `Assets/Art/Sprites/`.
  - **Procedural Audio Synthesis**: All sound effects and the looping background track in `Assets/Audio/` were synthesized programmatically via Python `wave`/DSP math with zero third-party copyrighted audio samples.
- **Asset & Code License**: All scripts, sprites, and synthesized audio assets in this repository are original project assets created for CSC 231 academic coursework (MIT / CC0-compatible for educational review).
