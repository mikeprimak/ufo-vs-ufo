# Weapon System Setup Guide (ELI5)

## What I Created

A complete weapon pickup and management system:

**Scripts:**
- ✅ **WeaponManager.cs** - Controls which weapon is active, handles single-use system
- ✅ **WeaponPickup.cs** - Floating pickup boxes that give weapons
- ✅ **WeaponUI.cs** - Displays current weapon on screen

**System Features:**
- UFO starts with NO weapons
- Must pick up weapon boxes to get weapons
- Each pickup = 1 use only
- After using weapon, UFO has no weapon until next pickup
- UI shows current weapon name

**Four Weapons:**
1. **Missile** - 1 straight-firing projectile
2. **Homing Missile** - 1 target-tracking missile
3. **Laser** - 1 continuous beam (3 seconds)
4. **Burst** - 1 burst of 13 rapid shots

---

## Setup Instructions

### Part 1: Setup WeaponManager on UFO (3 minutes)

1. **Select `UFO_Player`** in Hierarchy

2. **Add Component** → Type "WeaponManager"

3. **Configure weapon references:**
   - You should see empty slots for each weapon type
   - Leave them empty for now, we'll set them up next

4. **Add a SECOND WeaponSystem** for homing missiles:
   - Click "Add Component" → Type "WeaponSystem"
   - You now have TWO WeaponSystem components on UFO_Player
   - One for regular missiles, one for homing missiles

5. **Configure the second WeaponSystem:**
   - In the NEW WeaponSystem, drag `Projectile_Missile` (homing) into "Projectile Prefab"
   - Set Fire Rate: 0.5
   - Set Current Ammo: 1
   - Set Max Ammo: 1

6. **Go back to WeaponManager** and assign references:
   - `Weapon System`: Drag the FIRST WeaponSystem (the one with Projectile_Bullet)
   - `Homing Weapon System`: Drag the SECOND WeaponSystem (the one with Projectile_Missile)
   - `Laser Weapon`: Drag the LaserWeapon component
   - `Burst Weapon`: Drag the BurstWeapon component

7. **Set Current Weapon** to "None" in WeaponManager

8. **IMPORTANT: Disable all weapon components:**
   - Uncheck the box next to WeaponSystem (first one)
   - Uncheck the box next to WeaponSystem (second one)
   - Uncheck the box next to LaserWeapon
   - Uncheck the box next to BurstWeapon
   - WeaponManager will enable them when needed

---

### Part 2: Create UI Display (2 minutes)

1. **In Hierarchy**, right-click → **UI → Text**
   - This creates Canvas, EventSystem, and Text automatically

2. **Select the Text object** (should be called "Text")

3. **Position it** in top-left corner:
   - In Rect Transform, click the anchor preset (top-left box)
   - Hold Shift+Alt and click top-left anchor
   - Set Pos X: 100, Pos Y: -30

4. **Style the text:**
   - Text: "NO WEAPON"
   - Font Size: 24
   - Color: White
   - Horizontal/Vertical: Left/Top

5. **Add WeaponUI component:**
   - With Text still selected, click "Add Component"
   - Type "WeaponUI"
   - In WeaponUI component:
     - Drag `UFO_Player` into "Weapon Manager" slot
     - "Weapon Text" should auto-fill with the Text component

---

### Part 3: Create Weapon Pickups (5 minutes)

I'll show you how to make one, then you can duplicate for others:

**Create Missile Pickup:**

1. **In Hierarchy**, right-click → **3D Object → Cube**

2. **Rename** it to "WeaponPickup_Missile"

3. **Position** it somewhere visible (X: 10, Y: 2, Z: 0)

4. **Add Component** → Type "WeaponPickup"

5. **Configure WeaponPickup:**
   - Weapon Type: **Missile**
   - Respawns: ✓ (checked)
   - Respawn Time: 15
   - Rotation Speed: 90
   - Bob Speed: 1
   - Bob Height: 0.5

6. **Make it colorful** (so you know what it is):
   - Create a new Material: Right-click in Project → Create → Material
   - Name it "Material_Missile" (or "Red" or whatever)
   - Change its color to Red
   - Drag material onto the cube

7. **Test it:** Press Play, fly into the cube, check UI updates!

**Create Other Pickups:**

1. **Duplicate** the Missile pickup (Ctrl+D)
2. **Rename** to "WeaponPickup_HomingMissile"
3. **Move** it to new position (X: 20, Y: 2, Z: 0)
4. **Change WeaponPickup settings:**
   - Weapon Type: **HomingMissile**
5. **Change color** to Orange/Yellow

Repeat for **Laser** (green?) and **Burst** (blue?).

---

## How It Works

### Gameplay Loop:
1. **UFO starts** with no weapon
2. **Fly into pickup box** → Get that weapon
3. **Press Fire2** (B/Circle) → Use weapon once
4. **Weapon used up** → Back to no weapon
5. **Wait 15 seconds** → Pickup respawns
6. **Repeat!**

### Each Weapon:
- **Missile**: 1 shot, flies straight
- **Homing Missile**: 1 shot, tracks target
- **Laser**: Fires once, lasts 3 seconds
- **Burst**: Fires once, shoots 13 beams rapidly

---

## Expected Behavior

✅ **What Should Happen:**
- UI shows "NO WEAPON" at start
- Fly into pickup → UI changes to weapon name
- Fire weapon → It works
- After use → UI changes back to "NO WEAPON"
- Pickup disappears, respawns after 15s

---

## Troubleshooting

**"UI doesn't update":**
- Check WeaponUI has reference to WeaponManager
- Check Text component is assigned
- Look in Console for errors

**"Pickup doesn't work":**
- Check pickup has BoxCollider with "Is Trigger" checked
- Check UFO has WeaponManager component
- Check weapon type is set correctly in WeaponPickup

**"Weapon fires multiple times":**
- Check all weapon components are DISABLED by default
- Check WeaponManager "Current Weapon" is set to "None"
- Remove any other scripts firing weapons (like old input handlers)

**"Can't see pickups":**
- Check position (Y: 2 puts it at decent height)
- Check MeshRenderer is enabled
- Add different colored materials to distinguish them

**"Weapons don't fire":**
- Check weapon components exist on UFO_Player
- Check they're assigned in WeaponManager
- Check projectile prefabs are assigned in each weapon
- Look in Console for error messages

---

## Customization

### Change Respawn Time:
- Select pickup → Change "Respawn Time: 15" to whatever

### Make Pickups Not Respawn:
- Select pickup → Uncheck "Respawns"

### Change Pickup Size:
- Select pickup → Change Scale (X: 1.5, Y: 1.5, Z: 1.5) for bigger

### Change UI Text:
- Select Text object → Change font size, color, position
- In WeaponUI component, change "No Weapon Text" or "Weapon Prefix"

### Add More Pickups:
- Just duplicate existing ones and move them around the arena!

---

## Quick Checklist

- [ ] WeaponManager added to UFO_Player
- [ ] Two WeaponSystem components (regular + homing)
- [ ] All weapon components disabled by default
- [ ] All weapons assigned in WeaponManager
- [ ] UI Text created with WeaponUI component
- [ ] At least one weapon pickup created
- [ ] Pickup has BoxCollider (trigger)
- [ ] Pickup has WeaponPickup script
- [ ] Test: Fly into pickup, see UI update
- [ ] Test: Fire weapon, it works once
- [ ] Test: UI goes back to "NO WEAPON"

---

That's everything! You now have a complete weapon pickup system.

The core loop: Pickup → Fire → Empty → Pickup again!
