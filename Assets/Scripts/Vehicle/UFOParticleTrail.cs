using UnityEngine;

/// <summary>
/// Creates a particle trail effect behind the UFO
/// Particles emit continuously and fade out, creating a smooth motion trail
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class UFOParticleTrail : MonoBehaviour
{
    [Header("Particle Settings")]
    [Tooltip("Particle lifetime (seconds)")]
    public float particleLifetime = 1f;

    [Tooltip("Particle start size")]
    public float startSize = 0.5f;

    [Tooltip("Particle end size")]
    public float endSize = 0.1f;

    [Tooltip("Particles emitted per second")]
    public float emissionRate = 30f;

    [Tooltip("Particle start speed (how fast they move initially)")]
    public float startSpeed = 2f;

    [Header("Colors")]
    [Tooltip("Particle color at spawn")]
    public Color startColor = new Color(0f, 1f, 1f, 0.8f); // Cyan with transparency

    [Tooltip("Particle color at death")]
    public Color endColor = new Color(0f, 1f, 1f, 0f); // Transparent cyan

    [Header("Speed Response")]
    [Tooltip("Enable speed-based emission")]
    public bool enableSpeedResponse = true;

    [Tooltip("Minimum speed to emit particles (units/s)")]
    public float minSpeedForTrail = 5f;

    [Tooltip("Speed at which emission is at maximum")]
    public float maxSpeedForTrail = 30f;

    [Header("Trail Position")]
    [Tooltip("Offset from UFO center (local space)")]
    public Vector3 emissionOffset = new Vector3(0f, -0.5f, -1f);

    private ParticleSystem particleSystem;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Create particle system GameObject as child
        GameObject particleObj = new GameObject("ParticleTrail");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = emissionOffset;
        particleObj.transform.localRotation = Quaternion.identity;

        // Add and configure particle system
        particleSystem = particleObj.AddComponent<ParticleSystem>();

        var main = particleSystem.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // World space so they stay in place
        main.maxParticles = 100; // Limit for performance
        main.loop = true;

        // Color over lifetime
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Size over lifetime (shrink as they fade)
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f); // Start at full size
        sizeCurve.AddKey(1f, endSize / startSize); // End at smaller size
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Emission
        var emission = particleSystem.emission;
        emission.rateOverTime = emissionRate;

        // Shape (sphere for omnidirectional initial spread)
        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Renderer settings (GPU-friendly)
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Create material
        Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        particleMat.SetColor("_Color", startColor);
        particleMat.SetFloat("_Mode", 3); // Transparent
        particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive for glow
        particleMat.SetInt("_ZWrite", 0);
        particleMat.renderQueue = 3000;
        renderer.material = particleMat;

        Debug.Log($"[PARTICLE TRAIL] Particle trail created for {gameObject.name}");
    }

    void Update()
    {
        if (particleSystem == null || rb == null)
            return;

        if (!enableSpeedResponse)
            return;

        // Get current speed
        float currentSpeed = rb.velocity.magnitude;

        var emission = particleSystem.emission;

        // Control emission based on speed
        if (currentSpeed < minSpeedForTrail)
        {
            // Below minimum speed - stop emitting
            emission.enabled = false;
        }
        else
        {
            emission.enabled = true;

            // Scale emission rate based on speed
            float speedFactor = Mathf.Clamp01((currentSpeed - minSpeedForTrail) / (maxSpeedForTrail - minSpeedForTrail));
            emission.rateOverTime = emissionRate * speedFactor;
        }
    }
}
