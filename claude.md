# UFO vs UFO - Project Context

**Last Updated:** 2025-10-24
**Update Count:** 16

## Project Overview
N64 Mario Kart Battle Mode-style aerial combat game in Unity 2022.3 LTS (URP template).
- Low-poly UFO vehicles with arcade physics
- Small 3D arenas with vertical elements
- Third-person camera, wide FOV
- Simple projectile weapons and pickups
- Must run on low-end PC (no dedicated GPU)

## Current Phase
**Phase 1 Complete:** Basic UFO flight mechanics with keyboard and gamepad support

## File Structure
```
Assets/
├── Scripts/
│   ├── Vehicle/
│   │   ├── UFOController.cs - Main flight controller
│   │   ├── UFOCollision.cs - Collision bounce system
│   │   ├── UFOHoverWobble.cs - Hover bobbing effect (not in use)
│   │   └── UFOThrusterEffects.cs - Particle effects (not set up)
│   ├── Camera/
│   │   └── UFOCamera.cs - Third-person follow camera
│   ├── Combat/ (empty - future)
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
- Visual Pitch Amount: 30°
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

**Controls:**
- A / Controller Button 0 → Accelerate
- D / Controller Button 1 → Brake/Reverse
- Arrow Keys / Left Stick → Turn left/right, Ascend/Descend
- Q / RB (Button 5) → Barrel roll right
- E / LB (Button 4) → Barrel roll left

**Features:**
- Arcade physics: tight turns, instant brake, momentum-based movement
- Auto-leveling prevents tilting from impacts
- Banking effect when turning (visual only, applied to UFO_Visual child)
- Pitch effect when ascending/descending while moving forward (nose tilts up/down)
- Pitch only applies when horizontal speed > 5 units/sec (no tilt when hovering vertically)
- **Fast vertical movement**: 3x speed boost when moving only up/down OR during barrel rolls
  - Normal vertical speed: 8 units/sec
  - Pure vertical speed: 24 units/sec (no forward/backward movement)
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

**FOV Kick Settings (Game Feel - NEW):**
- Enable FOV Kick: true
- Acceleration FOV Boost: 5° (75 → 80 when accelerating)
- Brake FOV Reduction: 5° (75 → 70 when braking)
- Combo Boost FOV Boost: 10° (75 → 85 during combo boost)
- FOV Kick Speed: 5 (transition smoothness)

**Camera Shake Settings (Game Feel - NEW):**
- Enable Camera Shake: true
- Shake Duration: 0.2s
- Shake Intensity: 0.15 units
- Shake Decay Speed: 3

**Features:**
- Tight rotation tracking for forward-firing weapon aiming (0.5-1.0 recommended)
- Tracks UFO physics rotation only (not visual banking from UFO_Visual)
- Keeps horizon level for consistent aiming
- Dynamic vertical tilt: ascending = camera tilts up, descending = camera tilts down
- Smooth position following with retained smoothing for comfort
- Designed for center-screen aiming and forward-firing weapons
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
- Phase 2: Combat mechanics (projectiles, pickups)
- Phase 3: Multiple arenas
- Phase 4: Multiplayer/AI opponents
- Visual effects: Thruster particles, trail renderer (scripts exist but not set up)
- Audio: Engine sounds, impacts, weapon sounds
- Camera polish: Screen shake, FOV kick on acceleration

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
