using UnityEngine;

/// <summary>
/// AI controller for UFO enemies
/// Provides virtual inputs to UFOController for autonomous behavior
/// States: Patrol, SeekWeapon, Chase, Attack
/// </summary>
public class UFOAIController : MonoBehaviour
{
    [Header("AI Behavior")]
    [Tooltip("How aggressively AI pursues targets (0-1)")]
    public float aggression = 0.7f;

    [Tooltip("How often AI makes decisions (seconds) - lower = more responsive")]
    public float decisionInterval = 0.3f;

    [Tooltip("Turn input smoothing speed (higher = smoother but slower to respond)")]
    public float turnSmoothSpeed = 5f;

    [Tooltip("Detection range for enemies")]
    public float detectionRange = 100f;

    [Tooltip("Attack range for firing weapons")]
    public float attackRange = 60f;

    [Tooltip("Minimum distance to maintain from walls")]
    public float wallAvoidanceDistance = 15f;

    [Header("Movement Settings")]
    [Tooltip("How close to get to target position before picking new target")]
    public float arrivalDistance = 5f;

    [Tooltip("Chance to barrel roll when evading (0-1)")]
    public float barrelRollChance = 0.3f;

    [Tooltip("Random patrol radius")]
    public float patrolRadius = 40f;

    // AI States
    private enum AIState
    {
        Patrol,      // Wander around
        SeekWeapon,  // Go to nearest weapon pickup
        Chase,       // Pursue enemy
        Attack       // Fire at enemy
    }

    private AIState currentState = AIState.Patrol;
    private Vector3 patrolTarget;
    private GameObject currentTarget; // Current enemy target
    private GameObject lastTarget; // Last enemy we fought (cooldown before re-targeting)
    private float lastTargetChangeTime; // Time when we last changed targets
    private GameObject targetWeaponPickup; // Current weapon pickup target
    private float nextDecisionTime;
    private float nextBarrelRollTime;
    private float targetCooldown = 3f; // Seconds before we can re-target same enemy

    // Components
    private UFOController ufoController;
    private WeaponManager weaponManager;
    private UFOHealth ufoHealth;

    // Virtual inputs (fed to UFOController)
    private float virtualAccelerate;
    private float virtualBrake;
    private float virtualTurn;
    private float virtualVertical;
    private bool virtualBarrelRollLeft;
    private bool virtualBarrelRollRight;

    // Smoothed inputs for less twitchy movement
    private float smoothedTurn;
    private float smoothedVertical;
    private float smoothedAccelerate;

    void Start()
    {
        // Get components
        ufoController = GetComponent<UFOController>();
        weaponManager = GetComponent<WeaponManager>();
        ufoHealth = GetComponent<UFOHealth>();

        // Set initial patrol target
        patrolTarget = GetRandomPatrolPoint();
        nextDecisionTime = Time.time + Random.Range(0f, decisionInterval);
    }

    void OnDestroy()
    {
        // Release any claimed pickup when AI is destroyed
        ReleaseCurrentPickupClaim();
    }

    void Update()
    {
        // Don't do anything if dead
        if (ufoHealth != null && ufoHealth.IsDead())
            return;

        // Make decisions periodically
        if (Time.time >= nextDecisionTime)
        {
            MakeDecision();
            nextDecisionTime = Time.time + decisionInterval;
        }

        // Execute current behavior
        ExecuteState();

        // Apply virtual inputs to UFOController
        ApplyVirtualInputs();
    }

