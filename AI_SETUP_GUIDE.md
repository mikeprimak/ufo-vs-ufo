# AI Enemy Setup Guide

## Quick Setup: Add AI UFO to Your Scene

### 1. Duplicate the Player UFO
1. In Unity Hierarchy, find `UFO_Player`
2. Right-click → Duplicate (Ctrl+D)
3. Rename to `UFO_AI_1`, `UFO_AI_2`, etc.
4. Move the AI UFO to a different spawn position in the arena

### 2. Configure UFOController for AI
1. Select the AI UFO in Hierarchy
2. In Inspector, find **UFOController** component
3. Check the box: **Use AI Input** ✓

### 3. Add AI Controller Component
1. With AI UFO selected, click **Add Component**
2. Search for `UFOAIController`
3. Add it to the UFO

### 4. Configure WeaponManager for AI
1. Find **WeaponManager** component on the AI UFO
2. Check the box: **Allow AI Control** ✓

### 5. Remove Player Camera Reference (Optional)
1. In **UFOController**, find **Aim Camera** field
2. Clear the camera reference (AI doesn't need it)

### 6. Test Your AI
- Press Play
- The AI should:
  - Patrol the arena when it has no weapon
  - Seek weapon pickups
  - Chase and attack you when armed
  - Avoid walls automatically

---

## AI Behavior Settings

### UFOAIController Inspector Settings

**AI Behavior:**
- **Aggression** (0-1): How aggressively AI pursues targets
  - 0.5 = Cautious
  - 0.7 = Balanced (default)
  - 1.0 = Very aggressive

- **Decision Interval** (seconds): How often AI makes decisions
  - 0.2s = Default (responsive)
  - Lower = More reactive but more CPU
  - Higher = Slower reactions but better performance

- **Detection Range**: How far AI can see enemies (default: 100 units)

- **Attack Range**: Distance at which AI starts firing (default: 60 units)

- **Wall Avoidance Distance**: Minimum distance from walls (default: 15 units)

**Movement Settings:**
- **Arrival Distance**: How close to get to targets (default: 10 units)
- **Barrel Roll Chance**: Probability of evasive rolls (default: 0.3 = 30%)
- **Patrol Radius**: How far AI wanders when idle (default: 30 units)

---

## AI States Explained

### 1. Patrol (Green)
- **When**: No weapon, no nearby pickups
- **Behavior**: Wanders randomly, explores arena
- **Visual**: Green wireframe sphere above UFO

### 2. Seek Weapon (Cyan)
- **When**: No weapon, weapon pickup detected
- **Behavior**: Flies directly to nearest weapon pickup
- **Visual**: Cyan line to target pickup

### 3. Chase (Yellow)
- **When**: Has weapon, enemy detected but out of range
- **Behavior**: Pursues enemy, closes distance
- **Visual**: Yellow line to target enemy

### 4. Attack (Red)
- **When**: Has weapon, enemy in range
- **Behavior**: Aims and fires, strafes around target
- **Visual**: Red line to target enemy

---

## Recommended Setup for 1v3 Gameplay

### Player UFO
- Tag: "Player"
- **UFOController**: Use AI Input = ❌ (unchecked)
- **UFOCamera**: Attached to Main Camera
- **WeaponManager**: Allow AI Control = ❌ (unchecked)

### AI UFO #1, #2, #3
- Tag: "Player" (so they can damage each other)
- **UFOController**: Use AI Input = ✓ (checked)
- **UFOAIController**: Aggression = 0.7
- **WeaponManager**: Allow AI Control = ✓ (checked)
- Remove Main Camera reference from UFOController

### Spawn Positions
Spread AI UFOs around the arena:
- Player: (0, 5, 0)
- AI #1: (-20, 5, -20)
- AI #2: (20, 5, -20)
- AI #3: (0, 5, 20)

---

## Troubleshooting

### AI doesn't move
- ✓ Check **Use AI Input** is enabled on UFOController
- ✓ Verify UFOAIController component is attached
- ✓ Check Rigidbody is not kinematic

### AI doesn't fire weapons
- ✓ Check **Allow AI Control** on WeaponManager
- ✓ Verify weapon pickups exist in scene
- ✓ Check AI has line of sight to targets

### AI crashes into walls
- ✓ Ensure walls have colliders
- ✓ Increase **Wall Avoidance Distance**
- ✓ Check Raycast layers aren't blocking detection

### AI ignores player
- ✓ Verify player has tag "Player"
- ✓ Check **Detection Range** is large enough
- ✓ Verify UFOHealth component exists on player

### Performance issues with multiple AI
- ✓ Increase **Decision Interval** (0.3-0.5s)
- ✓ Reduce **Detection Range**
- ✓ Limit to 3-4 AI UFOs max

---

## Advanced Tuning

### Making AI Easier
- Lower **Aggression** to 0.3-0.5
- Increase **Attack Range** (AI starts firing from further away, less accurate)
- Decrease **Detection Range** (AI spots player later)
- Increase **Decision Interval** to 0.5s (slower reactions)

### Making AI Harder
- Increase **Aggression** to 0.9-1.0
- Decrease **Attack Range** to 40 (AI gets closer before firing)
- Increase **Detection Range** to 150
- Decrease **Decision Interval** to 0.1s (faster reactions)
- Lower **Barrel Roll Chance** to 0.1 (AI dodges less, attacks more)

### Team vs Team Setup
You can create teams by modifying the AI target selection:
- Option 1: Use Unity Tags ("Team_Red", "Team_Blue")
- Option 2: Modify `FindNearestEnemy()` in UFOAIController.cs
- Option 3: Create team-based GameManager

---

## Debug Visualization

When scene is playing, AI UFOs show debug gizmos:
- **Colored sphere above UFO**: Current AI state
  - Green = Patrol
  - Cyan = Seeking weapon
  - Yellow = Chasing
  - Red = Attacking
- **Lines**: Show AI's current target (enemy or weapon pickup)

Enable Gizmos in Scene view to see AI behavior in real-time!
