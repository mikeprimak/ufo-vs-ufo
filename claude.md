# UFO vs UFO - Project Context

**Last Updated:** 2025-10-25
**Update Count:** 28

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
│   │   ├── UFOController.cs - Main flight controller (supports AI input)
│   │   ├── UFOAIController.cs - AI behavior controller (state machine)
│   │   ├── UFOCollision.cs - Collision bounce system
│   │   ├── UFOHealth.cs - Health and death system
│   │   ├── UFOHoverWobble.cs - Hover bobbing effect (not in use)
│   │   ├── UFOThrusterEffects.cs - Particle effects (not set up)
│   │   └── UFOParticleTrail.cs - Motion trail particles (integrated GPU optimized)
│   ├── Camera/
│   │   └── UFOCamera.cs - Third-person follow camera
│   ├── Combat/
│   │   ├── WeaponManager.cs - Weapon inventory and switching
│   │   ├── WeaponSystem.cs - Projectile weapon firing
│   │   ├── WeaponPickup.cs - Weapon pickup boxes (supports random)
│   │   ├── Projectile.cs - Basic projectile
│   │   ├── HomingProjectile.cs - Homing missile
│   │   ├── LaserWeapon.cs - Laser beam weapon
│   │   ├── BurstWeapon.cs - Burst fire weapon
│   │   └── StickyBomb.cs - Sticky bomb weapon
│   └── Arena/ (empty - future)
├── Scenes/
│   └── TestArena.unity - Main test scene
├── Materials/
│   └── UFO_Bouncy.physicMaterial - Zero-friction bounce material
└── Prefabs/ (empty - future)
```

## Key Scripts & Settings

### UFOController.cs
**Current Inspector Values (MUST be set manually):**
- Max Speed: 30 (2x original)
- Acceleration: 60 (2x original)
- Brake Force: 80 (2x original)
- Max Reverse Speed: 20 (2.5x original)
- Turn Speed: 180
- Vertical Speed: 8 (24 when moving only vertically)
- Pure Vertical Speed Multiplier: 3x
- Pure Vertical Threshold: 10 (forward/back speed threshold)
- Drag Amount: 2 (increased for quicker stopping)
- Auto Level Speed: 2
- Visual Model: UFO_Visual (for banking and pitch effects)
- Bank Amount: 25°
- Bank Speed: 3
- Visual Pitch Amount: 25° (reduced from 30° for less extreme tilt)
- Visual Pitch Speed: 3
- Min Speed For Pitch: 5
- Barrel Roll Distance: 18
- Barrel Roll Duration: 0.5s
- Barrel Roll Cooldown: 0 (no cooldown)
- Barrel Roll Buffer Window: 0.2s
- Combo Rolls Required: 3
- Combo Time Window: 2s
- Combo Speed Boost: 1.5x
- Combo Boost Duration: 3s
- Combo Boost Fade In Time: 0.3s
- Combo Boost Fade Out Time: 0.5s

**Controls (Player Mode):**
- A / Controller Button 0 → Accelerate
- D / Controller Button 1 → Brake/Reverse
- Arrow Keys / Left Stick → Turn left/right, Ascend/Descend
- Q / RB (Button 5) → Barrel roll right
- E / LB (Button 4) → Barrel roll left

**AI Input Support:**
- Use AI Input: false (player) / true (AI controlled)
- When enabled, reads from public AI input fields instead of Input system
- AI fields: aiAccelerate, aiBrake, aiTurn, aiVertical, aiBarrelRollLeft/Right, aiFire
- Allows UFOAIController to control movement via virtual inputs

**Features:**
- Arcade physics: tight turns, instant brake, momentum-based movement
- Auto-leveling prevents tilting from impacts
- Banking effect when turning (visual only, applied to UFO_Visual child)
- Pitch effect when ascending/descending while moving forward (nose tilts up/down)
- Pitch only applies when horizontal speed > 5 units/sec (no tilt when hovering vertically)
- **Fast vertical movement**: 3x speed boost when moving only up/down OR during barrel rolls
  - Normal vertical speed: 8 units/sec
  - Pure vertical speed: 24 units/sec (no forward/backward movement)
  - **Forward + vertical**: 16 units/sec (2x multiplier for steeper climb/dive angles)
  - Achieves ~28° climb/dive angle when accelerating forward + up/down
  - Smooth gradient transition based on forward speed (threshold: 10 units/sec)
  - **Barrel roll vertical boost**: Full 3x speed multiplier during barrel rolls regardless of forward speed
  - Allows aggressive evasive climbs/dives while barrel rolling forward
  - Barrel roll lateral movement doesn't cancel vertical speed boost
- **Barrel roll dodge mechanic**: Fast lateral dash with 360° roll animation
  - Primary evasion mechanic for dodging projectiles
  - Maintains forward momentum, adds lateral velocity
  - No cooldown - can be chained back-to-back
  - Input buffering: Queue next roll 0.2s before current finishes
  - Full control during roll (can accelerate, turn, ascend/descend)
  - Movement-based evasion (no invincibility frames)
  - Compatible with fast vertical movement (can dodge while climbing/descending)
  - **Combo system**: 3 barrel rolls within 2 seconds triggers speed boost
    - Any combination of left/right rolls counts toward combo
    - Activates 1.5x speed boost for 3 seconds (max speed 45, acceleration 90)
    - **Smooth transitions**: 0.3s fade-in, 0.5s fade-out for gradual acceleration/deceleration
    - No sudden speed changes - multiplier lerps smoothly between 1.0x and 1.5x
    - Combo counter resets after successful boost or timeout
    - Debug messages show combo progress in console
- No gravity - pure hovering flight
- Rigidbody constraints: FreezeRotationX | FreezeRotationZ
- Interpolation enabled to prevent jittery visuals
- Continuous collision detection prevents phasing through walls

### UFOCollision.cs
**Current Inspector Values (Wall Settings):**
- Wall Bounce Force: 20
- Min Wall Impact Speed: 3

**Current Inspector Values (Floor Settings):**
- Heavy Crash Threshold: 10 (vertical speed)
- Light Floor Bounce: 2
- Heavy Floor Bounce: 8
- Floor Slide Retention: 0.7 (keeps 70% horizontal momentum)
- Floor Angle Threshold: 45° (surfaces steeper than this are walls)

**Visual Feedback:**
- Flash Color: Red
- Ufo Renderer: UFO_Body (optional, for red flash)

**Wall Collision Features:**
- Uses physics reflection for natural bounces (Vector3.Reflect)
- High angle hits (90°) → bounces mostly straight back
- Shallow angle hits (10-30°) → deflects along wall naturally
- Locks rotation during bounce (UFO stays facing same direction)
- Bounce ends when velocity < 0.5 or after 1 second
- Brief red flash on impact (100ms)
- Continuous collision detection prevents phasing through walls at high speed

**Floor Collision Features:**
- Angle-based behavior (0° = straight down, 90° = horizontal scrape)
- **Steep descent (< 30°):**
  - Pure vertical landing (no horizontal movement): Dead stop, no bounce
  - Light touch with movement: Small bounce to prevent sticking
  - Heavy crash with movement (>10 speed): Bounce up + red flash + brief stun
- **Medium angle (30-60°):**
  - Keeps 70% horizontal momentum, bounces up to continue flying
  - Heavy crashes get stronger bounce + red flash
- **Shallow scrape (>60°):**
  - Keeps 90% horizontal momentum, minimal bounce
  - No flash, smooth glide along floor
- Temporarily disables vertical input (0.3s) after floor bounce to prevent control fighting
- Player retains acceleration/turning control throughout

**Important:** Wobble feature was attempted but removed due to conflicts with banking

### UFOHealth.cs
**Current Inspector Values:**
- Max Health: 3 HP (default)
- Invincibility Duration: 0.5 seconds (i-frames after taking damage)
- Death Explosion Prefab: (optional visual effect)
- Wreck Lifetime: 10 seconds (how long wreck stays before cleanup)
- Death Sound: (optional audio clip)

**Features:**
- **Invincibility Frames (i-frames)**: 0.5s immunity after taking damage
  - Prevents rapid-fire weapons (burst, laser) from instant-killing
  - First hit deals damage, subsequent hits blocked during i-frame window
  - Allows counterplay and dodge opportunities
  - Debug logging shows when damage is blocked
- **Death System**: When HP reaches 0, UFO becomes physics wreck
  - Disables flight controls (UFOController)
  - Disables collision system (UFOCollision)
  - Enables gravity and tumbling physics
  - Spawns explosion effect (if assigned)
  - Plays death sound (if assigned)
  - Auto-cleanup after wreck lifetime expires
- **Health Management**: Get/Set health, heal, reset for respawns
- **Public API**: IsDead(), IsInvincible(), GetCurrentHealth(), etc.

**Important:** I-frames prevent burst weapon from dealing all 3 damage in one burst (was instant death)

### UFOAIController.cs
**Current Inspector Values:**
- Aggression: 0.7 (0-1, how aggressively AI pursues)
- Decision Interval: 0.2s (how often AI makes decisions)
- Detection Range: 100 units (how far AI can see enemies)
- Attack Range: 60 units (distance to start firing)
- Wall Avoidance Distance: 15 units (minimum distance from walls)
- Arrival Distance: 10 units (how close to get to targets)
- Barrel Roll Chance: 0.3 (30% chance to barrel roll for evasion)
- Patrol Radius: 30 units (how far AI wanders when idle)

**AI States (State Machine):**
1. **Patrol** (Green) - No weapon, wanders randomly
2. **SeekWeapon** (Cyan) - No weapon, flies to nearest weapon pickup
3. **Chase** (Yellow) - Has weapon, pursues enemy but out of range
4. **Attack** (Red) - Has weapon, fires at enemy in range

**Features:**
- Uses UFOController's AI input system for movement
- Finds nearest enemies tagged "Player"
- Seeks weapon pickups when unarmed
- Wall avoidance using forward/side raycasts
- Aims at target when in attack range
- Strafes around target while attacking
- Random barrel rolls for evasion
- Debug gizmos show current state and target lines

**Setup Requirements:**
- UFOController: Use AI Input = true
- WeaponManager: Allow AI Control = true
- Tag must be "Player" to detect/be detected
- UFOHealth component required

**Tuning Tips:**
- Lower aggression (0.3-0.5) for easier AI
- Higher aggression (0.9-1.0) for harder AI
- Increase decision interval (0.3-0.5s) for better performance
- Decrease attack range (40) for more aggressive close-combat AI
- See AI_SETUP_GUIDE.md for detailed tuning guide

### UFOCamera.cs
**Current Inspector Values:**
- Target: UFO_Player
- Distance: 10
- Height: 5
- Smooth Speed: 5
- Rotation Smoothing: 0.8 (tight tracking for aiming)
- Look Down Angle: 10° (base downward tilt)
- Field of View: 75° (N64-style wide FOV)
- Vertical Height Offset: -0.2 (camera drops when ascending)
- Vertical Tilt Amount: 0.5 (reduced for more horizontal camera during vertical movement)
- Vertical Smoothing: 3
- Reverse Distance: 15 (camera pulls back when reversing)
- Reverse FOV: 90° (wider FOV when reversing)
- Reverse Speed Threshold: -1 (triggers reverse camera mode)
- Reverse Camera Smoothing: 3 (transition speed)

**Turn Zoom Out Settings (Game Feel - NEW):**
- Enable Turn Zoom Out: true
- Turn Zoom Out Distance: 3 units (additional distance during sharp turns)
- Turn Zoom Speed: 4 (transition smoothness)
- Turn Zoom Threshold: 90 degrees/sec (angular velocity to trigger zoom out)

**FOV Kick Settings (Game Feel):**
- Enable FOV Kick: true
- Acceleration FOV Boost: 5° (75 → 80 when accelerating)
- Brake FOV Reduction: 5° (75 → 70 when braking)
- Combo Boost FOV Boost: 10° (75 → 85 during combo boost)
- FOV Kick Speed: 5 (transition smoothness)

**Camera Shake Settings (Game Feel - FINAL TUNED):**
- Enable Camera Shake: true
- Shake Duration: 0.3s (short, punchy shake)
- Shake Intensity: 0.4 units (subtle but visible)
- Shake Decay Speed: 5 (fades out quickly)
- Min Shake Speed: 15 units/s (no shake for light bumps - only medium/hard impacts)
- Shake Cooldown: 0.3s (prevents rapid-fire shaking when scraping walls)
- **CRITICAL BUG FIXED (UFOCamera.cs:252-257):** Shake was being smoothed out by camera lerp!
  - Old: Added shake to desiredPosition, THEN lerped toward it (shake got dampened)
  - New: Lerp to position, THEN add shake directly (shake is instant and visible)
  - Shake must be applied AFTER smoothing, not before
- **Improvements:**
  - Shake intensity scales with impact speed (harder hits = more shake)
  - Minimum speed threshold prevents shake on weak collisions
  - Cooldown prevents shake spam when bouncing repeatedly
- **Debug logging:** Shows impact speed, intensity, and skip reasons in console

**Features:**
- Tight rotation tracking for forward-firing weapon aiming (0.5-1.0 recommended)
- Tracks UFO physics rotation only (not visual banking from UFO_Visual)
- Keeps horizon level for consistent aiming
- Dynamic vertical tilt: ascending = camera tilts up, descending = camera tilts down
- Smooth position following with retained smoothing for comfort
- Designed for center-screen aiming and forward-firing weapons
- **Turn Zoom Out System (Zero GPU Cost)**: Camera pulls back during sharp turns
  - Measures rotation delta per frame (compatible with MoveRotation)
  - Zooms out up to 3 units when turning faster than 90°/sec
  - Prevents camera from catching up too much during tight maneuvers
  - Smooth lerp transition (4x speed)
- **Dynamic reverse camera**: When UFO reverses, camera pulls back and widens FOV for better visibility
  - Prevents UFO from approaching bottom edge of screen
  - Smooth transitions between normal and reverse camera states
  - Automatically detects reverse movement based on velocity
- **FOV Kick System (Zero GPU Cost)**: FOV dynamically adjusts based on input for speed rush feel
  - Widens FOV when accelerating (speed rush effect)
  - Narrows FOV when braking (focus/slow-down effect)
  - Maximum FOV boost during barrel roll combo (dramatic speed sensation)
  - Smooth transitions prevent jarring changes
- **Camera Shake System (Zero GPU Cost)**: Random position offset on impacts
  - Triggered automatically by UFOCollision.cs on wall/floor hits
  - Shake intensity scales with impact speed
  - Heavy floor crashes = full shake, angled hits = medium shake, wall hits = scaled by speed
  - Decays smoothly over shake duration
  - Public methods: `TriggerShake(intensity)` and `TriggerShakeFromImpact(speed, maxSpeed)`

### UFOParticleTrail.cs
**Current Inspector Values (BALANCED for visibility + performance):**
- Particle Lifetime: 0.2s (medium trail length)
- Start Size: 0.25 (larger for visibility)
- End Size: 0.05 (fade out)
- Emission Rate: 8 particles/sec (balanced - was 15 originally)
- Start Speed: 0.3 (slow drift)
- Start Color: Bright yellow-white (1, 1, 0.5) with **FULL OPACITY** (alpha = 1.0)
- End Color: Fade to transparent
- Min Speed For Trail: 10 units/sec (only emit when moving)
- Max Speed For Trail: 30 units/sec (full emission at max speed)

**Optimization Features (CRITICAL for integrated GPU):**
- **3 emitters per UFO**: Left, Right, Center trails
- **Max 20 particles per emitter** (was 100 - 80% reduction)
- **Larger size + full opacity** = better visibility without GPU cost
- **Standard alpha blend** instead of expensive additive blending
- **Unlit/Transparent shader** (not Particles/Standard Unlit)
- **32x32 texture resolution** (was 64x64 - 75% fewer pixels)
- **No shadows, no occlusion queries, no anisotropic filtering**
- **Total GPU load reduced by ~70%** with improved visibility
- World space simulation for motion trail effect
- Speed-based emission (stops emitting when hovering/stopped)

**Performance Impact:**
- 3 emitters × 8 particles/sec × 4 UFOs = **96 particles/sec max** (was 180)
- **Safe for integrated GPU** - size/opacity changes have zero GPU cost
- If crashes still occur, disable component entirely in Inspector

**Important:** Original additive blending version caused D3D11 swapchain crashes on integrated GPU

## Scene Setup (TestArena)

### Hierarchy Structure:
```
UFO_Player (Rigidbody, Sphere Collider, UFOController, UFOCollision)
├── UFO_Visual (empty container for visual tilting)
│   ├── UFO_Body (Sphere, scaled 1.5, 0.5, 1.5 - flying saucer shape)
│   └── DirectionIndicator (Cube, shows front of UFO)

