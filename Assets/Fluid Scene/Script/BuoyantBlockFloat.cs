using UnityEngine;

public class BuoyantBlockFloat : MonoBehaviour
{
    public SPHStandardMobile fluid;
    public Vector3 size = new Vector3(0.8f, 0.5f, 0.8f);
    public float density = 120f;
    public float buoyancyCoeff = 0f;
    public int sampleStride = 3;
    public bool useDynamicWaterLevel = true;
    public float waterPresenceRadius = 1.2f;
    public int maxLocalSamples = 64;
    public bool requireLocalPresence = true;
    public bool enableHydroDrag = true;
    public float dragCd = 1.1f;
    public float lateralDragScale = 1.6f;
    public float flowSampleRadius = 1.6f;
    public float forceDistributionRadius = 0.4f;
    public int sampleFrameStride = 2;
    int lastSampleFrame = -999;
    float lastWaterLevel = 0f;
    bool lastHasLocal = false;
    Vector3 lastFlow = Vector3.zero;
    public bool autoNeutralPlacement = true;
    public float buoyancyScale = 1.3f;
    public bool fallbackToGlobalWaterLevel = true;
    public bool fallbackFlowWhenZero = true;
    public float warmupTime = 0.8f;
    float awakeTime = 0f;
    float smoothedWaterLevel = 0f;
    bool smoothedInit = false;
    
    public float surfaceSpringK = 600f;
    public float surfaceDampingC = 95f;

