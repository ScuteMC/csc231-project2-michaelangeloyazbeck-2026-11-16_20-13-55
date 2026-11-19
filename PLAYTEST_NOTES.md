# Playtest Notes — Vesper: Twilight Courier (Level 1)

**Testers**: Lab peer review (2 classmates) + developer full-level verification playtest on Windows 64-bit build.

## Issues Observed During Testing
1. **Zone 1 Glide Gap Intimidation**: First-time testers dropped straight into an empty gap on their first attempt at *Shadow-Cloak Glide* before realizing they needed to hold `Space`/`Shift` after the jump peak.
2. **Overlapping & Floating Level Geometry**: Initial rooftop safety floors and upper ledges had overlapping X/Y footprints and floating undersides that felt disconnected from the skyline, while static spikes sat visibly above the ground surface and felt predictable.
3. **Animation & Particle Depth**: Early 2-frame run cycles and static props lacked kinetic life, and the abyss beneath rooftops felt empty without animated water or atmospheric depth.
4. **Sentinel Stomp Window & Rooftop Edge Snagging**: Jumping onto patrolling *Clockwork Sentinel Drones* while moving forward occasionally registered a side hit instead of a stomp, and rubbing vertically against tiled rooftop walls could briefly slow horizontal air control.

## Changes Made
1. **Zero-Overlap Intentional Level Architecture**: Re-engineered all four zones with mathematically disjoint platform coordinates (enforced at build time via `LevelBuilder.ValidateNoPlatformOverlaps()`), grounded all rooftop buildings straight into a new animated **Twilight Canal**, added grounded **Gothic Stone Pillars** beneath the Zone 2 aqueduct, and introduced strictly collision-free **Clockwork Moving Platforms** (`MovingPlatform.cs`) with glowing guide rails plus **Crumbling Stone Ledges** (`CrumblingPlatform.cs`).
2. **Ground-Embedded Pop-Up Spikes (`PopUpSpikes.cs`)**: Replaced static above-ground spikes with recessed metallic floor grates (`Tile_SpikeSlot`) and custom-range Unity `SpriteMask`s so blades remain hidden inside the stone until Vesper approaches (`ProximityAmbush`) or staggered timers fire (`TimedCycle`), preceded by a brief spark-trail telegraph.
3. **Full Multi-Keyframe Animations & Animated Water (`WaterCanal.cs`)**: Upgraded every entity to smooth multi-keyframe Unity `AnimationClip`s — Vesper (`6` Idle, `6` Run, `4` Jump, `4` Fall, `4` Glide, `4` Hurt), Clockwork Sentinels (`6`), Memory Shards (`6`), Watchtower Checkpoints (`4`), Sanctum Beacon (`6`), and dual-layer Twilight Canal Water (`6`) with canal mist and splash bursts.
4. **Expanded Unity Particle System Effects**: Upgraded particle systems across the level with `TrailsModule` (glide ribbons, updraft vortices, spike sparks, water splashes, victory fireworks), `NoiseModule` (ambient motes, canal mist, flames, sentinel exhaust), `VelocityOverLifetime`, and `RotationOverLifetime` (crumbling stone debris).
5. **Refined Combat & Physics Feel**: Added `stompVerticalTolerance = 0.35f` with `0.8f` falling-velocity tolerance and a crisp `12.5f` bounce in `PatrolEnemy.cs`, plus assigned a zero-friction `PlayerFrictionless.physicsMaterial2D` to Vesper's collider.
