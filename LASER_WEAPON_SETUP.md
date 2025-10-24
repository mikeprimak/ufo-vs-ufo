# Laser Weapon Setup Guide (ELI5)

## What I Created

**`LaserWeapon.cs`** - A continuous beam weapon that:
- ✅ Fires straight forward from UFO
- ✅ Stays active for 2 seconds
- ✅ Rotates with the UFO as you turn
- ✅ Deals continuous damage (30 damage/second)
- ✅ Uses raycast (instant hit, not a projectile)
- ✅ Has 1 second cooldown after firing
- ✅ Visual beam using LineRenderer

---

## What You Do In Unity (2 minutes)

### Step 1: Add Laser to Your UFO

1. **In Hierarchy**, select `UFO_Player`

2. **In Inspector**, click `Add Component`

3. **Type "LaserWeapon"** and add it

4. **Configure the settings** (they auto-fill with good defaults):
   ```
   Beam Duration: 2 seconds
   Beam Range: 100 units
   Beam Width: 0.5 units
   Damage Per Second: 30
   Cooldown: 1 second
   Beam Color: Red (or pick your color!)
   ```

5. **Done!** That's it for basic setup.

---

### Step 2: Test It

1. **Press Play**

2. **Press Fire3 button** to fire laser:
   - **Keyboard**: Usually mapped to a key (might need to set this)
   - **Controller**: Button 2 (X on Xbox / Square on PlayStation)
   - **Alternative**: Press `JoystickButton2`

3. **Watch the laser:**
   - Red beam shoots straight forward
   - Lasts 2 seconds
   - Follows your UFO as you turn
   - Check Console for "Laser hitting [target]" messages

---

## Input Setup (if Fire3 doesn't work)

If pressing the controller button doesn't work:

### Option A: Use a Different Key (Quick)

1. Open `LaserWeapon.cs` in your code editor

2. Find line ~67:
   ```csharp
   if (Input.GetButtonDown("Fire3") || Input.GetKeyDown(KeyCode.JoystickButton2))
   ```

3. Change to whatever key you want:
   ```csharp
   if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton2))
   ```
   Now Space = fire laser!

### Option B: Add Fire3 to Input Manager (Proper Way)

1. **Edit → Project Settings → Input Manager**
2. Expand "Fire3" (or create if missing)
3. Set button to "joystick button 2"

---

## Making It Look Better (Optional)

### Add a Glowing Material

1. **Right-click in Project** → Create → Material
2. **Name it** "LaserMaterial"
3. **Change Shader** to "Particles/Standard Unlit" or "Unlit/Color"
4. **Set color** to bright red/blue/green
5. **Enable Emission** and set emission color to same color
6. **Select UFO_Player** → Find LaserWeapon component
7. **Drag LaserMaterial** into the "Beam Material" slot

### Make It Wider/Narrower

- Select `UFO_Player`
- Find `LaserWeapon` component
- Change `Beam Width: 0.5` to whatever you want
  - `0.2` = thin sniper beam
  - `1.0` = thick death ray
  - `2.0` = massive beam

### Change Color

- Select `UFO_Player`
- Find `LaserWeapon` component
- Click the `Beam Color` box
- Pick any color!

---

## Balancing the Laser

### Make it more/less powerful:

**Longer duration:**
- `Beam Duration: 2` → `3` or `4` seconds

**More damage:**
- `Damage Per Second: 30` → `50` or `100`
- Total damage = Duration × DPS
- Example: 2 sec × 30 dps = 60 total damage

**Shorter range:**
- `Beam Range: 100` → `50` (close range only)

**Longer cooldown:**
- `Cooldown: 1` → `3` or `5` seconds (can't spam it)

---

## How It Works

1. **Press Fire3** → Laser activates for 2 seconds
2. **Every frame** while active:
   - Raycast from UFO forward
   - If it hits something tagged "Player" (and not yourself), deal damage
   - Update beam visual to show laser
3. **Follows UFO rotation** - turn your UFO, laser turns with it
4. **After 2 seconds** → Laser turns off, 1 second cooldown
5. **After cooldown** → Can fire again

---

## Testing With Target UFO

1. Fire the laser at your `UFO_Target` dummy
2. **Console should show**: "Laser hitting UFO_Target for X damage this frame!"
3. You'll see the red beam connecting you to the target
4. Turn your UFO while firing - beam should follow!

---

## Known Limitations

- **No health system yet** - Damage is calculated but not applied (just logged)
- **One laser per UFO** - Can't have multiple lasers on same UFO
- **Hits everything** - Will hit walls/floor too (stops beam at collision)

---

## Troubleshooting

**"Nothing happens when I press Fire3":**
- Check Console for error messages
- Try changing to KeyCode.Space (see Input Setup above)
- Make sure LaserWeapon component is enabled (checkbox is checked)

**"Can't see the laser beam":**
- Check `Beam Width` isn't 0
- Check `Beam Color` isn't transparent/black
- Make sure you're in Play mode
- Look at Console - does it say "Laser activated!"?

**"Laser doesn't follow my UFO":**
- This shouldn't happen - it uses `transform.forward` every frame
- Check if UFO is actually rotating (visual banking vs physics rotation)

**"Laser hits myself":**
- The code checks `hit.collider.gameObject != owner`
- Make sure both UFOs have different GameObjects

---

## Next Steps

Want to add:
- **Different colors per player?** Change `Beam Color` per UFO
- **Ammo system?** Add ammo counter like the bullet weapon
- **Charging time?** Add a charge-up delay before firing
- **Wider beam?** Increase `Beam Width`
- **Pulsing effect?** Animate the beam color over time

Let me know what you want to add!
