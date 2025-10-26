# UFO vs UFO - Project Context

**Last Updated:** 2025-10-26
**Update Count:** 44

---

## 📋 Additional Documentation

**[claude-secondary.md](claude-secondary.md)** - Detailed Inspector values, troubleshooting, and recovery procedures

**[considerations.md](considerations.md)** - Design ideas and MVP polish strategies

---

## Project Overview
N64 Mario Kart Battle Mode-style aerial combat game in Unity 2022.3 LTS (URP template).
- Low-poly UFO vehicles with arcade physics
- Small 3D arenas with vertical elements
- Third-person camera, wide FOV
- Simple projectile weapons and pickups
- AI opponents for single-player gameplay
- Must run on low-end PC (no dedicated GPU)

## Current Phase
**Phase 2 In Progress:** Combat mechanics and AI opponents

**Completed:**
- ✅ Basic UFO flight mechanics with keyboard and gamepad support
- ✅ Weapon system with pickups and firing
- ✅ Random weapon pickup boxes (mystery boxes)
- ✅ AI enemy implementation with state machine
- ✅ Laser weapon freezing bug fixed (OnDisable cleanup)
- ✅ UFO color materials created (Red, Blue, Green, Yellow)
- ✅ Game end screen with stats (kills, deaths, K/D, streaks, accuracy, MVP)

## Next Session
**TODO:** Test and tune AI behavior
- AI opponents can patrol, seek weapons, chase, and attack
- Need to test 1v3 gameplay balance
- May need tuning of aggression, detection ranges, and attack behavior
- Consider adding difficulty settings or AI personality variations

## File Structure
```
Assets/
├── Scripts/
│   ├── Vehicle/
│   │   ├── UFOController.cs - Main flight controller (supports AI input, boost system)
│   │   ├── UFOAIController.cs - AI behavior controller (state machine)
│   │   ├── UFOCollision.cs - Collision bounce system
│   │   ├── UFOHealth.cs - Health and death system
│   │   ├── UFOHoverWobble.cs - Hover bobbing effect (not in use)
│   │   ├── UFOThrusterEffects.cs - Particle effects (not set up)
│   │   └── UFOParticleTrail.cs - Motion trail particles (integrated GPU optimized)
│   ├── Camera/
│   │   └── UFOCamera.cs - Third-person follow camera
│   ├── UI/
│   │   ├── BoostMeter.cs - Boost meter UI display
│   │   └── VictoryScreenUI.cs - End-of-match statistics screen
│   ├── Combat/
│   │   ├── WeaponManager.cs - Weapon inventory and switching
│   │   ├── WeaponSystem.cs - Projectile weapon firing
│   │   ├── WeaponPickup.cs - Weapon pickup boxes (supports random)
│   │   ├── Projectile.cs - Basic projectile
│   │   ├── HomingProjectile.cs - Homing missile
│   │   ├── LaserWeapon.cs - Laser beam weapon (fixed: OnDisable cleanup prevents freeze)
│   │   ├── BurstWeapon.cs - Burst fire weapon
│   │   └── StickyBomb.cs - Sticky bomb weapon
│   ├── GameManager.cs - Match flow, win conditions, stats tracking
│   ├── PlayerStats.cs - Individual player statistics (kills, deaths, streaks, accuracy)
│   └── Arena/ (empty - future)
├── Scenes/
│   └── TestArena.unity - Main test scene
├── Materials/
│   ├── UFO_Bouncy.physicMaterial - Zero-friction bounce material
│   ├── UFO_Red.mat, UFO_Blue.mat, UFO_Green.mat, UFO_Yellow.mat - UFO colors
│   └── Projectile/Explosion materials
└── Prefabs/ (empty - future)
```

## Key Scripts Overview

> **Full Inspector values in [claude-secondary.md](claude-secondary.md)**

### UFOController.cs - Flight & Movement
**Key Features:**
- Arcade physics with tight turns and responsive controls
- Banking/pitch visual effects (UFO_Visual child)
- **Barrel roll dodge**: Directional evasion (Q/RB + stick direction)
  - No cooldown, can chain back-to-back
  - Combo system: 3 rolls in 2s = 1.5x speed boost for 3s
- **Manual boost**: Hold LB to boost (1.8x speed, drains meter)
  - Stacks with combo boost (max 2.7x speed)
- Fast vertical movement (3x speed when moving only up/down)
- AI input support for enemy control

**Controls:**
- A/D or Buttons 0/1: Accelerate/Brake
- Arrows/Left Stick: Turn, Ascend/Descend
- Q/RB (Button 5): Barrel Roll (direction from stick)
- LB (Button 4): Boost

### UFOCollision.cs - Bounce System
**Key Features:**
- **Wall bounces**: Physics reflection for natural deflection
- **Floor collisions**: Angle-based behavior
  - Steep descent: Dead stop or heavy bounce
  - Medium angle: Keep momentum, bounce up
  - Shallow scrape: Glide along floor
- Red flash on impact, brief stun on heavy crashes

### UFOHealth.cs - Health & Death
**Key Features:**
- **3 HP default**, 3-second invincibility frames after damage
- **Blink feedback** during i-frames (8 blinks/sec)
- **Death system**: Becomes physics wreck, spawns explosion, auto-cleanup
- Prevents burst weapons from instant-killing

