using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 与 MPM 耦合：在 LateUpdate 中于流体步进之后采样网格探针，避免 FixedUpdate 读到上一帧网格。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(40)]
public class FluidBoat : MonoBehaviour
{
    public MPMFluidMobile fluid;
    public SPHStandardMobile sphFluid;
    public float radius = 1.0f;
    [Tooltip("浮力增益（推荐 0.8–1.4）。旧场景里如果还是 10+ 的历史值会自动按兼容比例降到合理区间。")]
    public float buoyancyCoeff = 1.1f;
    public float dragCoeff = 2.0f;
    public Transform[] probePoints; // Assign manually or auto-generate

    [Header("Stability")]
    public Vector3 centerOfMassOffset = new Vector3(0, -1.0f, 0); // Lower center of mass = more stable
    public float uprightTorque = 50.0f; // Increased default torque
    public float damping = 2.0f; // Linear Damping
    [Tooltip("密度读回指数平滑（越大越快跟上流体，越小越稳）")]
    public float densitySmoothSpeed = 8.0f;
    [Tooltip("流体速度读回平滑，减轻网格采样抖动")]
    public float fluidVelocitySmoothSpeed = 14.0f;
    [Tooltip("相对静止密度低于该比例视为未浸没，抑制噪声")]
    [Range(0.02f, 0.35f)] public float minSubmersionDensityRatio = 0.08f;
    public float maxRiseSpeed = 3.5f;
    [Header("Anti-Takeoff")]
    [Tooltip("流体拖曳加速度上限（m/s²）。防止瞬时流场把船弹飞。")]
    [Range(1f, 20f)] public float maxHydroDragAccel = 6.5f;
    [Tooltip("向上运动时的反抬升阻尼（m/s² per m/s）。")]
    [Range(0f, 20f)] public float antiLaunchDamping = 7.5f;
    [Tooltip("随水平速度增长的下压力（m/s² per m/s）。")]
    [Range(0f, 6f)] public float speedDownforce = 1.2f;
    [Tooltip("下潜阶段允许的最大上向辅助加速度（m/s²）。")]
    [Range(0f, 6f)] public float maxUpwardAssistAccel = 1.8f;

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

    [Header("Spawn")]
    [Tooltip("启用后在启用时按当前流体边界自动摆放小船初始位置。")]
    public bool autoPlaceOnEnable = true;
    [Range(0f, 1f)] public float spawnX01 = 0.5f;
    [Range(0f, 1f)] public float spawnZ01 = 0.5f;
    public float spawnHeightOffset = 0.12f;

    [Header("Input (Debug/Script)")]
    [Range(-1f, 1f)] public float throttleInput = 0f;
    [Range(-1f, 1f)] public float steerInput = 0f;

    private Rigidbody rb;
    private ComputeBuffer probeBuf;
    private MPMFluidMobile.ProbeData[] probeData;
    private float[] smoothedDensities;
    private Vector3[] fluidVelocities;
    private MPMFluidMobile.HullSphere[] wakeSpheres; // Buffer array for wake emitters
    private SPHStandardMobile.BoatSphere[] sphWakeSpheres;
    private bool pendingReadback = false;
    private bool hasProbeData = false;
    private float initialDrag;
    private float initialAngularDrag;
    private float lastProbeSmoothTime = -1f;
    [Header("Reset Stabilization")]
    [Range(0, 60)] public int settleFramesAfterReset = 16;
    // 首次启动需要给流体足够时间沉降到稳态水位。
    // MPM/SPH 粒子从 spawn 顶部落到底部 ~1s，必须足够长，否则船会撞到尚未沉降的水面 → 抽搐。
    [Range(0, 240)] public int settleFramesOnStart = 60;
    [Tooltip("首启/重置后耦合力软启动时间（秒），用于抑制第一批帧抽搐。")]
    [Range(0f, 2.0f)] public float couplingRampSeconds = 0.35f;
    int settleFramesLeft = 0;
    float couplingRamp01 = 0f;
    bool lastSettlingState = false;
    bool cachedUseGravity = true;
    Renderer[] cachedRenderers;
    bool boatVisible = true;

