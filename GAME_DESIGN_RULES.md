# Game Design Rules & Engineering Blueprint — *Vesper: Twilight Courier*

This document codifies the core industry principles that make 2D action-platformers entertaining, fair, cohesive, and premium—and maps each rule directly to how it is implemented in *Vesper: Twilight Courier*.

---

## 1. Steve Swink’s *Game Feel* & "Juice"
> *Rule*: Real-time control, consistent physical space, and compounded sensory feedback ("juice") turn raw math into visceral, addictive weight. Never let an impact feel weightless.

- **Hit-Stop Freeze Frames (`GameFeelManager.TriggerHitStop`)**:
  - Brief unscaled micro-freezes (`0.035s` enemy KO, `0.055s` player hit, `0.08s` Ward Shatter, `0.085s` Crown CRIT stomp, `0.22s` Final Boss defeat slow-mo) give the brain time to register impact force.
- **World-Space Floating Combat Popups (`GameFeelManager.SpawnFloatingText`)**:
  - Eased rising numbers & callouts (`-9`, `-26 CRIT!`, `WARD SHATTER!`, `+10 HP`, `+1 LIFE`, `KO!`) confirm exact damage and rewards directly at the point of contact.
- **Squash, Stretch, Skids & Velocity-Scaled Landings (`PlayerController.cs`)**:
  - Jumping stretches Vesper `(0.90, 1.12)`, normal landing squashes `(1.14, 0.86)`, heavy falls (`< -13.5 u/s`) trigger deep squash `(1.26, 0.76)` + `sfx_hard_land.wav` + `16` dust particles + camera bump, and sharp directional reversals kick up skid dust.

---

## 2. Maddy Thorson’s *Celeste & Forgiveness* Mechanics
> *Rule*: Difficulty must come from reading the level and boss—never from fighting rigid physics or losing inputs by a single frame. Respect player intent.

- **Coyote Time (`0.12s`) & Jump Buffering (`0.12s`)**: Ledge departures and early jump taps execute cleanly on landing (`PlayerController.cs`).
- **Combat Fire Input Buffer (`0.10s`)**: Shooting Storm-Lightnings just before cooldown ends fires immediately (`fireBufferTimer`).
- **Halved-Gravity Jump Apex Hangtime (`0.52x`)**: Holding Jump near the peak (`|vy| < 2.25 u/s`) softens gravity so mid-air platforming and lightning shots feel readable (`PlayerController.cs`).
- **Ceiling Jump Corner Correction (`TryJumpCornerCorrection`)**: If the outer `0.31u` edge of Vesper's head clips a platform corner while the inner `0.12u` is clear, Vesper smoothly slides `0.28u` around the corner instead of losing vertical velocity.
- **Moving-Platform Lift Momentum Storage (`0.14s`)**: Jumping right as a moving platform reverses or stops retains stored platform velocity (`storedLiftVelocity`) so players are never cheated out of momentum.

---

## 3. Itay Keren’s *Scroll Back* 2D Camera Principles
> *Rule*: A great 2D camera anticipates the horizon without vibrating vertically on every hop or jittering on micro-corrections.

- **Horizontal Dead-Zone Window (`±0.75u`)**: Vesper moves comfortably without pushing the background constantly (`CameraController.cs`).
- **Look-Ahead Hysteresis (`0.16s`)**: Sustaining a facing direction shifts the camera `2.0u` ahead cleanly without snapping violently back and forth when nudging position.
- **Vertical Platform Snapping**: Locks camera Y tracking to grounded floor anchors (`anchoredGroundY`, jumping out only past `+2.5u` above or `-1.5u` below) to eliminate vertical bobbing during normal hops.
- **Boss Arena Camera Lock & Zoom Punch**: Entering Malakor's Sanctum smoothly pulls out (`ortho = 7.7f`, centered at `x = 468.5`) so both walls and all telegraphs remain visible; Ward Shatters and Boss Kills punch orthographic zoom smoothly.

---

## 4. Boss Fight Craft (*Hollow Knight* & *Cuphead*)
> *Rule*: High difficulty feels fair only when every attack follows **Telegraph -> Execution -> Punishment Window** with zero blind spots or off-screen ambushes.

