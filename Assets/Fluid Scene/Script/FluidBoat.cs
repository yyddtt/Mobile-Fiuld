using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
public class FluidBoat : MonoBehaviour
{
    public MPMFluidMobile fluid;
    public float radius = 1.0f;
    public float buoyancyCoeff = 15.0f;
    public float dragCoeff = 2.0f;
    public Transform[] probePoints; // Assign manually or auto-generate

    [Header("Stability")]
    public Vector3 centerOfMassOffset = new Vector3(0, -1.0f, 0); // Lower center of mass = more stable
    public float uprightTorque = 50.0f; // Increased default torque
    public float damping = 2.0f; // Linear Damping
    public float densitySmoothSpeed = 5.0f; // How fast density updates (Lower = smoother but laggier)
    public float maxRiseSpeed = 3.5f;

    [Header("Interaction")]
    [Range(0f, 1f)] public float velocityInteraction = 0.5f; // Reduces how much boat pushes water velocity
    [Range(0f, 10f)] public float flowStrength = 1.0f; // Multiplier for fluid pushing boat
    
    [Header("Effects")]
    public float extraGravity = 30.0f; // Extra gravity to reduce "hang time"
    public Transform[] wakeEmitters; // Objects at the tail to create ripples
    public float wakeRadius = 0.4f;
    public float wakeForceMultiplier = 1.5f; // New: Boost wake velocity

    [Header("Controls")]
    public bool enableControls = true;
    public float motorPower = 50.0f;
    public float steerPower = 5.0f;

    [Header("Input (Debug/Script)")]
    [Range(-1f, 1f)] public float throttleInput = 0f;
    [Range(-1f, 1f)] public float steerInput = 0f;

    private Rigidbody rb;
    private ComputeBuffer probeBuf;
    private MPMFluidMobile.ProbeData[] probeData;
    private float[] smoothedDensities;
    private Vector3[] fluidVelocities;
    private MPMFluidMobile.HullSphere[] wakeSpheres; // Buffer array for wake emitters
    private bool pendingReadback = false;
    private bool hasProbeData = false;
    private float initialDrag;
    private float initialAngularDrag;

    void Reset()
    {
        if (fluid == null)
            fluid = FindObjectOfType<MPMFluidMobile>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset; // Force lower center of mass
        initialDrag = rb.drag;
        initialAngularDrag = rb.angularDrag;
        
        if (fluid == null)
            fluid = FindObjectOfType<MPMFluidMobile>();

        if (probePoints == null || probePoints.Length == 0)
        {
            // Default to 4 corners + center if simple, or just create one
            GameObject p = new GameObject("Probe_Center");
            p.transform.parent = transform;
            p.transform.localPosition = Vector3.zero;
            probePoints = new Transform[] { p.transform };
        }

        probeData = new MPMFluidMobile.ProbeData[probePoints.Length];
        smoothedDensities = new float[probePoints.Length];
        fluidVelocities = new Vector3[probePoints.Length];
        // float3 pos, float3 vel, float density = 3+3+1 = 7 floats = 28 bytes
        probeBuf = new ComputeBuffer(probePoints.Length, 28); 

        if (wakeEmitters != null && wakeEmitters.Length > 0)
        {
            wakeSpheres = new MPMFluidMobile.HullSphere[wakeEmitters.Length];
        }
    }

    void OnDestroy()
    {
        if (probeBuf != null) probeBuf.Release();
    }

