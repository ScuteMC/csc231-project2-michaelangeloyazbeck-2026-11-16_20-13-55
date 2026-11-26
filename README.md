# Vesper: Twilight Courier (CSC 231 Project 2 — Platformer MVP)

A 2D Gothic action-platformer built in **Unity 6000.5.1f1** (URP 2D, Unity Input System, uGUI) for **CSC 231 Project 2: Platformer MVP (Level 1)**.

---

## Storyline & Goal

**The Falling Eclipse**: An eternal eclipse locks the rooftop citadel of **Noctis Reach** in frost. As **Vesper**, Shadow-Stitcher Courier, fly across six escalating districts (`x = -2 .. 496`, ~3-minute challenge run), gather crackling **Storm-Lightnings**, break **Malakor, The Eclipse Warden**, and ignite the **Sanctum Beacon** to restore dawn.

---

## Key Gameplay Features

- **Sculpted Diegetic Gothic HUD & Serif Typography**:
  - Top-left floating **Vesper portrait medallion**, **5 Life Soul diamonds** (starts with `3`, plus **3 Soul-Life Relics** across Aqueduct, Foundry & Belltower), a **100 HP bar with sculpted sapphire-filigree frame and delayed trail**, Storm-Lightning bolt pill, and shard tracker (`0/45`).
  - Bare floating top-center Zone & Sanctum progress tracker (`I · COURTYARDS` -> `VI · SANCTUM`) and compact top-right Timer, Audio Mute & Pause buttons (volume slider inside `PausePanel`).
  - Bottom-center **Dark Souls / Hollow Knight Boss Health Bar** framed in custom sculpted iron & gold filigree (`UI_BossBar_Frame.png`) with Ward Diamonds (`3 -> 4 -> 5`).
- **Studio Stereo Soundscape (`44100 Hz` 2-Channel Stereo)**:
  - All 17 SFX and BGM tracks synthesized in stereo with FFT cathedral reverb, Karplus-Strong plucked strings, FM crystal chimes, formant-filtered boss roars, 32.0s Gothic rooftop theme, and a driving **158 BPM D Harmonic Minor double-kick orchestral/metal boss track (`36.5s`)**.
- **Meaningful Collectible Storm-Lightnings (`45` + Boss Arena Respawns)**:
  - Each Storm-Lightning shard heals **+10 HP** and grants **+1 Lightning Bolt** (`LMB` / `RMB` / `E` / `F` / Gamepad `X`).
  - Lightning Bolts travel rapidly across the screen (`PlayerLightningBolt.cs`), blasting flying Gargoyles, Plague Casters, Sentinels, projectile orbs, shockwaves, and **shattering Malakor's Eclipse Ward Diamonds** (`[3 -> 4 -> 5]` pips with `0.36s` Ward hit ICD) to force him into a Stunned state.
- **Shadow-Cloak Glide & Thermal Updrafts**:
  - Hold **Shift** (or **Space** after apex) to glide smoothly (`-2.2 u/s`, upgraded via Shards) and catch rising **Thermal Vents** / **Canal Steam Chimneys** cleanly mid-glide.
- **Harder 6-District Gothic Gauntlet (`~3 Minutes`, `45` Shards, `5` Checkpoints)**:
  - Progress through **I · Courtyards**, **II · Aqueduct**, **III · Foundry**, **IV · Belltower Spires** (twin chimneys, crumbling spires & 3rd Soul Relic), **V · Obsidian Viaduct** (dual pendulums, timed killbox traps, & high-speed shuttles), and **VI · Twilight Sanctum**.

---

## Multi-Phase Final Boss: Malakor, The Eclipse Warden

At the summit of District VI, past a **safe Vestibule Checkpoint (`Arch_6_SanctumApproach` & `Checkpoint 5`)**, **Malakor, The Eclipse Warden** (`FinalBossAI.cs` — **220 HP**, 6 states including aerial **Meteor Leap Slam**, dynamically escalating **158 BPM boss theme** `bgm_boss_warden.wav`, closing `BossArenaGate`, crimson Phase III flashes, and wide-framing arena camera) guards the Sanctum Beacon:

