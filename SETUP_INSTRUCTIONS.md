# UFO vs UFO - Phase 1 Setup Instructions

## Step-by-Step Unity Editor Setup

### Part 1: Create the Test Arena

1. **Create a new scene**
   - File → New Scene
   - Choose "Basic (URP)" template
   - File → Save As → Save in `Assets/Scenes/` as `TestArena`

2. **Create the arena floor**
   - Right-click in Hierarchy → 3D Object → Plane
   - Rename it to "ArenaFloor"
   - In Inspector, set Transform:
     - Position: (0, 0, 0)
     - Rotation: (0, 0, 0)
     - Scale: (5, 1, 5) — this creates a 50x50 unit arena

3. **Add some walls (optional but helpful)**
   - Right-click in Hierarchy → 3D Object → Cube
   - Rename to "Wall_North"
   - Set Transform:
     - Position: (0, 2.5, 25)
     - Scale: (50, 5, 1)
   - Duplicate (Ctrl+D) and create other walls:
     - Wall_South: Position (0, 2.5, -25)
     - Wall_East: Position (25, 2.5, 0), Scale (1, 5, 50)
     - Wall_West: Position (-25, 2.5, 0), Scale (1, 5, 50)

### Part 2: Create the UFO

4. **Create UFO GameObject**
   - Right-click in Hierarchy → 3D Object → Sphere
   - Rename it to "UFO_Player"
   - In Inspector, set Transform:
     - Position: (0, 3, 0)
     - Scale: (1.5, 0.5, 1.5) — flattened sphere looks like a UFO

5. **Add visual indicator for front direction**
   - Right-click on UFO_Player → 3D Object → Cube
   - Rename to "DirectionIndicator"
   - Set Transform:
     - Position: (0, 0, 0.8) — moves it forward
     - Scale: (0.3, 0.1, 0.5)

6. **Add a Rigidbody to the UFO**
   - Select UFO_Player
   - In Inspector → Add Component
   - Search for "Rigidbody" and add it
   - Set Rigidbody settings:
     - Mass: 1
     - Drag: 0 (script will handle this)
     - Angular Drag: 0
     - **UNCHECK** "Use Gravity"
     - Constraints: Freeze Rotation X and Z (leave Y unfrozen)

7. **Add the UFO Controller script**
   - Select UFO_Player
   - In Inspector → Add Component
   - Search for "UFO Controller" and add it
   - You can adjust values in Inspector (defaults are good to start):
     - Max Speed: 15
     - Acceleration: 30
     - Turn Speed: 180
     - Vertical Speed: 8

### Part 3: Setup the Camera

8. **Select the Main Camera**
   - Click on "Main Camera" in Hierarchy
   - Set Transform Position: (0, 8, -12) — starting position

9. **Add the UFO Camera script**
   - With Main Camera selected
   - Inspector → Add Component
   - Search for "UFO Camera" and add it
   - In the script component:
     - **Drag UFO_Player from Hierarchy into the "Target" field**
     - Adjust settings (defaults are good):
       - Distance: 10
       - Height: 5
       - Field Of View: 75

### Part 4: Add Lighting (N64 Style)

10. **Adjust Directional Light**
    - Select "Directional Light" in Hierarchy
    - Set Rotation: (50, -30, 0)
    - Set Intensity: 1
    - Color: Slightly warm white

### Part 5: Test the Game!

11. **Play the game**
    - Click the Play button at the top (or press Ctrl+P)

12. **Controls:**
    - **A** - Accelerate forward
    - **D** - Brake / Reverse (hold after stopping to go backward)
    - **Left/Right Arrow** - Turn left/right
    - **Up Arrow** - Ascend
    - **Down Arrow** - Descend

### Part 6: Optional Visual Improvements

13. **Create a simple material for the UFO**
    - Right-click in Assets/Materials → Create → Material
    - Name it "UFO_Material"
    - Set Albedo color to bright blue or green
    - Drag material onto UFO_Player in Hierarchy

14. **Create arena material**
    - Create another material named "Arena_Material"
    - Set Albedo to gray or brown
    - Drag onto ArenaFloor

## Troubleshooting

**UFO falls through the floor:**
- Make sure Rigidbody "Use Gravity" is UNCHECKED

**UFO doesn't move:**
- Check that UFOController script is attached
- Make sure Rigidbody constraints are set correctly

**Camera doesn't follow:**
- Ensure UFO_Player is dragged into the "Target" field on UFOCamera script

**UFO rotates weirdly:**
- Check Rigidbody Constraints: X and Z rotation should be frozen

## Next Steps

Once this is working, you can:
- Add more visual details to the UFO (add child objects)
- Create obstacles in the arena
- Adjust physics values for better feel
- Add particle effects for thrust
- Start working on combat mechanics

## Performance Tips for Low-End PC

- Keep poly count low (use simple shapes)
- Limit draw calls
- Use URP's performance settings
- Avoid real-time shadows (use baked if needed)
- Keep texture sizes small (512x512 or lower)