- **Pre-Fight Sanctuary & Fair Entry**: A dedicated safe Vestibule (`x = 444.0..454.5` with Checkpoint 5) precedes the arena; Malakor remains stationary across the hall (`x = 473.5`) until Vesper steps past `x >= 456.0`, triggering the closing `BossArenaGate`, `1.30s` `Intro` roar, and `158 BPM` D Harmonic Minor orchestral/metal boss theme (`bgm_boss_warden.wav`).
- **Diegetic Visual + Audio Tells**:
  - `Halberd Slam` paints a pulsing crimson ground impact strip (`SlamZoneTelegraph`) + plays `sfx_boss_telegraph.wav` before cleaving.
  - `Phantom Dash` paints a pulsing crimson runway laser (`DashFloorTelegraph`) + trims collider height during dash to permit clean hops, escalating to a Phase III Double Cross-Dash with crimson afterimages.
  - `Meteor Leap Slam` & `Rolling Sky-Spear Storm` telegraph aerial dives and sweeping vertical beams (`BossSkyStrike.cs`) across the arena.
- **Readable Phase Escalation (`220 -> 149 -> 74 -> 0 HP`)**: Phase II & III clear hazards cleanly on transition, expand Ward diamonds (`3 -> 4 -> 5`, `0.36s` ICD), raise boss theme pitch (`1.06x` -> `1.12x`), and reward mastery via three ways to progress (Lightning Bolts, Stunned Crown CRIT stomps `38–46 dmg`, or Armored Helm bounces + dodge exertion).

---

## 5. Nintendo *Kishōtenketsu* Level Pacing & *Flow Theory*
> *Rule*: Introduce every mechanic safely without threats first (*Ki*), develop it with light obstacles (*Shō*), twist it (*Ten*), and synthesize everything in the climax (*Ketsu*), alternating intensity spikes with safe breathing room.

- **District I — Courtyards (*Ki*)**: Courtyard ruins teach movement, Storm-Lightnings, a zero-enemy Shadow-Cloak Glide chasm (with recessed safety ledges and step-up block), and a first Plague Caster right after a glowing Checkpoint Lantern.
- **District II — Aqueduct (*Shō*)**: Couples Thermal Updraft Vents with Gothic Pillar spans, swinging Pendulum Scythes, Gargoyle Swoopers overhead, an optional Soul-Life Relic skill perch, and staggered Clockwork Shuttle rides.
- **District III — Foundry (*Ten*)**: Combines crumbling stone bridges, dual non-colliding lifted platforms over the canal, timed out-of-phase spike grates, and mixed ranged/aerial enemies.
- **District IV — Belltower Spires**: Adds double-chimney vertical ascents, crumbling tower gaps, vertical elevators, and a 3rd Soul-Life Relic perch guarded by Gargoyles and Pendulums.
- **District V — Obsidian Viaduct Gauntlet**: High-tempo shuttles, twin timed spike killboxes, pendulum alleys, and rapid chimney surges demanding full movement mastery.
- **District VI — Sanctum Climax (*Ketsu*)**: Ascends into a **zero-ambush Vestibule rest (`Checkpoint 5`)** before culminating in the 5-attack, 3-phase Malakor duel (`220 HP`) and Sanctum Beacon ignition.

---

## 6. Interactive Audio Soundscape & Premium UI Polish
> *Rule*: Sound tells the player how actions feel; UI should glide like physical dark glass rather than snapping like slides.

- **6-Voice Pooled Audio + Pitch Randomization (`AudioManager.cs`)**: Eliminates repetitive "machine-gun" SFX on jumps (`0.95..1.08`), footsteps (`0.88..1.14`, `sfx_footstep.wav`), bolts (`0.94..1.10`), and hits.
- **Pentatonic Collectible Combo Ladder**: Collecting Storm-Lightnings within `1.65s` steps up an 8-note musical scale (`1.00x -> 1.498x`).
- **Dynamic BGM Ducking (`DuckBgm`)**: Ducks music volume smoothly when Malakor roars, Wards shatter (`sfx_ward_shatter.wav`), or Dawn breaks.
- **Tactile Menu & HUD Feedback (`UiButtonJuice.cs`, `UIManager.cs`)**: Unscaled `CanvasGroup` fade + scale easing (`0.18s`), spring hover/press button response (`sfx_ui_hover.wav`, `sfx_ui_click.wav`), delayed white-gold HP/Boss drain trails, crimson damage border vignette + low-HP (`<= 30 HP`) heartbeat tint, and smooth respawn fade flashes.