1. **Fair Arena Entry**: Malakor stands stationary across the arena until Vesper crosses `x >= 456.0`, then seals the Crimson Eclipse Gate behind her, launches Boss Battle Music, and roars (`Intro`) before attacking.
2. **Break the Escalating Eclipse Ward (`3 -> 4 -> 5` Pips, `0.36s` ICD)**: Shoot **Storm-Lightnings** (`LMB` / `E` — click-spam prevented by ICD + `0.30s` stun bolt cap), land **Armored Helm Bounces** (`6` dmg + `-1` pip safely), or outlast attacks until all Ward pips shatter (`sfx_ward_shatter.wav`), stunning him and exposing his crown.
3. **5-Attack Escalation Across Three Phases (`220 -> 149 -> 74 -> 0 HP`)**:
   - **Phase I (`220 -> 150 HP`, 3 Ward Pips)**: **Halberd Slam + Shockwave**, **Phantom Dash**, and **Sky Spear Barrage**.
   - **Phase II — Enraged (`149 -> 75 HP`, 4 Ward Pips, `1.06x` music pitch)**: Unlocks **Meteor Leap Slam** (high parabolic dive with shockwaves both ways), **Rolling Sky-Spear Storm**, and **4-way Eclipse Orb Fans**.
   - **Phase III — Nightmare (`74 -> 0 HP`, 5 Ward Pips, `1.12x` music pitch)**: Unleashes **2-attack Chains**, **Phase III Double Cross-Dash** with crimson afterimages, **5-way Eclipse Orb Fans**, and periodic crimson flash pulses. Backed by `4.2s`-respawning arena Lightning wisps (`ArenaAmmoSpawner.cs`); **Crown CRIT Stomps (`38–46 dmg`)** and **Lightning Bolts (`8–12 dmg` shielded, `10 dmg` stunned)** kill him and unseal the Sanctum Beacon.

---

## Controls

