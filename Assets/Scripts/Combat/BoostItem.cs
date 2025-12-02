using UnityEngine;

/// <summary>
/// Boost defensive item - gives UFO a sustained speed boost like Mario Kart mushroom
/// Increases max speed and applies continuous forward thrust for duration
/// </summary>
public class BoostItem : MonoBehaviour
{
    [Header("Boost Settings")]
    [Tooltip("How long the boost lasts (seconds)")]
    public float boostDuration = 3f;

    [Tooltip("Speed multiplier during boost (2 = double speed)")]
    public float speedMultiplier = 2f;

    [Tooltip("Extra forward acceleration during boost")]
    public float boostAcceleration = 200f;

    [Header("Visual Settings - Blue Trails")]
    [Tooltip("Color of blue boost trail")]
    public Color blueTrailColor = new Color(0.3f, 0.6f, 1f, 0.8f); // Blue

    [Tooltip("Size of blue boost trail")]
    public float blueTrailSize = 0.2f;

    [Tooltip("How long blue particles live (seconds)")]
    public float blueParticleLifetime = 0.15f;

    [Header("Visual Settings - Red Trails")]
    [Tooltip("Color of red boost trail")]
    public Color redTrailColor = new Color(1f, 0.3f, 0.2f, 0.8f); // Red

    [Tooltip("Size of red boost trail")]
    public float redTrailSize = 0.2f;

    [Tooltip("How long red particles live (seconds)")]
    public float redParticleLifetime = 0.15f;

    [Header("Trail Positions (match regular trails)")]
    [Tooltip("Left/right distance from UFO center")]
    public float lateralOffset = 2.4f;

    [Tooltip("Forward/back offset from UFO center")]
    public float forwardOffset = -1f;

    [Tooltip("Up/down offset from UFO center")]
    public float verticalOffset = -0.5f;

    [Header("Audio")]
    [Tooltip("Sound to play when boost activates")]
    public AudioClip boostSound;

    // Boost state
    private bool isActive = false;
    private float boostEndTime = 0f;
    private Rigidbody rb;
    private UFOController ufoController;
    private float originalMaxSpeed;
    private float originalDrag;
    private GameObject leftBlueTrail;
    private GameObject rightBlueTrail;
    private GameObject centerBlueTrail;
    private GameObject leftRedTrail;
    private GameObject rightRedTrail;
    private GameObject centerRedTrail;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ufoController = GetComponent<UFOController>();
    }

    void Update()
    {
        if (!isActive)
            return;

        // Check if boost ended
        if (Time.time >= boostEndTime)
        {
            DeactivateBoost();
        }
    }

    void FixedUpdate()
    {
        if (!isActive)
            return;

        // Apply continuous forward thrust during boost
        if (rb != null)
        {
            rb.AddForce(transform.forward * boostAcceleration, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Check if boost can be activated
    /// </summary>
    public bool CanActivate()
    {
        return !isActive && enabled;
    }

    /// <summary>
    /// Try to activate the boost
    /// </summary>
    public bool TryActivate()
    {
        if (!CanActivate())
            return false;

        ActivateBoost();
        return true;
    }

    /// <summary>
    /// Activate the sustained boost
    /// </summary>
    void ActivateBoost()
    {
        isActive = true;
        boostEndTime = Time.time + boostDuration;

        Debug.Log($"[BOOST] {gameObject.name} activated boost for {boostDuration} seconds!");

        // Increase max speed temporarily
        if (ufoController != null)
        {
            originalMaxSpeed = ufoController.maxSpeed;
            ufoController.maxSpeed = originalMaxSpeed * speedMultiplier;
            Debug.Log($"[BOOST] Max speed: {originalMaxSpeed} -> {ufoController.maxSpeed}");
        }

        // Reduce drag for faster acceleration
        if (rb != null)
        {
            originalDrag = rb.drag;
            rb.drag = originalDrag * 0.3f; // 70% less drag
        }

        // Create boost trail effects
        CreateBoostTrails();

        // Play boost sound
        if (boostSound != null)
        {
            AudioSource.PlayClipAtPoint(boostSound, transform.position, 1f);
        }
    }

    /// <summary>
    /// Deactivate boost and restore normal settings
    /// </summary>
    void DeactivateBoost()
    {
        isActive = false;

        Debug.Log($"[BOOST] {gameObject.name} boost ended");

        // Restore original max speed
        if (ufoController != null)
        {
            ufoController.maxSpeed = originalMaxSpeed;
        }

        // Restore original drag
        if (rb != null)
        {
            rb.drag = originalDrag;
        }

        // Destroy blue trail effects
        if (leftBlueTrail != null) { Destroy(leftBlueTrail); leftBlueTrail = null; }
        if (rightBlueTrail != null) { Destroy(rightBlueTrail); rightBlueTrail = null; }
        if (centerBlueTrail != null) { Destroy(centerBlueTrail); centerBlueTrail = null; }

        // Destroy red trail effects
        if (leftRedTrail != null) { Destroy(leftRedTrail); leftRedTrail = null; }
        if (rightRedTrail != null) { Destroy(rightRedTrail); rightRedTrail = null; }
        if (centerRedTrail != null) { Destroy(centerRedTrail); centerRedTrail = null; }
    }

    /// <summary>
    /// Create six particle trail effects behind UFO during boost (3 blue + 3 red at same positions)
    /// </summary>
    void CreateBoostTrails()
    {
        Vector3 leftPos = new Vector3(-lateralOffset, verticalOffset, forwardOffset);
        Vector3 rightPos = new Vector3(lateralOffset, verticalOffset, forwardOffset);
        Vector3 centerPos = new Vector3(0f, verticalOffset, forwardOffset - 2.5f);

        // Blue trails
        leftBlueTrail = CreateSingleBoostTrail("BoostTrail_Blue_Left", leftPos, blueTrailColor, blueTrailSize, blueParticleLifetime);
        rightBlueTrail = CreateSingleBoostTrail("BoostTrail_Blue_Right", rightPos, blueTrailColor, blueTrailSize, blueParticleLifetime);
        centerBlueTrail = CreateSingleBoostTrail("BoostTrail_Blue_Center", centerPos, blueTrailColor, blueTrailSize, blueParticleLifetime);

        // Red trails
        leftRedTrail = CreateSingleBoostTrail("BoostTrail_Red_Left", leftPos, redTrailColor, redTrailSize, redParticleLifetime);
        rightRedTrail = CreateSingleBoostTrail("BoostTrail_Red_Right", rightPos, redTrailColor, redTrailSize, redParticleLifetime);
        centerRedTrail = CreateSingleBoostTrail("BoostTrail_Red_Center", centerPos, redTrailColor, redTrailSize, redParticleLifetime);
    }

    /// <summary>
    /// Create a single boost trail particle system
    /// </summary>
    GameObject CreateSingleBoostTrail(string name, Vector3 localPosition, Color color, float size, float lifetime)
    {
        GameObject trailObj = new GameObject(name);
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = localPosition;

        ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = 3f;
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color * 0.5f, 1f) // Fade to darker version
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Use simple particle material
        var renderer = trailObj.GetComponent<ParticleSystemRenderer>();
        Material trailMat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (trailMat != null)
        {
            trailMat.color = color;
            renderer.material = trailMat;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return trailObj;
    }

    /// <summary>
    /// Check if boost is currently active
    /// </summary>
    public bool IsBoostActive()
    {
        return isActive;
    }

    void OnDisable()
    {
        // Clean up if disabled while active
        if (isActive)
        {
            DeactivateBoost();
        }
    }
}
