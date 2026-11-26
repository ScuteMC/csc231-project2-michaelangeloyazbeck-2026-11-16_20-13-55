# Playtest Notes — Vesper: Twilight Courier (Level 1)

**Testers**: Michaelangelo Yazbeck (author, keyboard & mouse playtests across Districts I–VI) plus two peer playthroughs during iterative tuning.

## What Went Wrong During Playtesting
1. **Aqueduct Overhang Softlock (`Roof_2_RelicPerch`)**: Beneath the first elevated Relic platform in District II, clearance (`1.60u`) nearly matched Vesper's collider height (`~1.61u`), blocking horizontal movement along the deck.
2. **Updraft-While-Gliding & Fair Boss Entry Bugs**: Entering thermal updrafts while already holding Glide dropped vertical lift, and cross-triggering the Sanctum Boss early allowed hits before entering the arena while rapid bolt clicks bypassed Ward shield pacing.
3. **Level Pacing (< 1:30) & Flat First-Pass Feedback**: Initial 4-district layout took under 90 seconds with forgiving 100 HP boss tuning, generic flat UI boxes, and mono audio.

## What Was Changed After Testing
1. **Relic Ledges & Headroom Validation (`LevelBuilder.cs`)**: Replaced `2.0u` elevated Relic blocks with `0.85u` carved ledges (`2.35u–3.55u` clearance) and added an automated `>= 2.10u` headroom check alongside overlap validation (`45` footprints, `0` issues).
2. **Updraft Physics & Fair Boss Duel (`PlayerController.cs`, `FinalBossAI.cs`)**: Zeroed downward gravity inside updrafts, added Checkpoint 5 safe Sanctum Vestibule + closing `BossArenaGate`, raised Malakor to `220 HP` across 5 attacks with `0.36s` Ward hit ICD (`3 -> 4 -> 5` pips), and added floor telegraphs.
3. **3-Minute 6-District Expansion & Polish (`LevelBuilder.cs`, `Assets/Art/UI/`, `Assets/Audio/`)**: Expanded to 6 districts (`45` Shards, `5` Checkpoints), upgraded to sculpted Gothic UI + bottom-center Boss Bar, stereo DSP SFX/music (`158 BPM` boss theme), hit-stop, and *Celeste* jump forgiveness.