| Action | Keyboard / Mouse | Gamepad |
| :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` or `Left` / `Right` Arrow | Left Stick |
| **Jump** | `Space`, `W`, or `Up` Arrow (hold for higher jump) | South Button (`A` / Cross) |
| **Shadow-Cloak Glide** | Hold `Shift` (or `Space` after apex) | Hold South Button or Right Trigger |
| **Shoot Storm-Lightning** | `Left-Click` (on playfield), `Right-Click`, `E`, or `F` | West Button (`X` / Square) |
| **Stomp Enemy / Boss Crown** | Land cleanly on top of enemies or Stunned Malakor | Land cleanly on top |
| **Pause / Retry / Confirm** | `Esc` / `P`, `R`, `Enter` | Start / HUD Pause |

---

## Intentional Minimal-Text UI & Animated Field Guide

- **Streamlined Minimal Copy**: Crisp serif typography, sleek full-bleed Title Screen (`PLAY`, `STORY`, `GUIDE`, `QUIT`), diegetic HUD clusters, micro district tags (`I · COURTYARDS` -> `VI · SANCTUM`), and clean iconized Victory stats (`45/45 · MM:SS · ♥ ×Retries`).
- **Animated Visual Field Guide (`AnimatedUiSprite.cs`)**: All 6 tutorial cards animate continuously with unscaled frame playback and bob/stride motion (*Move & Jump*, *Cloak Glide*, *Updrafts*, *Traps & Scythes*, *Storm Bolt*, *Stomp Foes*).

---

## Verification Video & Screenshots Gallery

- **Gameplay Demo Video (1:12, 1280×720 30 FPS Stereo Audio)**: [`DemoVideo/VesperTwilightCourier_Demo.mp4`](DemoVideo/VesperTwilightCourier_Demo.mp4) — showcases the **Title Screen**, District I Shadow-Cloak Glide, **Aqueduct Death & Checkpoint Respawn**, Districts II–V hazard gauntlets, the **5-Attack Malakor Boss Fight**, and the **DAWN RESTORED Win Screen**.

| Main Menu | Portrait, HP, Lives & Diegetic HUD |
| :---: | :---: |
| ![Title Screen](Screenshots/title_screen.png) | ![Gameplay Preview](Screenshots/gameplay_preview.png) |

| Zone IV: Belltower Spires & Soul Relic | Zone V: Obsidian Viaduct Gauntlet |
| :---: | :---: |
| ![Zone 4 Belltower](Screenshots/zone4_belltower_preview.png) | ![Zone 5 Viaduct](Screenshots/zone5_viaduct_preview.png) |

| District VI Boss Arena: Bottom-Center Boss Bar | Malakor Phase II Enraged: Sky Spears & Storm Bolt |
| :---: | :---: |
| ![Level Goal](Screenshots/level_goal_preview.png) | ![Boss Attack Preview](Screenshots/boss_attack_preview.png) |

| Malakor Phase III Nightmare: Stunned Crown Stomp | Dawn Restored Epilogue |
| :---: | :---: |
| ![Boss Stunned Preview](Screenshots/boss_stunned_preview.png) | ![Win Screen](Screenshots/win_screen_preview.png) |

---

## Research-Backed Game Design & Feel (`GAME_DESIGN_RULES.md`)

A deep design synthesis in [`GAME_DESIGN_RULES.md`](GAME_DESIGN_RULES.md) documents the core rules applied throughout the game:
- **Steve Swink *Game Feel* & Juice (`GameFeelManager.cs`)**: Hit-stop freeze frames (`0.032s..0.22s`), floating damage/heal popups (`-9`, `-26 CRIT!`, `WARD SHATTER!`, `+10 HP`), velocity-scaled hard landings (`sfx_hard_land.wav`), and skid turn dust.
- ***Celeste* Forgiveness (`PlayerController.cs`)**: `0.12s` coyote time & jump buffer, `0.10s` lightning fire buffer, `0.52x` halved-gravity jump apex hangtime, ceiling corner correction (`0.28u` slide), and moving-platform lift momentum storage (`0.14s`).
- **Itay Keren *Scroll Back* Camera (`CameraController.cs`)**: Horizontal dead-zone (`±0.75u`), look-ahead hysteresis (`0.16s`), vertical platform snapping, boss arena framing (`ortho = 7.7f`), and zoom punches.
- **Boss Readability & *Kishōtenketsu* Pacing (`FinalBossAI.cs`, `LevelBuilder.cs`)**: Zone 1's glide chasm is a threat-free onboarding space (*Ki*), and Malakor's attacks feature pulsing crimson floor telegraphs + audio cues (`sfx_boss_telegraph.wav`).
- **Pooled Adaptive Audio & UI Polish (`AudioManager.cs`, `UiButtonJuice.cs`, `UIManager.cs`)**: 6-voice pitched SFX pool (`sfx_footstep.wav`, `sfx_ui_hover.wav`, `sfx_ui_click.wav`), pentatonic collectible pitch ladder, BGM ducking, `CanvasGroup` panel fades, crimson hit vignette, and low-HP pulse.

---

## Standalone Build

- **Executable**: `Builds/Windows/VesperTwilightCourier.exe` (`StandaloneWindows64`)
- **Archived Distribution**: `Builds/VesperTwilightCourier_WindowsBuild.zip`
- Automated validation in `LevelBuilder.BuildAll` passed cleanly with **0 overlapping footprints and `>= 2.10u` walking headroom across all 45 footprints**, `VerifyCombatAndBossSystems()` passing (`45` shards, `36.5s` stereo boss music, swept bolt hits across all 4 enemy classes, full `220->0 HP` boss kill cycle unsealing the Beacon), and exit code `0`.

---

## Tools & Credits

- **Game Engine**: Unity `6000.5.1f1` (Universal Render Pipeline 2D `17.5.0`, Input System `1.19.0`, uGUI `2.5.0`, C#).
- **AI Tools Used (Full Disclosure)**:
  - **Google Antigravity / Gemini**: Assisted with C# gameplay/editor scripting, level architecture iteration, and PlayMode test/video automation harnesses; all game design rules, character identity (*Vesper*), boss mechanics, and creative direction are original.
  - **AI Image Generation (`image-generator`) + Python (`Pillow` / `NumPy`)**: Used to generate base illustrations for *Vesper*, enemies, tiles, Malakor keyframes, and sculpted Gothic UI frames (`Assets/Art/`), followed by Python chroma-keying, rim cleanup, and 9-slice framing.
  - **Custom Python DSP Audio Synthesis (`NumPy` / `wave`)**: All 17 stereo `44100 Hz` `.wav` sound effects and music tracks (`Assets/Audio/`) were procedurally synthesized cleanly in Python (zero copyrighted audio).
- **External Third-Party Assets**: None — all artwork, audio, scripts, and scenes were created specifically for this project.
