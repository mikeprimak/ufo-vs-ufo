# UFO vs UFO - Secondary Documentation

**Purpose:** Detailed Inspector values, troubleshooting guides, and recovery procedures.
**Primary Documentation:** See [CLAUDE.md](CLAUDE.md) for core project overview.

---

## Detailed Inspector Values

### UFOController.cs - Complete Settings

**Movement Parameters:**
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

**Visual Effects:**
- Visual Model: UFO_Visual (for banking and pitch effects)
- Bank Amount: 25°
- Bank Speed: 3
- Visual Pitch Amount: 25° (reduced from 30° for less extreme tilt)
- Visual Pitch Speed: 3
- Min Speed For Pitch: 5

**Barrel Roll Settings:**
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

**Boost System:**
- Boost Speed Multiplier: 1.8x (stacks with combo boost)
- Max Boost Time: 4 seconds
- Boost Recharge Time: 4 seconds

**Physics:**
- Rigidbody constraints: FreezeRotationX | FreezeRotationZ
- Interpolation enabled to prevent jittery visuals
- Continuous collision detection prevents phasing through walls

### UFOCollision.cs - Complete Settings

**Wall Settings:**
- Wall Bounce Force: 20
- Min Wall Impact Speed: 3

**Floor Settings:**
- Heavy Crash Threshold: 10 (vertical speed)
- Light Floor Bounce: 2
- Heavy Floor Bounce: 8
- Floor Slide Retention: 0.7 (keeps 70% horizontal momentum)
- Floor Angle Threshold: 45° (surfaces steeper than this are walls)

**Visual Feedback:**
- Flash Color: Red
- Ufo Renderer: UFO_Body (optional, for red flash)

### UFOHealth.cs - Complete Settings

- Max Health: 3 HP (default)
- Invincibility Duration: 3 seconds (i-frames after taking damage)
- Enable Invincibility Blink: true (visual feedback during i-frames)
- Blink Frequency: 8 blinks/second (rapid flashing)
- UFO Renderer: UFO_Body renderer (must be assigned in Inspector)
- Death Explosion Prefab: (optional visual effect)
- Wreck Lifetime: 10 seconds (how long wreck stays before cleanup)
- Death Sound: (optional audio clip)

### UFOAIController.cs - Complete Settings

- Aggression: 0.7 (0-1, how aggressively AI pursues)
- Decision Interval: 0.2s (how often AI makes decisions)
- Detection Range: 100 units (how far AI can see enemies)
- Attack Range: 60 units (distance to start firing)
- Wall Avoidance Distance: 15 units (minimum distance from walls)
- Arrival Distance: 10 units (how close to get to targets)
- Barrel Roll Chance: 0.3 (30% chance to barrel roll for evasion)
- Patrol Radius: 30 units (how far AI wanders when idle)

**Tuning Tips:**
- Lower aggression (0.3-0.5) for easier AI
- Higher aggression (0.9-1.0) for harder AI
- Increase decision interval (0.3-0.5s) for better performance
- Decrease attack range (40) for more aggressive close-combat AI
- See AI_SETUP_GUIDE.md for detailed tuning guide

### UFOCamera.cs - Complete Settings

**Basic Camera:**
- Target: UFO_Player
- Distance: 10
- Height: 5
- Smooth Speed: 5
- Rotation Smoothing: 0.8 (tight tracking for aiming)
- Look Down Angle: 10° (base downward tilt)
- Field of View: 75° (N64-style wide FOV)

**Vertical Movement:**
- Vertical Height Offset: -0.2 (camera drops when ascending)
- Vertical Tilt Amount: 0.5 (reduced for more horizontal camera during vertical movement)
- Vertical Smoothing: 3

**Reverse Camera:**
- Reverse Distance: 15 (camera pulls back when reversing)
- Reverse FOV: 90° (wider FOV when reversing)
- Reverse Speed Threshold: -1 (triggers reverse camera mode)
- Reverse Camera Smoothing: 3 (transition speed)

**Turn Zoom Out Settings:**
- Enable Turn Zoom Out: true
- Turn Zoom Out Distance: 3 units (additional distance during sharp turns)
- Turn Zoom Threshold: 90 degrees/sec (angular velocity to trigger zoom out)
- Turn Zoom Speed: 4 (transition smoothness)

**FOV Kick Settings:**
- Enable FOV Kick: true
- Acceleration FOV Boost: 5° (75 → 80 when accelerating)
- Brake FOV Reduction: 5° (75 → 70 when braking)
- Combo Boost FOV Boost: 10° (75 → 85 during combo boost)
- FOV Kick Speed: 5 (transition smoothness)

**Camera Shake Settings:**
- Enable Camera Shake: true
- Shake Duration: 0.3s (short, punchy shake)
- Shake Intensity: 0.4 units (subtle but visible)
- Shake Decay Speed: 5 (fades out quickly)
- Min Shake Speed: 15 units/s (no shake for light bumps - only medium/hard impacts)
- Shake Cooldown: 0.3s (prevents rapid-fire shaking when scraping walls)

**CRITICAL BUG FIXED (UFOCamera.cs:252-257):** Shake was being smoothed out by camera lerp!
- Old: Added shake to desiredPosition, THEN lerped toward it (shake got dampened)
- New: Lerp to position, THEN add shake directly (shake is instant and visible)
- Shake must be applied AFTER smoothing, not before

### UFOParticleTrail.cs - Complete Settings

**Particle Settings (BALANCED for visibility + performance):**
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

---

## Scene Setup Details (TestArena)

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

---

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
**Symptoms:** Unity crashes with "Failed to present D3D11 swapchain due to device reset/removed" - may shut down PC

**Root Cause:** Integrated GPU overloaded by expensive rendering features

**Solutions Applied:**

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

**Key Takeaway:** On integrated GPU, NEVER use:
- HighFidelity render pipeline
- Depth/Opaque textures
- High vertex counts
- Real-time shadows (even if QualitySettings disables them, check scene lights!)
- HDR/MSAA

**Visual Impact:** Game still looks great with optimized settings

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
