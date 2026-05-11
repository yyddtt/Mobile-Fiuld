using UnityEngine;
using UnityEngine.Rendering;

public class SPHStandardMobile : MonoBehaviour
{
    public const int MaxParticleCountMobile = 10000;
    public const int MinParticleCount = 256;

    public enum RenderMode { Fluid, GridParticles }
    [Header("Simulation")]
    [Range(256, 10000)]
    public int particleCount = 8000;
    public Vector3 boundsMin = new Vector3(0,0,0);
    public Vector3 boundsMax = new Vector3(15,10,5);
    [HideInInspector] public int gridResolution = 32;
    public float neighbourRadius = 0.35f;
    [HideInInspector] public float particleMass = 1.0f;
    public float restDensity = 1000.0f;
    [Tooltip("Mueller 粘性系数；标准移动端 SPH 取 0.020–0.040 即为轻盈水，>0.06 开始发糖浆。")]
    public float viscosity = 0.025f;
    [Tooltip("Tait EOS 的 γ：1=线性 EOS（移动端最稳定，几乎不沸腾）；7=硬水（更不可压，但易沸腾）。建议保持 1.")]
    public float eosGamma = 1.0f;
    [Tooltip("声速 c。EOS 刚度 K = ρ₀·c²/γ；过大刚度大易抖，过小则水太软可压缩明显。建议 28–40。")]
    public float soundSpeed = 32.0f;
    public Vector3 gravity = new Vector3(0,-9.8f,0);
    [HideInInspector] public float maxSpeed = 14.0f;
    [Tooltip("反射边界能量保留比例；过低会显「贴墙吸住」。建议 0.45–0.65。")]
    [HideInInspector] public float boundaryDamping = 0.55f;
    [HideInInspector] public float boundaryDampingZ = 0.55f;
    [HideInInspector] public float boundaryMaxBounceSpeedZ = 3.5f;
    [Tooltip("XSPH 速度平滑系数；0.04–0.08 平滑掉 SPH 经典的高频「沸腾」抖动而不至发胶。")]
    [HideInInspector] public float xsphC = 0.05f;
    [Tooltip("SSFR 椭球基准半径（世界单位）。SPH 排列较规则，可略小于 MPM；过大则屏上 splat 显胖、细节糊成一片。建议约 0.08–0.11。")]
    public float particleSize = 0.102f;
    public bool runSimulation = true;
    [HideInInspector] public bool enableLowParticleCountTuning = false;
    [Tooltip("速度上限（标准移动端 SPH 不做休眠）；保留字段以兼容老 inspector。")]
    public float minSpeed = 0.0f;
    
    [Tooltip("初始粒子生成盒最小角，坐标必须在 boundsMin～boundsMax 内；若越界会被钳制，x 偏小且 boundsMin.x 较大时会整团贴在左墙。")]
    public Vector3 spawnMin = new Vector3(4,2,1);
    [Tooltip("初始粒子生成盒最大角，须大于 spawnMin 且落在边界盒内。")]
    public Vector3 spawnMax = new Vector3(10,6,4);
    [HideInInspector] public bool autoCalibrateMass = true;
    [Tooltip("初始粒子质量缩放。<1 可抑制开局过压导致的瞬时膨胀；建议 0.93–1.00。")]
    [Range(0.85f, 1.15f)] public float initialMassScale = 0.96f;
    [HideInInspector] public bool autoNeighbourRadius = true;
    [HideInInspector] public float initialJitter = 0.02f;
    public bool enableSubstepping = true;
    public float fixedTimeStep = 0.004f;
    public int maxSubsteps = 8;
    public RenderMode renderMode = RenderMode.Fluid;
    [HideInInspector] public bool simulateInLateUpdate = true;
    [Header("Physics Preset")]
    public bool realisticWaterPreset = true;
    public bool sweepFlowPreset = true;
    [Header("Adaptive/Power")]
    [HideInInspector] public int targetFrameRate = 60;
    public enum MobileQualityProfile { Auto, Performance, Balanced, Quality }
    public MobileQualityProfile mobileQualityProfile = MobileQualityProfile.Balanced;
    public bool adaptiveQuality = false;
    [Range(30f, 120f)] public float qualityDownshiftFps = 50f;
    [Range(30f, 120f)] public float qualityUpshiftFps = 58f;
    [Range(10, 240)] public int qualityCheckIntervalFrames = 20;
    [Range(30, 600)] public int qualityCooldownFrames = 120;
    [Header("Camera")]
    public bool autoCameraClipTuning = true;
    public float clipMargin = 5f;

    [Header("Rendering (SSFR, aligned with MPM)")]
    public bool enableRendering = true;
    public bool renderParticles = false;
    public bool showDepthDebug = false;
    public bool showThicknessDebug = false;
    public bool showNormalDebug = false;
    [Tooltip("传给 SSFR/FluidComposite 的 _Color，与同场景 MPMFluidMobile 的 fluidColor 作用相同。")]
    public Color waterTint = new Color(0.84339625f, 0.9272471f, 1f, 1f);
    [Range(0f, 5f)] public float absorption = 1.45f;
    [Range(0f, 1f)] public float smoothness = 0.94f;
    [Range(0f, 1f)] public float specular = 0.56f;
    [Tooltip("FluidComposite 中从厚度里减去的偏置；过大等价于整体裁掉薄区域（易觉得被裁剪、浪尖消失）。MPM 常用约 0.05–0.08。")]
    [Range(0f, 0.5f)] public float thicknessCutoff = 0.065f;
    [Range(0f, 0.2f)] public float refractionStrength = 0.02f;
    [Header("Thickness")]
    [Range(0.01f, 0.5f)] public float thicknessContribution = 0.052f;
    [Range(0, 5)] public int thicknessBlurIterations = 1;
    [Range(1, 20)] public int thicknessBlurRadius = 5;
    [Range(1, 5)] public int thicknessDownsample = 2;
    [Header("Normals")]
    [Tooltip("传给 SSFR/FluidNormals；过大易显颗粒波纹，略低更「整片水面」。")]
    [Range(0.1f, 10f)] public float normalStrength = 0.86f;
    [Header("Anisotropy (grid particles & fluid splats)")]
    [Tooltip("乘在 particleSize 上（着色器内 splat 尺寸）。MPM 常需 1.4+ 填洞；SPH 可降到约 1.15–1.28 以保留轮廓细节。")]
    [Range(1f, 2f)] public float renderParticleScale = 1.22f;
    [Range(0f, 5f)] public float anisotropyScale = 0.26f;
    [Range(1f, 10f)] public float maxAnisotropy = 2.8f;
    [Header("Depth filtering")]
    public DepthFilterType filterType = DepthFilterType.Gaussian;
    public enum DepthFilterType { Bilateral, Gaussian }
    [Tooltip("深度 RT 目标高度；略提高可减轻锯齿（SPH 可略锐于 MPM）。0 则仅用 depthDownsample。")]
    public int targetDepthHeight = 520;
    [Range(1, 4)] public int depthDownsample = 2;
    [Range(0, 10)] public int blurIterations = 1;
    [Range(0.1f, 50f)] public float blurSigmaSpatial = 5.4f;
    [Range(0.01f, 5f)] public float blurSigmaRange = 2.1f;
    [Range(1, 20)] public int blurRadius = 6;
    [Header("Particle Surface Tuning")]
    public bool autoTuneParticleSurface = false;
    [Range(0.45f, 0.9f)] public float particleOverlapRatio = 0.62f;
    [Range(0.2f, 0.8f)] public float minParticleToCellRatio = 0.34f;
    [Range(0.5f, 1.2f)] public float maxParticleToCellRatio = 0.88f;
    [Range(0.01f, 1f)] public float particleSurfaceTuneLerp = 0.18f;

    ComputeShader cs;
    int kClearGrid;
    int kBuildGrid;
    int kDensity;
    int kForces;
    int kIntegrate;

    ComputeBuffer bufX;
    ComputeBuffer bufV;
    ComputeBuffer bufRho;
    ComputeBuffer bufCellHead;
    ComputeBuffer bufNextIndex;
    ComputeBuffer bufImpulses;
    ComputeBuffer bufObstacles;
    ComputeBuffer bufBoatCouplingSpheres;
    ComputeBuffer bufBoatCouplingVelocities;
    // 力计算与积分分离用的中间缓冲（消除 kernel 内 race）
    ComputeBuffer bufAccOut;
    ComputeBuffer bufVCorrOut;

    ComputeBuffer particlesBuffer;
    Material gridParticleMat;

    CommandBuffer fluidCmd;
    int bgTexID;
    int depthTexID;
    Material depthMat;
    Material blurMat;
    Material gaussianMat;
    Material debugDepthMat;
    int thicknessTexID;
    Material thicknessMat;
    Material thicknessBlurMat;
    Material debugThicknessMat;
    int normalTexID;
    Material normalMat;
    Material debugNormalMat;
    Material compositeMat;
    MaterialPropertyBlock props;
    Camera mainCam;

    Mesh sphereMesh;
    Bounds drawBounds;
    public bool enableStir = false;
    public Transform stirTransform;
    public float stirRadius = 0.6f;
    public float stirStrength = 50f;
    public Transform[] obstacleTransforms;
    public float obstacleDefaultRadius = 0.5f;
    public float obstaclePushStrength = 8f;
    public float obstacleDamping = 0.52f;
    public Transform barrierTransform;
    public bool enableBarrierMove = true;
    public bool requireSelectionToMove = true;
    public LayerMask selectableMask = ~0;
    [Tooltip("已弃用：标准 WCSPH 不使用涡量约束。")]
    [HideInInspector] public float vorticityEps = 0.0f;
    [HideInInspector] public float surfaceTension = 0.0f;
    [HideInInspector] public float freeSurfaceDamping = 0.0f;
    [HideInInspector] public float freeSurfaceThreshold = 0.75f;

    [Header("Artificial Viscosity (Monaghan-Gingold)")]
    [Tooltip("线性人工粘性系数 α。让显式 WCSPH 真正稳定的关键项。水建议 0.3–0.6；过大显糖浆，过小则沸腾。")]
    [Range(0f, 2f)] public float artViscAlpha = 0.55f;
    [Tooltip("二次人工粘性系数 β。仅冲击波场景有意义，水可保持 0。")]
    [Range(0f, 4f)] public float artViscBeta = 0.0f;
    [Tooltip("单步加速度上限（m/s²）。安全网，防极端瞬间引爆 EOS。")]
    [Range(20f, 500f)] public float maxAcceleration = 80f;

    // 已弃用字段：保留仅为序列化兼容，shader 不读取。
    [HideInInspector] public float pressureRatioCap = 100f;
    [HideInInspector] public float internalJitterDamping = 0f;
    [HideInInspector] public float internalDampingBand = 1f;
    [HideInInspector] public float floorDampingBand = 0f;
    [HideInInspector] public float floorTangentialDamping = 0f;
    [HideInInspector] public float floorNormalDamping = 0f;
    