    void Reset()
    {
        if (fluid == null)
            fluid = FindObjectOfType<MPMFluidMobile>();
        if (sphFluid == null)
            sphFluid = FindObjectOfType<SPHStandardMobile>();
    }

    void Awake()
    {
        InitializeBoat();
    }

    void OnEnable()
    {
        settleFramesLeft = Mathf.Max(settleFramesAfterReset, settleFramesOnStart);
        couplingRamp01 = 0f;
        hasProbeData = false;
        // 提前缓存重力设置，避免 settle 切换时把 false（已被关掉）误存为「原始值」。
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null) cachedUseGravity = rb.useGravity;
        lastSettlingState = false;
        SetBoatVisible(settleFramesLeft <= 0);
        if (autoPlaceOnEnable) SnapToActiveFluidStart(true);
    }

    public void NotifyFluidReset()
    {
        hasProbeData = false;
        pendingReadback = false;
        lastProbeSmoothTime = -1f;
        settleFramesLeft = Mathf.Max(0, settleFramesAfterReset);
        couplingRamp01 = 0f;
        SetBoatVisible(settleFramesLeft <= 0);
        // 重置时若刚体当前为「settle 中（gravity 已关）」状态，不能把 false 误当作原值：
        // 仅在不在 settle 中时刷新缓存。
        if (rb != null && !lastSettlingState) cachedUseGravity = rb.useGravity;
        if (smoothedDensities != null)
        {
            for (int i = 0; i < smoothedDensities.Length; i++) smoothedDensities[i] = 0f;
        }
        if (fluidVelocities != null)
        {
            for (int i = 0; i < fluidVelocities.Length; i++) fluidVelocities[i] = Vector3.zero;
        }
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (UseMpm() && fluid != null)
        {
            fluid.colliderVelocity = Vector3.zero;
            fluid.SetBoatSpheres(null, 0);
        }
        if (UseSph() && sphFluid != null)
        {
            sphFluid.SetBoatSpheres(null, 0);
        }
        SnapToActiveFluidStart(true);
    }

    void InitializeBoat()
    {
        if (probeBuf != null)
            return;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass = centerOfMassOffset;
            initialDrag = rb.drag;
            initialAngularDrag = rb.angularDrag;
        }

        if (fluid == null)
            fluid = FindObjectOfType<MPMFluidMobile>();
        if (sphFluid == null)
            sphFluid = FindObjectOfType<SPHStandardMobile>();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        if (probePoints == null || probePoints.Length == 0)
        {
            var p = new GameObject("Probe_Center");
            p.transform.SetParent(transform, false);
            p.transform.localPosition = Vector3.zero;
            probePoints = new Transform[] { p.transform };
        }

        probeData = new MPMFluidMobile.ProbeData[probePoints.Length];
        smoothedDensities = new float[probePoints.Length];
        fluidVelocities = new Vector3[probePoints.Length];
        probeBuf = new ComputeBuffer(probePoints.Length, 28);

        if (wakeEmitters != null && wakeEmitters.Length > 0)
        {
            wakeSpheres = new MPMFluidMobile.HullSphere[wakeEmitters.Length];
            sphWakeSpheres = new SPHStandardMobile.BoatSphere[wakeEmitters.Length + 1];
        }
    }

    void OnDestroy()
    {
        if (probeBuf != null) probeBuf.Release();
        if (sphFluid != null) sphFluid.SetBoatSpheres(null, 0);
    }

    void FixedUpdate()
    {
        if (probeBuf == null || probeData == null || probePoints == null || rb == null)
            InitializeBoat();
        if (probeBuf == null || probeData == null || probePoints == null || rb == null)
            return;

        // settle 期间冻结刚体（屏蔽重力 + 碰撞），等流体沉降到稳态再激活物理。
        // 这样船不会在水位还没就位时就高速下落 → 撞水 → 抽搐。
        bool nowSettling = settleFramesLeft > 0;
        if (nowSettling != lastSettlingState)
        {
            if (nowSettling)
            {
                cachedUseGravity = rb.useGravity;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                rb.useGravity = cachedUseGravity;
            }
            lastSettlingState = nowSettling;
        }
        if (nowSettling)
        {
            // 保持静止；继续推 collider 位置但 velocity 为 0，避免被流体扰动。
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (settleFramesLeft > 0) settleFramesLeft--;
        SetBoatVisible(settleFramesLeft <= 0);
        float couplingW = EvaluateCouplingWeight();

        bool useMpm = UseMpm();
        bool useSph = !useMpm && UseSph();
        if (!useMpm && !useSph) return;
        if (useMpm && !fluid.runSimulation) return;
        if (useSph && !sphFluid.runSimulation) return;

        // 1. Boat -> Fluid：本帧刚体状态推给 MPM（在 LateUpdate 中流体会用这些数据步进）
        // settle 期间不把 collider/wake 推给流体，避免冷启动时下落粒子撞到「冻结」的船。
        if (!nowSettling)
        {
            if (useMpm)
            {
                Vector3 colliderPos = rb.position;
                fluid.colliderSphere = new Vector4(colliderPos.x, colliderPos.y, colliderPos.z, radius);
                fluid.colliderVelocity = rb.velocity * velocityInteraction;
            }
            UpdateWake();
        }
        else
        {
            if (useMpm)
            {
                fluid.colliderSphere = Vector4.zero;
                fluid.colliderVelocity = Vector3.zero;
            }
            if (useSph) sphFluid.SetBoatSpheres(null, 0);
        }

        // 探针调度改到 LateUpdate，见 SampleFluidProbesLate()

        if (hasProbeData && couplingW > 1e-4f)
        {
            ApplyForces(couplingW);
            if (enableControls) ProcessControls();
        }

        ClampPosition();
    }

    void LateUpdate()
    {
        SampleFluidProbesLate();
    }

    void SampleFluidProbesLate()
    {
        if (probeBuf == null || probeData == null || probePoints == null || rb == null)
            return;
        bool useMpm = UseMpm();
        bool useSph = !useMpm && UseSph();
        if (!useMpm && !useSph) return;
        if (settleFramesLeft > 0) return;

        if (useMpm)
        {
            if (!fluid.runSimulation || !fluid.isActiveAndEnabled) return;
            for (int i = 0; i < probePoints.Length; i++)
            {
                if (probePoints[i] != null)
                    probeData[i].position = probePoints[i].position;
            }
            probeBuf.SetData(probeData);

            fluid.DispatchProbe(probeBuf, probePoints.Length);

            if (!pendingReadback)
            {
                AsyncGPUReadback.Request(probeBuf, OnReadback);
                pendingReadback = true;
            }
            return;
        }

        if (useSph)
        {
            SampleSphProbes();
        }
    }

    void SampleSphProbes()
    {
        if (sphFluid == null) return;
        float restRho = CurrentRestDensity();
        float dtSmooth = Mathf.Clamp(Time.deltaTime, 0.001f, 0.08f);
        float aRho = ExpSmoothAlpha(densitySmoothSpeed, dtSmooth);
        float aVel = ExpSmoothAlpha(fluidVelocitySmoothSpeed, dtSmooth);
        int submergedCount = 0;
        float sampleRadius = Mathf.Max(radius * 1.8f, 0.45f);

        for (int i = 0; i < probePoints.Length; i++)
        {
            if (probePoints[i] == null) continue;
            Vector3 pp = probePoints[i].position;
            Vector3 flow;
            float wl;
            bool gotFlow = sphFluid.TryGetLocalFlowCached(pp, sampleRadius, out flow);
            bool gotLevel = sphFluid.TryGetLocalWaterLevelCached(pp, sampleRadius, out wl);
            float depth = gotLevel ? (wl - pp.y) : -radius;
            float sub = Mathf.Clamp01((depth + radius) / Mathf.Max(0.001f, radius * 2f));
            float targetRho = restRho * sub;
            smoothedDensities[i] = Mathf.Lerp(smoothedDensities[i], targetRho, aRho);
            fluidVelocities[i] = Vector3.Lerp(fluidVelocities[i], gotFlow ? flow : Vector3.zero, aVel);
            if (smoothedDensities[i] >= restRho * minSubmersionDensityRatio) submergedCount++;
        }
        hasProbeData = submergedCount > 0;
    }

    static float ExpSmoothAlpha(float speed, float dt)
    {
        if (speed <= 0f || dt <= 0f) return 1f;
        return 1f - Mathf.Exp(-speed * dt);
    }

    float EvaluateBuoyancyGain()
    {
        // 兼容历史场景：早期默认值是 15（且公式里乘了密度），在新公式下会导致“起飞”。
        // 如果用户场景里仍保留 10+ 的老值，按 15->1 的比例自动映射。
        float k = buoyancyCoeff;
        if (k > 5f) k *= (1f / 15f);
        return Mathf.Clamp(k, 0.2f, 2.2f);
    }

    void SetBoatVisible(bool visible)
    {
        if (boatVisible == visible) return;
        boatVisible = visible;
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedRenderers == null) return;
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null) cachedRenderers[i].enabled = visible;
        }
    }

    float EvaluateCouplingWeight()
    {
        if (settleFramesLeft > 0) return 0f;
        if (couplingRampSeconds <= 1e-4f) return 1f;
        couplingRamp01 = Mathf.Clamp01(couplingRamp01 + Time.fixedDeltaTime / couplingRampSeconds);
        // smoothstep(0,1,t)
        return couplingRamp01 * couplingRamp01 * (3f - 2f * couplingRamp01);
    }

    bool UseMpm()
    {
        return fluid != null && fluid.isActiveAndEnabled && fluid.gameObject.activeInHierarchy;
    }

    bool UseSph()
    {
        return sphFluid != null && sphFluid.isActiveAndEnabled && sphFluid.gameObject.activeInHierarchy;
    }

    public void SnapToActiveFluidStart(bool resetVelocity)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 bmin;
        Vector3 bmax;
        Vector3 smin;
        Vector3 smax;
        if (UseMpm())
        {
            bmin = fluid.boundsMin;
            bmax = fluid.boundsMax;
            smin = fluid.spawnMin;
            smax = fluid.spawnMax;
        }
        else if (UseSph())
        {
            bmin = sphFluid.boundsMin;
            bmax = sphFluid.boundsMax;
            smin = sphFluid.spawnMin;
            smax = sphFluid.spawnMax;
        }
        else return;

        if (smax.x <= smin.x || smax.y <= smin.y || smax.z <= smin.z)
        {
            smin = bmin;
            smax = bmax;
        }

        float px = Mathf.Lerp(smin.x, smax.x, Mathf.Clamp01(spawnX01));
        float pz = Mathf.Lerp(smin.z, smax.z, Mathf.Clamp01(spawnZ01));

        // 关键修复：估算流体「稳态水位」并把船放到那个高度，避免船从 spawn 顶部
        // 高速自由落体冲击未完全沉降的水面 → 第一次「抽搐」。
        // V_spawn 是流体总体积，A_floor 是容器底面积，settledY ≈ bmin.y + V_spawn / A_floor。
        Vector3 spawnSize = smax - smin;
        Vector3 boundsSize = bmax - bmin;
        float spawnVolume = Mathf.Max(1e-3f, spawnSize.x * spawnSize.y * spawnSize.z);
        float floorArea = Mathf.Max(1e-3f, boundsSize.x * boundsSize.z);
        float settledY = bmin.y + spawnVolume / floorArea;
        // 船吃水：把船心置于水面之上半径 0.45 处，保留小幅落差让它自然「坐」到水面
        float py = settledY + radius * 0.45f + spawnHeightOffset;
        py = Mathf.Clamp(py, bmin.y + radius * 1.05f, bmax.y - radius * 0.3f);
        Vector3 pos = new Vector3(px, py, pz);

        rb.position = pos;
        transform.position = pos;
        if (resetVelocity)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    float CurrentRestDensity()
    {
        if (UseMpm()) return Mathf.Max(fluid.restDensity, 1f);
        if (UseSph()) return Mathf.Max(sphFluid.restDensity, 1f);
        return 1000f;
    }

    void ClampPosition()
    {
        Vector3 bmin;
        Vector3 bmax;
        if (UseMpm())
        {
            bmin = fluid.boundsMin;
            bmax = fluid.boundsMax;
        }
        else if (UseSph())
        {
            bmin = sphFluid.boundsMin;
            bmax = sphFluid.boundsMax;
        }
        else return;
        
        Vector3 pos = rb.position;
        Vector3 min = bmin + Vector3.one * radius;
        Vector3 max = bmax - Vector3.one * radius;

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
        if (req.hasError || !Application.isPlaying) return;

        float now = Time.time;
        float dtSmooth = (lastProbeSmoothTime < 0f) ? Time.fixedDeltaTime : Mathf.Clamp(now - lastProbeSmoothTime, 0.001f, 0.12f);
        lastProbeSmoothTime = now;

        float aRho = ExpSmoothAlpha(densitySmoothSpeed, dtSmooth);
        float aVel = ExpSmoothAlpha(fluidVelocitySmoothSpeed, dtSmooth);
        float restRho = CurrentRestDensity();
        float submersionThreshold = restRho * minSubmersionDensityRatio;

        var data = req.GetData<MPMFluidMobile.ProbeData>();
        int submergedCount = 0;

        for (int i = 0; i < data.Length; i++)
        {
            MPMFluidMobile.ProbeData p = data[i];

            if (p.density >= submersionThreshold)
            {
                submergedCount++;
                float rawDensity = Mathf.Clamp(p.density, 0f, restRho * 2.5f);
                smoothedDensities[i] = Mathf.Lerp(smoothedDensities[i], rawDensity, aRho);
                fluidVelocities[i] = Vector3.Lerp(fluidVelocities[i], p.velocity, aVel);
            }
            else
            {
                smoothedDensities[i] = Mathf.Lerp(smoothedDensities[i], 0f, ExpSmoothAlpha(densitySmoothSpeed * 1.5f, dtSmooth));
                fluidVelocities[i] = Vector3.Lerp(fluidVelocities[i], Vector3.zero, aVel);
            }
        }

        hasProbeData = submergedCount > 0;
    }

    void ApplyForces(float couplingW)
    {
        bool sphMode = !UseMpm() && UseSph();
        int submergedCount = 0;
        int totalProbes = probePoints.Length;
        float gravityMag = Mathf.Max(1f, Mathf.Abs(Physics.gravity.y));
        float buoyancyGain = EvaluateBuoyancyGain();
        float probeNorm = 1f / Mathf.Max(1, totalProbes);

        for (int i = 0; i < totalProbes; i++)
        {
            if (probePoints[i] == null) continue;
            float d = smoothedDensities[i];
            if (d <= 0f) continue;

            submergedCount++;
            Vector3 probePos = probePoints[i].position;

            float restR = CurrentRestDensity();
            float share = Mathf.Clamp(d / restR, 0f, 1.35f);
            // 使用加速度模式并按探针数归一，避免密度量纲与探针数量把浮力放大到“起飞”。
            float upwardAcc = gravityMag * buoyancyGain * share * probeNorm;

            Vector3 boatVelAtPoint = rb.GetPointVelocity(probePos);
            if (boatVelAtPoint.y > 0)
            {
                float upDamp = 1.0f / (1.0f + boatVelAtPoint.y * 2.0f);
                upDamp = Mathf.Clamp(upDamp, 0.3f, 1.0f);
                upwardAcc *= upDamp;
            }

            Vector3 buoyancy = Vector3.up * upwardAcc;
            rb.AddForceAtPosition(buoyancy * couplingW, probePos, ForceMode.Acceleration);

            Vector3 fluidVel = fluidVelocities[i];
            Vector3 boatVel = rb.GetPointVelocity(probePos);
            Vector3 relVel = fluidVel - boatVel;
            float dragScale = sphMode ? 0.38f : 1f;
            // 统一改为“加速度”模型并限幅，避免 d(密度)量纲把力放大造成起飞。
            Vector3 dragAcc = relVel * dragCoeff * flowStrength * dragScale * share;
            float dragCap = Mathf.Max(1f, maxHydroDragAccel) * (sphMode ? 0.95f : 1.1f);
            if (dragAcc.magnitude > dragCap)
                dragAcc = dragAcc.normalized * dragCap;
            rb.AddForceAtPosition(dragAcc * couplingW, probePos, ForceMode.Acceleration);

            // 反起飞：上升时施加强阻尼；下潜时仅给小幅上向辅助，避免“蹦床”感。
            float upVel = boatVel.y;
            float antiLaunchAcc = (upVel > 0f)
                ? (-upVel * antiLaunchDamping * (0.35f + 0.65f * share))
                : (-upVel * antiLaunchDamping * 0.10f);
            antiLaunchAcc = Mathf.Clamp(antiLaunchAcc, -14f, Mathf.Max(0f, maxUpwardAssistAccel));
            rb.AddForceAtPosition(Vector3.up * antiLaunchAcc * couplingW, probePos, ForceMode.Acceleration);
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

        // 高速滑行时给一点下压力，抑制“贴浪起飞”（MPM/SPH 通用）。
        float speedXZ = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        float dynDownAcc = speedXZ * Mathf.Max(0f, speedDownforce) * submergenceRatio;
        if (dynDownAcc > 0.001f)
            rb.AddForce(Vector3.down * dynDownAcc, ForceMode.Acceleration);

        // Apply upright torque to prevent flipping
        if (submergedCount > 0)
        {
            Vector3 currentUp = transform.up;
            Vector3 targetUp = Vector3.up;
            Vector3 torqueAxis = Vector3.Cross(currentUp, targetUp);
            rb.AddTorque(torqueAxis * uprightTorque * submergenceRatio * couplingW);
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
        bool useMpm = UseMpm();
        bool useSph = !useMpm && UseSph();
        if (!useMpm && !useSph) return;
        if (wakeEmitters == null || wakeEmitters.Length == 0)
        {
            if (useMpm) fluid.SetBoatSpheres(null, 0);
            if (useSph && sphWakeSpheres != null && sphWakeSpheres.Length > 0)
            {
                Vector3 bp = rb.position;
                sphWakeSpheres[0].sphere = new Vector4(bp.x, bp.y, bp.z, Mathf.Max(radius * 0.92f, wakeRadius));
                sphWakeSpheres[0].velocity = rb.velocity * velocityInteraction;
                sphWakeSpheres[0].padding = 0;
                sphFluid.SetBoatSpheres(sphWakeSpheres, 1);
            }
            return;
        }

        // Ensure buffer array matches emitters length (in case changed at runtime)
        if (wakeEmitters != null && (wakeSpheres == null || wakeSpheres.Length != wakeEmitters.Length))
        {
            wakeSpheres = new MPMFluidMobile.HullSphere[wakeEmitters.Length];
        }
        if (wakeEmitters != null && (sphWakeSpheres == null || sphWakeSpheres.Length < wakeEmitters.Length + 1))
        {
            sphWakeSpheres = new SPHStandardMobile.BoatSphere[wakeEmitters.Length + 1];
        }

        int activeCount = 0;
        if (wakeEmitters != null)
        {
            for(int i=0; i<wakeEmitters.Length; i++)
            {
                if(wakeEmitters[i] == null) continue;
                Vector3 ep = wakeEmitters[i].position;
                Vector3 pointVel = rb.GetPointVelocity(ep) * velocityInteraction * wakeForceMultiplier;
                if (useMpm)
                {
                    wakeSpheres[activeCount].sphere = new Vector4(ep.x, ep.y, ep.z, wakeRadius);
                    wakeSpheres[activeCount].velocity = pointVel;
                    wakeSpheres[activeCount].padding = 0;
                }
                if (useSph)
                {
                    sphWakeSpheres[activeCount + 1].sphere = new Vector4(ep.x, ep.y, ep.z, wakeRadius);
                    sphWakeSpheres[activeCount + 1].velocity = pointVel;
                    sphWakeSpheres[activeCount + 1].padding = 0;
                }
                activeCount++;
            }
        }
        if (useMpm)
        {
            fluid.SetBoatSpheres(wakeSpheres, activeCount);
        }
        else if (useSph)
        {
            // SPH 下先只喂主船体球，避免尾流自激导致船体抽搐。
            Vector3 bp = rb.position;
            sphWakeSpheres[0].sphere = new Vector4(bp.x, bp.y, bp.z, Mathf.Max(radius * 0.92f, wakeRadius));
            sphWakeSpheres[0].velocity = rb.velocity * velocityInteraction;
            sphWakeSpheres[0].padding = 0;
            sphFluid.SetBoatSpheres(sphWakeSpheres, 1);
        }
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