    void FixedUpdate()
    {
        if (fluid == null) return;

        // 1. Update Collider (Boat -> Fluid)
        // Pass the boat's main sphere collider to the fluid simulation
        fluid.colliderSphere = new Vector4(transform.position.x, transform.position.y, transform.position.z, radius);
        fluid.colliderVelocity = rb.velocity * velocityInteraction;

        // Update Wake Emitters (Compound Collision)
        UpdateWake();

        // 2. Prepare Probe Positions
        for (int i = 0; i < probePoints.Length; i++)
        {
            if (probePoints[i] != null)
                probeData[i].position = probePoints[i].position;
        }
        probeBuf.SetData(probeData);

        // 3. Dispatch Probe Kernel (Fluid -> Boat calculation on GPU)
        fluid.DispatchProbe(probeBuf, probePoints.Length);

        // 4. Readback (Async)
        if (!pendingReadback)
        {
            AsyncGPUReadback.Request(probeBuf, OnReadback);
            pendingReadback = true;
        }

        if (hasProbeData)
        {
            ApplyForces();
            if (enableControls) ProcessControls();
        }

        // 5. Boundary Constraint
        ClampPosition();
    }

    void ClampPosition()
    {
        if (fluid == null) return;
        
        Vector3 pos = rb.position;
        Vector3 min = fluid.boundsMin + Vector3.one * radius;
        Vector3 max = fluid.boundsMax - Vector3.one * radius;

        // Keep boat inside the water tank horizontally
        // Allow it to go above max Y (jumping), but clamp bottom
        pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        pos.z = Mathf.Clamp(pos.z, min.z, max.z);
        pos.y = Mathf.Max(pos.y, min.y); 

        if (pos != rb.position)
        {
            rb.position = pos;
            // Kill velocity if hitting wall to prevent sticking/jittering
            Vector3 vel = rb.velocity;
            if (pos.x == min.x || pos.x == max.x) vel.x = 0;
            if (pos.z == min.z || pos.z == max.z) vel.z = 0;
            if (pos.y == min.y) vel.y = 0;
            rb.velocity = vel;
        }
    }

    void OnReadback(AsyncGPUReadbackRequest req)
    {
        pendingReadback = false;
        if (req.hasError || !Application.isPlaying) return; // Safety check

        var data = req.GetData<MPMFluidMobile.ProbeData>();
        bool any = false;

        int submergedCount = 0;

        for (int i = 0; i < data.Length; i++)
        {
            MPMFluidMobile.ProbeData p = data[i];
            
            // Check if probe is valid and submerged (density > 0)
            if (p.density > 0)
            {
                submergedCount++;
                float rawDensity = Mathf.Clamp(p.density, 0f, 2000f);
                smoothedDensities[i] = Mathf.Lerp(smoothedDensities[i], rawDensity, Time.deltaTime * densitySmoothSpeed);
                fluidVelocities[i] = p.velocity;
                any = true;
            }
            else
            {
                smoothedDensities[i] = Mathf.Lerp(smoothedDensities[i], 0f, Time.deltaTime * densitySmoothSpeed * 2f);
                fluidVelocities[i] = Vector3.zero;
            }
        }

        hasProbeData = any && submergedCount > 0;
    }

