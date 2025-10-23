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

    [Tooltip("How fast the UFO rotates up/down")]
    public float pitchSpeed = 100f;

    [Header("Vertical Movement")]
    [Tooltip("Speed of ascending/descending")]
    public float verticalSpeed = 8f;

    [Tooltip("Maximum height above ground")]
    public float maxHeight = 20f;

    [Tooltip("Minimum height above ground")]
    public float minHeight = 0.5f;

    [Header("Reverse Settings")]
    [Tooltip("Maximum reverse speed")]
    public float maxReverseSpeed = 20f;

    [Header("Physics")]
    [Tooltip("Natural momentum drag (lower = more momentum)")]
    public float dragAmount = 0.5f;

    [Tooltip("How quickly the UFO levels out when not pitching")]
    public float autoLevelSpeed = 2f;

    [Header("Visual Feedback")]
    [Tooltip("UFO model to tilt (leave empty to tilt whole object)")]
    public Transform visualModel;

    [Tooltip("How much the UFO banks when turning (degrees)")]
    public float bankAmount = 25f;

    [Tooltip("How quickly the UFO banks")]
    public float bankSpeed = 3f;

    [Tooltip("How much the UFO pitches when ascending/descending (degrees)")]
    public float visualPitchAmount = 30f;

    [Tooltip("How quickly the UFO pitches visually")]
    public float visualPitchSpeed = 3f;

    [Tooltip("Minimum forward speed required for pitch tilt")]
    public float minSpeedForPitch = 5f;

    // Components
    private Rigidbody rb;

    // Input values
    private bool accelerateInput;
    private bool brakeInput;
    private float turnInput;
    private float verticalInput;

    // Current speed tracking
    private float currentForwardSpeed;

    // Visual feedback
    private float currentBankAngle;
    private float currentPitchAngle;

    // Floor bounce control
    private bool disableVerticalControl;
    private float verticalControlReenableTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Configure rigidbody for arcade physics
        rb.drag = dragAmount;
        rb.angularDrag = 3f;
        rb.useGravity = false; // UFO hovers, no gravity
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Use Continuous collision detection to prevent phasing through walls at high speed
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        // Get input from keyboard and controller
        GetInput();

        // Apply visual feedback
        ApplyBankingAndPitch();
    }

    void FixedUpdate()
    {
        // Apply movement and rotation
        HandleMovement();
        HandleRotation();
        HandleVerticalMovement();
        EnforceHeightLimits();

        // Keep UFO level (prevent tilting from impacts)
        AutoLevel();
    }

    void GetInput()
    {
        // Keyboard controls:
        // A = Accelerate forward
        // D = Brake/Reverse
        accelerateInput = Input.GetKey(KeyCode.A);
        brakeInput = Input.GetKey(KeyCode.D);

        // Controller face buttons (Button 0 = A/Cross, Button 1 = B/Circle)
        if (Input.GetButton("Fire1")) // Typically button 0
            accelerateInput = true;
        if (Input.GetButton("Fire2")) // Typically button 1
            brakeInput = true;

        // Arrow keys for turning (Left/Right) + Controller Left Stick X-axis
        turnInput = Input.GetAxis("Horizontal");

        // Arrow keys for vertical movement (Up/Down) + Controller Left Stick Y-axis
        verticalInput = Input.GetAxis("Vertical");
    }

    void HandleMovement()
    {
        // Get current forward speed (positive = forward, negative = backward)
        currentForwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        if (accelerateInput && brakeInput)
        {
            // Both keys pressed - do nothing (maintain momentum)
            return;
        }
        else if (accelerateInput)
        {
            // A pressed - accelerate forward
            if (currentForwardSpeed < maxSpeed)
            {
                rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);
            }

            // Clamp to max forward speed
            if (currentForwardSpeed > maxSpeed)
            {
                Vector3 horizontalVelocity = rb.velocity;
                horizontalVelocity.y = 0;
                rb.velocity = transform.forward * maxSpeed + Vector3.up * rb.velocity.y;
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

    void HandleRotation()
    {
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            // Tight arcade-style turning
            float turn = turnInput * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
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
            // Simple up/down movement
            Vector3 verticalMove = Vector3.up * verticalInput * verticalSpeed;
            rb.velocity = new Vector3(rb.velocity.x, verticalMove.y, rb.velocity.z);
        }
    }

    // Public method for UFOCollision to disable vertical control temporarily
    public void DisableVerticalControl(float duration)
    {
        disableVerticalControl = true;
        verticalControlReenableTime = Time.time + duration;
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
            targetPitchAngle = -verticalInput * visualPitchAmount;

            // Debug logging
            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                Debug.Log($"Pitch Active - Speed: {horizontalSpeed:F1}, VertInput: {verticalInput:F2}, TargetPitch: {targetPitchAngle:F1}°");
            }
        }

        // Smoothly interpolate to target pitch angle
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, targetPitchAngle, visualPitchSpeed * Time.deltaTime);

        // Apply both banking (Z-axis roll) and pitch (X-axis rotation) to visual model
        visualModel.localRotation = Quaternion.Euler(currentPitchAngle, 0, currentBankAngle);
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
}
