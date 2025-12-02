# UFO vs UFO - Project Context

**Last Updated:** 2025-12-01
**Update Count:** 59

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
- ✅ UFO visual redesign: Unity primitive spheres (body + dome) instead of Blender model
- ✅ UFO color materials created (Red, Blue, Green, Yellow) + dome glass material
- ✅ Game end screen with stats (kills, deaths, K/D, streaks, accuracy, MVP)
- ✅ Start screen with "Start Game" button
- ✅ Combat log UI (kill feed showing color-coded hits and kills)
- ✅ Canvas scaling fixed for all resolutions (1920x1080 reference)
- ✅ Death camera zoom out (40 units) for dramatic effect
- ✅ Death explosion system: 2-second timer triggers 60-unit blast radius with massive knockback
- ✅ UFO breakup effect: Dome and body gently separate at explosion moment
- ✅ Camera collision detection (prevents clipping through walls)
- ✅ Combat log weapon names: Shows weapon used in hit/kill messages
- ✅ Combat log deduplication: Only shows kill message, not both hit and kill
- ✅ Minimap system: Circular rotating overhead view with UFO blips
- ✅ Dash weapon: Speed boost + ramming damage + blue force field visual
- ✅ Start screen gamepad support (A button, Start, Space, Enter)
- ✅ Laser weapon: Blue color, 2x range (200 units)
- ✅ Weapon pickup animations: Enhanced spinning/bobbing, 5x scale
- ✅ Particle trail adjustments: Tighter positioning, better visibility
- ✅ Manual boost disabled (only combo boost from barrel rolls remains)
- ✅ Barrel roll buffer window: 0.4 seconds for easier chaining
- ✅ Aim indicator: 3D reticle shows where UFO weapons are currently aimed
- ✅ Vertical input ramping: Up/down aiming now has ease-in like left/right turning
- ✅ Increased max pitch: Steeper climbs/dives (ascending 100%, descending 80%)
- ✅ Defensive item system: Separate slot for defensive items (shield, etc.)
- ✅ Shield item: Temporary invincibility bubble (5 seconds, blocks all damage)
- ✅ Controller remapping: A=Fire (SNES B), X=Deploy item (SNES Y), Q=Barrel roll
- ✅ Easier targeting: Light homing on missiles, generous hitboxes (4x), aim magnetism
- ✅ Auto-level roll: UFO automatically stays level (Z-axis only), no button needed

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
│   │   ├── UFOColorIdentity.cs - Explicit color assignment for combat log
│   │   ├── UFOHealthIndicator.cs - Health orb display (orbiting spheres)
│   │   ├── UFOHoverWobble.cs - Hover bobbing effect (not in use)
│   │   ├── UFOThrusterEffects.cs - Particle effects (not set up)
│   │   └── UFOParticleTrail.cs - Motion trail particles (integrated GPU optimized)
│   ├── Camera/
│   │   └── UFOCamera.cs - Third-person follow camera
│   ├── UI/
│   │   ├── BoostMeter.cs - Boost meter UI display
│   │   ├── StartScreenUI.cs - Start screen with button to begin match
│   │   ├── VictoryScreenUI.cs - End-of-match statistics screen
│   │   ├── CombatLogUI.cs - Kill feed showing color-coded combat events
│   │   ├── MinimapUI.cs - Circular rotating minimap with UFO blips
│   │   └── AimIndicator.cs - 3D reticle showing weapon aim direction
│   ├── Combat/
│   │   ├── WeaponManager.cs - Weapon inventory and switching
│   │   ├── WeaponSystem.cs - Projectile weapon firing
│   │   ├── WeaponPickup.cs - Weapon pickup boxes (supports random)
│   │   ├── Projectile.cs - Proximity missile (auto-detonates near enemies)
│   │   ├── HomingProjectile.cs - Homing missile (tracks targets)
│   │   ├── LaserWeapon.cs - Laser beam weapon (blue, 200 range, OnDisable cleanup)
│   │   ├── BurstWeapon.cs - Burst fire weapon
│   │   ├── StickyBomb.cs - Sticky bomb weapon
│   │   ├── DashWeapon.cs - Dash weapon (3x speed boost, ramming damage, force field)
│   │   ├── DefensiveItemManager.cs - Defensive item inventory (separate from weapons)
│   │   ├── DefensiveItemPickup.cs - Defensive item pickup boxes
│   │   └── ShieldItem.cs - Shield defensive item (temporary invincibility)
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
- **Turn ramping system**: Gradual ease-in for precise aiming
  - Starts at 0% sensitivity for fine adjustments
  - Ramps to 100% over ~0.3s when holding direction
  - Resets instantly on direction change or release
  - Controlled by `turnAcceleration` (default: 3)
