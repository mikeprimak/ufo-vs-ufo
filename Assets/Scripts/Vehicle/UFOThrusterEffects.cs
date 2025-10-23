using UnityEngine;

/// <summary>
/// Controls thruster particle effects based on UFO movement
/// Shows different particles for acceleration, braking, and reverse
/// </summary>
[RequireComponent(typeof(UFOController))]
public class UFOThrusterEffects : MonoBehaviour
{
    [Header("Particle Systems")]
    [Tooltip("Main thruster particles (accelerating forward)")]
    public ParticleSystem mainThruster;

    [Tooltip("Brake thruster particles (braking)")]
    public ParticleSystem brakeThruster;

    [Tooltip("Reverse thruster particles (going backward)")]
    public ParticleSystem reverseThruster;

    [Header("Emission Settings")]
    [Tooltip("Particle emission rate when active")]
    public float emissionRate = 50f;

    [Tooltip("How responsive the particles are to input changes")]
    public float emissionSmoothing = 5f;

    private UFOController ufoController;
    private float mainEmission;
    private float brakeEmission;
    private float reverseEmission;

    void Start()
    {
        ufoController = GetComponent<UFOController>();

        // Set initial emission to zero
        SetEmissionRate(mainThruster, 0);
        SetEmissionRate(brakeThruster, 0);
        SetEmissionRate(reverseThruster, 0);
    }

    void Update()
    {
        if (ufoController == null)
            return;

        // Get input state from controller (we'll check via Input since UFOController doesn't expose these)
        bool isAccelerating = Input.GetKey(KeyCode.A) || Input.GetButton("Fire1");
        bool isBraking = Input.GetKey(KeyCode.D) || Input.GetButton("Fire2");

        // Update main thruster (forward acceleration)
        float targetMainEmission = isAccelerating && !isBraking ? emissionRate : 0f;
        mainEmission = Mathf.Lerp(mainEmission, targetMainEmission, emissionSmoothing * Time.deltaTime);
        SetEmissionRate(mainThruster, mainEmission);

        // Update brake thruster
        float targetBrakeEmission = isBraking && !isAccelerating ? emissionRate : 0f;
        brakeEmission = Mathf.Lerp(brakeEmission, targetBrakeEmission, emissionSmoothing * Time.deltaTime);
        SetEmissionRate(brakeThruster, brakeEmission);

        // Update reverse thruster (only when actually moving backward)
        // We'll check velocity for this
        Rigidbody rb = GetComponent<Rigidbody>();
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float targetReverseEmission = (forwardSpeed < -0.5f && isBraking) ? emissionRate : 0f;
        reverseEmission = Mathf.Lerp(reverseEmission, targetReverseEmission, emissionSmoothing * Time.deltaTime);
        SetEmissionRate(reverseThruster, reverseEmission);
    }

    void SetEmissionRate(ParticleSystem ps, float rate)
    {
        if (ps == null)
            return;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        // Start or stop the particle system
        if (rate > 0 && !ps.isPlaying)
            ps.Play();
        else if (rate <= 0 && ps.isPlaying)
            ps.Stop();
    }
}