Main Camera (UFOCamera)

ArenaFloor (Plane, scale 5,1,5 = 50x50 units)

Wall_North, Wall_South, Wall_East, Wall_West (Cubes with Box Colliders)
```

### Physics Materials:
- **UFO_Bouncy**: Dynamic/Static Friction = 0, Bounciness = 0.5, applied to UFO and walls
- **Floor_Material**: Dynamic/Static Friction = 0, Bounciness = 0, applied to ArenaFloor
  - Separate material allows different collision behavior for floor vs walls

### Important Notes:
- UFO_Visual must be assigned to BOTH UFOController and UFOCollision (if using visual features)
- DirectionIndicator should NOT have collider (visual only)
- UFO_Visual should NOT have collider (just container)
- Only UFO_Player parent should have Sphere Collider and Rigidbody

## Controller Support
- Keyboard and gamepad work simultaneously
- Left stick mapped to Horizontal/Vertical axes (turn + altitude)
- Face buttons mapped to Fire1/Fire2 (accelerate + brake)
- Input Manager has default Unity axes (no custom trigger axis needed)
- ControllerDebug.cs available for testing input detection

## Known Issues & Solutions

### Script Changes Don't Apply:
- Unity caches Inspector values from when component was first added
- **Solution:** Manually update values in Inspector after script changes
- Or remove component and re-add it to get new defaults

### UFO Flies Through Walls:
- Caused by Bounce Force being too high (>100) or colliders missing
- **Solution:** Keep Bounce Force reasonable (10-50), ensure colliders exist

### Banking/Wobble Conflicts:
- Multiple scripts controlling same transform causes issues
- **Solution:** Banking uses UFO_Visual child, parent stays for physics
- Wobble feature removed due to conflicts

### Need to Reimport:
- Auto Refresh should be enabled (Edit → Preferences → General)
- If disabled, right-click script → Reimport after changes

### GPU Device Reset / D3D11 Swapchain Crash (CRITICAL):
- **Symptoms:** Unity crashes with "Failed to present D3D11 swapchain due to device reset/removed" - may shut down PC
- **Root Cause:** Integrated GPU overloaded by expensive rendering features
- **Solutions Applied:**
  1. **MOST IMPORTANT - Graphics Pipeline (ProjectSettings/GraphicsSettings.asset):**
     - **MUST use URP-Performant** (GUID: d0e2fc18fe036412f8223b3b3d9ad574)
     - **NEVER use URP-HighFidelity** (has HDR, 4x MSAA, 4096 shadow maps, reflection probes = instant GPU death)
     - Check: Edit → Project Settings → Graphics → "Scriptable Render Pipeline Settings"
     - Should say "URP-Performant" NOT "URP-HighFidelity"
  2. **CRITICAL - Depth/Opaque Textures (NEVER ENABLE THESE):**
     - `m_RequireDepthTexture: 0` - MUST stay 0 (enabling = doubles GPU workload)
     - `m_RequireOpaqueTexture: 0` - MUST stay 0 (enabling = massive memory bandwidth)
     - Location: URP-Performant asset settings
     - **Particle Sorting:** Use Render Queue or Sorting Layers instead (zero GPU cost)
  3. **LaserWeapon.cs LineRenderer Settings (Lines 113-120):**
     - `numCornerVertices = 2` (was 16 - major GPU killer!)
     - `numCapVertices = 2` (was 16 - major GPU killer!)
     - `shadowCastingMode = Off` (was On - real-time shadows on dynamic laser = GPU death)
     - `receiveShadows = false` (was true)
     - `generateLightingData = false` (was true - huge overhead every frame)
     - Removed emission keyword (adds shader complexity)
  4. **Camera Settings (TestArena.unity):**
     - `m_HDR: 0` (was 1 - HDR is very expensive on integrated GPU)
     - `m_AllowMSAA: 0` (was 1 - anti-aliasing kills integrated GPU)
     - `m_AllowHDROutput: 0` (was 1)
  5. **CRITICAL - Directional Light Shadows (TestArena.unity - Line 308):**
     - `m_Shadows: m_Type: 0` (MUST be 0 - NO SHADOWS)
     - Was set to 2 (Soft Shadows) - MAJOR GPU KILLER
     - Real-time dynamic shadows on integrated GPU = instant crash
     - QualitySettings.asset already has `shadows: 0` globally
     - **ALWAYS verify light shadow settings in scene file**
- **Key Takeaway:** On integrated GPU, NEVER use:
  - HighFidelity render pipeline
  - Depth/Opaque textures
  - High vertex counts
  - Real-time shadows (even if QualitySettings disables them, check scene lights!)
  - HDR/MSAA
- **Visual Impact:** Game still looks great with optimized settings

## Git Workflow
```bash
git add .
git commit -m "message"
git push
```

**Repository:** https://github.com/mikeprimak/ufo-vs-ufo

## Future Development Plans
- **Primary Goal:** Make a simple, fun, free game that works well on any PC
- **Launch Target:** Free on Steam (and/or itch.io)
- Phase 2: Combat mechanics (projectiles, pickups, AI opponents)
- Phase 3: Polish single-player/local multiplayer experience
- Phase 4: Release and see if people want online multiplayer
- Phase 5: Add online multiplayer only if there's demand (keep it simple)
- Visual effects: Thruster particles, trail renderer (scripts exist but not set up)
- Audio: Engine sounds, impacts, weapon sounds

## Multiplayer Strategy (Keep It Simple)

### **Philosophy: Fun First, Complexity Later**
**Primary goal is NOT monetization** - it's making a simple, fun, free game that works well.

**Approach: Start Single-Player, Add Online Only If Needed**

### **Phase 1: Single-Player / Local Multiplayer (Launch Target)**
- **AI opponents** for practice/solo play
- **Local multiplayer** (split-screen or hot-seat if feasible)
- **No networking complexity** - just make the gameplay fun
- **Launch as free game** on Steam and/or itch.io
- **See if people actually want online multiplayer** before building it

**Why start here:**
- Focus on making the core game fun, not infrastructure
- Works offline - no servers, no costs, no maintenance
- Much simpler to develop and test
- Many successful indie games started this way

### **Phase 2: Online Multiplayer (Only If There's Demand)**

If people play the game and ask for online multiplayer, pick the simplest approach:

**Option A: Steam P2P (Simplest for casual play)**
- **Free forever** - no servers, no monthly costs
- **Friend invites** via Steam - built-in
- **Easy to implement** - Steamworks API handles everything
- **Downside:** Players can cheat (but does it matter for casual fun?)
- **Best for:** Playing with friends, casual matches

**Option B: Photon Free Tier (If you need matchmaking)**
- **Free for 20 concurrent users** - likely enough for a small free game
- **Easy to add** to existing Unity code
- **Built-in matchmaking** and lobbies
- **Only pay if game gets popular** ($95/mo for 100 CCU)
- **Best for:** Strangers finding matches, ranked play

**Option C: Complex Infrastructure (Only if game really takes off)**
- Mirror + dedicated servers + EOS/PlayFab
- For thousands of concurrent players
- Anti-cheat, server authority, regional servers
- **Don't build this unless you need it** - massive complexity for uncertain benefit

### **Decision Tree:**
1. **Game is fun but nobody plays it** → No online multiplayer needed, move on to next project
2. **People play and want to play with friends** → Add Steam P2P (1 week of work)
3. **People want matchmaking with strangers** → Add Photon (2-3 weeks of work)
4. **Game unexpectedly blows up** → Migrate to dedicated servers (revisit complex architecture)

### **Current Plan:**
- Build combat mechanics and AI opponents first
- Release single-player version as free game
- Wait for player feedback
- Add online multiplayer only if people ask for it
- Keep it simple - use Steam P2P or Photon free tier initially

## Performance Targets
- Must run on low-end PC (no dedicated GPU)
- Simple geometry, low poly counts
- URP with performance settings
- No real-time shadows (baked if needed)
- Small texture sizes (512x512 or lower)

---

## Project Recovery & Troubleshooting

### D3D11 Swapchain Crash - Complete Resolution Process (2025-10-24)

**Original Problem:** "Failed to present D3D11 swapchain due to device reset/removed" crash on integrated GPU

**Root Causes Identified:**
1. **Directional Light had Soft Shadows enabled** (m_Type: 2 instead of 0)
   - Fixed in: Assets/Scenes/TestArena.unity line 308
   - Even with QualitySettings shadows disabled, scene lights can override
2. **Corrupted GUID in prefab** (WeaponPickup_Missile.prefab)
   - Script reference had null GUID (all zeros)
   - Fixed by replacing with correct WeaponPickup.cs GUID: cc41b4a11ca72de4dae4370299701ea3

**Recovery Process Used:**
1. Deleted Library/ and Temp/ folders to clear Unity cache
2. Fixed corrupted prefab GUID while Unity was closed
3. Reopened Unity to regenerate clean cache with fixes in place

**Key Lessons:**
- Library folder cache can be safely deleted - Unity regenerates it from scene/asset files
- All critical data is in git: scenes, scripts, prefabs, materials, ProjectSettings
- Scene files store EVERY Inspector value in YAML format - zero manual setup needed after clone
- Always check individual Light component shadow settings in scene files, not just global QualitySettings

### Nuclear Recovery Options (Tested & Verified)

**Option 1: Clear Unity Cache (Safest - 30 seconds)**
```bash
# Close Unity first!
cd C:\Users\avoca\ufo-vs-ufo
rmdir /s /q Library
rmdir /s /q Temp
# Reopen Unity - it regenerates everything automatically
```
**Manual work required:** ZERO

**Option 2: Fresh Clone from GitHub (Safe - 2 minutes)**
```bash
cd C:\Users\avoca
git clone https://github.com/mikeprimak/ufo-vs-ufo.git ufo-fresh
# Open ufo-fresh in Unity Hub
```
**Manual work required:** ZERO (all Inspector values are in scene/prefab YAML files)

**Option 3: Full Unity Reinstall (Nuclear - 15 minutes)**
1. Uninstall Unity 2022.3 LTS via Unity Hub
2. Delete Unity cache folders:
   - `C:\Users\avoca\AppData\Local\Unity\cache`
   - `C:\Users\avoca\AppData\LocalLow\Unity`
   - `C:\Users\avoca\AppData\Roaming\Unity`
3. Reinstall Unity 2022.3 LTS
4. Clone fresh from GitHub
5. Open in Unity
**Manual work required:** ZERO

**What's Saved in GitHub (100% Recoverable):**
- ✅ All scenes with complete Inspector settings
- ✅ All scripts
- ✅ All prefabs with component configurations
- ✅ All materials and physics materials
- ✅ All URP pipeline assets (including critical URP-Performant settings)
- ✅ All ProjectSettings (Graphics, Quality, Input, Physics)
- ✅ All meta files with GUIDs

**What's NOT Saved (Auto-regenerated by Unity):**
- Library/ (cache - regenerates in 2-5 minutes)
- Temp/ (temporary files)
- .csproj/.sln (Visual Studio files)

---

## Meta Instructions
- **ALWAYS update this file when pushing to GitHub**
- **Every 20 updates:** Condense file while preserving all critical project knowledge
- This file serves as session continuity for future Claude Code sessions