- **Vertical ramping system**: Conditional ease-in for up/down aiming
  - **Only active when moving forward** (speed >= 0.1 units/sec)
  - When hovering/stationary: instant vertical control (no ramping)
  - When flying forward: slow ramp for precision target acquisition
  - Makes aiming much easier, prevents over-correction
  - Controlled by `verticalAcceleration` (default: 1.5 - half speed of turn ramp)
  - Forward threshold: `minForwardSpeedForRamping` (default: 0.1)
- **Velocity-based aim pitch**: Weapon aim calculated from actual velocity
  - Ascending: 100% of velocity angle (full pitch up)
  - Descending: 80% of velocity angle (slightly reduced pitch down)
- **Barrel roll dodge**: Directional evasion (Q/RB + stick direction)
  - No cooldown, can chain back-to-back
  - Buffer window: 0.4 seconds (was 0.2s - easier chaining)
  - Combo system: 3 rolls in 2s = 1.5x speed boost for 3s
- **Manual boost**: DISABLED (was LB to boost, now removed)
  - Only combo boost from barrel rolls remains
- Fast vertical movement (3x speed when moving only up/down)
- AI input support for enemy control
- **Aim magnetism**: Subtle auto-aim assist for easier targeting
  - When crosshair is within 15° of enemy, aim pulls slightly toward them
  - Closer to target = stronger pull (up to 30% blend)
  - Range: 100 units, only affects player (not AI)
  - Configurable: `enableAimMagnetism`, `magnetismAngle`, `magnetismStrength`, `magnetismRange`
- **Auto-level roll**: UFO automatically stays level (arcade style, beginner-friendly)
  - Only affects roll (Z-axis) - pitch (up/down) stays free for 3D flight
  - Faster leveling when not turning (120°/sec), slower during turns (40°/sec)
  - Self-corrects after collisions, explosions, barrel rolls
  - Configurable: `autoLevelRoll`, `autoLevelRollSpeed`, `autoLevelSpeedWhileTurning`

**Controls:**
- Auto-accelerate (always moving forward)
- D key / RB (Button 5): Brake (also enables sharp turns)
- Arrows/Left Stick: Turn, Ascend/Descend
- Q: Barrel Roll (direction from stick)
- A button (Button 0): Fire weapon (SNES "B" - bottom button)
- X button (Button 2): Deploy defensive item (SNES "Y" - left button)
- LB (Button 4): Currently unused (available for future feature)

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
- **Death system**: Natural physics death with gentle tumble
  - Reduces velocity to 30% on death (no acrobatics)
  - UFO stays intact while falling (body + dome together)
  - Gentle random tumble (not crazy spinning)
  - High drag (2.0) and angular drag (3.0) for controlled fall
  - Becomes physics wreck, spawns small explosion, auto-cleanup
- **Death explosion (2 seconds after death)**:
  - Timer-based trigger (no collision detection needed)
  - 60-unit blast radius at UFO position (wherever it is)
  - Deals 1 HP damage to all UFOs in range
  - Applies massive knockback (80 force, vs 30 for regular explosions)
  - Properly attributes kills to original killer
  - Auto-scales explosion visual based on radius (3x missile size)
- **UFO breakup effect** (at explosion moment):
  - Dome detaches and becomes independent physics object
  - Gentle separation: dome drifts up slightly (0.5-1 unit) with small sideways drift (±0.5)
  - Body gets small opposite push so pieces drift apart slowly
  - High drag (2.0) prevents pieces flying off screen
  - Lazy tumbling on both pieces (not dramatic)
  - Both pieces cleaned up after wreck lifetime (10s)
- Prevents burst weapons from instant-killing

**Death Explosion Setup:**
- `enableGroundExplosion` - Enable/disable feature (default: true)
- `groundExplosionPrefab` - **REQUIRED**: Assign ExplosionEffect.prefab from Assets/Prefabs/
- `groundExplosionRadius` - 60 units (3x larger than missiles)
- `groundExplosionDamage` - 1 HP damage
- **Fallback visual**: If no prefab assigned, creates orange semi-transparent sphere (120 unit diameter)

