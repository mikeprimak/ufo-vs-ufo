# Mario Kart-Style Collision Setup

This guide shows you how to add arcade-style bouncy collisions to your UFO.

## Part 1: Add Collision Script

1. **Select UFO_Player** in Hierarchy
2. **Inspector → Add Component**
3. Search for **"UFO Collision"** and add it
4. **Configure settings:**
   - **Bounce Force**: 10 (how hard you bounce back)
   - **Stun Duration**: 0.3 (seconds of immobility - 300ms)
   - **Min Impact Speed**: 3 (minimum speed to trigger bounce)

5. **(Optional) Add visual flash:**
   - Find **UFO_Body** under UFO_Visual in Hierarchy
   - Drag it into the **"Ufo Renderer"** field
   - Set **Flash Color** to red (or any color)

## Part 2: Create Bouncy Physics Material

To make collisions feel bouncy like Mario Kart:

1. **In Project window**, go to **Assets/Materials**
2. **Right-click → Create → Physics Material**
3. **Name it "UFO_Bouncy"**
4. **Select it and set in Inspector:**
   - **Dynamic Friction**: 0
   - **Static Friction**: 0
   - **Bounciness**: 0.5
   - **Friction Combine**: Minimum
   - **Bounce Combine**: Maximum

5. **Select UFO_Player** in Hierarchy
6. **Find the Sphere Collider** (or whatever collider it has)
7. **Drag "UFO_Bouncy"** material into the **"Material"** field on the collider

## Part 3: Make Walls Bouncy Too

For best results, walls should also bounce:

1. **Select a wall** (Wall_North, etc.)
2. **Find its Box Collider** component
3. **Drag "UFO_Bouncy"** into the **Material** field
4. **Repeat for all walls**

## How It Works

When you hit a wall:
1. **Bounce** - UFO reflects off the wall
2. **Flash** - UFO briefly turns red (if renderer assigned)
3. **Stun** - Can't control UFO for 300ms
4. **Auto-recover** - Full control returns automatically

## Adjusting the Feel

**Too much bounce?**
- Reduce **Bounce Force** to 5-7
- Lower **Bounciness** in physics material to 0.3

**Too long stun?**
- Reduce **Stun Duration** to 0.2 or 0.15

**Bouncing on small bumps?**
- Increase **Min Impact Speed** to 5

**Want more impact?**
- Increase **Stun Duration** to 0.5
- Add camera shake (future feature)
- Add impact sound effect

## Troubleshooting

**UFO doesn't bounce:**
- Make sure UFO_Bouncy material is on UFO's collider
- Check Bounce Force is not 0
- Ensure you're hitting walls fast enough (try Min Impact Speed = 1)

**UFO sticks to walls:**
- Check friction is 0 in physics material
- Make sure Friction Combine is "Minimum"

**Stun lasts forever:**
- Check Console for errors
- Make sure UFOController component exists

**No visual flash:**
- Assign UFO_Body's renderer to Ufo Renderer field
- Make sure Flash Color is different from UFO color

## Performance Note

This system is very lightweight - perfect for low-end PCs!