    void ApplyForces()
    {
        int submergedCount = 0;
        int totalProbes = probePoints.Length;

        for (int i = 0; i < totalProbes; i++)
        {
            if (probePoints[i] == null) continue;
            float d = smoothedDensities[i];
            if (d <= 0f) continue;

            submergedCount++;
            Vector3 probePos = probePoints[i].position;

            float upwardForce = d * buoyancyCoeff;

            Vector3 boatVelAtPoint = rb.GetPointVelocity(probePos);
            if (boatVelAtPoint.y > 0)
            {
                float upDamp = 1.0f / (1.0f + boatVelAtPoint.y * 2.0f);
                upDamp = Mathf.Clamp(upDamp, 0.3f, 1.0f);
                upwardForce *= upDamp;
            }

            Vector3 buoyancy = Vector3.up * upwardForce;
            rb.AddForceAtPosition(buoyancy, probePos);

            Vector3 fluidVel = fluidVelocities[i];
            Vector3 boatVel = rb.GetPointVelocity(probePos);
            Vector3 relVel = fluidVel - boatVel;

            rb.AddForceAtPosition(relVel * dragCoeff * d * flowStrength, probePos);

            float vertRelVel = relVel.y;
            Vector3 vertDampForce = Vector3.up * vertRelVel * damping * d;
            rb.AddForceAtPosition(vertDampForce, probePos);
        }

        Vector3 vel = rb.velocity;
        if (vel.y > maxRiseSpeed) vel.y = maxRiseSpeed;
        rb.velocity = vel;

        // Stability & Damping
        float submergenceRatio = (float)submergedCount / (float)totalProbes;
        
        // Dynamic Drag: Only apply full damping when fully submerged
        // This prevents high drag when jumping out of water, reducing "hang time"
        rb.drag = Mathf.Lerp(initialDrag, initialDrag + damping, submergenceRatio);
        rb.angularDrag = Mathf.Lerp(initialAngularDrag, initialAngularDrag + damping, submergenceRatio);

        // Apply upright torque to prevent flipping
        if (submergedCount > 0)
        {
            Vector3 currentUp = transform.up;
            Vector3 targetUp = Vector3.up;
            Vector3 torqueAxis = Vector3.Cross(currentUp, targetUp);
            rb.AddTorque(torqueAxis * uprightTorque * submergenceRatio);
        }

        // Apply extra gravity proportional to how much is OUT of the water
        // If 100% out of water, apply full extraGravity. If 100% in water, apply none.
        float gravityFactor = 1.0f - submergenceRatio;
        if (gravityFactor > 0)
        {
            rb.AddForce(Vector3.down * extraGravity * gravityFactor, ForceMode.Acceleration);
        }
    }

    void UpdateWake()
    {
        if (wakeEmitters == null || wakeEmitters.Length == 0 || fluid == null) return;

        // Ensure buffer array matches emitters length (in case changed at runtime)
        if (wakeSpheres == null || wakeSpheres.Length != wakeEmitters.Length)
        {
            wakeSpheres = new MPMFluidMobile.HullSphere[wakeEmitters.Length];
        }

        int activeCount = 0;
        for(int i=0; i<wakeEmitters.Length; i++)
        {
            if(wakeEmitters[i] == null) continue;
            
            wakeSpheres[activeCount].sphere = new Vector4(
                wakeEmitters[i].position.x,
                wakeEmitters[i].position.y,
                wakeEmitters[i].position.z,
                wakeRadius
            );
            
            Vector3 pointVel = rb.GetPointVelocity(wakeEmitters[i].position);
            // Apply multiplier to boost wake effect
            wakeSpheres[activeCount].velocity = pointVel * velocityInteraction * wakeForceMultiplier;
            wakeSpheres[activeCount].padding = 0;
            
            activeCount++;
        }
        
        fluid.SetBoatSpheres(wakeSpheres, activeCount);
    }

    void ProcessControls()
    {
        // Use public variables instead of Input.GetAxis for mobile/debug flexibility
        float v = throttleInput;
        float h = steerInput;

        if (Mathf.Abs(v) > 0.01f)
        {
            rb.AddRelativeForce(Vector3.forward * v * motorPower, ForceMode.Acceleration);
        }

        if (Mathf.Abs(h) > 0.01f)
        {
            rb.AddRelativeTorque(Vector3.up * h * steerPower, ForceMode.Acceleration);
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.2f);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.TransformPoint(centerOfMassOffset), 0.2f);
        }

        if (probePoints != null)
        {
            Gizmos.color = Color.red;
            foreach(var p in probePoints)
            {
                if(p) Gizmos.DrawSphere(p.position, 0.1f);
            }
        }

        if (wakeEmitters != null)
        {
            Gizmos.color = Color.cyan;
            foreach(var w in wakeEmitters)
            {
                if(w)
                {
                    Gizmos.DrawWireSphere(w.position, wakeRadius);
                    if (Application.isPlaying && rb != null)
                    {
                        Vector3 vel = rb.GetPointVelocity(w.position) * velocityInteraction * wakeForceMultiplier;
                        Gizmos.DrawLine(w.position, w.position + vel);
                    }
                }
            }
        }
    }
}