    Rigidbody rb;
    SphereCollider sc;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        float volume = Mathf.Max(1e-6f, size.x * size.y * size.z);
        rb.mass = Mathf.Max(0.02f, density * volume);
        rb.useGravity = true;
        rb.drag = 1.2f;
        rb.angularDrag = 0.6f;
        awakeTime = Time.time;
        sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = false;
        sc.radius = Mathf.Max(size.x, size.z) * 0.5f;
        transform.localScale = size;
        if (fluid != null && autoNeutralPlacement)
        {
            float waterY = (fluid.spawnMin.y + fluid.spawnMax.y) * 0.5f;
            float neutralY = waterY - size.y * (density / Mathf.Max(fluid.restDensity, 1e-3f)) + size.y * 0.5f;
            Vector3 p = transform.position;
            p.y = Mathf.Clamp(neutralY, fluid.boundsMin.y + size.y * 0.5f + 0.02f, fluid.boundsMax.y - size.y * 0.5f - 0.02f);
            transform.position = p;
        }
        var dup = FindObjectsOfType<BuoyantBlockFloat>();
        if (dup != null && dup.Length > 1)
        {
            var keep = dup[0];
            if (keep != this)
            {
                Destroy(gameObject);
                return;
            }
            for (int i = 1; i < dup.Length; i++)
            {
                if (dup[i] != keep && dup[i] != null && dup[i].gameObject != null)
                {
                    Destroy(dup[i].gameObject);
                }
            }
        }
        if (fluid != null)
        {
            var list = new System.Collections.Generic.List<Transform>();
            if (fluid.obstacleTransforms != null) list.AddRange(fluid.obstacleTransforms);
            if (!list.Contains(transform))
            {
                list.Add(transform);
                fluid.obstacleTransforms = list.ToArray();
            }
        }
    }

    void Update()
    {
        if (fluid == null || rb == null) return;
        float waterLevel = (fluid.spawnMin.y + fluid.spawnMax.y) * 0.5f;
        int f = Time.frameCount;
        if ((f - lastSampleFrame) >= Mathf.Max(1, sampleFrameStride))
        {
            float wl;
            lastHasLocal = false;
            if (useDynamicWaterLevel)
            {
                if (requireLocalPresence)
                {
                    lastHasLocal = fluid.TryGetLocalWaterLevelCached(transform.position, Mathf.Max(waterPresenceRadius, 0.2f), out wl);
                    if (lastHasLocal) lastWaterLevel = wl;
                    else if (fallbackToGlobalWaterLevel)
                    {
                        if (fluid.TryGetWaterLevel(out wl, Mathf.Min(2048, fluid.particleCount), 0))
                        {
                            lastWaterLevel = wl;
                            lastHasLocal = true;
                        }
                        else
                        {
                            lastWaterLevel = (fluid.spawnMin.y + fluid.spawnMax.y) * 0.5f;
                            lastHasLocal = true;
                        }
                    }
                }
                else if (fluid.TryGetWaterLevel(out wl, Mathf.Min(2048, fluid.particleCount), 0))
                {
                    lastWaterLevel = wl;
                    lastHasLocal = true;
                }
                
            }
            if (enableHydroDrag)
            {
                Vector3 flow;
                bool gotFlow = fluid.TryGetLocalFlowCached(transform.position, Mathf.Max(flowSampleRadius, 0.4f), out flow);
                if (!gotFlow && fallbackFlowWhenZero)
                {
                    gotFlow = fluid.TryGetLocalFlow(transform.position, Mathf.Max(flowSampleRadius * 1.25f, 0.5f), Mathf.Min(256, fluid.particleCount), out flow);
                }
                if (gotFlow) lastFlow = Vector3.Lerp(lastFlow, flow, 0.5f);
                else lastFlow *= 0.9f;
                
            }
            lastSampleFrame = f;
        }
        if (lastHasLocal)
        {
            if (!smoothedInit) { smoothedWaterLevel = lastWaterLevel; smoothedInit = true; }
            else smoothedWaterLevel = Mathf.Lerp(smoothedWaterLevel, lastWaterLevel, 0.35f);
            waterLevel = smoothedWaterLevel;
        }
        float halfH = size.y * 0.5f;
        // 分布式浮力：在底面中心与四角采样局部水位，分别施加浮力与扭矩
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(0f, -halfH, 0f),
            new Vector3( size.x * 0.5f, -halfH,  size.z * 0.5f),
            new Vector3(-size.x * 0.5f, -halfH,  size.z * 0.5f),
            new Vector3( size.x * 0.5f, -halfH, -size.z * 0.5f),
            new Vector3(-size.x * 0.5f, -halfH, -size.z * 0.5f),
        };
        float volume = Mathf.Max(1e-6f, size.x * size.y * size.z);
        float coeff = buoyancyCoeff > 0f ? buoyancyCoeff : (fluid.restDensity * 9.81f);
        float ramp = warmupTime > 0f ? Mathf.Clamp01((Time.time - awakeTime) / Mathf.Max(warmupTime, 1e-3f)) : 1f;
        if (lastHasLocal)
        {
            float perWeight = 1f / offsets.Length;
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(offsets[i]);
                float wlLocal = lastWaterLevel;
                float subDepth = Mathf.Clamp(wlLocal - worldPos.y, 0f, size.y);
                float subFrac = subDepth / Mathf.Max(size.y, 1e-6f);
                float displaced = volume * subFrac * perWeight;
                float scale = Mathf.Max(0.5f, buoyancyScale);
                Vector3 fUp = Vector3.up * (coeff * displaced * scale * ramp);
                rb.AddForceAtPosition(fUp, worldPos);
            }
        }
        
        // 水流冲击：基于局部平均流速的阻力
        if (enableHydroDrag)
        {
            Vector3 flow = lastFlow;
            if (flow != Vector3.zero)
            {
                Vector3 vrel = flow - rb.velocity;
                Vector3 vhor = new Vector3(vrel.x, 0f, vrel.z);
                float rho = fluid.restDensity;
                float Ah = size.y * Mathf.Max(size.x, size.z);
                float Av = size.x * size.z;
                Vector3 Fh = vhor.sqrMagnitude > 1e-6f ? vhor.normalized * (0.5f * rho * dragCd * Ah * vhor.magnitude * vhor.magnitude) * lateralDragScale : Vector3.zero;
                Vector3 Fy = Mathf.Abs(vrel.y) > 1e-6f ? Vector3.up * Mathf.Sign(vrel.y) * (0.5f * rho * dragCd * Av * vrel.y * vrel.y) : Vector3.zero;
                Vector3[] forcePts = new Vector3[]
                {
                    transform.TransformPoint(new Vector3( forceDistributionRadius, 0f, 0f)),
                    transform.TransformPoint(new Vector3(-forceDistributionRadius, 0f, 0f)),
                    transform.TransformPoint(new Vector3(0f, 0f,  forceDistributionRadius)),
                    transform.TransformPoint(new Vector3(0f, 0f, -forceDistributionRadius)),
                };
                Vector3 F = (Fh + Fy) * ramp;
                float w = 1f / forcePts.Length;
                for (int i = 0; i < forcePts.Length; i++)
                {
                    rb.AddForceAtPosition(F * w, forcePts[i]);
                }
            }
        }
        if (lastHasLocal)
        {
            float subFracEq = Mathf.Clamp(density / Mathf.Max(fluid.restDensity, 1e-3f), 0f, 1f);
            float targetY = waterLevel - size.y * subFracEq + halfH;
            float vy = rb.velocity.y;
            float fy = (surfaceSpringK * (targetY - transform.position.y) - surfaceDampingC * vy) * ramp;
            rb.AddForce(new Vector3(0f, fy, 0f));
        }
        var v = rb.velocity;
        v.y = Mathf.Clamp(v.y, -6f, 6f);
        rb.velocity = v;
        Vector3 p = transform.position;
        Vector3 bmin = fluid.boundsMin;
        Vector3 bmax = fluid.boundsMax;
        p = new Vector3(
            Mathf.Clamp(p.x, bmin.x, bmax.x),
            Mathf.Clamp(p.y, bmin.y, bmax.y),
            Mathf.Clamp(p.z, bmin.z, bmax.z)
        );
        transform.position = p;
    }
}