### UFOColorIdentity.cs - Color Assignment
**Key Features:**
- Explicit color name assignment for each UFO (e.g., "Red", "Blue", "Green", "Yellow")
- Display color for UI formatting (RGB values)
- Used by CombatLogUI for reliable player identification
- **Setup**: Add to each UFO, set colorName and displayColor in Inspector

### UFOHealthIndicator.cs - Health Orbs
**Key Features:**
- 3 glowing orbs orbit above UFO showing current HP
- Color changes based on health: Green (3 HP) → Yellow (2 HP) → Red (1 HP)
- Orbs disappear as health decreases
- Configurable: height (1), radius (1.5), speed (100), scale (0.4)

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
- **Death camera**: Elevated bird's eye view when player dies
  - Positions camera 15 units behind and 25 units above falling UFO
  - Locks to direction UFO was facing when it died (no spinning)
  - Smoothly looks down at UFO as it falls to ground
  - Completely separate from normal camera logic (early return)
- **Camera collision detection**: Prevents clipping through walls using SphereCast
  - Fast pull-in when obstructed (10 speed)
  - Slow recovery when clear (5 speed)
  - 0.5 unit collision sphere radius for padding
  - Minimum 1 unit distance from target
  - Debug visualization in Scene view (green = clear, red = obstructed)
- Dynamic vertical tilt when ascending/descending
- All effects have zero GPU cost

**Camera Collision Setup:**
- `enableCameraCollision` - Enable/disable (default: true)
- `cameraCollisionRadius` - Sphere size for padding (default: 0.5)
- `collisionLayers` - Layers to check (default: all layers)
- `collisionPullInSpeed` - How fast camera pulls in when blocked (default: 10)
- `collisionRecoverySpeed` - How fast camera returns when clear (default: 5)

### UFOParticleTrail.cs - Motion Trails
**Optimizations (Critical for Integrated GPU):**
- 3 emitters per UFO, 20 max particles each (was 100)
- 8 particles/sec emission rate (96 total/sec for 4 UFOs)
- Standard alpha blend (not additive), Unlit/Transparent shader
- 32x32 texture, no shadows/occlusion
- **70% GPU load reduction** vs original
- Speed-based emission (7-30 units/sec threshold, was 10-30)

**Trail Positioning (Updated):**
- Lateral offset: 2.4 (left/right trail distance)
- Forward offset: -1 (rear position)
- Vertical offset: -0.5 (below UFO center)
- Center trail: (0, -0.5, -3.5) - further back than side trails

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

### StartScreenUI.cs - Start Screen
**Key Features:**
- Shows "UFO vs UFO" title
- "Press A To Begin" instruction text
- **Gamepad support**: A button, Start button (Xbox/PlayStation)
- **Keyboard support**: Space bar, Enter key
- **Mouse support**: Click "START GAME" button
- Game waits in WaitingToStart state until input received
- UFOs frozen until match starts
- Calls GameManager.StartMatch() when activated

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

### CombatLogUI.cs - Kill Feed
**Key Features:**
- Displays combat events in top-left corner (vertical list)
- **Color detection**: Uses UFOColorIdentity component (hardcoded) for reliable color names
- **Fallback**: Auto-detects color from most saturated material if no UFOColorIdentity
- **Event types**:
  - Hits: "Yellow hit Green with Missile!" (white text, shows weapon name)
  - Kills: "Red killed Blue with Laser!" (yellowish text, shows weapon name)
  - Suicides: "Green self-destructed!" (gray text)
  - Weapon pickups: "Yellow picked up HomingMissile" (light gray, human player only)
- **Deduplication**: When a hit kills a UFO, only kill message appears (not both hit + kill)
- **Auto-fading**: Messages fade out after 4 seconds
- **Message limit**: Shows max 5 messages at once
- **Canvas Scaler**: 1920x1080 reference, scales with screen size
- **Setup**: Vertical Layout Group with TextMeshPro log entry prefab (500x30px, 24pt font)

**Weapon Names Tracked:**
- Missile, Missile Explosion
- Homing Missile, Homing Missile Explosion
- Laser
- Sticky Bomb, Sticky Bomb Explosion
- Death Explosion (from dead UFO exploding)