### UFOAIController.cs - AI Behavior
**State Machine:**
1. **Patrol** - Wanders randomly
2. **SeekWeapon** - Flies to nearest weapon pickup
3. **Chase** - Pursues enemy (out of range)
4. **Attack** - Fires at enemy (in range)

**Key Settings:**
- Aggression: 0.7 (tune 0.3-1.0 for difficulty)
- Detection Range: 100, Attack Range: 60
- Wall avoidance, strafing, random barrel rolls

**Setup:** UFOController.useAIInput = true, WeaponManager.allowAIControl = true, Tag = "Player"

### UFOCamera.cs - Third-Person Camera
**Key Features:**
- **75° FOV** (N64-style), tight rotation tracking for aiming
- **Turn zoom out**: Pulls back +3 units during sharp turns (>90°/sec)
- **Reverse camera**: Pulls back to 15 units, widens FOV to 90° when reversing
- **FOV kick**: Widens on acceleration, narrows on brake, max boost during combo
- **Camera shake**: Impact-based shake (intensity scales with speed)
  - **CRITICAL FIX**: Shake applied AFTER smoothing, not before (was being dampened)
- Dynamic vertical tilt when ascending/descending
- All effects have zero GPU cost

### UFOParticleTrail.cs - Motion Trails
**Optimizations (Critical for Integrated GPU):**
- 3 emitters per UFO, 20 max particles each (was 100)
- 8 particles/sec emission rate (96 total/sec for 4 UFOs)
- Standard alpha blend (not additive), Unlit/Transparent shader
- 32x32 texture, no shadows/occlusion
- **70% GPU load reduction** vs original
- Speed-based emission (10-30 units/sec threshold)

### GameManager.cs - Match Flow & Stats
**Key Features:**
- **Game state machine**: WaitingToStart → Starting → InProgress → MatchOver
- **Player tracking**: Auto-detects all UFOs with "Player" tag
- **Win condition**: Last UFO standing (1v3 elimination mode)
- **Stats tracking**: Automatically adds PlayerStats component to all UFOs
- **Victory screen**: Shows ranked players with kills, deaths, K/D ratios
- Countdown timer (3s default) before match starts
- Post-match delay (5s default) before cleanup

### PlayerStats.cs - Individual Statistics
**Tracks per player:**
- Kills, deaths, K/D ratio
- Current kill streak, longest kill streak
- Shots fired, shots hit, accuracy %
- Damage dealt, damage taken

### VictoryScreenUI.cs - End Screen Display
**Shows:**
- **Title**: "VICTORY!" (green) for winner, "DEFEAT" (red) for loser
- **Final standings**: Ranked by kills (1st/2nd/3rd/4th with K/D ratios)
- **MVP**: Player with most kills
- **Longest kill streak**: Best streak of the match
- **Best accuracy**: Highest hit % (minimum shots required)
- **Most damage**: Total HP dealt
- **Rematch button**: Reload scene to play again
- Color-coded: Cyan for human player, Orange for AI

## Scene Setup Notes

**Hierarchy:** UFO_Player (Rigidbody + Sphere Collider) → UFO_Visual (visual tilting container) → UFO_Body + DirectionIndicator

**Physics Materials:**
- UFO_Bouncy: Friction 0, Bounce 0.5 (UFO + walls)
- Floor_Material: Friction 0, Bounce 0 (arena floor)

**Important:** UFO_Visual assigned to both UFOController and UFOCollision. Only parent has collider.

## Performance & Optimization

**Critical GPU Settings (Integrated GPU - NO dedicated GPU):**
- **MUST use URP-Performant** (NOT URP-HighFidelity)
- **NO real-time shadows** (check scene lights individually!)
- **NO HDR/MSAA** on camera
- **NO Depth/Opaque textures** in URP settings
- LaserWeapon: Low vertex counts (2), no shadows, no lighting data

**Common Issues:**
- Script changes don't apply → Update Inspector values manually or re-add component
- UFO phases through walls → Keep Bounce Force reasonable (10-50), check colliders
- D3D11 crash → See [claude-secondary.md](claude-secondary.md) for full troubleshooting

## Development Roadmap

**Repository:** https://github.com/mikeprimak/ufo-vs-ufo

**Launch Plan:**
1. **Phase 2** (current): Combat mechanics + AI opponents
2. **Phase 3**: Polish single-player/local multiplayer
3. **Phase 4**: Release free on Steam/itch.io
4. **Phase 5**: Add online multiplayer ONLY if there's demand (Steam P2P or Photon free tier)

**Philosophy:** Fun first, complexity later. Focus on making core gameplay fun before adding networking infrastructure.

---

## Project Recovery

**Quick Recovery (30 seconds):**
```bash
# Close Unity, delete cache, reopen
rmdir /s /q Library
rmdir /s /q Temp
```

**Nuclear Option:** Clone fresh from GitHub - zero manual setup required (all Inspector values in scene YAML files)

See [claude-secondary.md](claude-secondary.md) for detailed recovery procedures.

---

## Meta Instructions
- **ALWAYS update this file when pushing to GitHub**
- **Every 20 updates:** Condense file while preserving all critical project knowledge
- This file serves as session continuity for future Claude Code sessions
