using UnityEngine;

/// <summary>
/// Arcade-style UFO flight controller inspired by N64 Mario Kart Battle Mode
/// Features: tight turns, instant brake, high acceleration, simplified physics
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class UFOController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum speed the UFO can reach")]
    public float maxSpeed = 30f;

    [Tooltip("How quickly the UFO accelerates")]
    public float acceleration = 60f;

    [Tooltip("How quickly the UFO decelerates when not accelerating")]
    public float deceleration = 20f;

    [Tooltip("How quickly the UFO stops when braking")]
    public float brakeForce = 80f;

    [Header("Rotation Settings")]
    [Tooltip("How fast the UFO turns left/right")]
    public float turnSpeed = 180f;

    [Tooltip("How quickly turn speed ramps up when you start turning (higher = faster ramp)")]
    public float turnAcceleration = 3f;

    [Tooltip("How fast the UFO rotates up/down")]
    public float pitchSpeed = 100f;

    [Tooltip("How quickly vertical input ramps up when moving forward for precise aiming (higher = faster ramp)")]
    public float verticalAcceleration = 1.5f;

    [Tooltip("Minimum forward speed to trigger vertical ramping (below this = instant vertical control)")]
    public float minForwardSpeedForRamping = 0.1f;

    [Header("Vertical Movement")]
    [Tooltip("Speed of ascending/descending")]
    public float verticalSpeed = 8f;

    [Tooltip("Vertical speed multiplier when moving ONLY up/down (no horizontal movement)")]
    public float pureVerticalSpeedMultiplier = 3f;

    [Tooltip("Horizontal speed threshold below which pure vertical boost applies")]
    public float pureVerticalThreshold = 10f;

    [Tooltip("Maximum height above ground")]
    public float maxHeight = 20f;

    [Tooltip("Minimum height above ground")]
    public float minHeight = 0.5f;

    [Header("Reverse Settings")]
    [Tooltip("Maximum reverse speed")]
    public float maxReverseSpeed = 20f;

    [Header("Physics")]
    [Tooltip("Natural momentum drag (higher = stops faster, lower = more gliding)")]
    public float dragAmount = 2f;

    [Tooltip("How quickly the UFO levels out when not pitching")]
    public float autoLevelSpeed = 2f;

    [Header("Visual Feedback")]
    [Tooltip("UFO model to tilt (leave empty to tilt whole object)")]
    public Transform visualModel;

    [Tooltip("How much the UFO banks when turning (degrees)")]
    public float bankAmount = 45f;

    [Tooltip("How quickly the UFO banks")]
    public float bankSpeed = 3f;

    [Tooltip("How much the UFO pitches when ascending/descending (degrees)")]
    public float visualPitchAmount = 25f;

    [Tooltip("How quickly the UFO pitches visually")]
    public float visualPitchSpeed = 3f;

    [Tooltip("Minimum forward speed required for pitch tilt")]
    public float minSpeedForPitch = 5f;

    [Header("Barrel Roll Settings")]
    [Tooltip("Lateral dodge distance (in units, ~18 = 3 UFO widths)")]
    public float barrelRollDistance = 18f;

    [Tooltip("How long the barrel roll takes (seconds)")]
    public float barrelRollDuration = 0.5f;

    [Tooltip("Cooldown between barrel rolls (seconds, 0 = no cooldown)")]
    public float barrelRollCooldown = 0f;

    [Tooltip("How early you can buffer the next barrel roll (seconds before current one ends)")]
    public float barrelRollBufferWindow = 0.4f;

    [Header("Boost Settings")]
    [Tooltip("Speed to reach in first second of boost")]
    public float boostFirstSecondSpeed = 60f;

    [Tooltip("Additional speed gained per second after first second")]
    public float boostSpeedGainPerSecond = 20f;

    [Tooltip("Maximum speed achievable during boost")]
    public float maxBoostSpeed = 120f;

    [Tooltip("Time to decelerate from boost speed back to normal speed (seconds)")]
    public float boostDecelerationTime = 4f;

    [Tooltip("Maximum boost time available (seconds)")]
    public float maxBoostTime = 4f;

    [Tooltip("Time to fully recharge boost from empty (seconds)")]
    public float boostRechargeTime = 4f;

    [Header("Movement Control")]
    [Tooltip("If false, UFO cannot move (rotation still allowed)")]
    public bool movementEnabled = true;

    [Header("AI Control (Optional)")]
    [Tooltip("If true, reads from AI input fields instead of Input system")]
    public bool useAIInput = false;

    [Tooltip("AI accelerate input (0-1)")]
    [HideInInspector] public float aiAccelerate = 0f;

    [Tooltip("AI brake input (0-1)")]
    [HideInInspector] public float aiBrake = 0f;

    [Tooltip("AI turn input (-1 to 1)")]
    [HideInInspector] public float aiTurn = 0f;

    [Tooltip("AI vertical input (-1 to 1)")]
    [HideInInspector] public float aiVertical = 0f;

    [Tooltip("AI barrel roll left trigger")]
    [HideInInspector] public bool aiBarrelRollLeft = false;

    [Tooltip("AI barrel roll right trigger")]
    [HideInInspector] public bool aiBarrelRollRight = false;

    [Tooltip("AI fire weapon trigger")]
    [HideInInspector] public bool aiFire = false;

    // Components
    private Rigidbody rb;
    private WeaponSystem weaponSystem;

    [Header("Aiming (Optional)")]
    [Tooltip("Camera for aiming direction (if not assigned, uses velocity-based aiming)")]
    public Camera aimCamera;

    // Input values
    private bool accelerateInput;
    private bool brakeInput;
    private float turnInput;
    private float verticalInput;
    private bool wasAcceleratingLastFrame;
    private bool fireInput;

    // Current speed tracking
    private float currentForwardSpeed;

    // Turn ramping for precise aiming
    private float currentTurnSpeed;
    private float lastTurnDirection;

    // Vertical ramping for precise aiming
    private float currentVerticalSpeed;
    private float lastVerticalDirection;

    // Visual feedback
    private float currentBankAngle;
    private float currentPitchAngle;

    // Floor bounce control
    private bool disableVerticalControl;
    private float verticalControlReenableTime;

    // Barrel roll state
    private bool isBarrelRolling;
    private float barrelRollEndTime;
    private float barrelRollCooldownEndTime;
    private float barrelRollDirection; // 1 = right, -1 = left, 0 = in-place
    private float barrelRollStartTime;
    private bool hasBufferedRoll; // Is there a queued roll?
    private float bufferedRollDirection; // Direction of queued roll
    private bool isInPlaceRoll; // True if barrel roll has no lateral movement
    private bool nextRollIsInPlace; // Flag for next roll to be in-place

    // Boost system
    private float currentBoostTime; // Current boost available (0 to maxBoostTime)
    private bool isBoosting; // Is boost currently active?
    private float boostStartTime; // Time when boost was activated
    private bool isDeceleratingFromBoost; // Is currently decelerating after boost ended?
    private float boostEndTime; // Time when boost ended (for deceleration tracking)
    private float speedWhenBoostEnded; // Speed when boost ended (for smooth deceleration)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        weaponSystem = GetComponent<WeaponSystem>();

        // Initialize boost to full
        currentBoostTime = maxBoostTime;

        // Configure rigidbody for arcade physics
        rb.drag = dragAmount;
        rb.angularDrag = 3f;
        rb.useGravity = false; // UFO hovers, no gravity
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Use Continuous collision detection to prevent phasing through walls at high speed
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Enable interpolation to smooth movement between FixedUpdate calls
        // This prevents jittery visuals when camera follows in LateUpdate
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // Check if stunned (cannot move/shoot when hit)
        UFOHitEffect hitEffect = GetComponent<UFOHitEffect>();
        bool isStunned = (hitEffect != null) && hitEffect.IsStunned();

        // Get input from keyboard and controller (disabled during stun)
        if (!isStunned)
        {
            GetInput();

            // Handle weapon firing
            if (fireInput && weaponSystem != null)
            {
                weaponSystem.TryFire();
            }
        }
        else
        {
            // During stun, zero out all inputs
            accelerateInput = false;
            brakeInput = false;
            turnInput = 0f;
            verticalInput = 0f;
            fireInput = false;
        }

        // Apply visual feedback
        ApplyBankingAndPitch();
    }

    void FixedUpdate()
    {
        // Apply movement and rotation (with stun slowdown if hit)
        UFOHitEffect hitEffect = GetComponent<UFOHitEffect>();
        float stunSpeedMultiplier = (hitEffect != null) ? hitEffect.GetStunSpeedMultiplier() : 1f;

        // Apply stun slowdown to velocity
        if (stunSpeedMultiplier < 1f)
        {
            rb.velocity *= stunSpeedMultiplier;
        }

        // Movement can be disabled (e.g., during countdown), but rotation is always allowed
        if (movementEnabled)
        {
            HandleMovement();
            HandleVerticalMovement();
            EnforceHeightLimits();
            HandleBarrelRoll(); // Barrel roll physics
        }

        HandleRotation(); // Always allow rotation, even during countdown

        // Keep UFO level (prevent tilting from impacts)
        AutoLevel();

        // Track input state for next frame
        wasAcceleratingLastFrame = accelerateInput;
    }

    void GetInput()
    {
        // Declare barrel roll variables once at method scope
        bool wantsLeftRoll = false;
        bool wantsRightRoll = false;

        if (useAIInput)
        {
            // Read from AI inputs
            accelerateInput = aiAccelerate > 0.1f;
            brakeInput = aiBrake > 0.1f;
            turnInput = aiTurn;
            verticalInput = aiVertical;
            fireInput = aiFire;

            // Barrel roll input from AI
            wantsLeftRoll = aiBarrelRollLeft;
            wantsRightRoll = aiBarrelRollRight;

            // Reset AI triggers
            aiBarrelRollLeft = false;
            aiBarrelRollRight = false;
            aiFire = false;
        }
        else
        {
            // Player input (original code)
            // Keyboard controls:
            // A = Accelerate forward
            // D = Brake/Reverse
            accelerateInput = Input.GetKey(KeyCode.A);
            brakeInput = Input.GetKey(KeyCode.D);

            // Controller face buttons:
            // Button 0 (A/Cross) = Accelerate
            // Button 1 (B/Circle) = Weapon Fire (reserved for future)
            // Button 3 (Y/Triangle) = Brake/Reverse
            if (Input.GetButton("Fire1")) // Button 0 (A/Cross) = Accelerate
                accelerateInput = true;
            if (Input.GetKey(KeyCode.JoystickButton3)) // Button 3 (Y/Triangle) = Brake/Reverse
                brakeInput = true;

            // Fire weapon with Button 1 (B/Circle) or Fire3
            fireInput = Input.GetButton("Fire2") || Input.GetKeyDown(KeyCode.JoystickButton1);

            // Arrow keys for turning (Left/Right) + Controller Left Stick X-axis
            turnInput = Input.GetAxis("Horizontal");

            // Arrow keys for vertical movement (Up/Down) + Controller Left Stick Y-axis
            verticalInput = Input.GetAxis("Vertical");

            // Barrel roll input - RB only, direction from stick/dpad
            bool barrelRollPressed = Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.Q);

            if (barrelRollPressed)
            {
                // Determine direction from horizontal input (stick/dpad)
                if (Mathf.Abs(turnInput) > 0.1f)
                {
                    // Directional input detected - barrel roll with lateral movement
                    if (turnInput > 0.1f)
                    {
                        wantsRightRoll = true; // Pushing right
                    }
                    else
                    {
                        wantsLeftRoll = true; // Pushing left
                    }
                }
                else
                {
                    // No directional input - barrel roll in place (no lateral movement)
                    wantsRightRoll = true; // Trigger the roll
                    nextRollIsInPlace = true; // Flag for next roll to be in-place
                }
            }

            // Boost input (DISABLED - manual boost removed)
            // bool boostInput = Input.GetKey(KeyCode.JoystickButton4); // LB only
            HandleBoost(false); // Manual boost disabled
        }

        // Process barrel rolls (shared logic for both AI and player)

        if (wantsLeftRoll || wantsRightRoll)
        {
            // Determine direction (use right=1 for in-place rolls)
            float desiredDirection;
            if (nextRollIsInPlace)
            {
                desiredDirection = 1f; // Default to right spin for in-place
            }
            else
            {
                desiredDirection = wantsLeftRoll ? -1f : 1f;
            }

            // If not currently rolling and off cooldown, start immediately
            if (!isBarrelRolling && Time.time >= barrelRollCooldownEndTime)
            {
                isInPlaceRoll = nextRollIsInPlace; // Transfer flag to active roll
                nextRollIsInPlace = false; // Reset next roll flag
                StartBarrelRoll(desiredDirection);
            }
            // If currently rolling and within buffer window, queue the next roll
            else if (isBarrelRolling && Time.time >= (barrelRollEndTime - barrelRollBufferWindow))
            {
                hasBufferedRoll = true;
                bufferedRollDirection = desiredDirection;
            }
        }
    }

    void HandleMovement()
    {
        // Get current forward speed (positive = forward, negative = backward)
        currentForwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        // Determine effective max speed based on boost state OR deceleration state
        float effectiveMaxSpeed;
        if (isBoosting)
        {
            effectiveMaxSpeed = maxBoostSpeed;
        }
        else if (isDeceleratingFromBoost)
        {
            // During deceleration, don't clamp to normal max speed yet
            effectiveMaxSpeed = maxBoostSpeed;
        }
        else
        {
            effectiveMaxSpeed = maxSpeed;
        }

        if (accelerateInput && brakeInput)
        {
            // Both keys pressed - do nothing (maintain momentum)
            return;
        }
        else if (accelerateInput)
        {
            // A pressed - accelerate forward
            // During deceleration, don't apply normal acceleration (let deceleration system handle it)
            if (!isDeceleratingFromBoost && currentForwardSpeed < effectiveMaxSpeed)
            {
                rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);
            }

            // Clamp to max forward speed (but not during deceleration)
            if (!isDeceleratingFromBoost && currentForwardSpeed > effectiveMaxSpeed)
            {
                Vector3 horizontalVelocity = rb.velocity;
                horizontalVelocity.y = 0;
                rb.velocity = transform.forward * effectiveMaxSpeed + Vector3.up * rb.velocity.y;
            }
        }

        // Apply boost acceleration (works independently of regular acceleration)
        if (isBoosting)
        {
            // Drain boost meter (using FixedDeltaTime to sync with FixedUpdate timing)
            currentBoostTime -= Time.fixedDeltaTime;
            currentBoostTime = Mathf.Max(0f, currentBoostTime);

            // Calculate time since boost started
            float boostDuration = Time.time - boostStartTime;

            // Calculate target speed based on boost duration
            // First second: 0 → 60 units/sec
            // After first second: +10 units/sec per second (60 → 70 → 80 → 90)
            float targetBoostSpeed;
            if (boostDuration <= 1f)
            {
                // First second: immediately target 60 speed (aggressive acceleration)
                targetBoostSpeed = boostFirstSecondSpeed;
            }
            else
            {
                // After first second: add 10 per second
                float additionalTime = boostDuration - 1f;
                targetBoostSpeed = boostFirstSecondSpeed + (boostSpeedGainPerSecond * additionalTime);
            }

            // Clamp to max boost speed
            targetBoostSpeed = Mathf.Min(targetBoostSpeed, maxBoostSpeed);

            // Calculate acceleration needed to reach target speed
            float speedDifference = targetBoostSpeed - currentForwardSpeed;

            // Apply strong force to quickly reach target speed
            if (speedDifference > 0)
            {
                // Aggressive acceleration to reach target (higher multiplier = faster response)
                float accelerationForce = speedDifference * 10f;
                rb.AddForce(transform.forward * accelerationForce, ForceMode.Acceleration);
            }

            // Hard clamp to max boost speed (safety)
            if (currentForwardSpeed > maxBoostSpeed)
            {
                rb.velocity = transform.forward * maxBoostSpeed + Vector3.up * rb.velocity.y;
            }
        }
        // Handle smooth deceleration when boost ends
        else if (isDeceleratingFromBoost)
        {
            float timeSinceBoostEnded = Time.time - boostEndTime;

            // Check if deceleration period is complete
            if (timeSinceBoostEnded >= boostDecelerationTime)
            {
                // Deceleration complete - return to normal physics
                isDeceleratingFromBoost = false;
            }
            else
            {
                // Calculate target speed: smoothly lerp from boost speed to max normal speed
                float decayProgress = timeSinceBoostEnded / boostDecelerationTime;
                float targetSpeed = Mathf.Lerp(speedWhenBoostEnded, maxSpeed, decayProgress);

                // Apply controlled deceleration directly to velocity
                // Set velocity exactly to target speed to override drag/physics interference
                rb.velocity = transform.forward * targetSpeed + Vector3.up * rb.velocity.y;
            }
        }
        else if (brakeInput)
        {
            // D pressed - brake and reverse
            if (currentForwardSpeed > 0.1f)
            {
                // Moving forward - apply brake to slow down
                rb.AddForce(-transform.forward * brakeForce, ForceMode.Acceleration);
            }
            else if (currentForwardSpeed > -0.1f && currentForwardSpeed <= 0.1f)
            {
                // Stopped or nearly stopped - start reversing
                rb.AddForce(-transform.forward * acceleration * 0.5f, ForceMode.Acceleration);
            }
            else
            {
                // Already reversing - continue accelerating in reverse
                if (currentForwardSpeed > -maxReverseSpeed)
                {
                    rb.AddForce(-transform.forward * acceleration * 0.5f, ForceMode.Acceleration);
                }
            }

            // Clamp to max reverse speed
            if (currentForwardSpeed < -maxReverseSpeed)
            {
                rb.velocity = transform.forward * -maxReverseSpeed + Vector3.up * rb.velocity.y;
            }
        }
        else
        {
            // No input - maintain momentum with natural drag (rigidbody drag handles this)
            // No active deceleration, UFO coasts
        }
    }

    void HandleBoost(bool boostInput)
    {
        if (boostInput && currentBoostTime > 0f)
        {
            // Boost button held and boost available
            if (!isBoosting)
            {
                // Just started boosting - record start time and cancel deceleration
                isBoosting = true;
                boostStartTime = Time.time;
                isDeceleratingFromBoost = false;
            }

            // NOTE: Meter drain moved to FixedUpdate to sync with boost duration timing
        }
        else
        {
            // Boost button not held or boost depleted
            if (isBoosting)
            {
                // Just stopped boosting - start deceleration period (ONLY ONCE!)
                isBoosting = false;
                isDeceleratingFromBoost = true;
                boostEndTime = Time.time;
                speedWhenBoostEnded = currentForwardSpeed;
            }
            // NOTE: Don't reset deceleration variables if already decelerating!

            // Recharge boost over time (when not boosting)
            // Recharge starts immediately when boost button is released
            if (currentBoostTime < maxBoostTime)
            {
                float rechargeRate = maxBoostTime / boostRechargeTime;
                currentBoostTime += rechargeRate * Time.deltaTime;
                currentBoostTime = Mathf.Min(maxBoostTime, currentBoostTime);
            }
        }
    }

    void HandleRotation()
    {
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float turnDirection = Mathf.Sign(turnInput);

            // Detect direction change - reset turn speed for new direction
            if (turnDirection != lastTurnDirection)
            {
                currentTurnSpeed = 0f;
                lastTurnDirection = turnDirection;
            }

            // Gradually ramp up turn speed (ease-in)
            // Starts slow for precision, reaches full speed after brief hold
            currentTurnSpeed = Mathf.Lerp(currentTurnSpeed, 1f, turnAcceleration * Time.fixedDeltaTime);

            // Apply turn with ramped speed
            float turn = turnInput * turnSpeed * currentTurnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
        else
        {
            // No turn input - reset for next turn
            currentTurnSpeed = 0f;
            lastTurnDirection = 0f;
        }
    }

    void HandleVerticalMovement()
    {
        // Check if vertical control is temporarily disabled (after floor bounce)
        if (disableVerticalControl)
        {
            if (Time.time >= verticalControlReenableTime)
            {
                disableVerticalControl = false;
            }
            else
            {
                return; // Skip vertical movement while disabled
            }
        }

        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            // Calculate forward/backward speed first (ignore lateral barrel roll movement)
            // Project velocity onto forward direction to get only forward/back speed
            float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
            float absForwardSpeed = Mathf.Abs(forwardSpeed);

            // Determine if we should apply ramping based on forward motion
            bool isMovingForward = absForwardSpeed >= minForwardSpeedForRamping;

            float verticalDirection = Mathf.Sign(verticalInput);

            // Detect direction change - reset vertical speed ONLY if changing up/down direction
            // If resuming same direction (e.g., was climbing, still climbing), keep current ramp progress
            if (verticalDirection != lastVerticalDirection && lastVerticalDirection != 0f)
            {
                // True direction reversal (up to down or vice versa) - reset
                currentVerticalSpeed = 0f;
                lastVerticalDirection = verticalDirection;
            }
            else if (lastVerticalDirection == 0f)
            {
                // Was at zero (no input), now starting input
                // Check current vertical velocity to see if we're continuing momentum
                if (Mathf.Abs(rb.velocity.y) > 0.5f && Mathf.Sign(rb.velocity.y) == verticalDirection)
                {
                    // Continuing in same direction as current vertical velocity - start at higher ramp value
                    // Scale the starting ramp based on current vertical velocity magnitude
                    currentVerticalSpeed = Mathf.Clamp01(Mathf.Abs(rb.velocity.y) / 10f);
                }
                else
                {
                    // New direction or no significant momentum - start fresh
                    currentVerticalSpeed = 0f;
                }
                lastVerticalDirection = verticalDirection;
            }

            // Apply ramping only when moving forward (for aiming precision)
            // When hovering/stationary, give instant vertical control
            if (isMovingForward)
            {
                // Gradually ramp up vertical input speed (ease-in)
                // Starts slow for precision aiming, reaches full speed after brief hold
                currentVerticalSpeed = Mathf.Lerp(currentVerticalSpeed, 1f, verticalAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Instant response when not moving forward (hovering, stationary)
                currentVerticalSpeed = 1f;
            }

            // Apply ramped vertical input to all calculations below
            float rampedVerticalInput = verticalInput * currentVerticalSpeed;

            // Apply exponential curve to reduce sensitivity at low inputs while preserving full range
            // This makes small taps create smaller pitch changes, but full input still reaches maximum
            // Power of 1.2 gives subtle reduction: 10% → 6.3%, 50% → 46.4%, 100% → 100%
            float inputMagnitude = Mathf.Abs(rampedVerticalInput);
            float curvedInput = Mathf.Pow(inputMagnitude, 1.2f) * Mathf.Sign(rampedVerticalInput);
            rampedVerticalInput = curvedInput;

            // Apply smooth gradient for speed boost based on forward speed
            // Barrel roll lateral movement doesn't count toward threshold
            // SPECIAL: During barrel roll, always apply full 3x multiplier for fast evasive climbs/dives
            // At 0 forward speed: full multiplier
            // At threshold forward speed: no multiplier (1x)
            float speedMultiplier = 1f;

            if (isBarrelRolling)
            {
                // Full speed boost during barrel roll for aggressive vertical maneuvers
                speedMultiplier = pureVerticalSpeedMultiplier;
            }
            else if (absForwardSpeed < pureVerticalThreshold * 2f) // Kick in at 2x threshold (20 units/sec instead of 10)
            {
                // Smooth gradient: Inverse relationship between horizontal and vertical speed
                // Starts boosting MUCH earlier while still moving forward
                // Use smoothstep for even smoother transition curve
                float t = absForwardSpeed / (pureVerticalThreshold * 2f); // 0 to 1
                t = Mathf.SmoothStep(0f, 1f, t); // Apply smoothing to the transition
                speedMultiplier = Mathf.Lerp(pureVerticalSpeedMultiplier, 1f, t);
            }

            // MOMENTUM CONSERVATION: Maintain constant total speed during transition
            // When not accelerating, preserve total velocity magnitude
            if (!accelerateInput && !brakeInput)
            {
                // Capture current total speed to preserve
                float totalSpeed = rb.velocity.magnitude;

                // Calculate effective vertical speed with multiplier
                float effectiveVerticalSpeed = verticalSpeed * speedMultiplier;

                // Get current horizontal velocity (x and z components)
                Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                float currentHorizontalSpeed = horizontalVelocity.magnitude;

                // Calculate target vertical speed using Pythagorean theorem to maintain total speed
                // total^2 = horizontal^2 + vertical^2
                // Therefore: vertical = sqrt(total^2 - horizontal^2)
                float targetVerticalSpeed = Mathf.Sqrt(Mathf.Max(0, totalSpeed * totalSpeed - currentHorizontalSpeed * currentHorizontalSpeed));

                // Ensure we meet minimum effective vertical speed (from multiplier system)
                targetVerticalSpeed = Mathf.Max(targetVerticalSpeed, effectiveVerticalSpeed);

                // Apply the calculated vertical velocity
                rb.velocity = new Vector3(rb.velocity.x, targetVerticalSpeed * Mathf.Sign(rampedVerticalInput), rb.velocity.z);
            }
            else
            {
                // When accelerating/braking, allow steeper climbs/dives
                // Use a higher multiplier (2x) to enable aggressive forward+up/down flight paths
                float activeFlightMultiplier = 2f;
                float effectiveVerticalSpeed = verticalSpeed * speedMultiplier * activeFlightMultiplier;
                Vector3 verticalMove = Vector3.up * rampedVerticalInput * effectiveVerticalSpeed;
                rb.velocity = new Vector3(rb.velocity.x, verticalMove.y, rb.velocity.z);
            }
        }
        else
        {
            // No vertical input - reset for next vertical movement
            currentVerticalSpeed = 0f;
            lastVerticalDirection = 0f;
        }
    }

    // Public method for UFOCollision to disable vertical control temporarily
    public void DisableVerticalControl(float duration)
    {
        disableVerticalControl = true;
        verticalControlReenableTime = Time.time + duration;
    }

    /// <summary>
    /// Get the aiming direction for weapon firing
    /// Combines camera yaw with UFO velocity pitch for natural aiming
    /// </summary>
    public Quaternion GetAimDirection()
    {
        Vector3 aimDirection;

        // OPTION 1: Hybrid camera + velocity aiming
        if (aimCamera != null)
        {
            // Get the UFO's yaw (camera tracks this for left/right aiming)
            float yaw = transform.eulerAngles.y;

            // Calculate pitch from actual velocity (if moving forward)
            float pitchAngle = 0f;
            if (rb != null && rb.velocity.magnitude > 5f)
            {
                // Get horizontal and vertical velocity components
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
                float horizontalSpeed = horizontalVelocity.magnitude;
                float verticalVelocity = rb.velocity.y;

                // ONLY apply pitch if there's significant forward/backward movement
                // Pure vertical movement (hovering up/down) = shoot horizontal
                if (horizontalSpeed > 3f)
                {
                    // Calculate pitch angle from velocity
                    pitchAngle = Mathf.Atan2(verticalVelocity, horizontalSpeed) * Mathf.Rad2Deg;

                    // Scale pitch angles for natural aiming feel
                    if (pitchAngle > 0) // Ascending
                    {
                        pitchAngle *= 1.0f; // Full upward angle (100%)
                    }
                    else // Descending
                    {
                        pitchAngle *= 0.8f; // Downward angle at 80%
                    }
                }
                // else: pure vertical = pitchAngle stays 0 (horizontal shot)
            }

            // Create aim direction: UFO's yaw + velocity pitch
            // Negative pitch because Unity's pitch is inverted (positive = down)
            Quaternion aimRotation = Quaternion.Euler(-pitchAngle, yaw, 0);
            aimDirection = aimRotation * Vector3.forward;

        }
        // OPTION 2: Visual model aiming (matches UFO visual tilt)
        else if (visualModel != null)
        {
            // Use visual model's world rotation (includes pitch and yaw)
            Vector3 visualForward = visualModel.TransformDirection(Vector3.forward);
            aimDirection = visualForward;
        }
        // OPTION 3: Fallback to horizontal forward
        else
        {
            aimDirection = transform.forward;
        }

        // Create rotation from aim direction
        return Quaternion.LookRotation(aimDirection);
    }

    /// <summary>
    /// Get current boost amount (0 to maxBoostTime)
    /// </summary>
    public float GetCurrentBoost()
    {
        return currentBoostTime;
    }

    /// <summary>
    /// Get maximum boost amount
    /// </summary>
    public float GetMaxBoost()
    {
        return maxBoostTime;
    }

    /// <summary>
    /// Get boost percentage (0 to 1)
    /// </summary>
    public float GetBoostPercent()
    {
        return currentBoostTime / maxBoostTime;
    }

    /// <summary>
    /// Check if boost is currently active
    /// </summary>
    public bool IsBoosting()
    {
        return isBoosting;
    }

    void EnforceHeightLimits()
    {
        // Keep UFO within height bounds
        Vector3 pos = transform.position;

        if (pos.y > maxHeight)
        {
            pos.y = maxHeight;
            transform.position = pos;

            // Stop upward velocity
            if (rb.velocity.y > 0)
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
        else if (pos.y < minHeight)
        {
            pos.y = minHeight;
            transform.position = pos;

            // Stop downward velocity
            if (rb.velocity.y < 0)
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
    }

    void ApplyBankingAndPitch()
    {
        // Only apply tilting if we have a separate visual model assigned
        // If no visual model, skip to avoid interfering with physics rotation
        if (visualModel == null)
            return;

        // Get wobble offset (if UFO is hit) - declared once for entire method
        UFOHitEffect hitEffect = GetComponent<UFOHitEffect>();
        Vector3 wobbleOffset = (hitEffect != null) ? hitEffect.GetWobbleOffset() : Vector3.zero;

        // If barrel rolling, override with barrel roll animation
        if (isBarrelRolling)
        {
            // Calculate barrel roll progress (0 to 1)
            float progress = (Time.time - barrelRollStartTime) / barrelRollDuration;
            progress = Mathf.Clamp01(progress);

            // Calculate 360 degree roll based on progress
            // Negate direction to match visual roll with movement direction
            float rollAngle = progress * 360f * -barrelRollDirection;

            // Apply pure roll animation (Z-axis rotation) with wobble
            visualModel.localRotation = Quaternion.Euler(wobbleOffset.x, wobbleOffset.y, rollAngle + wobbleOffset.z);
            return;
        }

        // Normal banking and pitch (when not barrel rolling)
        // Calculate target bank angle based on turn input
        float targetBankAngle = -turnInput * bankAmount;

        // Smoothly interpolate to target bank angle
        currentBankAngle = Mathf.Lerp(currentBankAngle, targetBankAngle, bankSpeed * Time.deltaTime);

        // Calculate target pitch angle based on vertical input and forward speed
        float targetPitchAngle = 0f;

        // Only pitch if moving forward at minimum speed
        float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        if (horizontalSpeed >= minSpeedForPitch)
        {
            // Ascending = nose up (positive pitch), Descending = nose down (negative pitch)
            // Use raw verticalInput for visual pitch (visual should match stick position)
            targetPitchAngle = -verticalInput * visualPitchAmount;

        }

        // Smoothly interpolate to target pitch angle
        // Use slower speed when returning to level (no vertical input) for gradual transition
        float pitchLerpSpeed = (Mathf.Abs(verticalInput) > 0.1f) ? visualPitchSpeed : visualPitchSpeed * 0.3f;
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, targetPitchAngle, pitchLerpSpeed * Time.deltaTime);

        // Apply both banking (Z-axis roll) and pitch (X-axis rotation) to visual model with wobble
        visualModel.localRotation = Quaternion.Euler(
            currentPitchAngle + wobbleOffset.x,
            wobbleOffset.y,
            currentBankAngle + wobbleOffset.z
        );
    }

    void AutoLevel()
    {
        // Force the UFO to stay perfectly level (X and Z rotation = 0)
        // This corrects any tilting from impacts while preserving turning
        Vector3 currentEuler = transform.eulerAngles;

        // Normalize angles to -180 to 180 range for better checking
        float xRot = currentEuler.x > 180 ? currentEuler.x - 360 : currentEuler.x;
        float zRot = currentEuler.z > 180 ? currentEuler.z - 360 : currentEuler.z;

        // Only correct if there's significant tilt (more than 1 degree)
        if (Mathf.Abs(xRot) > 1f || Mathf.Abs(zRot) > 1f)
        {
            // Keep Y rotation (turning), force X and Z to 0
            Quaternion levelRotation = Quaternion.Euler(0, currentEuler.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, levelRotation, autoLevelSpeed * Time.fixedDeltaTime);
        }
    }

    void StartBarrelRoll(float direction)
    {
        isBarrelRolling = true;
        barrelRollDirection = direction;
        barrelRollStartTime = Time.time;
        barrelRollEndTime = Time.time + barrelRollDuration;
        barrelRollCooldownEndTime = barrelRollEndTime + barrelRollCooldown;
    }

    void HandleBarrelRoll()
    {
        if (!isBarrelRolling)
            return;

        // Check if barrel roll is complete
        if (Time.time >= barrelRollEndTime)
        {
            isBarrelRolling = false;
            isInPlaceRoll = false; // Reset in-place flag

            // If there's a buffered roll, start it immediately
            if (hasBufferedRoll)
            {
                hasBufferedRoll = false;
                StartBarrelRoll(bufferedRollDirection);
            }
            return;
        }

        // Apply lateral dodge force (skip if in-place roll)
        if (!isInPlaceRoll)
        {
            // Calculate impulse needed to travel barrelRollDistance over duration
            float dodgeSpeed = barrelRollDistance / barrelRollDuration;

            // Get the UFO's right vector (local X-axis) for lateral movement
            Vector3 dodgeDirection = transform.right * barrelRollDirection;

            // Apply continuous force during the roll to maintain dodge speed
            // Use velocity change mode for immediate response
            Vector3 lateralVelocity = dodgeDirection * dodgeSpeed;
            Vector3 currentLateralVelocity = Vector3.Project(rb.velocity, transform.right);
            Vector3 neededVelocity = lateralVelocity - currentLateralVelocity;

            // Apply strong force for fast, snappy dodge movement
            rb.AddForce(neededVelocity * 15f, ForceMode.Acceleration);
        }
        // If in-place roll, UFO just spins without lateral movement (visual only)
    }
}

