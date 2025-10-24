# Homing Missile Setup Guide (ELI5)

## What I Did For You

I created:
1. ✅ `HomingProjectile.cs` - The script that makes missiles track targets
2. ✅ `Projectile_Missile.prefab` - The missile object (needs fixing in Unity)

## What You Need To Do In Unity

### Step 1: Fix the Missile Prefab (1 minute)

1. **Open Unity** and wait for it to import the new files

2. **In Project window**, navigate to:
   - `Assets/Prefabs/` folder
   - Find `Projectile_Missile` (it might have a warning icon ⚠️)

3. **Click on `Projectile_Missile`** prefab to select it

4. **In Inspector window**, look for the script component at the bottom:
   - You'll see "Script: None (Script)" or similar
   - Click the small circle button ⊙ next to it
   - Search for "HomingProjectile"
   - Double-click it to assign

5. **Verify the settings** (they should auto-fill):
   ```
   Speed: 40
   Lifetime: 8
   Damage: 20
   Turn Rate: 180
   Acceleration: 20
   Max Speed: 60
   Detection Radius: 100
   Target Tag: Player
   Homing Delay: 0.2
   ```

6. **Save** (Ctrl+S)

### Step 2: Test It Quickly (2 minutes)

**Option A - Replace Existing Weapon:**

1. **In Hierarchy**, select `UFO_Player`

2. **In Inspector**, find `Weapon System` component

3. Find the `Projectile Prefab` field (currently shows `Projectile_Bullet`)

4. **Drag `Projectile_Missile`** from Project window into this field

5. **Press Play** and fire (Button 1 / B on controller)
   - The missile should track the other UFO if there is one
   - Or it flies straight if no target found

**Option B - Add Second Weapon (if you want both):**

1. **In Hierarchy**, select `UFO_Player`

2. **In Inspector**, click `Add Component`

3. Search for "Weapon System" and add a second one

4. Configure the new Weapon System:
   - Projectile Prefab: Drag `Projectile_Missile` here
   - Fire Rate: 0.5 (slower than bullets)
   - Current Ammo: 10
   - Max Ammo: 20

5. You'll need different input keys for each weapon (ask if you want this)

### Step 3: Make Sure UFOs Can Be Targeted (30 seconds)

**IMPORTANT:** Missiles find targets by the "Player" tag.

1. **In Hierarchy**, select `UFO_Player`

2. **At top of Inspector**, find the "Tag" dropdown (currently might be "Untagged")

3. **Change it to "Player"**

4. **Repeat for any other UFOs** you want the missile to track

### Step 4: Test With Two UFOs (Optional)

If you want to test missile tracking:

1. **Duplicate `UFO_Player`** in Hierarchy (Ctrl+D)

2. **Rename** it to `UFO_Enemy`

3. **Move it** away from the original (X: 20, Y: 5, Z: 0)

4. **Make sure it has Tag: Player**

5. **Disable the camera** on UFO_Enemy (uncheck UFOCamera component)

6. **Press Play**, fire a missile, watch it track the enemy!

## Expected Behavior

✅ **What Should Happen:**
- Missile fires forward for 0.2 seconds
- Then smoothly curves toward nearest "Player" tagged object
- Accelerates from 40 to 60 speed
- Turns at 180 degrees per second (smooth arc, not instant)
- Explodes on impact with anything

❌ **If It's Not Working:**
- Check both UFOs have "Player" tag
- Check the missile prefab has HomingProjectile script attached
- Check Console for "Homing missile locked onto: [name]" message
- Check Detection Radius isn't too small (should be 100)

## Tweaking The Missile (Optional)

Select `Projectile_Missile` prefab and adjust:

- **Turn Rate: 180** → Higher = sharper turns (try 90 for slower, 360 for crazy)
- **Max Speed: 60** → How fast it goes at full acceleration
- **Acceleration: 20** → How quickly it speeds up
- **Homing Delay: 0.2** → Time before tracking starts
- **Detection Radius: 100** → How far it can "see" targets

## Quick Troubleshooting

**Missile flies straight, doesn't track:**
- Check UFO has "Player" tag
- Check Detection Radius is large enough (100)
- Check Console for lock-on message

**Missile spins in circles:**
- Turn Rate might be too high
- Or it's trying to hit the UFO that fired it (shouldn't happen)

**Script shows "None" or won't attach:**
- Unity needs to compile the script first (wait a few seconds)
- Check Console for red errors
- Try right-clicking HomingProjectile.cs → Reimport

**"Can't find HomingProjectile":**
- Make sure `HomingProjectile.cs` is in `Assets/Scripts/Combat/`
- Let Unity finish importing (spinner in bottom right)

---

## That's It!

Once you do Step 1 (fix the prefab) and Step 2 (assign to weapon), you're done!

Let me know if you get stuck on any step.