    public int maxImpulses = 8;
    public float stirStrengthScale = 2f;
    public Vector3 swirlAxis = new Vector3(0,1,0);
    public float impulseNormalCoeff = 1.0f;
    public float impulseTangentialCoeff = 1.0f;
    public bool stirUseCapsuleSegments = true;
    public int stirSegments = 8;
    public float stirSpeedBoost = 0.8f;
    public float impulseRadiusScaleFromCapsule = 1.2f;
    public float obstaclePushSpeedScale = 0.8f;
    public float smallStickRadiusRef = 1.0f;
    public float smallStickBoostMax = 2.5f;
    public float minImpulseRadius = 0.8f;
    public float stirAngularBoost = 2.8f;
    public float obstacleTangentialStrength = 45f;
    public float obstacleFriction = 0.08f;
    [Header("Boat Coupling")]
    [Tooltip("船体径向冲量峰值 m/s²；只有船在动时才生效，仅做造尾迹用，主推还是靠耦合球的非穿透。")]
    public float boatImpulseStrength = 6f;
    [Tooltip("船速→冲量权重缩放；0.05–0.15 即可，过大则船一动就把附近水踢翻。")]
    public float boatImpulseVelocityScale = 0.08f;
    public float boatObstacleStrengthScale = 0.2f;
    [Range(0.02f, 1.2f)] public float couplingBandWidth = 0.32f;
    [Range(0f, 30f)] public float couplingBandStrength = 2.5f;
    [Range(0f, 1f)] public float couplingVelocityBlend = 0.12f;
    [Range(0f, 2f)] public float couplingTangentialFriction = 0.55f;
    public struct BoatSphere
    {
        public Vector4 sphere;
        public Vector3 velocity;
        public float padding;
    }
    BoatSphere[] externalBoatSpheres;
    int externalBoatSphereCount = 0;
    enum InputSelectedTarget { None, Stir, Barrier }
    InputSelectedTarget inputSelected = InputSelectedTarget.None;
    int activePointerId = -1;
    float stirStrengthFactor = 1f;
    Vector3 stirDragOffset = Vector3.zero;
    Vector3 barrierDragOffset = Vector3.zero;
    float stirSpeed = 0f;
    Vector3 lastStirPos = Vector3.zero;
    bool stirPosInitialized = false;
    Vector3 stirDirXZ = Vector3.forward;
    Quaternion lastStickRot = Quaternion.identity;
    bool stickRotInitialized = false;
    float stickAngularSpeed = 0f;
    
    Vector3[] cpuXCache;
    Vector3[] cpuVCache;
    int cpuCacheFrame = -999;
    public int cpuCacheStrideFrames = 4;
    int baseTargetDepthHeight;
    int baseDepthDownsample;
    int baseThicknessDownsample;
    int baseBlurIterations;
    int baseThicknessBlurIterations;
    int baseMaxSubsteps;
    float baseFixedTimeStep;
    float baseRenderParticleScale;
    float baseParticleSize;
    float baseThicknessContribution;
    int runtimeTargetDepthHeight;
    int runtimeDepthDownsample;
    int runtimeThicknessDownsample;
    int runtimeBlurIterations;
    int runtimeThicknessBlurIterations;
    int runtimeMaxSubsteps;
    float runtimeFixedTimeStep;
    float runtimeRenderParticleScale;
    float runtimeParticleSize;
    float runtimeThicknessContribution;
    int runtimeQualityLevel = 2;
    float frameTimeEma = 1f / 60f;
    // 经典 fixed-step 累加器：dt 不再随帧率波动，避免「dt 抖动 → 压力响应抖动」。
    float simAccumulator = 0f;
    int nextQualityEvalFrame = 0;
    Vector4[] impulseTmp;
    Vector4[] obstacleTmp;
    Vector4[] couplingSpheresTmp;
    Vector4[] couplingVelsTmp;
    Vector4[] chainTmp;

    [Tooltip("首帧前在 GPU 预跑的子步数。重力按 0→1 平滑斜坡，让密度场先建立、再加全重力，可显著抑制初始沸腾。")]
    [Range(0, 96)]
    public int simulationWarmupSteps = 32;

#if UNITY_EDITOR
    void OnValidate()
    {
        particleCount = Mathf.Clamp(particleCount, MinParticleCount, MaxParticleCountMobile);
        gridResolution = Mathf.Clamp(gridResolution, 16, 48);
        maxImpulses = Mathf.Max(1, maxImpulses);
        if (qualityUpshiftFps < qualityDownshiftFps + 2f) qualityUpshiftFps = qualityDownshiftFps + 2f;
        if (maxParticleToCellRatio < minParticleToCellRatio + 0.05f) maxParticleToCellRatio = minParticleToCellRatio + 0.05f;
        couplingBandWidth = Mathf.Max(0.001f, couplingBandWidth);
        couplingBandStrength = Mathf.Max(0f, couplingBandStrength);
        couplingTangentialFriction = Mathf.Max(0f, couplingTangentialFriction);
    }
#endif

    void Start()
    {
        cs = Resources.Load<ComputeShader>("Shader/Compute Shader/SPH/Mobile/sph_standard_mobile");
        if (cs == null) { enabled = false; return; }
        particleCount = Mathf.Clamp(particleCount, MinParticleCount, MaxParticleCountMobile);
        gridResolution = Mathf.Clamp(gridResolution, 16, 48);
        kClearGrid = cs.FindKernel("ClearGrid");
        kBuildGrid = cs.FindKernel("BuildGrid");
        kDensity = cs.FindKernel("ComputeDensity");
        kForces = cs.FindKernel("SPHForces");
        kIntegrate = cs.FindKernel("SPHIntegrate");

        int gridCount = gridResolution * gridResolution * gridResolution;
        bufX = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufV = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufRho = new ComputeBuffer(particleCount, sizeof(float));
        bufCellHead = new ComputeBuffer(gridCount, sizeof(int));
        bufNextIndex = new ComputeBuffer(particleCount, sizeof(int));
        bufAccOut = new ComputeBuffer(particleCount, sizeof(float) * 4);
        bufVCorrOut = new ComputeBuffer(particleCount, sizeof(float) * 4);
        particlesBuffer = new ComputeBuffer(particleCount, sizeof(float) * 12);
        int impulseCapacity = Mathf.Max(1, maxImpulses);
        if (enableStir)
            impulseCapacity = Mathf.Max(impulseCapacity, 16);
        bufImpulses = new ComputeBuffer(impulseCapacity, sizeof(float) * 4);
        bufImpulses.SetData(new Vector4[1] { Vector4.zero });
        bufObstacles = new ComputeBuffer(Mathf.Max(1, obstacleTransforms != null ? obstacleTransforms.Length : 1), sizeof(float) * 4);
        bufBoatCouplingSpheres = new ComputeBuffer(1, sizeof(float) * 4);
        bufBoatCouplingVelocities = new ComputeBuffer(1, sizeof(float) * 4);
        bufBoatCouplingSpheres.SetData(new Vector4[1] { Vector4.zero });
        bufBoatCouplingVelocities.SetData(new Vector4[1] { Vector4.zero });

        BindSphPersistentKernelBuffers();

        var xInit = new Vector3[particleCount];
        var vInit = new Vector3[particleCount];
        FillInitial(xInit, vInit);
        bufX.SetData(xInit);
        bufV.SetData(vInit);
        bufRho.SetData(new float[particleCount]);
        
        bufCellHead.SetData(CreateFilled(gridCount, -1));
        bufNextIndex.SetData(CreateFilled(particleCount, -1));

        if (autoCalibrateMass || autoNeighbourRadius)
        {
            Vector3 sMin, sMax; GetSpawnBounds(out sMin, out sMax);
            Vector3 size = sMax - sMin;
            // 与 FillInitial 的各向同性 spawn 保持一致：用统一间距 s = (V/N)^(1/3)
            float spawnVol = Mathf.Max(1e-6f, size.x * size.y * size.z);
            float s = Mathf.Pow(spawnVol / Mathf.Max(1, particleCount), 1f / 3f);
            s = Mathf.Max(s, 1e-3f);
            if (autoCalibrateMass)
            {
                // 立方 cell 体积 = s³；mass = ρ₀ · s³，让 SPH 估算密度恰为 restDensity。
                float cellVol = s * s * s;
                particleMass = restDensity * cellVol * Mathf.Clamp(initialMassScale, 0.85f, 1.15f);
            }
            if (autoNeighbourRadius)
            {
                // 邻居半径 ≈ 2·s（≈8–32 个邻居），WCSPH 经典选择，不用再考虑各向异性。
                neighbourRadius = Mathf.Max(neighbourRadius, s * 2.0f);
            }
        }

        var sm = MobileSsfRenderShared.CreateSsfMaterials(particleSize, particlesBuffer);
        gridParticleMat = sm.gridParticle;
        depthMat = sm.depth;
        debugDepthMat = sm.debugDepth;
        blurMat = sm.blur;
        gaussianMat = sm.gaussian;
        thicknessMat = sm.thickness;
        thicknessBlurMat = sm.thicknessBlur;
        debugThicknessMat = sm.debugThickness;
        normalMat = sm.normal;
        debugNormalMat = sm.debugNormal;
        compositeMat = sm.composite;

        sphereMesh = MobileSsfRenderShared.CreateParticleSphereMesh();
        props = new MaterialPropertyBlock();
        drawBounds = new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin + Vector3.one * 2f);

        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.depthTextureMode |= DepthTextureMode.Depth;
            if (autoCameraClipTuning) ApplyCameraClipTuning(mainCam);

