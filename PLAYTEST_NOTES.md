# Playtest Notes — Vesper: Twilight Courier (Level 1)

**Testers**: Lab peer review (2 classmates) + developer full-level verification playtest on Windows 64-bit build.

## Issues Observed During Testing
1. **Zone 1 Glide Gap Intimidation**: First-time testers dropped straight into an empty gap on their first attempt at *Shadow-Cloak Glide* before realizing they needed to hold `Space`/`Shift` after the jump peak.
2. **Sentinel Stomp Window & Rooftop Edge Snagging**: Jumping onto patrolling *Clockwork Sentinel Drones* while moving forward occasionally registered a side hit instead of a stomp, and rubbing vertically against tiled rooftop walls could briefly slow horizontal air control.
3. **High-Altitude Sky Horizon & Checkpoint Visibility**: When soaring via *Updraft Vents* in Zone 4, the far-sky background needed taller vertical coverage, and mid-ground cloud panels showed vertical tile seams.

## Changes Made
1. **Added Risk-Free Glide Safety Floor**: Built `Roof_1_SafetyFloor` and step-up block beneath the Zone 1 glide tutorial gap alongside curved shard breadcrumbing so new players can practice gliding with zero death penalty.
2. **Refined Combat & Physics Feel**: Added `stompVerticalTolerance = 0.35f` with `0.8f` falling-velocity tolerance and a crisp `12.5f` bounce in `PatrolEnemy.cs`, plus assigned a zero-friction `PlayerFrictionless.physicsMaterial2D` to Vesper's collider.
3. **Polished Parallax & Lantern Feedback**: Raised and scaled `BG_FarSky` (`2.25x`), switched parallax sprites to `TextureWrapMode.Clamp` with alternating mirrored tiles (`flipX`) to remove seams, and added Checkpoint ember flame particles + HUD activation banners.
