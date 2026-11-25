# Vesper: Twilight Courier (CSC 231 Project 2 — Platformer MVP)

A 2D Gothic action-platformer built in **Unity 6000.5.1f1** (URP 2D, Unity Input System, uGUI) for **CSC 231 Project 2: Platformer MVP (Level 1)**.

---

## Storyline & Goal

**The Falling Eclipse**: An eternal eclipse locks the rooftop citadel of **Noctis Reach** in frost. As **Vesper**, Shadow-Stitcher Courier, fly across four perilous districts, gather crackling **Storm-Lightnings**, break **Malakor, The Eclipse Warden**, and ignite the **Sanctum Beacon** to restore dawn.

---

## Key Gameplay Features

- **100 HP + Soul Lives + Delayed-Trail Gothic HUD**:
  - Top-left HUD plaque featuring **Vesper's portrait medallion**, up to **5 Life Soul diamonds** (starts with `3`, collectible **Soul-Life Relics** restore `+1 Life` & `+50 HP`), a **100 HP bar with delayed white-gold drain trail**, Storm-Lightning bolt ammo pill, and run timer.
- **Meaningful Collectible Storm-Lightnings (`30` + Boss Arena Respawns)**:
  - Each Storm-Lightning shard heals **+10 HP** and grants **+1 Lightning Bolt** (`LMB` / `RMB` / `E` / `F` / Gamepad `X`).
  - Lightning Bolts travel rapidly across the screen (`PlayerLightningBolt.cs`), blasting flying Gargoyles, Plague Casters, Sentinels, projectile orbs, shockwaves, and **shattering Malakor's Eclipse Ward Diamonds** (`[3–4]` pips) to force him into a Stunned state.
- **Shadow-Cloak Glide & Thermal Updrafts**:
  - Hold **Shift** (or **Space** after apex) to glide smoothly (`-2.2 u/s`, upgraded via Shards) and catch rising **Thermal Vents** / **Canal Steam Chimneys** cleanly mid-glide.
- **Harder Gothic Enemies & Hazards**:
  - **Clockwork Sentinels (`PatrolEnemy.cs`)**: Rooftop drones (`20` dmg, stompable or zap-killable).
  - **Abyssal Gargoyle Swoopers (`SwoopEnemy.cs`)**: Perch and dive-swoop at Vesper with animated bat wings (`20` dmg, AI-illustrated 4-frame sprite cycle).
  - **Eclipse Plague Casters (`AetherShooterEnemy.cs`)**: Hooded sorcerers firing homing-free **Eclipse Orbs** (`EnemyProjectile.cs`, AI-illustrated 4-frame staff cast animation).
  - **Cathedral Pendulum Scythes (`PendulumHazard.cs`)**: Swinging crimson blades suspended from wrought-iron chains (`25` dmg).
  - **Ambush Pop-Up Spikes (`PopUpSpikes.cs`)**: Floor-recessed `SpriteMask` traps with crackling spark telegraphs (`25` dmg).
- **Cohesive Gothic Architecture**:
  - Purple-slate carved moving platforms (`Tile_MovingPlatform`) suspended from overhead bronze pulleys and wrought-iron chains (`Prop_IronChain`), stone-carved crumbling spans (`Tile_CrumblePlatform`), Gothic pillars, spires, railings, and animated canal water (`WaterCanal.cs`).

---

## Multi-Phase Final Boss: Malakor, The Eclipse Warden

At the summit of District IV, past a **safe Vestibule Checkpoint (`Arch_4_SanctumApproach` & `Checkpoint 4`)**, **Malakor, The Eclipse Warden** (`FinalBossAI.cs` — **100 HP**, 20 AI-illustrated animation frames across 5 states plus an intimidating `Intro` roar, dynamically escalating **150 BPM D Harmonic Minor pipe-organ boss theme** `bgm_boss_warden.wav`, closing `BossArenaGate`, and wide-framing arena camera) guards the Sanctum Beacon:

