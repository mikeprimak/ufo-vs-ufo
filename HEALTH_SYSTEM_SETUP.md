# Health System Setup Guide

## Overview
This guide walks you through setting up the health system with 3 HP per UFO, damage from weapons, and death with physics wreck.

## Step 1: Add UFOHealth Component to UFOs

1. **Select UFO_Player in Hierarchy**
2. **Add Component → Scripts → UFO Health**
3. **Configure Inspector values:**
   - Max Health: **3**
   - Death Explosion Prefab: *(leave empty for now, or use ExplosionEffect prefab)*
   - Wreck Lifetime: **10** (seconds before cleanup)
   - Death Sound: *(optional audio clip)*

4. **Repeat for UFO_Dummy**

## Step 2: Create Health UI

### Create Canvas:
1. **Right-click in Hierarchy → UI → Canvas**
2. **Name it "HealthCanvas"**
3. **Set Canvas Scaler:**
   - UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: **1920 x 1080**

### Create Health Display Container:
1. **Right-click HealthCanvas → Create Empty**
2. **Name it "PlayerHealthUI"**
3. **Set RectTransform (in Inspector):**
   - Anchor Preset: **Top Left** (click the anchor icon, hold Alt+Shift, click top-left)
   - Pos X: **20**
   - Pos Y: **-20**
   - Width: **200**
   - Height: **60**

4. **Add Component → Scripts → Health UI**
5. **Configure Inspector:**
   - Target UFO: **Drag UFO_Player here**
   - Full Heart Sprite: *(see below for creating sprites)*
   - Empty Heart Sprite: *(see below for creating sprites)*
   - Heart Size: **50**
   - Heart Spacing: **10**

### Create Heart Sprites (Temporary - Use Colored Squares):

**For now, let's use Unity's default UI sprites:**

1. **In HealthUI Inspector:**
   - Full Heart Sprite: Leave empty (will show as white square)
   - Empty Heart Sprite: Leave empty

2. **After adding the component, the hearts will auto-generate as children of PlayerHealthUI**

3. **To make them visible:**
   - Select each Heart_0, Heart_1, Heart_2 child
   - In Image component, set Color:
     - Full hearts: **Green** (RGB: 0, 255, 0)
     - Empty hearts: **Red** (RGB: 255, 0, 0)

**Later, you can replace with actual heart sprites if you want prettier visuals.**

## Step 3: Test Damage System

1. **Play the scene**
2. **Shoot UFO_Dummy with weapons**
3. **Watch the console logs:**
   - `[UFO HEALTH] UFO_Dummy took 1 damage. Health: 2/3`
   - After 3 hits: `[UFO HEALTH] UFO_Dummy has been destroyed!`

4. **Observe death behavior:**
   - UFO stops responding to controls
   - Gravity turns on
   - UFO falls and bounces
   - UFO spins and tumbles
   - After 10 seconds, wreck disappears

## Step 4: Verify All Weapons Deal Damage

**Test each weapon:**
- **Basic Projectile (S key):** 1 damage per hit
- **Homing Missile (W key):** 1 damage per hit
- **Burst Cannon (R key):** 1 damage per bullet (13 bullets = potential 13 damage if all hit)
- **Laser (Fire3/Button 2):** 1 damage per beam activation
- **Sticky Bomb (B key):** 1 damage on contact + 1 damage if in explosion radius = 2 damage total

## Step 5: Adjust Damage Values (Optional)

If you want different damage values:

1. **Open the weapon scripts in Inspector when selecting a weapon's GameObject**
2. **Adjust the "Damage" field:**
   - Projectile.cs: damage (default 1)
   - HomingProjectile.cs: damage (default 1)
   - StickyBomb.cs: contactDamage and explosionDamage (both default 1)
   - LaserWeapon.cs: damage (default 1)

## Step 6: Add Explosion Effect on Death (Optional)

1. **Use the existing ExplosionEffect prefab:**
   - Find `Assets/Prefabs/ExplosionEffect.prefab`
   - Drag it into UFOHealth's "Death Explosion Prefab" slot

2. **Or leave empty for now**

## Troubleshooting

### Health UI doesn't show:
- Make sure HealthUI component has Target UFO assigned
- Check that Canvas is set to Screen Space - Overlay
- Verify PlayerHealthUI is positioned correctly (top-left corner)

### Weapons don't deal damage:
- Ensure UFO_Player and UFO_Dummy both have UFOHealth component
- Check that UFOs have tag "Player" (Inspector → Tag dropdown)
- Look for damage logs in Console window

### UFO doesn't fall when dead:
- Verify UFOHealth component is on the UFO with the Rigidbody
- Check that UFOController and UFOCollision components exist
- Ensure Rigidbody is not kinematic

### Hearts don't change color:
- The HealthUI script updates sprites automatically
- If using colored squares, manually set colors on Heart_0, Heart_1, Heart_2
- Check that Target UFO is correctly assigned

## Next Steps

Once this is working, you can:
- Add respawn system (call `ufoHealth.ResetHealth()` to restore HP)
- Create proper heart sprites (red heart outline for empty, filled for full)
- Add health pickups (call `ufoHealth.Heal(1)`)
- Implement round system with score tracking
- Add particle effects on death (fire, smoke, debris)
