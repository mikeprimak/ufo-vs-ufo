# UFO vs UFO - Project Context

**Last Updated:** 2025-10-23
**Update Count:** 1

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
- Drag Amount: 0.5 (low for momentum)
- Auto Level Speed: 2
- Visual Model: UFO_Visual (for banking effect)
- Bank Amount: 25°
- Bank Speed: 3

**Controls:**
- A / Controller Button 0 → Accelerate
- D / Controller Button 1 → Brake/Reverse
- Arrow Keys / Left Stick → Turn left/right, Ascend/Descend

**Features:**
- Arcade physics: tight turns, instant brake, momentum-based movement
- Auto-leveling prevents tilting from impacts
- Banking effect when turning (visual only, applied to UFO_Visual child)
- No gravity - pure hovering flight
- Rigidbody constraints: FreezeRotationX | FreezeRotationZ

### UFOCollision.cs
**Current Inspector Values:**
- Bounce Force: 20 (user preference, default was 4000)
- Min Impact Speed: 3
- Stun Duration: 0.3 (not used)
- Flash Color: Red
- Ufo Renderer: UFO_Body (optional, for red flash)

**Features:**
- Bounces UFO in exact opposite direction of travel
- Locks rotation during bounce (UFO stays facing same direction)
- Bounce ends when velocity < 0.5 or after 1 second
- Brief red flash on impact (100ms)
- Player retains acceleration/turning control throughout

**Important:** Wobble feature was attempted but removed due to conflicts with banking

### UFOCamera.cs
**Current Inspector Values:**
- Target: UFO_Player
- Distance: 10
- Height: 5
- Smooth Speed: 5
- Rotation Smoothing: 3
- Field of View: 75° (N64-style wide FOV)
- Vertical Height Offset: -0.2 (camera drops when ascending)
- Vertical Tilt Amount: 1.5° (camera tilts with vertical movement)
- Vertical Smoothing: 3

**Features:**
- Tracks UFO rotation (camera rotates with turns)
- Dynamic vertical adjustment (drops/tilts when ascending/descending)
- Smooth following and rotation

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
