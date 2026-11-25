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

At the summit of District IV, **Malakor, The Eclipse Warden** (`FinalBossAI.cs` — **120 HP**, 20 AI-illustrated animation frames across 5 states: `Wandering`, `Attacking`, `Stunned`, `Dashing`, `Casting`) guards the sealed Sanctum Beacon behind escalating mechanics:

1. **Break the Eclipse Ward with Lightning**: Malakor wears an active **Eclipse Ward Shield** (`3` pips in Phase I, `4` pips in Phases II–III). Shoot him with **Storm-Lightnings** (`E` / Click) — each bolt shatters one diamond until his shield breaks and exposes his **Aether Crown Core** (`Stunned`).
2. **Stomp the Exposed Crown**: While `Stunned` (or after baiting heavy slams in earlier cycles), land cleanly on his glowing Crown for heavy **22+ damage** (zero contact recoil damage thanks to trigger colliders + post-stomp invulnerability).
3. **Three Escalating Phases**:
   - **Phase I (`120 -> 81 HP`)**: Armored patrol, **Halberd Slam + Shockwave**, and **Phantom Dash** lunges.
   - **Phase II — Enraged (`80 -> 41 HP`)**: Unlocks **Sky Spear Barrage** (`BossSkyStrike.cs` telegraphed vertical crimson lightning pillars) and speeds up attacks.
   - **Phase III — Berserk (`40 -> 0 HP`)**: Fires **2-attack combos** plus **3-way Eclipse Orb barrages**, spawning recharging arena Lightning Wisps (`ArenaAmmoSpawner.cs`) so you always have bolts to break his final Ward.

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
- Automated validation in `LevelBuilder.BuildAll` passed cleanly with **0 overlapping platform footprints across all 23 footprints** and exit code `0`.

---

## Tools & Credits

- **Game Engine**: Unity `6000.5.1f1` (URP 2D `17.5.0`, Input System `1.19.0`, uGUI `2.5.0`).
- **Art & Audio**: Original sprites and AI-illustrated character/boss keyframe sets (`image-generator` + Python PIL chroma-key & Gothic stonework compositing) alongside procedurally synthesized WAV sound effects (`Assets/Audio/`).