### MinimapUI.cs - Circular Rotating Minimap
**Key Features:**
- **Circular 2D overhead view** of play area
- **Rotating compass**: "Up" on minimap is always player's forward direction
- **UFO blips**: Color-coded dots showing all UFO positions
  - Player blip: Cyan (configurable)
  - Enemy blips: Match UFO team colors (uses UFOColorIdentity)
- **Range detection**:
  - In-range UFOs: Show at actual position (scaled)
  - Out-of-range UFOs: Clamped to edge of minimap circle (semi-transparent)
- **Performance**: Updates every 0.1s (not every frame)
- **Auto-cleanup**: Dead UFOs automatically removed from minimap

**Configuration:**
- `detectionRange` - 100 units (UFOs beyond this show on edge)
- `minimapRadius` - 75 pixels (circle size)
- `worldToMinimapScale` - 0.5 (zoom level: higher = closer view)
- `blipSize` - 8 pixels (size of UFO dots)
- `updateInterval` - 0.1 seconds (refresh rate)

**Setup Requirements:**
- Circular minimap background with Mask component
- BlipContainer (child RectTransform that rotates)
- Blip prefab (small circle Image, 8x8 pixels)
- Position: Bottom-right corner recommended (X=-100, Y=100)

### AimIndicator.cs - 3D Aim Reticle
**Key Features:**
- **3D reticle**: Floating crosshair in world space showing where weapons will fire
- **Auto-positioning**: Uses UFOController.GetAimDirection() for accurate aim tracking
- **Smooth movement**: Lerps to new position (configurable speed)
- **Auto-created visual**: Generates reticle geometry automatically (outer ring, center dot, tick marks)
- **Customizable appearance**: Color, size, distance, and pulse effect
- **Optional visibility control**: Can hide when weapon can't fire

**Configuration:**
- `reticleDistance` - 50 units (how far ahead to show reticle)
- `reticleSize` - 1.0 (scale multiplier)
- `reticleColor` - Cyan-green (default, Color can be changed at runtime)
- `smoothSpeed` - 15 (how quickly reticle tracks aim changes)
- `hideWhenCantFire` - false (set true to show only when weapon ready)
- `pulseSpeed` - 2.0 (breathing effect speed, 0 = no pulse)
- `pulseAmount` - 0.2 (pulse intensity, 0-1)

**Setup:**
- Add AimIndicator component to UFO_Player
- UFOController reference auto-assigned if on same GameObject
- Reticle auto-creates on Start (no prefabs needed)
- Use SetReticleColor() to change color per weapon type
- Use SetReticleDistance() to adjust for weapon range

**Performance:**
- Zero GPU cost (simple Unlit/Color shader)
- No shadows, no physics colliders
- Minimal geometry (5 primitives total)

### Projectile.cs - Proximity Missile
**Key Features:**
- **Proximity detonation**: Auto-explodes when within 10 units of enemy UFOs
- **Light homing**: 45°/sec turn rate toward nearest enemy within 60 units
  - Much weaker than full homing missile (180°/sec)
  - Helps correct small aiming errors without guaranteeing hits
