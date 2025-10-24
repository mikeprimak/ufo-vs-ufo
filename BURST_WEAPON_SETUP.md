# Burst Weapon Setup Guide (ELI5)

## What I Created

**BurstWeapon.cs** - Rapid-fire burst cannon that:
- ✅ Fires 13 projectiles in rapid succession with one trigger press
- ✅ Each beam fires at the UFO's current aim direction (tracks movement during burst)
- ✅ Alternates between left and right sides of UFO
- ✅ 0.08s delay between shots (~1 second total burst)
- ✅ Beam-shaped projectiles (1m long capsules)

**Projectile_Beam.prefab** - Elongated laser bolt:
- ✅ Capsule shape (0.2 x 0.2 x 1.0) = ~1m long beam
- ✅ Fast speed (70 units/sec, same as missiles)
- ✅ Low damage per shot (5 damage, but 13 shots = 65 total)
- ✅ 3 second lifetime

---

## What You Do In Unity (2 minutes)

### Step 1: Fix the Beam Prefab

1. **Open Unity** → Wait for files to import

2. **In Project**, go to `Assets/Prefabs/Projectile_Beam`

3. **Click it**, in Inspector find the script component

4. **Fix the script reference:**
   - Click the ⊙ circle next to "Script: None"
   - Type "Projectile"
   - Select it

5. **Verify settings** (should show):
   ```
   Speed: 70
   Lifetime: 3
   Damage: 5
   ```

### Step 2: Add Burst Weapon to UFO

1. **Select `UFO_Player`** in Hierarchy

2. **Add Component** → Type "BurstWeapon"

3. **Drag `Projectile_Beam`** into the "Projectile Prefab" slot

4. **Settings are already good:**
   ```
   Burst Count: 13 shots
   Burst Delay: 0.08s between shots
   Cooldown: 2s after burst
   Fire Point Offset: 2 (left/right distance)
   Current Ammo: 50
   Max Ammo: 100
   Ammo Per Burst: 13
   ```

### Step 3: Test It

1. **Press Play**

2. **Press Space** or **Controller Button 3** (Y/Triangle)

3. **Watch 13 beams fire rapidly** alternating left-right!

---

## How It Works

1. **Press Space/Button 3** → Burst starts
2. **Every 0.08 seconds:**
   - Fires one beam from left or right side
   - Aims at UFO's CURRENT direction (not locked)
   - Alternates sides (left, right, left, right...)
3. **After 13 shots** (~1 second) → 2 second cooldown
4. **Each beam:**
   - Flies straight at 70 speed
   - 1m long capsule shape
   - 5 damage per hit
   - Lasts 3 seconds

---

## Expected Behavior

✅ **What Should Happen:**
- Single trigger press fires all 13 beams automatically
- Beams come from left and right sides, alternating
- If you turn during burst, beams follow your new aim
- Sounds like rapid-fire cannon (if you add audio)
- Each beam is elongated, not round

---

## Customizing

### Change Fire Rate
- `Burst Delay: 0.08` → Lower = faster burst, Higher = slower

### Change Shot Count
- `Burst Count: 13` → Any number you want

### Change Damage
- Select `Projectile_Beam` prefab
- Change `Damage: 5` → Higher per shot

### Change Beam Speed
- Select `Projectile_Beam` prefab
- Change `Speed: 70` → Faster/slower

### Change Beam Size
- Select `Projectile_Beam` prefab
- Change Scale: `(0.2, 0.2, 1.0)`
  - X/Y = thickness
  - Z = length (1.0 = 1 meter)

### Change Left/Right Spacing
- `Fire Point Offset: 2` → Wider/narrower spread

---

## Input Setup

**Default key:** Space (keyboard) or Button 3 (controller Y/Triangle)

**To change:**
1. Open `BurstWeapon.cs`
2. Find line ~66:
   ```csharp
   if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton3))
   ```
3. Change `KeyCode.Space` to whatever you want

---

## Advanced: Manual Fire Points

If you want precise left/right fire positions:

1. **In Hierarchy**, select `UFO_Player`
2. **Right-click** → Create Empty
3. **Name it** "LeftFirePoint"
4. **Position it** on left side (X: -2, Y: 0, Z: 2)
5. **Repeat** for "RightFirePoint" (X: 2, Y: 0, Z: 2)
6. **Drag them** into BurstWeapon's `Left Fire Point` and `Right Fire Point` slots

Now beams fire from exact positions you set!

---

## Troubleshooting

**"Nothing happens when I press Space":**
- Check Console for ammo warnings
- Check BurstWeapon component is enabled
- Check `Projectile Prefab` is assigned
- Try a different key

**"Beams are round, not elongated":**
- Select `Projectile_Beam` prefab
- Check Scale is (0.2, 0.2, 1.0) not (0.3, 0.3, 0.3)
- The Z value (third number) should be 1.0

**"All beams fire from same spot":**
- Check `Fire Point Offset: 2` isn't set to 0
- Beams should alternate visibly from left/right

**"Beams don't follow aim during burst":**
- This is correct! Each beam fires at aim direction when THAT beam launches
- Turn during burst to see them spread out

**"Script shows None":**
- Unity needs to compile first (wait for spinner)
- Right-click `Projectile_Beam.cs` → Reimport

---

## Stats Summary

**Burst Weapon:**
- 13 shots per burst
- 0.08s between shots = ~1 second total
- 2 second cooldown
- 13 ammo per burst
- Alternates left/right

**Beam Projectile:**
- Speed: 70 (same as missile)
- Damage: 5 per beam (65 total per burst)
- Lifetime: 3 seconds
- Shape: 1m long capsule

---

That's it! Press Space and unleash a storm of laser beams!
