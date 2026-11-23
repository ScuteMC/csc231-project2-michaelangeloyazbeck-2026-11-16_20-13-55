# Vesper: Twilight Courier (CSC 231 Project 2 — Platformer MVP)

A 2D atmospheric platformer built in **Unity 6000.5.1f1** (URP 2D, Unity Input System, uGUI) for **CSC 231 Project 2: Platformer MVP (Level 1)**.

---

## Storyline, Character Backstory & Cause

**The Falling Eclipse**: High above the moonlit **Twilight Canal**, the skyline city of **Noctis Reach** depends on condensed dawnlight stored in golden District Lanterns. Tonight, an unnatural **Eternal Eclipse** struck the Sanctum Spire, shattering its central crystal into **25 Memory Shards** and plunging the city toward endless night.

**Vesper's Oath**: As **Vesper**, youngest of the **Shadow-Stitcher Couriers**, you wear a dusk-woven cloak trimmed with luminescent threads. Your cause is clear: traverse all four rooftop districts, reclaim the scattered Memory Shards, outsmart the corrupted Clockwork Sentinels and floor ambush traps, and reignite the **Sanctum Beacon** to bring dawn back to Noctis Reach.

- **Signature Move — Shadow-Cloak Glide**: Holding **Space** (after jump apex) or **Shift** in mid-air unfurls Vesper's shadow-stitched cloak to slow her descent (`-2.2 u/s`), boost horizontal air control (`1.2x`), trail glowing shadow ribbons, and soar up **Thermal Vent Updrafts** even if you enter them mid-glide!
- **Multi-Keyframe Animations (Unity `AnimationClip`s + `AnimatorController`s)**:
  - **Vesper (`VesperAnimator.controller`)**: `Idle` (`6` frames), **Articulated `Run` (`8` frames: Contact -> Down -> Passing -> Drive/Apex for both left & right strides)**, `Jump` (`4` frames), `Fall` (`4` frames), `Glide` (`4` frames), and `Hurt` (`4` frames).
  - **World & Entities**: `SentinelAnimator` (`6` frames), `ShardAnimator` (`6` frames), `CheckpointAnimator` (`4` frames), `GoalBeaconAnimator` (`6` frames), and `WaterCanalAnimator` (`6` seamless Twilight Canal wave frames).

---

## Controls

