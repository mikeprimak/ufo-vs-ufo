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
    public float particleLifetime = 0.2f;

    [Tooltip("Particle start size")]
    public float startSize = 0.15f;

    [Tooltip("Particle end size")]
    public float endSize = 0.01f;

    [Tooltip("Particles emitted per second")]
    public float emissionRate = 15f;

    [Tooltip("Particle start speed (how fast they move initially)")]
    public float startSpeed = 0.5f;

    [Header("Colors")]
    [Tooltip("Particle color at spawn")]
    public Color startColor = new Color(1f, 1f, 0f, 0.8f); // Yellow with transparency

    [Tooltip("Particle color at death")]
    public Color endColor = new Color(1f, 1f, 0f, 0f); // Transparent yellow

    [Header("Speed Response")]
    [Tooltip("Enable speed-based emission")]
    public bool enableSpeedResponse = true;

    [Tooltip("Minimum speed to emit particles (units/s)")]
    public float minSpeedForTrail = 10f;

    [Tooltip("Speed at which emission is at maximum")]
    public float maxSpeedForTrail = 30f;

    [Header("Trail Position")]
    [Tooltip("Left/right distance from UFO center")]
    public float lateralOffset = 4f;

    [Tooltip("Forward/back offset from UFO center (negative = rear)")]
    public float forwardOffset = -3f;

    [Tooltip("Up/down offset from UFO center")]
    public float verticalOffset = -1f;

    private ParticleSystem leftTrailParticles;
    private ParticleSystem rightTrailParticles;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Create LEFT particle emitter
        Vector3 leftPosition = new Vector3(-lateralOffset, verticalOffset, forwardOffset);
        leftTrailParticles = CreateParticleSystem("ParticleTrail_Left", leftPosition);

        // Create RIGHT particle emitter
        Vector3 rightPosition = new Vector3(lateralOffset, verticalOffset, forwardOffset);
        rightTrailParticles = CreateParticleSystem("ParticleTrail_Right", rightPosition);

        Debug.Log($"[PARTICLE TRAIL] Dual particle trails created for {gameObject.name}");
    }

    ParticleSystem CreateParticleSystem(string name, Vector3 localPosition)
    {
        // Create particle system GameObject as child
        GameObject particleObj = new GameObject(name);
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = localPosition;
        particleObj.transform.localRotation = Quaternion.identity;

        // Add and configure particle system
        ParticleSystem trailParticles = particleObj.AddComponent<ParticleSystem>();

        var main = trailParticles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // World space so they stay in place
        main.maxParticles = 100; // Limit for performance
        main.loop = true;

        // Color over lifetime
        var colorOverLifetime = trailParticles.colorOverLifetime;
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
        var sizeOverLifetime = trailParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f); // Start at full size
        sizeCurve.AddKey(1f, endSize / startSize); // End at smaller size
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Emission
        var emission = trailParticles.emission;
        emission.rateOverTime = emissionRate;

        // Shape (sphere for omnidirectional initial spread)
        var shape = trailParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Renderer settings (GPU-friendly)
        var renderer = trailParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 1; // Render after other transparent objects

        // Create material - Original transparent additive for glowing sci-fi effect
        Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        particleMat.SetColor("_Color", startColor);
        particleMat.SetFloat("_Mode", 3); // Transparent
        particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive for glow
        particleMat.SetInt("_ZWrite", 0);
        particleMat.renderQueue = 3000;

        // Create circular gradient texture for round particles
        Texture2D circleTexture = CreateCircleTexture(64);
        particleMat.SetTexture("_MainTex", circleTexture);

        renderer.material = particleMat;

        return trailParticles;
    }

    Texture2D CreateCircleTexture(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float maxDistance = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Calculate distance from center
                float distance = Vector2.Distance(new Vector2(x, y), center);

                // Create smooth circular gradient (soft edges)
                float alpha = 1f - Mathf.Clamp01(distance / maxDistance);
                alpha = Mathf.Pow(alpha, 2f); // Smooth falloff

                // Set pixel color (white with alpha gradient)
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    void Update()
    {
        if (leftTrailParticles == null || rightTrailParticles == null || rb == null)
            return;

        if (!enableSpeedResponse)
            return;

        // Get current speed
        float currentSpeed = rb.velocity.magnitude;

        // Update LEFT particle system
        var leftEmission = leftTrailParticles.emission;
        UpdateEmission(leftEmission, currentSpeed);

        // Update RIGHT particle system
        var rightEmission = rightTrailParticles.emission;
        UpdateEmission(rightEmission, currentSpeed);
    }

    void UpdateEmission(ParticleSystem.EmissionModule emission, float currentSpeed)
    {
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