    void MakeDecision()
    {
        // Check if we have a weapon
        bool hasWeapon = weaponManager != null && weaponManager.HasWeapon();

        if (!hasWeapon)
        {
            // Priority: Get a weapon - immediately clear current target
            currentTarget = null;

            // Find and claim a weapon pickup
            GameObject newPickupTarget = FindNearestWeaponPickup();

            // If we're switching to a different pickup, release old claim
            if (newPickupTarget != targetWeaponPickup)
            {
                ReleaseCurrentPickupClaim();
                targetWeaponPickup = newPickupTarget;

                // Try to claim the new pickup
                if (targetWeaponPickup != null)
                {
                    WeaponPickup pickup = targetWeaponPickup.GetComponent<WeaponPickup>();
                    if (pickup != null)
                    {
                        pickup.TryClaim(gameObject);
                    }
                }
            }

            if (targetWeaponPickup != null)
            {
                currentState = AIState.SeekWeapon;
            }
            else
            {
                currentState = AIState.Patrol;
            }
        }
        else
        {
            // We have a weapon - release any pickup claim
            ReleaseCurrentPickupClaim();

            // Look for enemies
            currentTarget = FindNearestEnemy();

            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

                if (distanceToTarget <= attackRange)
                {
                    currentState = AIState.Attack;
                }
                else
                {
                    currentState = AIState.Chase;
                }
            }
            else
            {
                // No enemies found, patrol
                currentState = AIState.Patrol;
            }
        }

