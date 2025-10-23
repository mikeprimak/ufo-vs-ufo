# UFO vs UFO - Project Context

**Last Updated:** 2025-10-23
**Update Count:** 4

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
- Vertical Speed: 8
- Drag Amount: 2 (increased for quicker stopping)
- Auto Level Speed: 2
- Visual Model: UFO_Visual (for banking and pitch effects)
- Bank Amount: 25°
- Bank Speed: 3
- Visual Pitch Amount: 30°
- Visual Pitch Speed: 3
- Min Speed For Pitch: 5

**Controls:**
- A / Controller Button 0 → Accelerate
- D / Controller Button 1 → Brake/Reverse
- Arrow Keys / Left Stick → Turn left/right, Ascend/Descend

**Features:**
- Arcade physics: tight turns, instant brake, momentum-based movement
- Auto-leveling prevents tilting from impacts
- Banking effect when turning (visual only, applied to UFO_Visual child)
- Pitch effect when ascending/descending while moving forward (nose tilts up/down)
- Pitch only applies when horizontal speed > 5 units/sec (no tilt when hovering vertically)
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
  - Light touch: Dead stop with tiny bounce
  - Heavy crash (>10 speed): Bounce up + red flash + brief stun
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
- Vertical Tilt Amount: 1.5 (camera tilts with vertical movement)
- Vertical Smoothing: 3

**Features:**
- Tight rotation tracking for forward-firing weapon aiming (0.5-1.0 recommended)
- Tracks UFO physics rotation only (not visual banking from UFO_Visual)
- Keeps horizon level for consistent aiming
- Dynamic vertical tilt: ascending = camera tilts up, descending = camera tilts down
- Smooth position following with retained smoothing for comfort
- Designed for center-screen aiming and forward-firing weapons

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

## Meta Instructions
- **ALWAYS update this file when pushing to GitHub**
- **Every 20 updates:** Condense file while preserving all critical project knowledge
- This file serves as session continuity for future Claude Code sessions