- **Blast radius**: 10 units (damage + knockback)
- **Damage**: 1 HP direct hit, 1 HP blast damage
- Owner exclusion (never damages shooter)
- Duplicate damage prevention (one UFO can't be hit multiple times by same blast)

**Configuration (Projectile_Bullet.prefab):**
- `homingStrength`: 45 (degrees/sec, 0 = no homing)
- `homingRange`: 60 (detection range for targets)

### HomingProjectile.cs - Homing Missile
**Key Features:**
- **Target tracking**: Seeks nearest enemy UFO within detection radius (100 units)
- **Turn rate**: 180°/s, accelerates from 40 to 60 units/s
- **Homing delay**: 0.2s after launch before tracking activates
- **Blast radius**: 20 units on collision
- **Damage**: 1 HP direct hit, 1 HP blast damage
- Line-of-sight required for target acquisition
- Prioritizes forward targets (angle-weighted scoring)

**Note:** NO proximity detonation - only explodes on collision or lifetime expiration (8s)

### LaserWeapon.cs - Laser Beam
**Key Features:**
- **Color**: Blue (was red)
- **Range**: 200 units (was 100 - doubled for longer reach)
- **Duration**: 2 seconds continuous beam
- **Damage**: 1 HP (single hit per beam activation)
- **Cooldown**: 1 second after beam ends
- **Visual**: Line renderer with scaled width based on distance
- **Critical fix**: OnDisable cleanup prevents beam freezing during barrel rolls/weapon switches

**Performance optimized for integrated GPU:**
- 2 vertices per corner/cap (minimal geometry)
- No shadows, no lighting data
- Unlit/Color shader

### DashWeapon.cs - Dash (NEW)
**Key Features:**
- **Speed boost**: 3x forward speed multiplier for 6 seconds
- **Ramming damage**: 1 HP when colliding with other UFOs during dash
- **Blue force field**: Semi-transparent sphere (size 4, distance 2 in front)
- **Mechanics**:
  - Temporarily boosts UFO max speed to 3x (e.g., 30 → 90)
  - Reduces drag by 50% for faster acceleration
  - Applies continuous forward force to reach target speed
  - Only boosts forward velocity (vertical/lateral control unchanged)
- **Auto-cleanup**: Weapon disables when dash ends
- **Force field visual**: Auto-created blue semi-transparent sphere using Unlit/Transparent shader

**Setup:**
- Add DashWeapon component to UFO
- Link in WeaponManager's "Dash Weapon" field
- Force field auto-creates on Start

### WeaponPickup.cs - Pickup Boxes
**Key Features:**
- **Animations**: Spin (90°/sec) and bob (speed 3, height 0.5)
- **Scale**: 5x size (set manually in Transform, not auto-scaled by script)
- **Trigger size**: 1x multiplier (matches visual size)
- **Respawn**: 15 seconds default
- **Random weapons**: Enable randomWeapon to give random weapon type
- **AI reservation system**: Prevents multiple AIs from claiming same pickup

### DefensiveItemManager.cs - Defensive Item System
**Key Features:**
- **Separate slot from weapons**: Players can hold 1 weapon AND 1 defensive item
- **Mirrors WeaponManager structure**: Same inventory/switching pattern
- **Deploy button**: RB (Button 5) or R key
- **AI support**: TryUseItemAI() method for AI control

**Current Items:**
- Shield (see ShieldItem.cs below)

**Setup:**
- Add DefensiveItemManager component to UFO
- Add ShieldItem component to UFO
- Link ShieldItem reference in Inspector

### ShieldItem.cs - Shield Defensive Item
**Key Features:**
- **Temporary invincibility**: Blocks ALL damage while active
- **Duration**: 5 seconds (configurable)
- **Visual**: Cyan semi-transparent bubble around UFO
- **Pulse effect**: Gentle breathing animation on shield bubble
- **Single use**: Item removed from inventory after activation

**Configuration:**
- `shieldDuration` - 5 seconds (how long shield lasts)
- `shieldSize` - 5 units (bubble diameter)
- `shieldColor` - Cyan, 30% opacity
- `pulseSpeed` - 2.0 (breathing effect speed)
- `pulseAmount` - 0.15 (pulse intensity)

**Integration:**
- UFOHealth.cs checks ShieldItem.IsShieldActive() before applying damage
- Shield blocks damage from all sources (weapons, explosions, collisions)

### DefensiveItemPickup.cs - Defensive Item Pickups
**Key Features:**
- **Mirrors WeaponPickup.cs**: Same animation, respawn, and claim system
- **Item types**: Can be set to specific item or random
- **Respawn**: 15 seconds default
- **AI reservation system**: Same as weapon pickups

**Setup:**
- Create pickup GameObject with mesh and collider
- Add DefensiveItemPickup component
- Set itemType or enable randomItem
- Set respawn settings as needed

## Scene Setup Notes

**Hierarchy:**
```
UFO_Player (Rigidbody + Sphere Collider + UFOColorIdentity)
└── UFO_Visual (visual tilting container)
    ├── UFO_Body (Sphere primitive, flattened 3x1x3, team color material)
    ├── UFO_Dome (Sphere primitive, 1.2x1.2x1.2, glass material)
    └── DirectionIndicator
```

**UFO Visual Design:**
- **Body**: Flattened sphere primitive (Scale: 3, 1, 3) with team color (Red/Blue/Green/Yellow)
- **Dome**: Sphere primitive (Scale: 1.2, 1.2, 1.2, Y-offset: 0.44) with UFO_Dome_Glass material
- **No colliders on children** - only parent has physics collider
- **Generous hitbox**: SphereCollider radius 2.0 (4x original 0.5), diameter ~4 units vs 3-unit visual width

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
