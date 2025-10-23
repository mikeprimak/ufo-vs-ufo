# UFO Visual Effects Setup Guide

This guide will help you add visual polish to your UFO for better game feel.

## Part 1: UFO Banking (Tilt When Turning)

This makes the UFO lean into turns like a racing game.

### Quick Setup (Simple):

1. **Reimport UFOController.cs**
2. **Enter Play mode and test**
   - Turn left/right with arrow keys or stick
   - UFO should tilt/bank into turns automatically!

**That's it!** The UFO will now bank 25° when turning.

### Advanced Setup (Better Looking):

For better visuals, separate the visual model from the physics object:

1. **In Hierarchy**, select **UFO_Player**
2. **Right-click on UFO_Player → 3D Object → Cube** (or use your existing Sphere)
3. **Rename the new child to "UFO_Visual"**
4. **Move the Sphere mesh and DirectionIndicator INTO UFO_Visual** (drag them as children)
5. **Select UFO_Player** (the parent)
6. **In UFOController component**, drag **UFO_Visual** into the **"Visual Model"** field
7. **Test in Play mode** - now only the visual tilts, not the physics!

---

## Part 2: Hover Wobble (Gentle Floating Bob)

Makes the UFO feel alive with subtle movement.

### Setup:

1. **Select UFO_Player** in Hierarchy
2. **Inspector → Add Component → "UFO Hover Wobble"**
3. **If you did Advanced Setup above:**
   - Drag **UFO_Visual** into the **"Visual Model"** field
4. **If you skipped Advanced Setup:**
   - Leave **"Visual Model"** empty (it will wobble the whole UFO)

### Adjust Settings:

- **Bob Amount**: 0.1 (default) - how much it bobs up/down
- **Bob Speed**: 1.5 - how fast it bobs
- **Wobble Amount**: 0.5 - slight rotation wobble
- **Randomness**: 0.3 - makes it less uniform

**Test in Play mode** - UFO should gently bob and wobble!

**Too much?** Reduce Bob Amount to 0.05 and Wobble Amount to 0.3

---

## Part 3: Thruster Particles

Add particle effects for acceleration, braking, and reverse.

### Step 1: Create Particle Systems

1. **Right-click on UFO_Player** → Effects → Particle System
2. **Rename to "MainThruster"**
3. Set Transform Position: **(0, -0.5, -1)** (behind and below UFO)
4. **Configure the particle system:**
   - **Looping**: ✓ Checked
   - **Start Lifetime**: 0.5
   - **Start Speed**: 5
   - **Start Size**: 0.2
   - **Start Color**: Orange/Yellow (for thrust)
   - **Emission → Rate over Time**: 0 (we'll control this via script)
   - **Shape**: Cone, Angle: 15, Radius: 0.1

5. **Duplicate MainThruster twice** (Ctrl+D):
   - Rename copies to **"BrakeThruster"** and **"ReverseThruster"**

6. **Position BrakeThruster**: (0, -0.5, 1) - front of UFO
   - Change Start Color to **Red/Orange** (braking)

7. **Position ReverseThruster**: (0, -0.5, 1) - same as brake
   - Change Start Color to **Blue/Cyan** (reverse)

### Step 2: Add Thruster Script

1. **Select UFO_Player**
2. **Inspector → Add Component → "UFO Thruster Effects"**
3. **Drag particle systems into slots:**
   - **Main Thruster** → MainThruster
   - **Brake Thruster** → BrakeThruster
   - **Reverse Thruster** → ReverseThruster
4. **Emission Rate**: 50 (default is good)

**Test in Play mode:**
- Press **A** → Orange particles from back
- Press **D** → Red particles from front (braking)
- Hold **D** after stopping → Blue particles from front (reverse)

### Optional: Better Particle Look

For each particle system, try these settings:

**Color over Lifetime:**
- Add a gradient that fades from bright → transparent

**Size over Lifetime:**
- Make particles shrink: Start at 1, end at 0

**Texture Sheet Animation:**
- If you have sprite sheets, enable this for animated particles

---

## Part 4: Trail Renderer (Optional)

Add a cool trail behind the UFO like Mario Kart.

### Setup:

1. **Select UFO_Player**
2. **Inspector → Add Component → "Trail Renderer"**
3. **Configure:**
   - **Time**: 0.5 (how long trail lasts)
   - **Width**: Start 0.3, End 0
   - **Color**: Gradient from bright blue → transparent
   - **Material**: Default-Particle (or create your own)
   - **Min Vertex Distance**: 0.1

**Test** - UFO leaves a fading trail!

**Too intense?** Set Time to 0.3 or disable entirely.

---

## Part 5: Testing Everything Together

1. **Enter Play mode**
2. **Fly around and check:**
   - ✓ UFO tilts when turning
   - ✓ UFO gently bobs/wobbles when hovering
   - ✓ Orange particles when accelerating
   - ✓ Red particles when braking
   - ✓ Blue particles when reversing
   - ✓ (Optional) Trail follows the UFO

---

## Performance Notes (Low-End PC)

These effects are lightweight, but if you get lag:

- **Reduce particle emission rate** from 50 to 20
- **Shorten trail time** from 0.5 to 0.2
- **Disable wobble** if you don't like it (remove component)
- **Simplify particle systems** (reduce Max Particles to 50)

---

## Troubleshooting

**Banking doesn't work:**
- Reimport UFOController.cs
- Check that Bank Amount is not 0

**Wobble is too much:**
- Reduce Bob Amount to 0.05
- Reduce Wobble Amount to 0.2

**Particles don't show:**
- Check particle systems are children of UFO_Player
- Make sure they're assigned in UFOThrusterEffects component
- Check Emission Rate over Time is 0 (script controls it)

**Wobble fights with banking:**
- Make sure you're using the Advanced Setup (separate UFO_Visual)
- Wobble and banking should both reference UFO_Visual

---

## Next Steps

Once you have these working:
- Adjust values in Play mode to find what feels best
- Add sound effects (engine hum, thrust whoosh, brake screech)
- Create a custom material for particles (glowing sprites)
- Add camera shake on hard landings

Your UFO should now feel **much** more polished and fun to control!