| Action | Keyboard | Gamepad |
| :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` or `Left` / `Right` Arrow | Left Stick |
| **Jump** | `Space`, `W`, or `Up` Arrow (variable height) | South Button (`A` / Cross) |
| **Shadow-Cloak Glide** | Hold `Space` (after apex) or `Left/Right Shift` in air | Hold South Button or Right Trigger/Shoulder |
| **Stomp Sentinel Drone** | Land on top of a patrolling drone | Land on top of a patrolling drone |
| **Pause / Menu Shortcuts** | `Esc` or `P` (Pause/Back), `Enter` (Confirm), `R` (Retry) | HUD Pause Button / Start |
| **Audio Volume & Mute** | HUD top-right slider & Mute button, or Pause menu | — |

---

## Intentional Grounded Level 1 Design (2–5 Minute Run)

Level 1 (**Noctis Reach Rooftops**, ~230 world units across) is engineered with zero accidental floating or colliding pieces (validated automatically at build time by `LevelBuilder.ValidateNoPlatformOverlaps()` across all 21 static, chimney, crumbling, and swept moving-platform footprints):

1. **District I — Courtyard of Whispers**: Framed by the Courtyard Gatehouse Arch (`Prop_DistrictArch`), wrought-iron balustrades (`Prop_GothicRailing`), and glowing twilight streetlamps (`Prop_StreetLamp`). Grounded masonry buildings plunge straight into the Twilight Canal water, introducing movement, Memory Shards, and a risk-free **Shadow-Cloak Glide** chasm with a recessed safety floor and step-up block.
2. **District II — Gale-Draft Aqueduct**: Features **Checkpoint 1 (`Gale-Draft Overlook`)**, a flush bronze-and-cyan **Rooftop Thermal Vent (`Prop_UpdraftVent`)** explaining `Updraft_1_Intro`, an elevated stone aqueduct supported by twin grounded **Gothic Stone Pillars**, **Ground-Embedded Pop-Up Spikes (`PopUpSpikes.cs`)** hidden via Unity `SpriteMask` inside recessed metal floor grates, and a gantry-supported **Clockwork Moving Shuttle (`MovingPlat_2A_Shuttle`)** with brass gear pylons.
3. **District III — Clockwork Foundry**: Marks the district entry with a Foundry Gatehouse Arch and **Checkpoint 2 (`Clockwork Foundry Watch`)**, teaches stomping patrolling **Clockwork Sentinels**, introduces a **Broken-Arch Crumbling Stone Bridge (`Crumble_3A_Span`)** flanked by stone cliff corbels (`Prop_BrokenArch`), and crosses the Foundry Canal via dual strictly non-colliding **Clockwork Moving Platforms (`MovingPlat_3A_VerticalLift` & `MovingPlat_3B_Shuttle`)** on canal gantry piers.
4. **District IV — Twilight Sanctum**: Combines **Checkpoint 3 (`Sanctum Stair Gate`)**, patrolling sentinels, twin grounded **Canal Steam Chimneys (`Prop_SteamChimney` + `Prop_ChimneyCap`)** rising out of the water, a ruined watchtower **Crumbling Perch** supported by a weathered stone pillar + corbels, the Sanctum Approach Arch, final ambush spike grate, and the **Sanctum Beacon (`SanctumBeacon_Goal`)** triggering **Dawn Restored!** victory.

---

## Ornate Gothic UI, Story Screens & Unity Particle Systems

- **8 Complete Screens (`UIManager.cs`) with 9-Sliced Ornate Gothic Artwork**:
  - **Main Menu (`TitlePanel`)**: Live twilight courtyard backdrop, crescent-moon crest, Story Hook, `BEGIN DELIVERY`, `STORY & MISSION`, `HOW TO PLAY`, and `QUIT`.
  - **Interactive 3-Card Story Prologue (`StoryIntroPanel`)**: Illustrated cards (*I. The Falling Eclipse*, *II. Shattered Light*, *III. The Courier's Oath*) introducing Vesper's cause before entering District I.
  - **Story & Mission Briefing (`LorePanel`)**: Full world lore, backstory, and district roadmap accessible anytime from the Title Screen.
  - **Visual Field Guide (`TutorialPanel`)**: Dedicated 6-card sprite-and-keycap tutorial grid (*Move & Jump*, *Shadow-Cloak Glide*, *Thermal Updrafts*, *Ambush Spike Grates*, *Clockwork Sentinels*, *Shards & Beacon*) accessible from Main Menu and Pause Menu.
  - **In-Game HUD (`HUDPanel`)**: Top-left Stats Plaque (3 Hearts, Memory Shard counter with pop feedback, Timer), Top-center **District Objective + Sanctum Progress Bar (%)**, Top-right Audio Volume Slider + Mute + Pause buttons, Bottom-left Cloak status badge, and Bottom-center Whisper/Tutorial Banner.
  - **Pause (`PausePanel`)**, **Dawn Restored Victory (`WinPanel` with S/A/B Courier Rank & epilogue)**, and **Game Over (`GameOverPanel`)**.
- **Upgraded Unity Particle Systems**: Uses `TrailsModule`, `NoiseModule`, `VelocityOverLifetimeModule`, `RotationOverLifetimeModule`, `ColorOverLifetimeModule`, and `SizeOverLifetimeModule` across Glide shadow ribbons, swirling Updraft thermal columns, crackling spike-trap telegraph sparks, Canal Mist & dynamic Water Splashes, tumbling crumbling stone rubble, sentinel exhaust & stomp sparks, shard shimmer & pickup bursts, checkpoint ember flames, and Sanctum Beacon victory fireworks.

---

## Screenshots Gallery

| Main Menu & Crest | Story Prologue Intro |
| :---: | :---: |
| ![Title Screen](Screenshots/title_screen.png) | ![Story Intro](Screenshots/story_intro_preview.png) |

| Visual Courier Field Guide | District I: Courtyard HUD & Progress |
| :---: | :---: |
| ![Tutorial Screen](Screenshots/tutorial_screen_preview.png) | ![Gameplay Preview](Screenshots/gameplay_preview.png) |

| Shadow-Cloak Glide & Safety Chasm | District II: Thermal Vent, Pillars & Pop-Up Spikes |
| :---: | :---: |
| ![Glide Preview](Screenshots/glide_and_particles_preview.png) | ![Zone 2 Traps & Moving](Screenshots/zone2_traps_and_moving_preview.png) |

| District III: Broken-Arch Crumbling Bridge | District III: Non-Colliding Gantry Lifts |
| :---: | :---: |
| ![Zone 3 Crumble Span](Screenshots/zone3_crumble_span_preview.png) | ![Zone 3 Dynamic Platforms](Screenshots/zone3_dynamic_platforms_preview.png) |

| District IV: Twin Steam Chimneys & Ruined Pillar | District IV Summit & Sanctum Beacon |
| :---: | :---: |
| ![Zone 4 Chimney Ascent](Screenshots/zone4_chimney_ascent_preview.png) | ![Level Goal](Screenshots/level_goal_preview.png) |

| Dawn Restored Epilogue |
| :---: |
| ![Win Screen](Screenshots/win_screen_preview.png) |

---

## Code Walkthrough (Study Reference for Check-Ins)

- `Assets/Scripts/Player/PlayerController.cs`: Unity `InputSystem` controls with Legacy fallback, coyote time (`0.12s`), jump buffer (`0.12s`), variable jump cut (`0.5x`), Shadow-Cloak Glide clamp (`-2.2f`), gravity-suspended Thermal Updraft soaring with live `Physics2D.OverlapBox` + `OnTriggerStay2D` detection so updrafts always engage cleanly even if entered mid-glide, animator parameter sync, squash/stretch, footstep dust, and movement particles.
- `Assets/Scripts/Level/UpdraftVent.cs`: Rising twilight wind vent supporting `OnTriggerEnter2D`, `OnTriggerStay2D`, and `OnTriggerExit2D`.
- `Assets/Scripts/Level/PopUpSpikes.cs`: Ground-embedded retractable spike trap concealed inside `Tile_SpikeSlot` via custom-range `SpriteMask` with `ProximityAmbush` (spark telegraph -> fast eruption) and staggered `TimedCycle` surges.
- `Assets/Scripts/Level/MovingPlatform.cs`: Smooth sine-eased ping-pong clockwork platform moving riding `Rigidbody2D`s cleanly in `FixedUpdate`.
- `Assets/Scripts/Level/CrumblingPlatform.cs`: Shuddering cracked stone bridge emitting `RotationOverLifetime` debris before collapsing and reforming.
- `Assets/Scripts/Level/WaterCanal.cs`: Dual-layer animated Twilight Canal water surface (`6`-frame wave animation) with splash burst respawn.
- `Assets/Scripts/Level/TutorialTrigger.cs`: Triggers both HUD Whisper Banners and persistent District Objective transitions.
- `Assets/Scripts/Core/GameManager.cs` & `Assets/Scripts/UI/UIManager.cs`: Full 8-screen state machine (`Title`, `StoryIntro`, `Lore`, `Tutorial`, `Playing`, `Paused`, `GameOver`, `Victory`), live Sanctum Beacon progress percentage, Courier Rank calculation, persistent + fallback button listeners, and keyboard shortcuts.
- `Assets/Editor/LevelBuilder.cs`: Deterministic Editor pipeline configuring 9-sliced UI & pixel pivots, generating the 8-frame `Vesper_Run` + entity animators, enforcing zero platform overlaps across all 21 footprints, assembling `SampleScene.unity`, rendering all 11 verification screenshots, and building `VesperTwilightCourier.exe`.

---

## Standalone Build

- **Executable**: `Builds/Windows/VesperTwilightCourier.exe`
- **Archived Distribution**: `Builds/VesperTwilightCourier_WindowsBuild.zip`
- Built via `BuildPipeline.BuildPlayer` for `StandaloneWindows64`; extract the zip and run `VesperTwilightCourier.exe` directly without Unity installed.

---

## Tools & Credits

- **Game Engine & Packages**: Unity `6000.5.1f1` (Universal Render Pipeline 2D `17.5.0`, Input System `1.19.0`, uGUI `2.5.0`).
- **AI & Automation Disclosure**:
  - **Google Antigravity**: Used to author C# gameplay/story/UI scripts, the deterministic Unity Editor scene builder (`LevelBuilder.cs`), documentation, and project automation.
  - **Antigravity Image Generator (`image-generator`) + Python PIL Synthesis**: Used to generate cohesive original 2D sprites, the 8-frame articulated run cycle, grounded architectural/updraft props, 9-sliced ornate Gothic UI assets, and Story Prologue vignettes (`Assets/Art/`).
  - **Procedural Audio Synthesis**: All sound effects and the looping background track in `Assets/Audio/` were synthesized programmatically via Python `wave`/DSP math with zero third-party copyrighted audio samples.
- **Asset & Code License**: All scripts, sprites, UI art, and synthesized audio assets in this repository are original project assets created for CSC 231 academic coursework.
