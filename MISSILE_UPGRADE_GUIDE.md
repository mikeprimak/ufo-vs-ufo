# Missile Torpedo Shape Upgrade Guide

## Quick Instructions (5 minutes per missile type)

### Step 1: Open Projectile_Missile Prefab
1. In Unity Project window, navigate to `Assets/Prefabs`
2. Double-click `Projectile_Missile.prefab` to enter Prefab Edit mode

### Step 2: Delete the Current Sphere Visual
1. In Hierarchy, you'll see the current projectile (probably a Sphere)
2. Select any visual mesh (Sphere, Cube, etc.) - NOT the root GameObject
3. Delete it (but keep the root GameObject with scripts/Rigidbody/Collider)

### Step 3: Create Torpedo Body (Cylinder)
1. Right-click the root `Projectile_Missile` GameObject
2. Select `3D Object → Cylinder`
3. Rename it to `Torpedo_Body`
4. Set Transform values:
   - Position: `X: 0, Y: 0, Z: 0`
   - Rotation: `X: 0, Y: 0, Z: 90` (makes it point forward)
   - Scale: `X: 0.3, Y: 1.0, Z: 0.3` (thin body, stretched forward)

### Step 4: Create Torpedo Nose (Cone)
1. Right-click the root `Projectile_Missile` GameObject
2. Select `3D Object → Cone`
3. Rename it to `Torpedo_Nose`
4. Set Transform values:
   - Position: `X: 0, Y: 0, Z: 1.0` (front of body)
   - Rotation: `X: 0, Y: 0, Z: -90` (points forward)
   - Scale: `X: 0.3, Y: 0.4, Z: 0.3` (matches body width)

### Step 5: Create Tail Fins (Optional)
For a more missile-like look:

1. Right-click root → `3D Object → Cube`
2. Rename to `Tail_Fin_1`
3. Set Transform:
   - Position: `X: 0, Y: 0.2, Z: -0.8` (back of body, sticking out top)
   - Rotation: `X: 0, Y: 0, Z: 0`
   - Scale: `X: 0.05, Y: 0.3, Z: 0.3` (thin vertical fin)

4. Duplicate (Ctrl+D) and rename to `Tail_Fin_2`
   - Position: `X: 0, Y: -0.2, Z: -0.8` (bottom fin)

5. Duplicate again for `Tail_Fin_3`
   - Position: `X: 0.2, Y: 0, Z: -0.8` (right fin)

6. Duplicate for `Tail_Fin_4`
   - Position: `X: -0.2, Y: 0, Z: -0.8` (left fin)

### Step 6: Apply Material Color
1. In Project window, find your red missile material
2. Drag it onto all child objects (Body, Nose, Fins)
3. Or create a new red material:
   - Right-click in Materials folder → Create → Material
   - Name it `Missile_Red`
   - Set Albedo color to bright red

### Step 7: Save and Test
1. Click the `<` arrow at top left to exit Prefab mode
2. Auto-saves the prefab
3. Enter Play mode and fire a missile to see the new shape!

---

## Repeat for Homing Missile (Different Color)

Follow the same steps above, but for `Projectile_Missile.prefab`'s homing variant:
- Use a different material color (orange or yellow) to distinguish it
- Same shape works great for both missile types

---

## Alternative: Simpler 2-Part Design (60 seconds)

If you want ultra-fast setup:

**Just Body + Nose:**
1. Cylinder (rotated 90° on Z, scale 0.3, 1.0, 0.3)
2. Cone (at Z: 1.0, rotated -90° on Z, scale 0.3, 0.4, 0.3)
3. Apply red material
4. Done!

Skip the fins for a cleaner, faster look that still reads as "missile."

---

## Final Hierarchy Should Look Like:
```
Projectile_Missile (root - has scripts, Rigidbody, SphereCollider)
├─ Torpedo_Body (Cylinder)
├─ Torpedo_Nose (Cone)
├─ Tail_Fin_1 (Cube - optional)
├─ Tail_Fin_2 (Cube - optional)
├─ Tail_Fin_3 (Cube - optional)
└─ Tail_Fin_4 (Cube - optional)
```

**Note:** The SphereCollider on the root is fine - it doesn't need to match the visual shape exactly. Sphere colliders are faster and work great for projectiles.

---

## After Upgrading: Screenshot for UI Icons

1. Position missile prefab in empty area of TestArena scene
2. Add a bright directional light above it
3. Position Main Camera to get a nice angled view of the missile
4. In Game view, take a screenshot (Windows: Win+Shift+S)
5. Crop to square and save as `missile_icon.png`
6. Drag into Unity project
7. Use for UI weapon icon!