1. **Fair Arena Entry**: Malakor stands stationary across the arena until Vesper crosses into the arena threshold (`x >= 219.0`), then locks the Crimson Eclipse Gate behind her, launches Boss Battle Music, and roars (`Intro`) before attacking.
2. **Break the Eclipse Ward (`3` Pips)**: Shoot Malakor with **Storm-Lightnings** (`LMB` / `E` — swept `OverlapBox` physics with gentle smart-aim), land **Armored Helm Bounces** (`4` dmg + `-1` pip safely), or cleanly outlast heavy attacks: breaking his 3rd Ward pip shatters his shield (`sfx_ward_shatter.wav`) and stuns him.
3. **Finish Him Across Three Escalating Phases (`100 -> 66 -> 33 -> 0 HP`)**:
   - **Phase I (`100 -> 67 HP`)**: **Halberd Slam + Shockwave** and **Phantom Dash**.
   - **Phase II — Enraged (`66 -> 34 HP`, `1.06x` music pitch)**: Unlocks **Sky Spear Barrage** (`BossSkyStrike.cs`) and bidirectional shockwaves.
   - **Phase III — Berserk (`33 -> 0 HP`, `1.12x` music pitch)**: Fires **2-attack combos** plus **3-way Eclipse Orb fans**, backed by `4.5s`-respawning jump-height arena Lightning wisps (`ArenaAmmoSpawner.cs`). Both **Crown Stomps (`26–32 dmg`)** and **Lightning Bolts (`9–14 dmg` shielded, `12 dmg` stunned)** kill him cleanly and unseal the Sanctum Beacon.

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

- **Streamlined Minimal Copy**: Concise titles, crisp button labels (`PLAY`, `STORY`, `GUIDE`, `QUIT`), micro district tags (`I · COURTYARDS` -> `IV · SANCTUM`), and clean iconized Victory stats (`⚡ 30/30 · ⏱ MM:SS · ♥ ×Retries`).
- **Animated Visual Field Guide (`AnimatedUiSprite.cs`)**: All 6 tutorial cards animate continuously with unscaled frame playback and bob/stride motion (*Move & Jump*, *Cloak Glide*, *Updrafts*, *Traps & Scythes*, *Storm Bolt*, *Stomp Foes*).

---

## Verification & Screenshots Gallery

| Main Menu | Story Prologue |
| :---: | :---: |
| ![Title Screen](Screenshots/title_screen.png) | ![Story Intro](Screenshots/story_intro_preview.png) |

| Animated Field Guide | Portrait, 100 HP, Lives & Lightning HUD |
| :---: | :---: |
| ![Tutorial Screen](Screenshots/tutorial_screen_preview.png) | ![Gameplay Preview](Screenshots/gameplay_preview.png) |

| Zone II: Pendulums, Ambush Spikes & Pillars | Zone III: Gothic Chain Platforms & Foundry |
| :---: | :---: |
| ![Zone 2 Traps & Moving](Screenshots/zone2_traps_and_moving_preview.png) | ![Zone 3 Dynamic Platforms](Screenshots/zone3_dynamic_platforms_preview.png) |

| Zone IV: Steam Chimneys & Sanctum Ascent | Boss Arena: Malakor Phase I (4 Ward Pips) |
| :---: | :---: |
| ![Zone 4 Chimney Ascent](Screenshots/zone4_chimney_ascent_preview.png) | ![Level Goal](Screenshots/level_goal_preview.png) |

| Malakor Phase II Enraged: Sky Spears & Storm Bolt | Malakor Phase III: Stunned Crown Stomp |
| :---: | :---: |
| ![Boss Attack Preview](Screenshots/boss_attack_preview.png) | ![Boss Stunned Preview](Screenshots/boss_stunned_preview.png) |

| Dawn Restored Epilogue |
| :---: |
| ![Win Screen](Screenshots/win_screen_preview.png) |

---

## Standalone Build

- **Executable**: `Builds/Windows/VesperTwilightCourier.exe` (`StandaloneWindows64`)
- **Archived Distribution**: `Builds/VesperTwilightCourier_WindowsBuild.zip`
- Automated validation in `LevelBuilder.BuildAll` passed cleanly with **0 overlapping platform footprints across all 23 footprints**, `VerifyCombatAndBossSystems()` passing (`25.6s` boss music, swept bolt hits across all 4 enemy classes, full `100->0 HP` boss kill cycle unsealing the Beacon), and exit code `0`.

---

## Tools & Credits

- **Game Engine**: Unity `6000.5.1f1` (URP 2D `17.5.0`, Input System `1.19.0`, uGUI `2.5.0`).
- **Art & Audio**: Original sprites and AI-illustrated character/boss keyframe sets (`image-generator` + Python PIL chroma-key & Gothic stonework compositing) alongside procedurally synthesized WAV sound effects (`Assets/Audio/`).