        // Random barrel roll for evasion
        if (Time.time >= nextBarrelRollTime && Random.value < barrelRollChance * aggression)
        {
            nextBarrelRollTime = Time.time + Random.Range(2f, 5f);
        }
    }

    void ExecuteState()
    {
        // Reset virtual inputs
        virtualAccelerate = 0f;
        virtualBrake = 0f;
        virtualTurn = 0f;
        virtualVertical = 0f;
        virtualBarrelRollLeft = false;
        virtualBarrelRollRight = false;

        switch (currentState)
        {
            case AIState.Patrol:
                ExecutePatrol();
                break;

            case AIState.SeekWeapon:
                ExecuteSeekWeapon();
                break;

            case AIState.Chase:
                ExecuteChase();
                break;

            case AIState.Attack:
                ExecuteAttack();
                break;
        }

        // Wall avoidance (always active)
        ApplyWallAvoidance();
    }

    void ExecutePatrol()
    {
        // Check if reached patrol target
        float distanceToPatrol = Vector3.Distance(transform.position, patrolTarget);
        if (distanceToPatrol < arrivalDistance)
        {
            // Pick new patrol point
            patrolTarget = GetRandomPatrolPoint();
        }

        // Move toward patrol target at FULL SPEED
        MoveTowardTarget(patrolTarget, 1f);
    }

    void ExecuteSeekWeapon()
    {
        if (targetWeaponPickup == null)
        {
            currentState = AIState.Patrol;
            return;
        }

        // Check if target pickup is still available
        WeaponPickup pickup = targetWeaponPickup.GetComponent<WeaponPickup>();
        if (pickup == null || !pickup.IsAvailable())
        {
            // Pickup was collected or is respawning - release claim and find another
            ReleaseCurrentPickupClaim();
            currentState = AIState.Patrol;
            return;
        }

        // Calculate direction and distance to pickup
        Vector3 directionToPickup = (targetWeaponPickup.transform.position - transform.position).normalized;
        float distanceToPickup = Vector3.Distance(transform.position, targetWeaponPickup.transform.position);
        float angleToPickup = Vector3.SignedAngle(transform.forward, directionToPickup, Vector3.up);

        // FAR AWAY (> 15 units): Fly directly at it full speed
        if (distanceToPickup > 15f)
        {
            MoveTowardTarget(targetWeaponPickup.transform.position, 1f);
        }
        // CLOSE BUT MISALIGNED (pickup not in front): Stop, reorient, then charge
        else if (Mathf.Abs(angleToPickup) > 25f)
        {
            // STOP ACCELERATING - let momentum carry us
            virtualAccelerate = 0f;
            virtualBrake = 0.4f; // Light brake to slow down

            // PURE TURNING - turn to face pickup (with dead zone)
            if (Mathf.Abs(angleToPickup) > 10f)
            {
                virtualTurn = Mathf.Clamp(angleToPickup / 25f, -1f, 1f);
            }

            // PURE VERTICAL - match height (with dead zone)
            float heightDifference = targetWeaponPickup.transform.position.y - transform.position.y;
            if (Mathf.Abs(heightDifference) > 3f)
            {
                virtualVertical = Mathf.Clamp(heightDifference / 8f, -1f, 1f);
            }
        }
        // CLOSE AND ALIGNED (< 25 degrees): CHARGE AT IT
        else
        {
            // Pickup is directly in front - accelerate straight into it
            virtualAccelerate = 1f;
            virtualBrake = 0f;

            // Fine tune aim (with dead zone)
            if (Mathf.Abs(angleToPickup) > 5f)
            {
                virtualTurn = Mathf.Clamp(angleToPickup / 20f, -1f, 1f);
            }
            else
            {
                virtualTurn = 0f; // Dead center - stop adjusting
            }

            // Match height (with dead zone)
            float heightDifference = targetWeaponPickup.transform.position.y - transform.position.y;
            if (Mathf.Abs(heightDifference) > 2f)
            {
                virtualVertical = Mathf.Clamp(heightDifference / 8f, -1f, 1f);
            }
            else
            {
                virtualVertical = 0f; // Close enough
            }
        }
    }

    void ExecuteChase()
    {
        if (currentTarget == null)
        {
            currentState = AIState.Patrol;
            return;
        }

        // Move toward enemy
        MoveTowardTarget(currentTarget.transform.position, 1f);
    }

    void ExecuteAttack()
    {
        if (currentTarget == null)
        {
            currentState = AIState.Patrol;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        // If far away, chase directly
        if (distanceToTarget > attackRange * 0.7f)
        {
            MoveTowardTarget(currentTarget.transform.position, 1f);
        }
        // If in range, make strafing runs - fly PAST the target while shooting
        else
        {
            // Aim at target while flying
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

            // Only aim if within 45 degrees - otherwise keep flying straight
            if (Mathf.Abs(angleToTarget) < 45f)
            {
                virtualTurn = Mathf.Clamp(angleToTarget / 25f, -1f, 1f);
            }
            else
            {
                // Flying past - minimal turning, just keep going
                virtualTurn = Mathf.Clamp(angleToTarget / 60f, -1f, 1f);
            }

            // ALWAYS full speed forward - fly-by attack
            virtualAccelerate = 1f;

            // Match height
            float heightDifference = currentTarget.transform.position.y - transform.position.y;
            virtualVertical = Mathf.Clamp(heightDifference / 10f, -1f, 1f);
        }

        // Try to fire weapon
        TryFireWeapon();

        // Immediately check if we still have a weapon after firing
        if (weaponManager != null && !weaponManager.HasWeapon())
        {
            // Weapon depleted - immediately seek new weapon
            currentTarget = null;
            currentState = AIState.SeekWeapon;
        }
    }

    void MoveTowardTarget(Vector3 targetPosition, float speedMultiplier)
    {
        // Calculate direction to target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Get angle to target (relative to current forward)
        float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

        // Turn toward target with larger dead zone to reduce wobbling
        if (Mathf.Abs(angleToTarget) > 10f) // Larger dead zone (was 5)
        {
            virtualTurn = Mathf.Clamp(angleToTarget / 35f, -1f, 1f); // Gentler turn
        }
        else
        {
            virtualTurn = 0f; // No turn in dead zone
        }

        // ALWAYS ACCELERATE - arcade racing style
        // Only exception is if we're facing completely wrong way (> 120 degrees)
        if (Mathf.Abs(angleToTarget) > 120f)
        {
            // Sharp U-turn needed - light brake while turning
            virtualBrake = 0.3f;
            virtualAccelerate = 0f;
        }
        else
        {
            // Always go full speed and turn at the same time
            virtualAccelerate = speedMultiplier;
            virtualBrake = 0f;
        }

        // Vertical movement toward target with larger dead zone
        float heightDifference = targetPosition.y - transform.position.y;
        if (Mathf.Abs(heightDifference) > 5f) // Larger dead zone (was 3)
        {
            virtualVertical = Mathf.Clamp(heightDifference / 15f, -1f, 1f); // Gentler (was /10)
        }
        else
        {
            virtualVertical = 0f; // No vertical in dead zone
        }
    }

    void AimAtTarget(Vector3 targetPosition)
    {
        // Calculate direction to target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Get angle to target
        float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

        // Turn to face target with dead zone
        if (Mathf.Abs(angleToTarget) > 8f) // Dead zone for aiming
        {
            virtualTurn = Mathf.Clamp(angleToTarget / 30f, -1f, 1f); // Gentler aiming
        }
        else
        {
            virtualTurn = 0f; // Good enough - stop micro-adjusting
        }

        // Match height with dead zone
        float heightDifference = targetPosition.y - transform.position.y;
        if (Mathf.Abs(heightDifference) > 4f)
        {
            virtualVertical = Mathf.Clamp(heightDifference / 12f, -1f, 1f);
        }
        else
        {
            virtualVertical = 0f; // Close enough
        }

        // Don't set acceleration here - let the calling function handle it
    }

    void ApplyWallAvoidance()
    {
        // Cast rays forward and to sides to detect walls
        float rayDistance = wallAvoidanceDistance;

        // Forward ray - check multiple distances for graduated response
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance))
        {
            float distanceToWall = hit.distance;

            // Very close to wall (< 5 units) - emergency brake and turn
            if (distanceToWall < 5f)
            {
                virtualTurn += Random.value < 0.5f ? 1f : -1f;
                virtualBrake = 0.8f;
                virtualAccelerate = 0f;
            }
            // Medium distance (5-10 units) - turn hard but keep some speed
            else if (distanceToWall < 10f)
            {
                virtualTurn += Random.value < 0.5f ? 1f : -1f;
                virtualBrake = 0.3f;
            }
            // Far distance (10-15 units) - just turn, maintain speed
            else
            {
                virtualTurn += Random.value < 0.5f ? 0.8f : -0.8f;
                // Don't brake - just turn while flying
            }
        }

        // Left ray - gentler avoidance
        if (Physics.Raycast(transform.position, -transform.right, rayDistance * 0.5f))
        {
            // Wall on left - turn right
            virtualTurn += 0.4f;
        }

        // Right ray - gentler avoidance
        if (Physics.Raycast(transform.position, transform.right, rayDistance * 0.5f))
        {
            // Wall on right - turn left
            virtualTurn -= 0.4f;
        }

        // Clamp turn input
        virtualTurn = Mathf.Clamp(virtualTurn, -1f, 1f);
    }

    void TryFireWeapon()
    {
        if (weaponManager == null)
            return;

        // Check if facing target
        if (currentTarget != null)
        {
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            // Fire if roughly facing target (within 20 degrees)
            if (angleToTarget < 20f)
            {
                // Simulate fire button press (AI fires every frame when in attack state)
                // The weapon's fire rate will handle cooldown
                if (weaponManager.enabled)
                {
                    // Trigger fire via WeaponManager's Update detection
                    // We need to simulate the input - will handle this in ApplyVirtualInputs
                }
            }
        }
    }

    void ApplyVirtualInputs()
    {
        // Feed virtual inputs to UFOController's AI input system
        if (ufoController == null)
            return;

        // Smooth turn and vertical inputs to reduce wobbling
        smoothedTurn = Mathf.Lerp(smoothedTurn, virtualTurn, Time.deltaTime * turnSmoothSpeed);
        smoothedVertical = Mathf.Lerp(smoothedVertical, virtualVertical, Time.deltaTime * turnSmoothSpeed);
        smoothedAccelerate = Mathf.Lerp(smoothedAccelerate, virtualAccelerate, Time.deltaTime * turnSmoothSpeed * 1.5f);

        // Write smoothed inputs to UFOController's AI input fields
        ufoController.aiAccelerate = smoothedAccelerate;
        ufoController.aiBrake = virtualBrake; // Don't smooth brake - keep responsive
        ufoController.aiTurn = smoothedTurn;
        ufoController.aiVertical = smoothedVertical;
        ufoController.aiBarrelRollLeft = virtualBarrelRollLeft;
        ufoController.aiBarrelRollRight = virtualBarrelRollRight;
        ufoController.aiFire = false; // Weapon firing handled by WeaponManager

        // Trigger weapon firing separately via WeaponManager
        if (currentState == AIState.Attack && weaponManager != null)
        {
            // The AI will continuously try to fire when in attack state
            // WeaponManager will handle cooldowns and ammo
            weaponManager.TryFireWeaponAI();
        }
    }

    GameObject FindNearestEnemy()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        GameObject nearest = null;
        float nearestDistance = Mathf.Infinity;
        bool isInCooldown = (Time.time - lastTargetChangeTime) < targetCooldown;

        foreach (GameObject player in allPlayers)
        {
            // Don't target self
            if (player == gameObject)
                continue;

            // Don't target dead UFOs
            UFOHealth health = player.GetComponent<UFOHealth>();
            if (health != null && health.IsDead())
                continue;

            // Skip last target if still in cooldown (prefer different enemies)
            if (isInCooldown && player == lastTarget)
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < detectionRange && distance < nearestDistance)
            {
                nearest = player;
                nearestDistance = distance;
            }
        }

        // If we found no target but we're in cooldown, try again without cooldown
        if (nearest == null && isInCooldown && lastTarget != null)
        {
            // Check if last target is still valid
            UFOHealth health = lastTarget.GetComponent<UFOHealth>();
            if (health != null && !health.IsDead())
            {
                float distance = Vector3.Distance(transform.position, lastTarget.transform.position);
                if (distance < detectionRange)
                {
                    nearest = lastTarget;
                }
            }
        }

        // Track target changes
        if (nearest != currentTarget && nearest != null)
        {
            lastTarget = currentTarget;
            lastTargetChangeTime = Time.time;
        }

        return nearest;
    }

    GameObject FindNearestWeaponPickup()
    {
        WeaponPickup[] allPickups = FindObjectsOfType<WeaponPickup>();
        GameObject nearest = null;
        GameObject secondNearest = null;
        float nearestDistance = Mathf.Infinity;
        float secondNearestDistance = Mathf.Infinity;

        foreach (WeaponPickup pickup in allPickups)
        {
            // Only consider pickups that are currently available (not respawning)
            if (!pickup.IsAvailable())
                continue;

            // Skip pickups claimed by other AI
            if (pickup.IsClaimedByOther(gameObject))
                continue;

            float distance = Vector3.Distance(transform.position, pickup.transform.position);

            if (distance < nearestDistance)
            {
                // Shift previous nearest to second nearest
                secondNearest = nearest;
                secondNearestDistance = nearestDistance;

                nearest = pickup.gameObject;
                nearestDistance = distance;
            }
            else if (distance < secondNearestDistance)
            {
                secondNearest = pickup.gameObject;
                secondNearestDistance = distance;
            }
        }

        // 30% chance to go for second nearest instead of nearest (reduces clustering)
        if (secondNearest != null && Random.value < 0.3f)
        {
            return secondNearest;
        }

        return nearest;
    }

    /// <summary>
    /// Release claim on current target pickup
    /// </summary>
    void ReleaseCurrentPickupClaim()
    {
        if (targetWeaponPickup != null)
        {
            WeaponPickup pickup = targetWeaponPickup.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                pickup.ReleaseClaimBy(gameObject);
            }
            targetWeaponPickup = null;
        }
    }

    Vector3 GetRandomPatrolPoint()
    {
        // Get a random point within patrol radius around the arena center
        Vector3 randomPoint = new Vector3(
            Random.Range(-patrolRadius, patrolRadius),
            Random.Range(5f, 15f), // Keep reasonable height
            Random.Range(-patrolRadius, patrolRadius)
        );

        return randomPoint;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // Draw state indicator
        Gizmos.color = GetStateColor();
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 5f, 2f);

        // Draw target line
        if (currentState == AIState.Chase || currentState == AIState.Attack)
        {
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
        else if (currentState == AIState.SeekWeapon)
        {
            if (targetWeaponPickup != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetWeaponPickup.transform.position);
            }
        }
        else if (currentState == AIState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, patrolTarget);
            Gizmos.DrawWireSphere(patrolTarget, 3f);
        }
    }

    Color GetStateColor()
    {
        switch (currentState)
        {
            case AIState.Patrol: return Color.green;
            case AIState.SeekWeapon: return Color.cyan;
            case AIState.Chase: return Color.yellow;
            case AIState.Attack: return Color.red;
            default: return Color.white;
        }
    }
}