            fluidCmd = new CommandBuffer();
            fluidCmd.name = "SPH Fluid Rendering (SSFR)";
            bgTexID = Shader.PropertyToID("_FluidBackgroundTexture");
            depthTexID = Shader.PropertyToID("_FluidDepthTexture");
            thicknessTexID = Shader.PropertyToID("_FluidThicknessTexture");
            normalTexID = Shader.PropertyToID("_FluidNormalTexture");
        }
        RegisterFluidCommandBuffer();
        if (enableLowParticleCountTuning && particleCount < 8000)
        {
            // 仅微调时间步与子步上限；不再强抬粘度/边界阻尼，避免「糖浆+贴墙」感
            maxSubsteps = Mathf.Max(maxSubsteps, 6);
            fixedTimeStep = Mathf.Min(fixedTimeStep, 0.0048f);
        }
        if (realisticWaterPreset)
        {
            ApplyRealisticWaterPreset();
        }
        if (sweepFlowPreset)
        {
            ApplySweepFlowPreset();
        }
        if (particleCount >= 8000)
        {
            ApplyMediumParticleStability();
        }
        if (targetFrameRate > 0) Application.targetFrameRate = targetFrameRate;
        InitializeAdaptiveQualityState();
        if (Application.isMobilePlatform)
        {
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 4);
            if (QualitySettings.shadows == ShadowQuality.Disable) QualitySettings.shadows = ShadowQuality.HardOnly;
        }

        WarmupSimulation();
    }

    void OnEnable()
    {
        RegisterFluidCommandBuffer();
    }

    void OnDisable()
    {
        if (mainCam != null && fluidCmd != null)
            mainCam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
    }

    void RegisterFluidCommandBuffer()
    {
        if (!isActiveAndEnabled || mainCam == null || fluidCmd == null) return;
        mainCam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
        mainCam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
    }

    public void SetBoatSpheres(BoatSphere[] spheres, int count)
    {
        if (spheres == null || count <= 0)
        {
            externalBoatSphereCount = 0;
            return;
        }
        int c = Mathf.Clamp(count, 0, spheres.Length);
        if (externalBoatSpheres == null || externalBoatSpheres.Length < c)
            externalBoatSpheres = new BoatSphere[c];
        for (int i = 0; i < c; i++) externalBoatSpheres[i] = spheres[i];
        externalBoatSphereCount = c;
    }

    void WarmupSimulation()
    {
        if (!runSimulation || simulationWarmupSteps <= 0 || cs == null) return;
        if (bufX == null || bufV == null || bufImpulses == null || bufObstacles == null || particlesBuffer == null)
            return;
        // 渐进式重力暖启动：先在弱重力下让密度场建立、压力场达到准平衡，再过渡到正常重力。
        // 直接全重力 + 完美晶格初始 → 第一帧就有密度抖动 → SPH 沸腾，难以平复。
        float dtW = Mathf.Min(fixedTimeStep * 0.5f, 0.003f);
        Vector3 originalGravity = gravity;
        for (int i = 0; i < simulationWarmupSteps; i++)
        {
            float t = (i + 1f) / simulationWarmupSteps;
            // 0 → 1 的平滑曲线（缓启动重力，让密度场先建好）
            float gScale = Mathf.SmoothStep(0f, 1f, t);
            gravity = originalGravity * gScale;
            SimulateStep(dtW);
        }
        gravity = originalGravity;
    }
    

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            renderMode = (renderMode == RenderMode.Fluid) ? RenderMode.GridParticles : RenderMode.Fluid;
        }
        HandleMobileStirInput();
        if (simulateInLateUpdate) { if (!runSimulation) Draw(); return; }
        SimulateFrame();
    }

    void LateUpdate()
    {
        if (!simulateInLateUpdate) return;
        SimulateFrame();
    }

    void SimulateFrame()
    {
        UpdateAdaptiveQualityState();
        UpdateParticleSurfaceRuntime();
        if (!runSimulation) { Draw(); return; }

        float dtFrame = Mathf.Clamp(Time.deltaTime, 1e-4f, 0.05f);

        // 标准显式 WCSPH 的 CFL：dt < 0.4 * h / c。取 0.35 作为余量。
        float h = Mathf.Max(neighbourRadius, 0.01f);
        float c = Mathf.Max(soundSpeed, 8f);
        float dtCfl = 0.35f * h / c;

        // 关键修复：使用 *固定* 子步长 dt，避免帧率波动直接修改 dt 引发压力响应抖动。
        // dt 取 min(用户设定, CFL)，且每步都用同一个值。
        float fixedDt = Mathf.Max(1e-4f, runtimeFixedTimeStep);
        if (dtCfl > 1e-5f) fixedDt = Mathf.Min(fixedDt, dtCfl);

        if (!enableSubstepping)
        {
            SimulateStep(fixedDt);
            Draw();
            return;
        }

        // Glenn Fiedler 风格的累加器：积压实时时间，按固定 dt 推进，多余时间留给下一帧。
        simAccumulator += dtFrame;
        int maxStepsPerFrame = Mathf.Max(1, runtimeMaxSubsteps);
        int steps = 0;
        while (simAccumulator >= fixedDt && steps < maxStepsPerFrame)
        {
            SimulateStep(fixedDt);
            simAccumulator -= fixedDt;
            steps++;
        }
        // lag spike 后避免「补帧爆炸」：积压超过单帧上限就丢掉，回到稳态
        float maxAccumulated = fixedDt * maxStepsPerFrame * 1.25f;
        if (simAccumulator > maxAccumulated) simAccumulator = fixedDt * 0.5f;

        Draw();
    }

    void HandleMobileStirInput()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (Application.isMobilePlatform)
        {
            if (Input.touchCount > 0)
            {
                var t0 = Input.GetTouch(0);
                if (activePointerId < 0 && requireSelectionToMove)
                {
                    if (t0.phase == TouchPhase.Began)
                    {
                        Ray r = cam.ScreenPointToRay(t0.position);
                        var hits = Physics.RaycastAll(r, 1000f, selectableMask);
                        Transform hStir = null;
                        Transform hBarrier = null;
                        float dStir = float.PositiveInfinity;
                        float dBarrier = float.PositiveInfinity;
                        for (int i = 0; i < hits.Length; i++)
                        {
                            var ht = hits[i].transform;
                            bool stirHit = enableStir && stirTransform != null && (ht == stirTransform || ht.IsChildOf(stirTransform) || stirTransform.IsChildOf(ht));
                            bool barrierHit = enableBarrierMove && barrierTransform != null && (ht == barrierTransform || ht.IsChildOf(barrierTransform) || barrierTransform.IsChildOf(ht));
                            if (stirHit && hits[i].distance < dStir) { hStir = ht; dStir = hits[i].distance; }
                            if (barrierHit && hits[i].distance < dBarrier) { hBarrier = ht; dBarrier = hits[i].distance; }
                        }
                        if (hStir != null || hBarrier != null)
                        {
                            if (hStir != null && (dStir <= dBarrier || hBarrier == null))
                            {
                                inputSelected = InputSelectedTarget.Stir; activePointerId = t0.fingerId;
                                float planeY = stirTransform.position.y;
                                var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
                                float enterSel;
                                Vector3 pSel;
                                if (plane.Raycast(r, out enterSel)) { pSel = r.GetPoint(enterSel); pSel.y = planeY; } else { RayAABBIntersect(r, boundsMin, boundsMax, out pSel); }
                                stirDragOffset = stirTransform.position - ClampToBounds(pSel);
                                stirDragOffset.y = 0f;
                            }
                            else
                            {
                                inputSelected = InputSelectedTarget.Barrier; activePointerId = t0.fingerId;
                                if (barrierTransform != null)
                                {
                                    float planeYB = barrierTransform.position.y;
                                    var planeB = new Plane(Vector3.up, new Vector3(0f, planeYB, 0f));
                                    float enterSelB;
                                    Vector3 pSelB;
                                    if (planeB.Raycast(r, out enterSelB)) { pSelB = r.GetPoint(enterSelB); pSelB.y = planeYB; } else { RayAABBIntersect(r, boundsMin, boundsMax, out pSelB); }
                                    barrierDragOffset = barrierTransform.position - ClampToBounds(pSelB);
                                    barrierDragOffset.y = 0f;
                                }
                            }
                        }
                        else
                        {
                            Vector3 hitSel = Vector3.zero;
                            bool gotHit = false;
                            if (stirTransform != null)
                            {
                                float planeY = stirTransform.position.y;
                                Vector3 ptmp;
                                if (RayPlaneStable(cam, planeY, r, out ptmp)) { hitSel = ptmp; gotHit = true; }
                            }
                            if (!gotHit) gotHit = RayAABBIntersect(r, boundsMin, boundsMax, out hitSel);
                            if (gotHit && stirTransform != null)
                            {
                                Vector3 pSel = ClampToBounds(hitSel);
                                float tol = Mathf.Max(0.05f, stirRadius * 0.5f);
                                if (Vector3.Distance(pSel, stirTransform.position) <= tol)
                                {
                                    inputSelected = InputSelectedTarget.Stir; activePointerId = t0.fingerId;
                                    stirDragOffset = stirTransform.position - pSel;
                                    stirDragOffset.y = 0f;
                                }
                                else
                                {
                                    inputSelected = InputSelectedTarget.None; activePointerId = -1;
                                    stirDragOffset = Vector3.zero;
                                }
                            }
                            else { inputSelected = InputSelectedTarget.None; activePointerId = -1; }
                        }
                    }
                }
                if (activePointerId >= 0)
                {
                    Touch t = t0;
                    if (t.fingerId == activePointerId)
                    {
                        if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary || t.phase == TouchPhase.Began)
                        {
                            var ray = cam.ScreenPointToRay(t.position);
                            Vector3 p = Vector3.zero;
                            bool ok = false;
                            if (inputSelected == InputSelectedTarget.Stir && enableStir && stirTransform != null)
                            {
                                float planeY = stirTransform.position.y;
                                if (RayPlaneStable(cam, planeY, ray, out p)) ok = true;
                            }
                            if (!ok) ok = RayAABBIntersect(ray, boundsMin, boundsMax, out p);
                            if (ok || (inputSelected == InputSelectedTarget.Stir && enableStir && stirTransform != null))
                            {
                                if (inputSelected == InputSelectedTarget.Stir && enableStir && stirTransform != null)
                                {
                                    Vector2 d = t.deltaPosition;
                                    Vector3 right = (cam != null) ? cam.transform.right : Vector3.right;
                                    Vector3 fwdXZ = (cam != null) ? new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z) : Vector3.forward;
                                    if (fwdXZ.sqrMagnitude < 1e-6f) fwdXZ = Vector3.forward; else fwdXZ = fwdXZ.normalized;
                                    float moveScale = Mathf.Max(0.0001f, (boundsMax.x - boundsMin.x + boundsMax.z - boundsMin.z) * 0.35f);
                                    Vector3 move = right * (d.x / Mathf.Max(Screen.width, 1)) * moveScale + fwdXZ * (d.y / Mathf.Max(Screen.height, 1)) * moveScale;
                                    Vector3 cur = stirTransform.position;
                                    Vector3 target = new Vector3(cur.x + move.x, cur.y, cur.z + move.z);
                                    target = ClampToBounds(target);
                            if (stirTransform.parent != null)
                            {
                                Vector3 offset = stirTransform.position - stirTransform.parent.position;
                                stirTransform.parent.position = target - offset;
                            }
                            else
                            {
                                stirTransform.position = target;
                            }
                            UpdateStirMetrics(target);
                            stirStrengthFactor = 1f + Mathf.Clamp(d.magnitude / 18f, 0f, 3f);
                        }
                        else if (enableBarrierMove && barrierTransform != null)
                        {
                            p = ClampToBounds(p);
                            p.y = barrierTransform.position.y;
                            p += barrierDragOffset;
                            p = ClampToBounds(p);
                            barrierTransform.position = p;
                        }
                            }
                        }
                        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        {
                            inputSelected = InputSelectedTarget.None;
                            activePointerId = -1;
                            stirDragOffset = Vector3.zero;
                            barrierDragOffset = Vector3.zero;
                        }
                    }
                }
                if (!requireSelectionToMove && enableStir && stirTransform != null)
                {
                    var ray = cam.ScreenPointToRay(t0.position);
                    Vector3 p = Vector3.zero;
                    bool ok = false;
                    if (stirTransform != null)
                    {
                        float planeY = stirTransform.position.y;
                        if (RayPlaneStable(cam, planeY, ray, out p)) ok = true;
                    }
                    if (!ok) ok = RayAABBIntersect(ray, boundsMin, boundsMax, out p);
                    if (ok)
                    {
                        p = ClampToBounds(p);
                        p.y = stirTransform.position.y;
                        p += stirDragOffset;
                        p = ClampToBounds(p);
                        if (stirTransform.parent != null)
                        {
                            Vector3 offset = stirTransform.position - stirTransform.parent.position;
                            stirTransform.parent.position = p - offset;
                        }
                        else
                        {
                            stirTransform.position = p;
                        }
                        stirStrengthFactor = 1f;
                    }
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && requireSelectionToMove)
            {
                Ray r = cam.ScreenPointToRay(Input.mousePosition);
                var hits = Physics.RaycastAll(r, 1000f, selectableMask);
                Transform hStir = null;
                Transform hBarrier = null;
                float dStir = float.PositiveInfinity;
                float dBarrier = float.PositiveInfinity;
                for (int i = 0; i < hits.Length; i++)
                {
                    var ht = hits[i].transform;
                    bool stirHit = enableStir && stirTransform != null && (ht == stirTransform || ht.IsChildOf(stirTransform) || stirTransform.IsChildOf(ht));
                    bool barrierHit = enableBarrierMove && barrierTransform != null && (ht == barrierTransform || ht.IsChildOf(barrierTransform) || barrierTransform.IsChildOf(ht));
                    if (stirHit && hits[i].distance < dStir) { hStir = ht; dStir = hits[i].distance; }
                    if (barrierHit && hits[i].distance < dBarrier) { hBarrier = ht; dBarrier = hits[i].distance; }
                }
                if (hStir != null || hBarrier != null)
                {
                    if (hStir != null && (dStir <= dBarrier || hBarrier == null))
                    {
                        inputSelected = InputSelectedTarget.Stir;
                        float planeY = stirTransform.position.y;
                        Vector3 pSel;
                        if (!RayPlaneStable(cam, planeY, r, out pSel)) RayAABBIntersect(r, boundsMin, boundsMax, out pSel);
                        stirDragOffset = stirTransform.position - ClampToBounds(pSel);
                        stirDragOffset.y = 0f;
                    }
                    else
                    {
                        inputSelected = InputSelectedTarget.Barrier;
                        if (barrierTransform != null)
                        {
                            float planeYB = barrierTransform.position.y;
                            Vector3 pSelB;
                            if (!RayPlaneStable(cam, planeYB, r, out pSelB)) RayAABBIntersect(r, boundsMin, boundsMax, out pSelB);
                            barrierDragOffset = barrierTransform.position - ClampToBounds(pSelB);
                            barrierDragOffset.y = 0f;
                        }
                    }
                }
                else
                {
                    Vector3 hitSel = Vector3.zero;
                    bool gotHit = false;
                    if (stirTransform != null)
                    {
                        float planeY = stirTransform.position.y;
                        Vector3 ptmp;
                        if (RayPlaneStable(cam, planeY, r, out ptmp)) { hitSel = ptmp; gotHit = true; }
                    }
                    if (!gotHit) gotHit = RayAABBIntersect(r, boundsMin, boundsMax, out hitSel);
                    if (gotHit && stirTransform != null)
                    {
                        Vector3 pSel = ClampToBounds(hitSel);
                        float tol = Mathf.Max(0.05f, stirRadius * 0.5f);
                        if (Vector3.Distance(pSel, stirTransform.position) <= tol) { inputSelected = InputSelectedTarget.Stir; stirDragOffset = stirTransform.position - pSel; stirDragOffset.y = 0f; }
                        else { inputSelected = InputSelectedTarget.None; stirDragOffset = Vector3.zero; }
                    }
                    else inputSelected = InputSelectedTarget.None;
                }
            }
            if (Input.GetMouseButton(0))
            {
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                Vector3 p = Vector3.zero;
                bool ok = false;
                bool forStir = inputSelected == InputSelectedTarget.Stir && enableStir && stirTransform != null;
                if (forStir)
                {
                    float planeY = stirTransform.position.y;
                    if (RayPlaneStable(cam, planeY, ray, out p)) ok = true;
                }
                if (!ok) ok = RayAABBIntersect(ray, boundsMin, boundsMax, out p);
                if (ok || forStir)
                {
                    if (!requireSelectionToMove)
                    {
                        if (enableStir && stirTransform != null)
                        {
                            Vector3 right = (cam != null) ? cam.transform.right : Vector3.right;
                            Vector3 fwdXZ = (cam != null) ? new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z) : Vector3.forward;
                            if (fwdXZ.sqrMagnitude < 1e-6f) fwdXZ = Vector3.forward; else fwdXZ = fwdXZ.normalized;
                            float moveScale = Mathf.Max(0.0001f, (boundsMax.x - boundsMin.x + boundsMax.z - boundsMin.z) * 0.35f);
                            float dx = Input.GetAxis("Mouse X");
                            float dy = Input.GetAxis("Mouse Y");
                            Vector3 move = right * dx * moveScale * 0.02f + fwdXZ * dy * moveScale * 0.02f;
                            Vector3 cur = stirTransform.position;
                            Vector3 target = new Vector3(cur.x + move.x, cur.y, cur.z + move.z);
                            target = ClampToBounds(target);
                            if (stirTransform.parent != null)
                            {
                                Vector3 offset = stirTransform.position - stirTransform.parent.position;
                                stirTransform.parent.position = target - offset;
                            }
                            else
                            {
                                stirTransform.position = target;
                            }
                            UpdateStirMetrics(target);
                            stirStrengthFactor = 1f + Mathf.Clamp((Mathf.Abs(dx) + Mathf.Abs(dy)) * 30f, 0f, 3f);
                        }
                    }
                    else
                    {
                        if (inputSelected == InputSelectedTarget.Stir && enableStir && stirTransform != null)
                        {
                            Vector3 right = (cam != null) ? cam.transform.right : Vector3.right;
                            Vector3 fwdXZ = (cam != null) ? new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized : Vector3.forward;
                            float moveScale = Mathf.Max(0.0001f, (boundsMax.x - boundsMin.x + boundsMax.z - boundsMin.z) * 0.35f);
                            float dx = Input.GetAxis("Mouse X");
                            float dy = Input.GetAxis("Mouse Y");
                            Vector3 move = right * dx * moveScale * 0.02f + fwdXZ * dy * moveScale * 0.02f;
                            Vector3 cur = stirTransform.position;
                            Vector3 target = new Vector3(cur.x + move.x, cur.y, cur.z + move.z);
                            target = ClampToBounds(target);
                            if (stirTransform.parent != null)
                            {
                                Vector3 offset = stirTransform.position - stirTransform.parent.position;
                                stirTransform.parent.position = target - offset;
                            }
                            else
                            {
                                stirTransform.position = target;
                            }
                            UpdateStirMetrics(target);
                            float dxA = Mathf.Abs(Input.GetAxis("Mouse X"));
                            float dyA = Mathf.Abs(Input.GetAxis("Mouse Y"));
                            stirStrengthFactor = 1f + Mathf.Clamp((dxA + dyA) * 30f, 0f, 3f);
                        }
                        else if (inputSelected == InputSelectedTarget.Barrier && enableBarrierMove && barrierTransform != null)
                        {
                            p = ClampToBounds(p);
                            p.y = barrierTransform.position.y;
                            p += barrierDragOffset;
                            p = ClampToBounds(p);
                            barrierTransform.position = p;
                        }
                    }
                }
            }
            if (Input.GetMouseButtonUp(0)) inputSelected = InputSelectedTarget.None;
        }
    }

    bool RayAABBIntersect(Ray ray, Vector3 bmin, Vector3 bmax, out Vector3 hit)
    {
        hit = ray.origin;
        Vector3 dir = ray.direction;
        Vector3 orig = ray.origin;
        float tmin = (bmin.x - orig.x) / (Mathf.Abs(dir.x) < 1e-6f ? Mathf.Sign(dir.x) * 1e-6f : dir.x);
        float tmax = (bmax.x - orig.x) / (Mathf.Abs(dir.x) < 1e-6f ? Mathf.Sign(dir.x) * 1e-6f : dir.x);
        if (tmin > tmax) { float tmp = tmin; tmin = tmax; tmax = tmp; }
        float tymin = (bmin.y - orig.y) / (Mathf.Abs(dir.y) < 1e-6f ? Mathf.Sign(dir.y) * 1e-6f : dir.y);
        float tymax = (bmax.y - orig.y) / (Mathf.Abs(dir.y) < 1e-6f ? Mathf.Sign(dir.y) * 1e-6f : dir.y);
        if (tymin > tymax) { float tmp2 = tymin; tymin = tymax; tymax = tmp2; }
        if ((tmin > tymax) || (tymin > tmax)) { hit = ray.origin; return false; }
        if (tymin > tmin) tmin = tymin;
        if (tymax < tmax) tmax = tymax;
        float tzmin = (bmin.z - orig.z) / (Mathf.Abs(dir.z) < 1e-6f ? Mathf.Sign(dir.z) * 1e-6f : dir.z);
        float tzmax = (bmax.z - orig.z) / (Mathf.Abs(dir.z) < 1e-6f ? Mathf.Sign(dir.z) * 1e-6f : dir.z);
        if (tzmin > tzmax) { float tmp3 = tzmin; tzmin = tzmax; tzmax = tmp3; }
        if ((tmin > tzmax) || (tzmin > tmax)) { hit = ray.origin; return false; }
        if (tzmin > tmin) tmin = tzmin;
        if (tzmax < tmax) tmax = tzmax;
        float t = tmin;
        if (t < 0f) t = tmax;
        if (t < 0f) { hit = ray.origin; return false; }
        hit = ray.GetPoint(t);
        return true;
    }
    
    bool RayPlaneStable(Camera cam, float planeY, Ray ray, out Vector3 p)
    {
        p = ray.origin;
        Vector3 n = Vector3.up;
        float nd = Mathf.Abs(Vector3.Dot(ray.direction.normalized, n));
        if (nd < 1e-4f && cam != null)
        {
            Vector3 tweak = cam.transform.forward * 0.001f;
            n = (n + tweak).normalized;
        }
        Plane pl = new Plane(n, new Vector3(0f, planeY, 0f));
        float enter;
        if (pl.Raycast(ray, out enter))
        {
            p = ray.GetPoint(enter);
            p.y = planeY;
            return true;
        }
        return false;
    }

    void BindSphPersistentKernelBuffers()
    {
        cs.SetBuffer(kClearGrid, "cellHead", bufCellHead);
        cs.SetBuffer(kBuildGrid, "x", bufX);
        cs.SetBuffer(kBuildGrid, "cellHead", bufCellHead);
        cs.SetBuffer(kBuildGrid, "nextIndex", bufNextIndex);
        cs.SetBuffer(kDensity, "x", bufX);
        cs.SetBuffer(kDensity, "rho", bufRho);
        cs.SetBuffer(kDensity, "cellHead", bufCellHead);
        cs.SetBuffer(kDensity, "nextIndex", bufNextIndex);
        // SPHForces：纯只读 x/v/rho，写入 accOut / vCorrOut
        cs.SetBuffer(kForces, "x", bufX);
        cs.SetBuffer(kForces, "v", bufV);
        cs.SetBuffer(kForces, "rho", bufRho);
        cs.SetBuffer(kForces, "cellHead", bufCellHead);
        cs.SetBuffer(kForces, "nextIndex", bufNextIndex);
        cs.SetBuffer(kForces, "impulses", bufImpulses);
        cs.SetBuffer(kForces, "accOut", bufAccOut);
        cs.SetBuffer(kForces, "vCorrOut", bufVCorrOut);
        // SPHIntegrate：每个粒子只读自身 + accOut/vCorrOut + 障碍/船体输入
        cs.SetBuffer(kIntegrate, "x", bufX);
        cs.SetBuffer(kIntegrate, "v", bufV);
        cs.SetBuffer(kIntegrate, "rho", bufRho);
        cs.SetBuffer(kIntegrate, "accOut", bufAccOut);
        cs.SetBuffer(kIntegrate, "vCorrOut", bufVCorrOut);
        cs.SetBuffer(kIntegrate, "_particlesBuffer", particlesBuffer);
        cs.SetBuffer(kIntegrate, "obstacles", bufObstacles);
        cs.SetBuffer(kIntegrate, "boatCouplingSpheres", bufBoatCouplingSpheres);
        cs.SetBuffer(kIntegrate, "boatCouplingVelocities", bufBoatCouplingVelocities);
    }

    void SimulateStep(float dt)
    {
        if (cs == null || bufX == null || bufV == null || bufRho == null || bufCellHead == null || bufNextIndex == null
            || bufImpulses == null || bufObstacles == null || bufBoatCouplingSpheres == null || bufBoatCouplingVelocities == null || particlesBuffer == null)
            return;

        float hConst = Mathf.Max(neighbourRadius, 1e-3f);
        float poly6 = 315f / (64f * Mathf.PI * Mathf.Pow(hConst, 9f));
        float spiky = 45f / (Mathf.PI * Mathf.Pow(hConst, 6f));
        float visc = spiky;
        float eosKVal = restDensity * soundSpeed * soundSpeed / Mathf.Max(eosGamma, 1e-3f);
        cs.SetInt("n_grid", gridResolution);
        cs.SetInt("particle_num", particleCount);
        cs.SetVector("boundsMin", boundsMin);
        cs.SetVector("boundsMax", boundsMax);
        int groupsGrid = (gridResolution * gridResolution * gridResolution + 127) / 128;
        cs.Dispatch(kClearGrid, groupsGrid, 1, 1);

        int groupsMain = (particleCount + 127) / 128;
        cs.Dispatch(kBuildGrid, groupsMain, 1, 1);

        cs.SetFloat("restDensity", restDensity);
        cs.SetFloat("particleMass", particleMass);
        cs.SetFloat("neighbourRadius", neighbourRadius);
        cs.SetFloat("poly6_const", poly6);
        cs.SetFloat("spiky_const", spiky);
        cs.SetFloat("visc_const", visc);
        cs.SetFloat("eosK", eosKVal);
        cs.Dispatch(kDensity, groupsMain, 1, 1);

        cs.SetVector("gravity", gravity);
        cs.SetFloat("dt", dt);
        cs.SetFloat("viscosity", viscosity);
        cs.SetFloat("eosGamma", eosGamma);
        cs.SetFloat("soundSpeed", soundSpeed);
        cs.SetFloat("maxSpeed", maxSpeed);
        cs.SetFloat("boundaryDamping", boundaryDamping);
        cs.SetFloat("boundaryDampingZ", boundaryDampingZ);
        cs.SetFloat("boundaryMaxBounceSpeedZ", boundaryMaxBounceSpeedZ);
        cs.SetFloat("xsphC", xsphC);
        cs.SetFloat("minSpeed", Mathf.Max(0f, minSpeed));
        cs.SetFloat("vorticityEps", Mathf.Max(0f, vorticityEps));
        cs.SetFloat("surfaceTension", Mathf.Max(0f, surfaceTension));
        cs.SetFloat("freeSurfaceDamping", Mathf.Max(0f, freeSurfaceDamping));
        cs.SetFloat("freeSurfaceThreshold", Mathf.Clamp(freeSurfaceThreshold, 0f, 1.5f));
        // Monaghan-Gingold 人工粘性 + 加速度上限：让显式 WCSPH 稳定。
        cs.SetFloat("artViscAlpha", Mathf.Max(0f, artViscAlpha));
        cs.SetFloat("artViscBeta", Mathf.Max(0f, artViscBeta));
        cs.SetFloat("maxAcceleration", Mathf.Max(1f, maxAcceleration));
        // 已弃用，仍写以兼容旧 shader 残留 binding（实际不使用）
        cs.SetFloat("pressureRatioCap", Mathf.Max(1.05f, pressureRatioCap));
        cs.SetFloat("internalJitterDamping", Mathf.Max(0f, internalJitterDamping));
        cs.SetFloat("internalDampingBand", Mathf.Clamp(internalDampingBand, 0.05f, 1f));
        cs.SetFloat("floorDampingBand", Mathf.Max(0.001f, floorDampingBand));
        cs.SetFloat("floorTangentialDamping", Mathf.Max(0f, floorTangentialDamping));
        cs.SetFloat("floorNormalDamping", Mathf.Max(0f, floorNormalDamping));
        cs.SetVector("swirlAxis", Vector3.up);
        cs.SetFloat("impulseNormalCoeff", Mathf.Max(0f, impulseNormalCoeff));
        cs.SetFloat("impulseTangentialCoeff", Mathf.Max(0f, impulseTangentialCoeff));
        int boatCount = Mathf.Min(Mathf.Max(0, externalBoatSphereCount), 3);
        int desiredCount = Mathf.Max(1, (enableStir && stirTransform != null ? 1 : 0) + boatCount);
        bool impulsesBufferRebuilt = false;
        if (bufImpulses == null || bufImpulses.count != desiredCount)
        {
            if (bufImpulses != null) bufImpulses.Release();
            bufImpulses = new ComputeBuffer(desiredCount, sizeof(float) * 4);
            impulsesBufferRebuilt = true;
        }
        if (impulseTmp == null || impulseTmp.Length != desiredCount) impulseTmp = new Vector4[desiredCount];
        var arr = impulseTmp;
        System.Array.Clear(arr, 0, arr.Length);
        int wr = 0;
        float impulseRadiusRuntime = Mathf.Max(0.001f, stirRadius);
        float impulseStrengthRuntime = 0f;
        if (enableStir && stirTransform != null)
        {
            Vector3 p = ClampToBounds(stirTransform.position);
            arr[wr++] = new Vector4(p.x, p.y, p.z, 1f);
            impulseStrengthRuntime = Mathf.Max(impulseStrengthRuntime, stirStrength * Mathf.Max(1f, stirStrengthFactor));
        }
        if (boatCount > 0 && externalBoatSpheres != null)
        {
            for (int bi = 0; bi < boatCount && wr < desiredCount; bi++)
            {
                BoatSphere bs = externalBoatSpheres[bi];
                Vector3 bp = ClampToBounds(new Vector3(bs.sphere.x, bs.sphere.y, bs.sphere.z));
                float velMag = bs.velocity.magnitude;
                // 关键修复：w 与船速成正比；静止小船 → w=0 → 无径向冲量，
                // 否则即使船不动也会持续径向推水，制造「船周围一圈沸腾」。
                float w = Mathf.Clamp(velMag * Mathf.Max(boatImpulseVelocityScale, 0.1f) * 6f, 0f, 1.5f);
                if (w < 1e-3f) continue; // 完全静止时跳过该冲量点
                arr[wr++] = new Vector4(bp.x, bp.y, bp.z, w);
                impulseRadiusRuntime = Mathf.Max(impulseRadiusRuntime, Mathf.Max(0.02f, bs.sphere.w * 1.1f));
                impulseStrengthRuntime = Mathf.Max(impulseStrengthRuntime, boatImpulseStrength);
            }
        }
        if (wr == 0) arr[0] = Vector4.zero;
        bufImpulses.SetData(arr);
        cs.SetInt("impulseCount", wr);
        cs.SetFloat("impulseRadius", Mathf.Max(impulseRadiusRuntime, minImpulseRadius));
        cs.SetFloat("impulseStrength", impulseStrengthRuntime);
        if (impulsesBufferRebuilt) cs.SetBuffer(kForces, "impulses", bufImpulses);

        int obstCountBase = obstacleTransforms != null ? obstacleTransforms.Length : 0;
        int obstCount = obstCountBase;
        int allocCount = Mathf.Max(1, obstCount);
        bool obstaclesBufferRebuilt = false;
        if (bufObstacles == null || bufObstacles.count != allocCount)
        {
            if (bufObstacles != null) bufObstacles.Release();
            bufObstacles = new ComputeBuffer(allocCount, sizeof(float) * 4);
            obstaclesBufferRebuilt = true;
        }
        if (obstacleTmp == null || obstacleTmp.Length != allocCount) obstacleTmp = new Vector4[allocCount];
        var obs = obstacleTmp;
        System.Array.Clear(obs, 0, obs.Length);
        for (int oi = 0; oi < obstCountBase; oi++)
        {
            Transform t = obstacleTransforms[oi];
            if (t == null) continue;
            Vector3 op = ClampToBounds(t.position);
            float r = obstacleDefaultRadius;
            var sc2 = t.GetComponent<SphereCollider>();
            if (sc2 != null) r = Mathf.Max(0.001f, sc2.radius * Mathf.Max(t.lossyScale.x, Mathf.Max(t.lossyScale.y, t.lossyScale.z)));
            obs[oi] = new Vector4(op.x, op.y, op.z, r);
        }
        if (allocCount == 1 && obstCount == 0) obs[0] = Vector4.zero;
        bufObstacles.SetData(obs);
        cs.SetInt("obstacleCount", obstCount);
        cs.SetFloat("obstaclePushStrength", obstaclePushStrength);
        cs.SetFloat("obstacleDamping", obstacleDamping);
        cs.SetFloat("obstacleTangentialStrength", obstacleTangentialStrength);
        cs.SetFloat("obstacleFriction", obstacleFriction);
        if (obstaclesBufferRebuilt) cs.SetBuffer(kIntegrate, "obstacles", bufObstacles);

        int couplingCount = (externalBoatSpheres != null) ? boatCount : 0;
        int couplingAlloc = Mathf.Max(1, couplingCount);
        bool boatCouplingRebuilt = false;
        if (bufBoatCouplingSpheres == null || bufBoatCouplingSpheres.count != couplingAlloc)
        {
            if (bufBoatCouplingSpheres != null) bufBoatCouplingSpheres.Release();
            bufBoatCouplingSpheres = new ComputeBuffer(couplingAlloc, sizeof(float) * 4);
            boatCouplingRebuilt = true;
        }
        if (bufBoatCouplingVelocities == null || bufBoatCouplingVelocities.count != couplingAlloc)
        {
            if (bufBoatCouplingVelocities != null) bufBoatCouplingVelocities.Release();
            bufBoatCouplingVelocities = new ComputeBuffer(couplingAlloc, sizeof(float) * 4);
            boatCouplingRebuilt = true;
        }
        if (couplingSpheresTmp == null || couplingSpheresTmp.Length != couplingAlloc) couplingSpheresTmp = new Vector4[couplingAlloc];
        if (couplingVelsTmp == null || couplingVelsTmp.Length != couplingAlloc) couplingVelsTmp = new Vector4[couplingAlloc];
        var couplingSpheres = couplingSpheresTmp;
        var couplingVels = couplingVelsTmp;
        System.Array.Clear(couplingSpheres, 0, couplingSpheres.Length);
        System.Array.Clear(couplingVels, 0, couplingVels.Length);
        for (int bi = 0; bi < couplingCount; bi++)
        {
            BoatSphere bs = externalBoatSpheres[bi];
            Vector3 bp = ClampToBounds(new Vector3(bs.sphere.x, bs.sphere.y, bs.sphere.z));
            float rad = Mathf.Max(0.02f, bs.sphere.w);
            couplingSpheres[bi] = new Vector4(bp.x, bp.y, bp.z, rad);
            couplingVels[bi] = new Vector4(bs.velocity.x, bs.velocity.y, bs.velocity.z, 0f);
        }
        if (couplingAlloc == 1 && couplingCount == 0)
        {
            couplingSpheres[0] = Vector4.zero;
            couplingVels[0] = Vector4.zero;
        }
        bufBoatCouplingSpheres.SetData(couplingSpheres);
        bufBoatCouplingVelocities.SetData(couplingVels);
        cs.SetInt("boatCouplingCount", couplingCount);
        cs.SetFloat("couplingBandWidth", Mathf.Max(0.001f, couplingBandWidth));
        cs.SetFloat("couplingBandStrength", Mathf.Max(0f, couplingBandStrength));
        cs.SetFloat("couplingVelocityBlend", Mathf.Clamp01(couplingVelocityBlend));
        cs.SetFloat("couplingTangentialFriction", Mathf.Max(0f, couplingTangentialFriction));
        if (boatCouplingRebuilt)
        {
            cs.SetBuffer(kIntegrate, "boatCouplingSpheres", bufBoatCouplingSpheres);
            cs.SetBuffer(kIntegrate, "boatCouplingVelocities", bufBoatCouplingVelocities);
        }
        // 力计算（只读）→ 积分（只写自身）：两次 dispatch 之间 Unity 自动插入 barrier，
        // 彻底消除「kernel 内既读邻居又写自身」造成的非确定性扰动。
        cs.Dispatch(kForces, groupsMain, 1, 1);
        cs.Dispatch(kIntegrate, groupsMain, 1, 1);
    }

    void BindParticleSplatPropsAndGlobals()
    {
        // SPH 关闭各向异性：避免底层粒子被速度拉伸后触发不稳定的深度/厚度抖动。
        const float sphAnisoScale = 0f;
        const float sphMaxAniso = 1f;
        props.Clear();
        props.SetFloat("_size", runtimeParticleSize);
        props.SetFloat("_SizeScale", runtimeRenderParticleScale);
        props.SetFloat("_AnisotropyScale", sphAnisoScale);
        props.SetFloat("_MaxAnisotropy", sphMaxAniso);
        props.SetBuffer("_particlesBuffer", particlesBuffer);
        Shader.SetGlobalBuffer("_particlesBuffer", particlesBuffer);
        Shader.SetGlobalFloat("_SizeScale", runtimeRenderParticleScale);
        Shader.SetGlobalFloat("_size", runtimeParticleSize);
        Shader.SetGlobalFloat("_AnisotropyScale", sphAnisoScale);
        Shader.SetGlobalFloat("_MaxAnisotropy", sphMaxAniso);
    }

    void Draw()
    {
        if (sphereMesh == null || particlesBuffer == null) return;
        drawBounds.center = (boundsMin + boundsMax) * 0.5f;
        drawBounds.size = boundsMax - boundsMin + Vector3.one * 2f;

        if (renderMode == RenderMode.GridParticles)
        {
            if (gridParticleMat == null) return;
            BindParticleSplatPropsAndGlobals();
            Graphics.DrawMeshInstancedProcedural(sphereMesh, 0, gridParticleMat, drawBounds, particleCount, props, ShadowCastingMode.On, true);
            return;
        }

        if (fluidCmd != null && !enableRendering)
        {
            fluidCmd.Clear();
            return;
        }

        BindParticleSplatPropsAndGlobals();

        if (renderParticles && gridParticleMat != null)
        {
            Graphics.DrawMeshInstancedProcedural(sphereMesh, 0, gridParticleMat, drawBounds, particleCount, props, ShadowCastingMode.On, true);
        }

        if (fluidCmd != null)
        {
            fluidCmd.Clear();
            if (depthMat == null) return;

            int fluidDepthW = 0;
            int fluidDepthH = 0;

            fluidCmd.GetTemporaryRT(bgTexID, -1, -1, 0, FilterMode.Bilinear);
            fluidCmd.Blit(BuiltinRenderTextureType.CurrentActive, bgTexID);
            fluidCmd.SetGlobalTexture("_FluidBackgroundTexture", bgTexID);

            if (depthMat != null)
            {
                RenderTextureFormat depthFmt = MobileSsfRenderShared.SelectSingleChannelFloatFormat();

                int effectiveDownsample = runtimeDepthDownsample;
                if (runtimeTargetDepthHeight > 0)
                {
                    float scale = (float)Screen.height / (float)runtimeTargetDepthHeight;
                    effectiveDownsample = Mathf.Max(runtimeDepthDownsample, Mathf.RoundToInt(scale));
                }

                int dw = Mathf.Max(1, Screen.width / effectiveDownsample);
                int dh = Mathf.Max(1, Screen.height / effectiveDownsample);
                fluidDepthW = dw;
                fluidDepthH = dh;

                fluidCmd.GetTemporaryRT(depthTexID, dw, dh, 24, FilterMode.Bilinear, depthFmt);
                fluidCmd.SetRenderTarget(depthTexID);
                fluidCmd.ClearRenderTarget(true, true, new Color(10000f, 10000f, 10000f, 10000f));
                fluidCmd.DrawMeshInstancedProcedural(sphereMesh, 0, depthMat, 0, particleCount, props);

                Material currentBlurMat = (filterType == DepthFilterType.Gaussian && gaussianMat != null) ? gaussianMat : blurMat;

                if (currentBlurMat != null && runtimeBlurIterations > 0)
                {
                    currentBlurMat.SetFloat("_SigmaSpatial", blurSigmaSpatial);
                    currentBlurMat.SetFloat("_SigmaRange", blurSigmaRange);
                    currentBlurMat.SetInt("_FilterRadius", blurRadius);

                    int tempDepthID = Shader.PropertyToID("_FluidDepthTemp");
                    fluidCmd.GetTemporaryRT(tempDepthID, dw, dh, 0, FilterMode.Bilinear, depthFmt);

                    for (int i = 0; i < runtimeBlurIterations; i++)
                    {
                        fluidCmd.Blit(depthTexID, tempDepthID, currentBlurMat, 0);
                        fluidCmd.Blit(tempDepthID, depthTexID, currentBlurMat, 1);
                    }
                    fluidCmd.ReleaseTemporaryRT(tempDepthID);
                }
                fluidCmd.SetGlobalTexture("_FluidDepthTexture", depthTexID);
            }

            if (thicknessMat != null)
            {
                props.SetFloat("_ContributionScale", runtimeThicknessContribution);
                props.SetFloat("_SizeScale", runtimeRenderParticleScale);
                int w = Screen.width / runtimeThicknessDownsample;
                int h = Screen.height / runtimeThicknessDownsample;
                RenderTextureFormat thickFmt = MobileSsfRenderShared.SelectSingleChannelFloatFormat();

                fluidCmd.GetTemporaryRT(thicknessTexID, w, h, 0, FilterMode.Bilinear, thickFmt);
                fluidCmd.SetRenderTarget(thicknessTexID);
                fluidCmd.ClearRenderTarget(false, true, Color.black);
                fluidCmd.DrawMeshInstancedProcedural(sphereMesh, 0, thicknessMat, 0, particleCount, props);

                if (thicknessBlurMat != null && runtimeThicknessBlurIterations > 0)
                {
                    thicknessBlurMat.SetInt("_FilterRadius", thicknessBlurRadius);
                    int tempID = Shader.PropertyToID("_FluidThicknessTemp");
                    fluidCmd.GetTemporaryRT(tempID, w, h, 0, FilterMode.Bilinear, thickFmt);
                    for (int i = 0; i < runtimeThicknessBlurIterations; i++)
                    {
                        fluidCmd.Blit(thicknessTexID, tempID, thicknessBlurMat, 0);
                        fluidCmd.Blit(tempID, thicknessTexID, thicknessBlurMat, 1);
                    }
                    fluidCmd.ReleaseTemporaryRT(tempID);
                }
                fluidCmd.SetGlobalTexture("_FluidThicknessTexture", thicknessTexID);
            }

            if (normalMat != null && depthMat != null && fluidDepthW > 0 && fluidDepthH > 0)
            {
                normalMat.SetFloat("_NormalStrength", normalStrength);
                fluidCmd.GetTemporaryRT(normalTexID, fluidDepthW, fluidDepthH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
                fluidCmd.Blit(depthTexID, normalTexID, normalMat);
                fluidCmd.SetGlobalTexture("_FluidNormalTexture", normalTexID);
            }

            if (showDepthDebug && depthMat != null)
            {
                if (debugDepthMat != null) fluidCmd.Blit(depthTexID, BuiltinRenderTextureType.CameraTarget, debugDepthMat);
                else fluidCmd.Blit(depthTexID, BuiltinRenderTextureType.CameraTarget);
            }
            else if (showThicknessDebug && thicknessMat != null)
            {
                if (debugThicknessMat != null) fluidCmd.Blit(thicknessTexID, BuiltinRenderTextureType.CameraTarget, debugThicknessMat);
                else fluidCmd.Blit(thicknessTexID, BuiltinRenderTextureType.CameraTarget);
            }
            else if (showNormalDebug && normalMat != null)
            {
                fluidCmd.Blit(normalTexID, BuiltinRenderTextureType.CameraTarget);
            }
            else if (compositeMat != null)
            {
                compositeMat.SetColor("_Color", waterTint);
                compositeMat.SetFloat("_Absorption", absorption);
                compositeMat.SetFloat("_Smoothness", smoothness);
                compositeMat.SetFloat("_Specular", specular);
                compositeMat.SetFloat("_ThicknessCutoff", thicknessCutoff);
                compositeMat.SetFloat("_RefractionStrength", refractionStrength);
                fluidCmd.Blit(bgTexID, BuiltinRenderTextureType.CameraTarget, compositeMat);
            }
        }
    }

    void OnGUI()
    {
        if (showDepthDebug && debugDepthMat != null)
        {
            if (Event.current.type.Equals(EventType.Repaint))
                Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, debugDepthMat);
        }
        else if (showThicknessDebug && debugThicknessMat != null)
        {
            if (Event.current.type.Equals(EventType.Repaint))
                Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, debugThicknessMat);
        }
        else if (showNormalDebug && debugNormalMat != null)
        {
            if (Event.current.type.Equals(EventType.Repaint))
                Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, debugNormalMat);
        }
    }

    void OnDestroy()
    {
        if (mainCam != null && fluidCmd != null)
        {
            mainCam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
            fluidCmd.Release();
            fluidCmd = null;
        }
        Release(bufX); Release(bufV); Release(bufRho); Release(bufCellHead); Release(bufNextIndex); Release(particlesBuffer); Release(bufImpulses); Release(bufObstacles); Release(bufBoatCouplingSpheres); Release(bufBoatCouplingVelocities); Release(bufAccOut); Release(bufVCorrOut);
    }

    void ApplyCameraClipTuning(Camera cam)
    {
        Vector3 bmin = boundsMin;
        Vector3 bmax = boundsMax;
        Vector3[] pts = new Vector3[8];
        pts[0] = new Vector3(bmin.x, bmin.y, bmin.z);
        pts[1] = new Vector3(bmax.x, bmin.y, bmin.z);
        pts[2] = new Vector3(bmin.x, bmax.y, bmin.z);
        pts[3] = new Vector3(bmax.x, bmax.y, bmin.z);
        pts[4] = new Vector3(bmin.x, bmin.y, bmax.z);
        pts[5] = new Vector3(bmax.x, bmin.y, bmax.z);
        pts[6] = new Vector3(bmin.x, bmax.y, bmax.z);
        pts[7] = new Vector3(bmax.x, bmax.y, bmax.z);
        float minZ = float.PositiveInfinity;
        float maxZ = 0f;
        for (int i = 0; i < 8; i++)
        {
            float z = cam.WorldToViewportPoint(pts[i]).z;
            if (z > 0f)
            {
                minZ = Mathf.Min(minZ, z);
                maxZ = Mathf.Max(maxZ, z);
            }
        }
        if (!float.IsFinite(minZ) || maxZ <= 0f)
        {
            // fallback: use center
            Vector3 center = (bmin + bmax) * 0.5f;
            float zc = cam.WorldToViewportPoint(center).z;
            minZ = Mathf.Max(0.3f, zc - 10f);
            maxZ = zc + 30f;
        }
        float margin = Mathf.Max(clipMargin, (bmax - bmin).magnitude * 0.1f);
        float near = Mathf.Max(0.2f, minZ - margin);
        float far = Mathf.Max(near + 10f, maxZ + margin);
        // clamp to reasonable range
        far = Mathf.Min(far, near + 120f);
        cam.nearClipPlane = near;
        cam.farClipPlane = far;
    }

    void InitializeAdaptiveQualityState()
    {
        baseTargetDepthHeight = targetDepthHeight;
        baseDepthDownsample = depthDownsample;
        baseThicknessDownsample = thicknessDownsample;
        baseBlurIterations = blurIterations;
        baseThicknessBlurIterations = thicknessBlurIterations;
        baseMaxSubsteps = maxSubsteps;
        baseFixedTimeStep = fixedTimeStep;
        baseRenderParticleScale = renderParticleScale;
        baseParticleSize = particleSize;
        baseThicknessContribution = thicknessContribution;

        runtimeQualityLevel = ResolveInitialQualityLevel();
        ApplyQualityLevel(runtimeQualityLevel, false);
        runtimeParticleSize = particleSize;
        runtimeThicknessContribution = thicknessContribution;
        UpdateParticleSurfaceRuntime();
    }

    int ResolveInitialQualityLevel()
    {
        if (mobileQualityProfile == MobileQualityProfile.Performance) return 0;
        if (mobileQualityProfile == MobileQualityProfile.Balanced) return 1;
        if (mobileQualityProfile == MobileQualityProfile.Quality) return 2;
        if (!Application.isMobilePlatform) return 2;
        bool lowMemory = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 3000;
        bool lowGpuMemory = SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize <= 1200;
        if (lowMemory || lowGpuMemory) return 0;
        bool midMemory = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 5000;
        return midMemory ? 1 : 2;
    }

    void UpdateAdaptiveQualityState()
    {
        frameTimeEma = Mathf.Lerp(frameTimeEma, Mathf.Clamp(Time.unscaledDeltaTime, 1e-4f, 0.1f), 0.08f);
        if (!adaptiveQuality || qualityDownshiftFps >= qualityUpshiftFps) return;
        if (Time.frameCount < nextQualityEvalFrame) return;
        if ((Time.frameCount % Mathf.Max(1, qualityCheckIntervalFrames)) != 0) return;
        float fps = 1f / Mathf.Max(frameTimeEma, 1e-4f);
        if (fps < qualityDownshiftFps && runtimeQualityLevel > 0)
        {
            ApplyQualityLevel(runtimeQualityLevel - 1, true);
            nextQualityEvalFrame = Time.frameCount + Mathf.Max(1, qualityCooldownFrames);
        }
        else if (fps > qualityUpshiftFps && runtimeQualityLevel < 2)
        {
            ApplyQualityLevel(runtimeQualityLevel + 1, true);
            nextQualityEvalFrame = Time.frameCount + Mathf.Max(1, qualityCooldownFrames);
        }
    }

    void ApplyQualityLevel(int level, bool logChange)
    {
        runtimeQualityLevel = Mathf.Clamp(level, 0, 2);
        int depthDsAdd = 2 - runtimeQualityLevel;
        runtimeTargetDepthHeight = Mathf.Max(0, baseTargetDepthHeight - (2 - runtimeQualityLevel) * 120);
        runtimeDepthDownsample = Mathf.Clamp(baseDepthDownsample + depthDsAdd, 1, 4);
        runtimeThicknessDownsample = Mathf.Clamp(baseThicknessDownsample + depthDsAdd, 1, 5);
        runtimeBlurIterations = Mathf.Clamp(baseBlurIterations - (2 - runtimeQualityLevel), 0, 10);
        runtimeThicknessBlurIterations = Mathf.Clamp(baseThicknessBlurIterations - (2 - runtimeQualityLevel), 0, 5);
        runtimeMaxSubsteps = Mathf.Clamp(baseMaxSubsteps - (2 - runtimeQualityLevel), 1, 12);
        runtimeFixedTimeStep = Mathf.Clamp(baseFixedTimeStep * (1f + (2 - runtimeQualityLevel) * 0.08f), 0.0025f, 0.02f);
        runtimeRenderParticleScale = Mathf.Clamp(baseRenderParticleScale + (2 - runtimeQualityLevel) * 0.04f, 1f, 2f);
        if (logChange)
            Debug.Log($"SPH Adaptive Quality -> L{runtimeQualityLevel} fps~{(1f / Mathf.Max(frameTimeEma, 1e-4f)):F1}, depthDS={runtimeDepthDownsample}, thickDS={runtimeThicknessDownsample}, substeps={runtimeMaxSubsteps}");
    }

    void UpdateParticleSurfaceRuntime()
    {
        if (!autoTuneParticleSurface)
        {
            runtimeParticleSize = particleSize;
            runtimeThicknessContribution = thicknessContribution;
            return;
        }
        Vector3 simSize = boundsMax - boundsMin;
        float cellSize = (simSize.x + simSize.y + simSize.z) / (3f * Mathf.Max(1, gridResolution));
        cellSize = Mathf.Max(cellSize, 1e-4f);
        Vector3 sMin, sMax;
        GetSpawnBounds(out sMin, out sMax);
        Vector3 spawnSize = sMax - sMin;
        float spawnVol = Mathf.Max(1e-6f, spawnSize.x * spawnSize.y * spawnSize.z);
        float spacing = Mathf.Pow(spawnVol / Mathf.Max(1, particleCount), 1f / 3f);
        float targetEffectiveRadius = spacing * particleOverlapRatio;
        float minEffectiveRadius = cellSize * minParticleToCellRatio;
        float maxEffectiveRadius = cellSize * maxParticleToCellRatio;
        float effectiveRadius = Mathf.Clamp(targetEffectiveRadius, minEffectiveRadius, maxEffectiveRadius);
        float baseEffective = Mathf.Max(1e-4f, baseParticleSize * Mathf.Max(baseRenderParticleScale, 0.01f));
        effectiveRadius = Mathf.Max(effectiveRadius, baseEffective * 0.84f);
        float targetParticleSize = effectiveRadius / Mathf.Max(0.01f, runtimeRenderParticleScale);
        targetParticleSize = Mathf.Clamp(targetParticleSize, 0.045f, 0.5f);
        runtimeParticleSize = Mathf.Lerp(runtimeParticleSize, targetParticleSize, Mathf.Clamp01(particleSurfaceTuneLerp));
        float effectiveNow = runtimeParticleSize * runtimeRenderParticleScale;
        float shrink = Mathf.Clamp(baseEffective / Mathf.Max(effectiveNow, 1e-4f), 0.75f, 1.9f);
        float thicknessBoost = Mathf.Pow(shrink, 0.85f);
        runtimeThicknessContribution = thicknessContribution * thicknessBoost;
        runtimeThicknessContribution = Mathf.Clamp(runtimeThicknessContribution, thicknessContribution * 0.92f, thicknessContribution * 1.85f);
    }

    void ApplyMediumParticleStability()
    {
        // 标准 WCSPH 中粒子档：依靠人工粘性维持稳定。
        maxSubsteps = Mathf.Clamp(Mathf.Max(maxSubsteps, 6), 6, 12);
        fixedTimeStep = Mathf.Clamp(fixedTimeStep, 0.0035f, 0.005f);
        viscosity = Mathf.Clamp(viscosity, 0.015f, 0.04f);
        xsphC = Mathf.Clamp(xsphC, 0.03f, 0.10f);
        boundaryDamping = Mathf.Clamp(boundaryDamping, 0.45f, 0.65f);
        boundaryDampingZ = Mathf.Clamp(boundaryDampingZ, 0.45f, 0.65f);
        soundSpeed = Mathf.Clamp(soundSpeed, 28f, 42f);
        maxSpeed = Mathf.Min(maxSpeed, 14.0f);
        initialJitter = Mathf.Clamp(initialJitter, 0.015f, 0.04f);
        eosGamma = 1.0f;
        // 人工粘性是真正让显式 WCSPH 稳的关键，不能为 0
        artViscAlpha = Mathf.Clamp(artViscAlpha, 0.4f, 0.85f);
        maxAcceleration = Mathf.Clamp(maxAcceleration, 60f, 150f);
    }
    
    void ApplyRealisticWaterPreset()
    {
        // 标准移动端 SPH 「活水」基线：线性 EOS + 人工粘性 + 弱 Mueller + 弱 XSPH。
        enableLowParticleCountTuning = false;
        viscosity = 0.02f;
        xsphC = 0.05f;
        boundaryDamping = 0.55f;
        boundaryDampingZ = 0.55f;
        boundaryMaxBounceSpeedZ = 3.5f;
        minSpeed = 0.0f;
        eosGamma = 1.0f;
        soundSpeed = Mathf.Clamp(soundSpeed, 28f, 38f);
        maxSpeed = Mathf.Min(maxSpeed, 14.0f);
        artViscAlpha = 0.55f;     // 关键稳定项；0.5–0.7 是水的常用区间
        artViscBeta = 0.0f;
        maxAcceleration = 80f;
    }
    void ApplySweepFlowPreset()
    {
        // 仅调外部交互强度与渲染外观；不再额外修改物理阻尼。
        obstaclePushStrength = 0f;       // 已不在 shader 内推力，仅球体非穿透
        obstacleTangentialStrength = 0f;  // 同上
        obstacleFriction = 0.0f;
        stirStrengthScale = 2.0f;
        stirAngularBoost = 2.0f;
        impulseNormalCoeff = 1.0f;
        impulseTangentialCoeff = 1.0f;
        minImpulseRadius = Mathf.Max(minImpulseRadius, 0.6f);
        normalStrength = Mathf.Clamp(normalStrength, 0.85f, 1.6f);
        renderParticleScale = Mathf.Clamp(renderParticleScale, 1.12f, 1.32f);
        anisotropyScale = 0f;            // SPH 不做各向异性
        blurIterations = Mathf.Clamp(blurIterations, 1, 2);
        thicknessBlurIterations = Mathf.Clamp(thicknessBlurIterations, 1, 2);
    }
    public void ResetSimulation()
    {
        var xInit = new Vector3[particleCount];
        var vInit = new Vector3[particleCount];
        FillInitial(xInit, vInit);
        if (bufX != null) bufX.SetData(xInit);
        if (bufV != null) bufV.SetData(vInit);
        if (bufRho != null) bufRho.SetData(new float[particleCount]);
        int gridCount = gridResolution * gridResolution * gridResolution;
        if (bufCellHead != null) bufCellHead.SetData(CreateFilled(gridCount, -1));
        if (bufNextIndex != null) bufNextIndex.SetData(CreateFilled(particleCount, -1));
        externalBoatSphereCount = 0;
        if (bufImpulses != null) bufImpulses.SetData(new Vector4[Mathf.Max(1, bufImpulses.count)]);
        if (bufObstacles != null) bufObstacles.SetData(new Vector4[Mathf.Max(1, bufObstacles.count)]);
        if (bufBoatCouplingSpheres != null) bufBoatCouplingSpheres.SetData(new Vector4[Mathf.Max(1, bufBoatCouplingSpheres.count)]);
        if (bufBoatCouplingVelocities != null) bufBoatCouplingVelocities.SetData(new Vector4[Mathf.Max(1, bufBoatCouplingVelocities.count)]);
        if (bufAccOut != null) bufAccOut.SetData(new Vector4[particleCount]);
        if (bufVCorrOut != null) bufVCorrOut.SetData(new Vector4[particleCount]);
        simAccumulator = 0f;
        stirSpeed = 0f;
        stickAngularSpeed = 0f;
        stirPosInitialized = false;
        stickRotInitialized = false;
        runSimulation = true;
        WarmupSimulation();
    }

    public void Pause()
    {
        runSimulation = false;
    }

    public void Resume()
    {
        runSimulation = true;
    }

    public void StepOnce()
    {
        if (cs == null || !isActiveAndEnabled) return;
        float dt = Mathf.Clamp(runtimeFixedTimeStep, 1e-4f, 0.05f);
        SimulateStep(dt);
        Draw();
    }

    void Release(ComputeBuffer b){ if (b != null) b.Release(); }

    int[] CreateFilled(int n, int v)
    {
        var a = new int[n];
        for (int i = 0; i < n; i++) a[i] = v;
        return a;
    }

    void FillInitial(Vector3[] xInit, Vector3[] vInit)
    {
        Vector3 sMin, sMax; GetSpawnBounds(out sMin, out sMax);
        Vector3 size = sMax - sMin;

        // 各向同性排布：根据 spawn 盒体积与目标粒子数推一个统一间距 s，
        // 然后按 s 反算 (nx, ny, nz)。这样三方向间距相等，避免初始密度的方向偏差
        // 引发的「方向性压力梯度 → 沸腾」。
        float spawnVol = Mathf.Max(1e-6f, size.x * size.y * size.z);
        float s = Mathf.Pow(spawnVol / Mathf.Max(1, particleCount), 1f / 3f);
        s = Mathf.Max(s, 1e-3f);
        int nx = Mathf.Max(1, Mathf.CeilToInt(size.x / s));
        int ny = Mathf.Max(1, Mathf.CeilToInt(size.y / s));
        int nz = Mathf.Max(1, Mathf.CeilToInt(size.z / s));
        // 微调使三向 step 接近 s（避免最后一行/列被压扁）
        Vector3 step = new Vector3(size.x / nx, size.y / ny, size.z / nz);

        int idx = 0;
        for (int iz = 0; iz < nz && idx < particleCount; iz++)
        for (int iy = 0; iy < ny && idx < particleCount; iy++)
        for (int ix = 0; ix < nx && idx < particleCount; ix++)
        {
            // 抖动用各向同性的 s，而不是 step（否则又把 step 的差异带回来）
            Vector3 jitter = new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f) * (s * initialJitter);
            Vector3 p = sMin + new Vector3((ix + 0.5f) * step.x, (iy + 0.5f) * step.y, (iz + 0.5f) * step.z) + jitter;
            xInit[idx] = p;
            vInit[idx] = Vector3.zero;
            idx++;
        }
        for (; idx < particleCount; idx++)
        {
            xInit[idx] = sMin + 0.5f * size;
            vInit[idx] = Vector3.zero;
        }
    }

    void GetSpawnBounds(out Vector3 sMin, out Vector3 sMax)
    {
        sMin = ClampToBounds(spawnMin);
        sMax = ClampToBounds(spawnMax);
        if (sMax.x <= sMin.x || sMax.y <= sMin.y || sMax.z <= sMin.z)
        {
            sMin = boundsMin;
            sMax = boundsMax;
        }
    }

    Vector3 ClampToBounds(Vector3 p)
    {
        return new Vector3(
            Mathf.Clamp(p.x, boundsMin.x, boundsMax.x),
            Mathf.Clamp(p.y, boundsMin.y, boundsMax.y),
            Mathf.Clamp(p.z, boundsMin.z, boundsMax.z)
        );
    }
    
    public bool TryGetWaterLevel(out float level, int sampleCount = 1024, int start = 0)
    {
        level = (spawnMin.y + spawnMax.y) * 0.5f;
        int total = Mathf.Max(1, particleCount);
        int count = Mathf.Clamp(sampleCount, 1, total);
        int cbStart = Mathf.Clamp(start, 0, Mathf.Max(0, total - count));
        if (bufX == null) return false;
        var tmp = new Vector3[count];
        try
        {
            bufX.GetData(tmp, 0, cbStart, count);
        }
        catch
        {
            return false;
        }
        float sum = 0f;
        int n = 0;
        float yMin = boundsMin.y + 0.01f;
        for (int i = 0; i < tmp.Length; i++)
        {
            float y = tmp[i].y;
            if (y > yMin) { sum += y; n++; }
        }
        if (n > 0) level = sum / n;
        return n > 0;
    }
    
    // 维护 top-K 高度的小型有序数组（升序）。
    static void InsertTopK(float[] topK, ref int topCount, int K, float y)
    {
        if (topCount < K)
        {
            int idx = topCount;
            while (idx > 0 && topK[idx - 1] > y) { topK[idx] = topK[idx - 1]; idx--; }
            topK[idx] = y;
            topCount++;
        }
        else if (y > topK[0])
        {
            int idx = 0;
            while (idx + 1 < K && topK[idx + 1] < y) { topK[idx] = topK[idx + 1]; idx++; }
            topK[idx] = y;
        }
    }

    public bool TryGetLocalWaterLevel(Vector3 center, float radius, int maxSamples, out float level)
    {
        level = (spawnMin.y + spawnMax.y) * 0.5f;
        if (bufCellHead == null || bufNextIndex == null || bufX == null) return false;
        Vector3 size = boundsMax - boundsMin;
        Vector3 cellSize = new Vector3(
            Mathf.Max(size.x / Mathf.Max(1, gridResolution), 1e-6f),
            Mathf.Max(size.y / Mathf.Max(1, gridResolution), 1e-6f),
            Mathf.Max(size.z / Mathf.Max(1, gridResolution), 1e-6f)
        );
        Vector3 rel = center - boundsMin;
        int cx = Mathf.Clamp(Mathf.FloorToInt(rel.x / cellSize.x), 0, gridResolution - 1);
        int cy = Mathf.Clamp(Mathf.FloorToInt(rel.y / cellSize.y), 0, gridResolution - 1);
        int cz = Mathf.Clamp(Mathf.FloorToInt(rel.z / cellSize.z), 0, gridResolution - 1);
        int rx = Mathf.Clamp(Mathf.CeilToInt(radius / cellSize.x), 0, gridResolution - 1);
        int ry = Mathf.Clamp(Mathf.CeilToInt(radius / cellSize.y), 0, gridResolution - 1);
        int rz = Mathf.Clamp(Mathf.CeilToInt(radius / cellSize.z), 0, gridResolution - 1);
        // 关键修正：水位必须是「上表面」，所以对附近粒子的 y 取 top-K 平均值，
        // 而不是简单平均（旧实现会把整个水柱中点当作水位 → SPH 模式船一直沉）。
        const int K = 6;
        float[] topK = new float[K];
        int topCount = 0;
        int n = 0;
        int maxN = Mathf.Max(1, maxSamples);
        float r2 = radius * radius;
        for (int iz = Mathf.Max(0, cz - rz); iz <= Mathf.Min(gridResolution - 1, cz + rz) && n < maxN; iz++)
        {
            for (int iy = Mathf.Max(0, cy - ry); iy <= Mathf.Min(gridResolution - 1, cy + ry) && n < maxN; iy++)
            {
                for (int ix = Mathf.Max(0, cx - rx); ix <= Mathf.Min(gridResolution - 1, cx + rx) && n < maxN; ix++)
                {
                    int cellIndex = ix + iy * gridResolution + iz * gridResolution * gridResolution;
                    var headArr = new int[1];
                    try { bufCellHead.GetData(headArr, 0, cellIndex, 1); } catch { continue; }
                    int pi = headArr[0];
                    int chainCount = 0;
                    while (pi >= 0 && n < maxN && chainCount < 16)
                    {
                        var posArr = new Vector3[1];
                        try { bufX.GetData(posArr, 0, pi, 1); } catch { break; }
                        Vector3 p = posArr[0];
                        Vector3 d = p - center;
                        d.y = 0f;
                        if (d.sqrMagnitude <= r2)
                        {
                            InsertTopK(topK, ref topCount, K, p.y);
                            n++;
                        }
                        var nextArr = new int[1];
                        try { bufNextIndex.GetData(nextArr, 0, pi, 1); } catch { break; }
                        pi = nextArr[0];
                        chainCount++;
                    }
                }
            }
        }
        if (topCount > 0)
        {
            float s = 0f;
            for (int i = 0; i < topCount; i++) s += topK[i];
            level = s / topCount;
        }
        return topCount > 0;
    }
    
    public bool TryGetLocalFlow(Vector3 center, float radius, int maxSamples, out Vector3 avgVel)
    {
        avgVel = Vector3.zero;
        if (bufCellHead == null || bufNextIndex == null || bufX == null || bufV == null) return false;
        Vector3 size = boundsMax - boundsMin;
        Vector3 cellSize = new Vector3(
            Mathf.Max(size.x / Mathf.Max(1, gridResolution), 1e-6f),
            Mathf.Max(size.y / Mathf.Max(1, gridResolution), 1e-6f),
            Mathf.Max(size.z / Mathf.Max(1, gridResolution), 1e-6f)
        );
        Vector3 rel = center - boundsMin;
        int cx = Mathf.Clamp(Mathf.FloorToInt(rel.x / cellSize.x), 0, gridResolution - 1);
        int cy = Mathf.Clamp(Mathf.FloorToInt(rel.y / cellSize.y), 0, gridResolution - 1);
        int cz = Mathf.Clamp(Mathf.FloorToInt(rel.z / cellSize.z), 0, gridResolution - 1);
        int rx = Mathf.Clamp(Mathf.CeilToInt(radius / cellSize.x), 0, gridResolution - 1);
        int ry = Mathf.Clamp(Mathf.CeilToInt(radius / cellSize.y), 0, gridResolution - 1);
        int rz = Mathf.Clamp(Mathf.CeilToInt(radius / cellSize.z), 0, gridResolution - 1);
        Vector3 sumV = Vector3.zero;
        int n = 0;
        int maxN = Mathf.Max(1, maxSamples);
        for (int iz = Mathf.Max(0, cz - rz); iz <= Mathf.Min(gridResolution - 1, cz + rz) && n < maxN; iz++)
        {
            for (int iy = Mathf.Max(0, cy - ry); iy <= Mathf.Min(gridResolution - 1, cy + ry) && n < maxN; iy++)
            {
                for (int ix = Mathf.Max(0, cx - rx); ix <= Mathf.Min(gridResolution - 1, cx + rx) && n < maxN; ix++)
                {
                    int cellIndex = ix + iy * gridResolution + iz * gridResolution * gridResolution;
                    var headArr = new int[1];
                    try { bufCellHead.GetData(headArr, 0, cellIndex, 1); } catch { continue; }
                    int pi = headArr[0];
                    int chainCount = 0;
                    while (pi >= 0 && n < maxN && chainCount < 16)
                    {
                        var posArr = new Vector3[1];
                        try { bufX.GetData(posArr, 0, pi, 1); } catch { break; }
                        Vector3 p = posArr[0];
                        Vector3 d = p - center; d.y = 0f;
                        if (d.sqrMagnitude <= radius * radius)
                        {
                            var velArr = new Vector3[1];
                            try { bufV.GetData(velArr, 0, pi, 1); } catch { break; }
                            sumV += velArr[0];
                            n++;
                        }
                        var nextArr = new int[1];
                        try { bufNextIndex.GetData(nextArr, 0, pi, 1); } catch { break; }
                        pi = nextArr[0];
                        chainCount++;
                    }
                }
            }
        }
        if (n > 0) avgVel = sumV / n;
        return n > 0;
    }
    
    void UpdateCpuCacheIfNeeded(bool needVel)
    {
        int f = Time.frameCount;
        if (cpuXCache == null || cpuXCache.Length != particleCount) cpuXCache = new Vector3[particleCount];
        if (needVel && (cpuVCache == null || cpuVCache.Length != particleCount)) cpuVCache = new Vector3[particleCount];
        if ((f - cpuCacheFrame) >= Mathf.Max(1, cpuCacheStrideFrames))
        {
            if (bufX != null) { try { bufX.GetData(cpuXCache); } catch {} }
            if (needVel && bufV != null) { try { bufV.GetData(cpuVCache); } catch {} }
            cpuCacheFrame = f;
        }
    }
    
    public bool TryGetLocalWaterLevelCached(Vector3 center, float radius, out float level)
    {
        level = (spawnMin.y + spawnMax.y) * 0.5f;
        UpdateCpuCacheIfNeeded(false);
        if (cpuXCache == null) return false;
        // 关键修正：以 top-K 平均（最高粒子）作为水位，避免对水柱整体取平均把船的水位
        // 永远拉到水柱中点 → SPH 浮力始终偏弱、小船下沉。
        const int K = 8;
        float[] topK = new float[K];
        int topCount = 0;
        int n = 0;
        float r2 = radius * radius;
        for (int i = 0; i < cpuXCache.Length; i++)
        {
            Vector3 p = cpuXCache[i];
            Vector3 d = p - center; d.y = 0f;
            if (d.sqrMagnitude <= r2)
            {
                InsertTopK(topK, ref topCount, K, p.y);
                n++;
            }
        }
        if (topCount > 0)
        {
            float s = 0f;
            for (int i = 0; i < topCount; i++) s += topK[i];
            level = s / topCount;
        }
        return n > 0;
    }
    
    public bool TryGetLocalFlowCached(Vector3 center, float radius, out Vector3 avgVel)
    {
        avgVel = Vector3.zero;
        UpdateCpuCacheIfNeeded(true);
        if (cpuXCache == null || cpuVCache == null) return false;
        Vector3 sumV = Vector3.zero;
        int n = 0;
        float r2 = radius * radius;
        for (int i = 0; i < cpuXCache.Length; i++)
        {
            Vector3 p = cpuXCache[i];
            Vector3 d = p - center; d.y = 0f;
            if (d.sqrMagnitude <= r2)
            {
                sumV += cpuVCache[i];
                n++;
            }
        }
        if (n > 0) avgVel = sumV / Mathf.Max(1, n);
        return n > 0;
    }
    
    
    void UpdateStirMetrics(Vector3 newWorldPos)
    {
        if (!stirPosInitialized)
        {
            lastStirPos = newWorldPos;
            stirPosInitialized = true;
            stirSpeed = 0f;
            return;
        }
        float ds = (newWorldPos - lastStirPos).magnitude;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float sp = ds / dt;
        stirSpeed = Mathf.Lerp(stirSpeed, sp, 0.65f);
        Vector3 d = newWorldPos - lastStirPos;
        Vector3 dXZ = new Vector3(d.x, 0f, d.z);
        if (dXZ.sqrMagnitude > 1e-6f) stirDirXZ = dXZ.normalized;
        lastStirPos = newWorldPos;
    }
    
}
