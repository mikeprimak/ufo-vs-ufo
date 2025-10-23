# Game Controller Setup Guide

## Quick Test First!

Before setup, let's test if your controller works out-of-the-box:

1. **Plug in your Logitech controller**
2. **Reimport UFOController.cs** (right-click → Reimport)
3. **Enter Play mode**
4. **Try these controller inputs:**
   - **Left Stick** → Should move/turn UFO (replaces arrow keys)
   - **Button 0** (usually bottom face button) → Accelerate
   - **Button 1** (usually right face button) → Brake

If those work, you're done! If not, continue to the setup below.

---

## Unity Input Manager Setup

### Step 1: Open Input Manager

1. Go to **Edit → Project Settings**
2. Click **Input Manager** in the left panel
3. Expand **Axes** at the top

### Step 2: Add Trigger Axis (for RT/LT)

Unity's default setup might not have triggers configured. Let's add them:

1. **Change the "Size" number** at the top (it's probably 18)
2. **Increase it by 1** (e.g., 18 → 19)
3. **Scroll to the bottom** - you'll see a new axis slot

4. **Expand the new axis** and configure it:
   - **Name**: `Triggers`
   - **Gravity**: 3
   - **Dead**: 0.001
   - **Sensitivity**: 3
   - **Type**: Joystick Axis
   - **Axis**: 3rd axis (Joysticks)
   - **Joy Num**: Get Motion from all Joysticks

### Step 3: Verify Existing Axes

Make sure these are configured (they usually are by default):

**Horizontal:**
- Should have TWO entries:
  - One for keyboard (A/D keys)
  - One for joystick (X axis - 1st axis)

**Vertical:**
- Should have TWO entries:
  - One for keyboard (W/S keys)
  - One for joystick (Y axis - 2nd axis)

---

## Controller Button Mapping

Your Logitech controller buttons should map like this:

| Unity Button | Typical Logitech Button |
|--------------|-------------------------|
| Fire1 (Button 0) | Button 1 (bottom) |
| Fire2 (Button 1) | Button 2 (right) |
| Fire3 (Button 2) | Button 3 (left) |
| Jump (Button 3) | Button 4 (top) |

---

## Controls Summary

### Keyboard:
- **A** - Accelerate
- **D** - Brake/Reverse
- **Arrow Keys** - Turn left/right, Ascend/Descend

### Controller:
- **Left Stick** - Turn (left/right), Altitude (up/down)
- **Right Trigger (RT)** - Accelerate
- **Left Trigger (LT)** - Brake/Reverse
- **Face Button 0** - Accelerate (backup)
- **Face Button 1** - Brake (backup)

---

## Troubleshooting

### Controller not detected at all:
1. Check Windows recognizes it (Control Panel → Devices and Printers)
2. Try a different USB port
3. Restart Unity with controller plugged in

### Triggers don't work:
- Logitech controllers vary - try changing **Axis** in the Triggers setup:
  - Try `3rd axis`
  - Try `9th axis`
  - Try `10th axis`

### Left stick doesn't work:
1. In Input Manager, check **Horizontal** and **Vertical** axes
2. Make sure one entry has:
   - Type: Joystick Axis
   - Axis: X axis (1st) for Horizontal
   - Axis: Y axis (2nd) for Vertical

### Buttons are wrong:
- Different controllers number buttons differently
- Test in Play mode and watch the Console for errors
- You can remap by changing which button numbers are used in the code

---

## Finding Your Controller's Axis Numbers

If triggers/sticks don't work, we need to find which axis your controller uses:

1. I can create a debug script that prints all axis values
2. You move the sticks/triggers and see which numbers change
3. Then we update the Input Manager

**Want me to create this debug script?** Just ask!

---

## Notes

- Both keyboard and controller work simultaneously
- You can mix inputs (keyboard + controller at the same time)
- The script automatically detects and combines all inputs
