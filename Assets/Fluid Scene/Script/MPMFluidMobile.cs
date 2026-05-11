using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-20)]
public class MPMFluidMobile : MonoBehaviour
{
    public const int MaxParticleCountMobile = 10000;
    public const int MinParticleCount = 256;

    [Header("Simulation")]
    [Tooltip("Total number of particles in the simulation.")]
    [Range(256, 10000)]
    public int particleCount = 8000;
    [Tooltip("Minimum corner of the simulation boundary box.")]
    public Vector3 boundsMin = new Vector3(0, 0, 0);
    [Tooltip("Maximum corner of the simulation boundary box.")]
    public Vector3 boundsMax = new Vector3(15, 10, 5);
    [Tooltip("Grid resolution. Higher values (e.g. 64) reduce blockiness but cost performance. 普通机型建议 32–40。")]
    [Range(16, 48)]
    public int gridResolution = 40;
    [Tooltip("Target density of the fluid (kg/m^3).")]
    public float restDensity = 1000.0f;
    [Tooltip("Stiffness of the fluid (Sound Speed). Higher values make the fluid less compressible but more explosive.")]
    public float soundSpeed = 45.0f;
    [Tooltip("Exponent for the Equation of State (EOS). Typically 7 for water.")]
    public float eosGamma = 7.0f;
    [Tooltip("网格速度平滑（0–1）：减轻抖动但会拖慢运动；过大显「粘稠」。轻盈感可试约 0.2–0.35。")]
    [Range(0f, 1f)]
    public float gridSmooth = 0.28f;
    [Tooltip("Friction of the boundary walls. 0 = Super slippery, 1 = Sticky.")]
    [Range(0f, 1f)]
    public float boundaryFriction = 0.02f;
    [Tooltip("Scale factor for initial particle mass. < 1.0 reduces initial expansion/explosion.")]
    [Range(0.8f, 1.2f)]
    public float initialMassScale = 0.95f;
    public float viscosity = 0.014f;
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public float particleSize = 0.12f; // Reverted to original for detail
    public bool runSimulation = true;
    [Range(0f, 1.5f)] public float initialJitter = 0.0f;
    public bool enableSubstepping = true;
    public float fixedTimeStep = 0.0045f;
    public int maxSubsteps = 5;
    
    [Header("Thickness")]
    [Range(0.01f, 0.5f)] public float thicknessContribution = 0.052f;
    [Range(0, 5)] public int thicknessBlurIterations = 1;
    [Range(1, 20)] public int thicknessBlurRadius = 10;
    [Range(1, 5)] public int thicknessDownsample = 2;

    [Header("Normals")]
    [Range(0.1f, 10f)] public float normalStrength = 0.88f;
    
    [Header("Debug")]
    public bool enableRendering = true; // Master switch for fluid rendering
    public bool renderParticles = false; // Default off to see fluid only
    public bool showDepthDebug = false;
    public bool showThicknessDebug = false;
    public bool showNormalDebug = false;
    public Color particleColor = new Color(0.1f, 0.45f, 1f, 1f);

    [Header("Interaction")]
    public Vector4 colliderSphere = Vector4.zero; // xyz, radius
    public Vector3 colliderVelocity = Vector3.zero;

    public Vector4 stirrerSphere = Vector4.zero; // xyz, radius
    public Vector3 stirrerVelocity = Vector3.zero;

    public struct HullSphere
    {
        public Vector4 sphere; // xyz, radius
        public Vector3 velocity;
        public float padding; // align to 32 bytes (4+3+1 = 8 floats)
    }
    ComputeBuffer boatSpheresBuffer;
    int boatSphereCount = 0;

    public void SetBoatSpheres(HullSphere[] spheres, int count)
    {
        if (spheres == null || count == 0)
        {
            boatSphereCount = 0;
            return;
        }

        if (boatSpheresBuffer == null || boatSpheresBuffer.count < count)
        {
            if (boatSpheresBuffer != null) boatSpheresBuffer.Release();
            boatSpheresBuffer = new ComputeBuffer(count, 32); // 8 floats * 4 bytes
        }

        boatSpheresBuffer.SetData(spheres, 0, 0, count);
        boatSphereCount = count;
        if (cs != null) cs.SetBuffer(kGridUpdate, "boatSpheres", boatSpheresBuffer);
    }

    public struct ProbeData
    {
        public Vector3 position;
        public Vector3 velocity;
        public float density;
    }

    [HideInInspector] public bool simulateInLateUpdate = true;
    [Header("Adaptive/Power")]
    [HideInInspector] public int targetFrameRate = 60;
    public enum MobileQualityProfile { Auto, Performance, Balanced, Quality }
    [Tooltip("移动端质量档：Auto 会按设备能力给初始档位，并可在运行时自适应。")]
    public MobileQualityProfile mobileQualityProfile = MobileQualityProfile.Balanced;
    [Tooltip("运行时根据帧率动态升降质。开启后优先保证帧率，再尽量保留液面细节。")]
    public bool adaptiveQuality = false;
    [Range(30f, 120f)] public float qualityDownshiftFps = 50f;
    [Range(30f, 120f)] public float qualityUpshiftFps = 58f;
    [Range(10, 240)] public int qualityCheckIntervalFrames = 20;
    [Range(30, 600)] public int qualityCooldownFrames = 120;
    [Header("Camera")]
    public bool autoCameraClipTuning = true;
    public float clipMargin = 5f;
    public bool debugLogStats = false;
    public int debugLogStrideFrames = 60;
    public int debugSampleCount = 256;
    public float pressureWeight = 0.12f;
    public float maxSpeed = 25f;
    public float pressureRampTime = 0.6f;
    float simTime = 0f;
    [HideInInspector] public float particleMass = 1.0f;

    ComputeShader cs;
    int kClearGrid;
    int kP2G;
    int kGridUpdate;
    int kG2P;
    int kGridToRender;
    int kProbeGrid;

    ComputeBuffer bufX;
    ComputeBuffer bufV;
    ComputeBuffer bufC0;
    ComputeBuffer bufC1;
    ComputeBuffer bufC2;
    ComputeBuffer bufGridMassI;
    ComputeBuffer bufGridMomX;
    ComputeBuffer bufGridMomY;
    ComputeBuffer bufGridMomZ;

    struct Particle { public Vector4 position; public Vector4 color; public Vector4 velocity; }
    ComputeBuffer particlesBuffer;
    Material gridParticleMat;

    [Header("Anisotropy")]
    [Range(1f, 2f)] public float renderParticleScale = 1.45f;
    [Range(0f, 5f)] public float anisotropyScale = 0.3f;
    [Range(1f, 10f)] public float maxAnisotropy = 3.0f;

    [Header("Depth Filtering")]
    public DepthFilterType filterType = DepthFilterType.Gaussian;
    public enum DepthFilterType { Bilateral, Gaussian }
    
    [Tooltip("Target vertical resolution for the depth buffer (e.g. 720p). 0 = Manual Downsample。普通机型建议 360–480。")]
    public int targetDepthHeight = 520;
    [Range(1, 4)] public int depthDownsample = 2;
    [Range(0, 10)] public int blurIterations = 2;
    [Range(0.1f, 50f)] public float blurSigmaSpatial = 9.5f;
    [Range(0.01f, 5f)] public float blurSigmaRange = 2.5f;
    [Range(1, 20)] public int blurRadius = 8;

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
    [Header("Rendering")]
    public Color fluidColor = new Color(0.0f, 0.5f, 1.0f, 1.0f);
    [Range(0f, 5f)] public float absorption = 0.82f;
    [Range(0f, 1f)] public float smoothness = 0.9f;
    [Range(0f, 1f)] public float specular = 0.58f;
    [Range(0f, 0.5f)] public float thicknessCutoff = 0.05f; // New threshold to trim wavy edges
    [Range(0f, 0.2f)] public float refractionStrength = 0.02f; // New refraction strength
    [Header("Particle Surface Tuning")]
    [Tooltip("根据粒子平均间距自动匹配渲染半径与厚度，减少颗粒感/糊面。")]
    public bool autoTuneParticleSurface = false;
    [Range(0.45f, 0.9f)] public float particleOverlapRatio = 0.62f;
    [Range(0.2f, 0.8f)] public float minParticleToCellRatio = 0.34f;
    [Range(0.5f, 1.2f)] public float maxParticleToCellRatio = 0.88f;
    [Range(0.01f, 1f)] public float particleSurfaceTuneLerp = 0.18f;

    MaterialPropertyBlock props;
    
    Camera mainCam;

    Mesh sphereMesh;
    Bounds drawBounds;
    int baseTargetDepthHeight;
    int baseDepthDownsample;
    int baseThicknessDownsample;
    int baseBlurIterations;
    int baseThicknessBlurIterations;
    int baseMaxSubsteps;
    float baseFixedTimeStep;
    float baseGridSmooth;
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
    float runtimeGridSmooth;
    float runtimeRenderParticleScale;
    float runtimeParticleSize;
    float runtimeThicknessContribution;
    int runtimeQualityLevel = 2;
    float frameTimeEma = 1f / 60f;
    int nextQualityEvalFrame = 0;

    [Header("Spawn Settings")]
    [Tooltip("How to distribute particles initially.")]
    public SpawnMode spawnMode = SpawnMode.PureRandom;
    public enum SpawnMode { GridRandom, PureRandom }

    [Tooltip("Minimum corner of the particle spawn volume.")]
    public Vector3 spawnMin = new Vector3(4, 2, 1);
    [Tooltip("Maximum corner of the particle spawn volume.")]
    public Vector3 spawnMax = new Vector3(10, 6, 4);

#if UNITY_EDITOR
    void OnValidate()
    {
        particleCount = Mathf.Clamp(particleCount, MinParticleCount, MaxParticleCountMobile);
        gridResolution = Mathf.Clamp(gridResolution, 16, 48);
        if (qualityUpshiftFps < qualityDownshiftFps + 2f) qualityUpshiftFps = qualityDownshiftFps + 2f;
        if (maxParticleToCellRatio < minParticleToCellRatio + 0.05f) maxParticleToCellRatio = minParticleToCellRatio + 0.05f;
    }
#endif

    void Start()
    {
        cs = Resources.Load<ComputeShader>("Shader/Compute Shader/MPM/Mobile/mpm_fluid_mobile");
        if (cs == null) { enabled = false; return; }
        particleCount = Mathf.Clamp(particleCount, MinParticleCount, MaxParticleCountMobile);
        gridResolution = Mathf.Clamp(gridResolution, 16, 48);
        kClearGrid = cs.FindKernel("ClearGrid");
        kP2G = cs.FindKernel("P2G");
        kGridUpdate = cs.FindKernel("GridUpdate");
        kG2P = cs.FindKernel("G2P");
        kGridToRender = cs.FindKernel("GridToRenderParticles");
        kProbeGrid = cs.FindKernel("ProbeGrid");

        int gridCount = gridResolution * gridResolution * gridResolution;
        bufX = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufV = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufC0 = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufC1 = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufC2 = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufGridMassI = new ComputeBuffer(gridCount, sizeof(int));
        bufGridMomX = new ComputeBuffer(gridCount, sizeof(int));
        bufGridMomY = new ComputeBuffer(gridCount, sizeof(int));
        bufGridMomZ = new ComputeBuffer(gridCount, sizeof(int));
        particlesBuffer = new ComputeBuffer(particleCount, sizeof(float) * 12); // 4+4+4

        var xInit = new Vector3[particleCount];
        var vInit = new Vector3[particleCount];
        FillInitial(xInit, vInit);
        bufX.SetData(xInit);
        bufV.SetData(vInit);
        bufC0.SetData(new Vector3[particleCount]);
        bufC1.SetData(new Vector3[particleCount]);
        bufC2.SetData(new Vector3[particleCount]);
        bufGridMassI.SetData(new int[gridCount]);
        bufGridMomX.SetData(new int[gridCount]);
        bufGridMomY.SetData(new int[gridCount]);
        bufGridMomZ.SetData(new int[gridCount]);

        // Initialize boat spheres buffer with dummy data to prevent shader errors
        boatSpheresBuffer = new ComputeBuffer(1, 32);
        boatSpheresBuffer.SetData(new HullSphere[1]);
        boatSphereCount = 0;

        BindMpmPersistentKernelBuffers();
        cs.SetBuffer(kGridUpdate, "boatSpheres", boatSpheresBuffer);

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

        if (targetFrameRate > 0) Application.targetFrameRate = targetFrameRate;
        InitializeAdaptiveQualityState();

        // 启动 warmup：模拟少量步（无重力或小重力都可以保持当前重力），让粒子从 spawn
        // 区下落开始接近稳态。FluidBoat 还会有 settle 期，组合起来确保第一帧画面已经稳定。
        // 注意：这里仅是「先跑几步」让 GPU buffer 进入有效状态，避免编辑器编译热重载时
        // 第一帧因 buffer 尚未填充导致渲染瞬间空帧/突跳。
        {
            float dtWarmStart = Mathf.Clamp(runtimeFixedTimeStep, 1e-4f, 0.01f);
            for (int i = 0; i < 3; i++) SimulateStep(dtWarmStart);
        }

        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.depthTextureMode |= DepthTextureMode.Depth;

            fluidCmd = new CommandBuffer();
            fluidCmd.name = "MPM Fluid Rendering";
            
            bgTexID = Shader.PropertyToID("_FluidBackgroundTexture");
            depthTexID = Shader.PropertyToID("_FluidDepthTexture");
            thicknessTexID = Shader.PropertyToID("_FluidThicknessTexture");
            normalTexID = Shader.PropertyToID("_FluidNormalTexture");
        }
        RegisterFluidCommandBuffer();
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

    public void Pause() { runSimulation = false; }

    public void Resume() { runSimulation = true; }

    public void ResetSimulation()
    {
        if (bufX == null || bufV == null || cs == null) return;
        simTime = 0f;
        var xInit = new Vector3[particleCount];
        var vInit = new Vector3[particleCount];
        FillInitial(xInit, vInit);
        bufX.SetData(xInit);
        bufV.SetData(vInit);
        bufC0.SetData(new Vector3[particleCount]);
        bufC1.SetData(new Vector3[particleCount]);
        bufC2.SetData(new Vector3[particleCount]);
        int gridCount = gridResolution * gridResolution * gridResolution;
        bufGridMassI.SetData(new int[gridCount]);
        bufGridMomX.SetData(new int[gridCount]);
        bufGridMomY.SetData(new int[gridCount]);
        bufGridMomZ.SetData(new int[gridCount]);
        boatSphereCount = 0;
        if (boatSpheresBuffer != null)
        {
            boatSpheresBuffer.SetData(new HullSphere[1]);
            if (cs != null) cs.SetBuffer(kGridUpdate, "boatSpheres", boatSpheresBuffer);
        }
        colliderSphere = Vector4.zero;
        colliderVelocity = Vector3.zero;
        stirrerSphere = Vector4.zero;
        stirrerVelocity = Vector3.zero;
        // 重置后做少量预热，减少第一帧压力尖峰导致的抽搐。
        float dtWarm = Mathf.Clamp(runtimeFixedTimeStep * 0.8f, 1e-4f, 0.02f);
        for (int i = 0; i < 4; i++) SimulateStep(dtWarm);
        runSimulation = true;
    }

    public void StepOnce()
    {
        if (cs == null || !isActiveAndEnabled) return;
        float dt = Mathf.Clamp(runtimeFixedTimeStep, 1e-4f, 0.05f);
        SimulateStep(dt);
        Draw();
    }

    void Update()
    {
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
        int steps = enableSubstepping ? Mathf.Clamp(Mathf.CeilToInt(dtFrame / runtimeFixedTimeStep), 1, runtimeMaxSubsteps) : 1;
        float dtStep = dtFrame / steps;
        for (int s = 0; s < steps; s++)
        {
            SimulateStep(dtStep);
        }
        if (debugLogStats) LogStatsIfNeeded();
        Draw();
    }

    void SimulateStep(float dt)
    {
        if (cs == null || bufX == null || bufGridMassI == null || particlesBuffer == null) return;

        int gridCount = gridResolution * gridResolution * gridResolution;
        int groupsGrid = (gridCount + 127) / 128;
        int groupsParticle = (particleCount + 127) / 128;

        // 每步重新绑定：shader 在编辑器重编译等情况下会清空内核 buffer 绑定，否则 Dispatch 报 Property not set。
        BindMpmPersistentKernelBuffers();
        if (boatSpheresBuffer != null)
            cs.SetBuffer(kGridUpdate, "boatSpheres", boatSpheresBuffer);

        cs.SetInt("n_grid", gridResolution);
        cs.SetInt("particle_num", particleCount);
        cs.SetVector("boundsMin", boundsMin);
        cs.SetVector("boundsMax", boundsMax);
        cs.SetFloat("dt", dt);
        cs.SetFloat("restDensity", restDensity);
        cs.SetFloat("particleMass", particleMass);
        cs.SetFloat("viscosity", viscosity);
        cs.SetVector("gravity", gravity);
        cs.SetFloat("eosGamma", eosGamma);
        cs.SetFloat("soundSpeed", soundSpeed);
        cs.SetFloat("gridSmoothWeight", runtimeGridSmooth);
        cs.SetFloat("boundaryFriction", boundaryFriction);
        simTime += dt;
        float ramp = pressureRampTime > 0f ? Mathf.Clamp01(simTime / pressureRampTime) : 1f;
        cs.SetFloat("pressureWeight", Mathf.Clamp(pressureWeight * ramp, 0f, 1f));
        cs.SetFloat("maxSpeed", Mathf.Max(1f, maxSpeed));
        cs.SetVector("renderTint", particleColor);
        cs.SetVector("colliderSphere", colliderSphere);
        cs.SetVector("colliderVelocity", colliderVelocity);

        cs.SetVector("stirrerSphere", stirrerSphere);
        cs.SetVector("stirrerVelocity", stirrerVelocity);

        cs.SetInt("boatSphereCount", boatSphereCount);

        cs.Dispatch(kClearGrid, groupsGrid, 1, 1);

        cs.Dispatch(kP2G, groupsParticle, 1, 1);

        cs.Dispatch(kGridUpdate, groupsGrid, 1, 1);

        cs.Dispatch(kG2P, groupsParticle, 1, 1);

        // Grid To Render Particles (Smoothing)
        cs.Dispatch(kGridToRender, groupsParticle, 1, 1);
    }

    void BindMpmPersistentKernelBuffers()
    {
        cs.SetBuffer(kClearGrid, "gridMassI_rw", bufGridMassI);
        cs.SetBuffer(kClearGrid, "gridMomX_rw", bufGridMomX);
        cs.SetBuffer(kClearGrid, "gridMomY_rw", bufGridMomY);
        cs.SetBuffer(kClearGrid, "gridMomZ_rw", bufGridMomZ);

        cs.SetBuffer(kP2G, "x_ro", bufX);
        cs.SetBuffer(kP2G, "v_ro", bufV);
        cs.SetBuffer(kP2G, "C0_ro", bufC0);
        cs.SetBuffer(kP2G, "C1_ro", bufC1);
        cs.SetBuffer(kP2G, "C2_ro", bufC2);
        cs.SetBuffer(kP2G, "gridMassI_rw", bufGridMassI);
        cs.SetBuffer(kP2G, "gridMomX_rw", bufGridMomX);
        cs.SetBuffer(kP2G, "gridMomY_rw", bufGridMomY);
        cs.SetBuffer(kP2G, "gridMomZ_rw", bufGridMomZ);

        cs.SetBuffer(kGridUpdate, "gridMassI_rw", bufGridMassI);
        cs.SetBuffer(kGridUpdate, "gridMomX_rw", bufGridMomX);
        cs.SetBuffer(kGridUpdate, "gridMomY_rw", bufGridMomY);
        cs.SetBuffer(kGridUpdate, "gridMomZ_rw", bufGridMomZ);

        cs.SetBuffer(kG2P, "x", bufX);
        cs.SetBuffer(kG2P, "v", bufV);
        cs.SetBuffer(kG2P, "C0", bufC0);
        cs.SetBuffer(kG2P, "C1", bufC1);
        cs.SetBuffer(kG2P, "C2", bufC2);
        cs.SetBuffer(kG2P, "gridMassI", bufGridMassI);
        cs.SetBuffer(kG2P, "gridMomX", bufGridMomX);
        cs.SetBuffer(kG2P, "gridMomY", bufGridMomY);
        cs.SetBuffer(kG2P, "gridMomZ", bufGridMomZ);

        cs.SetBuffer(kGridToRender, "x", bufX);
        cs.SetBuffer(kGridToRender, "v", bufV);
        cs.SetBuffer(kGridToRender, "gridMassI", bufGridMassI);
        cs.SetBuffer(kGridToRender, "gridMomX", bufGridMomX);
        cs.SetBuffer(kGridToRender, "gridMomY", bufGridMomY);
        cs.SetBuffer(kGridToRender, "gridMomZ", bufGridMomZ);
        cs.SetBuffer(kGridToRender, "_particlesBuffer", particlesBuffer);
    }

    public void DispatchProbe(ComputeBuffer probeBuf, int count)
    {
        if (probeBuf == null || count == 0) return;

        cs.SetInt("_probeCount", count);
        cs.SetBuffer(kProbeGrid, "_probeBuffer", probeBuf);

        // Bind grid buffers (must be consistent with G2P inputs)
        cs.SetBuffer(kProbeGrid, "gridMassI", bufGridMassI);
        cs.SetBuffer(kProbeGrid, "gridMomX", bufGridMomX);
        cs.SetBuffer(kProbeGrid, "gridMomY", bufGridMomY);
        cs.SetBuffer(kProbeGrid, "gridMomZ", bufGridMomZ);

        int groups = (count + 63) / 64;
        cs.Dispatch(kProbeGrid, groups, 1, 1);
    }

    void Draw()
    {
        if (fluidCmd != null && !enableRendering)
        {
            fluidCmd.Clear();
            return;
        }

        if (sphereMesh == null || gridParticleMat == null || particlesBuffer == null) return;
        drawBounds.center = (boundsMin + boundsMax) * 0.5f;
        drawBounds.size = boundsMax - boundsMin + Vector3.one * 2f;
        
        props.Clear();
        props.SetFloat("_size", runtimeParticleSize);
        props.SetFloat("_SizeScale", runtimeRenderParticleScale);
        props.SetFloat("_AnisotropyScale", anisotropyScale);
        props.SetFloat("_MaxAnisotropy", maxAnisotropy);
        props.SetBuffer("_particlesBuffer", particlesBuffer);
        
        // GLOBAL BUFFER SETTING (Critical for CommandBuffer Instancing)
        Shader.SetGlobalBuffer("_particlesBuffer", particlesBuffer);
        Shader.SetGlobalFloat("_SizeScale", runtimeRenderParticleScale);
        Shader.SetGlobalFloat("_size", runtimeParticleSize);
        Shader.SetGlobalFloat("_AnisotropyScale", anisotropyScale);
        Shader.SetGlobalFloat("_MaxAnisotropy", maxAnisotropy);

        if (renderParticles)
        {
            Graphics.DrawMeshInstancedProcedural(sphereMesh, 0, gridParticleMat, drawBounds, particleCount, props, ShadowCastingMode.On, true);
        }

        if (fluidCmd != null)
        {
            fluidCmd.Clear();
            int fluidDepthW = 0;
            int fluidDepthH = 0;

            // 1. Background Capture
            fluidCmd.GetTemporaryRT(bgTexID, -1, -1, 0, FilterMode.Bilinear);
            fluidCmd.Blit(BuiltinRenderTextureType.CurrentActive, bgTexID);
            fluidCmd.SetGlobalTexture("_FluidBackgroundTexture", bgTexID);
            
            // 2. Depth Pass
            if (depthMat != null)
            {
                RenderTextureFormat depthFmt = MobileSsfRenderShared.SelectSingleChannelFloatFormat();
                
                // Adaptive Downsampling: Ensure consistent blur radius across different screen DPIs
                // If targetDepthHeight > 0, we calculate downsample to match that height roughly.
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

                // Depth Blur（中间 RT 必须与深度同分辨率；原先用全屏 -1 在移动端会浪费大量带宽）
                Material currentBlurMat = (filterType == DepthFilterType.Gaussian && gaussianMat != null) ? gaussianMat : blurMat;

                if (currentBlurMat != null && runtimeBlurIterations > 0)
                {
                    currentBlurMat.SetFloat("_SigmaSpatial", blurSigmaSpatial);
                    // Always pass SigmaRange now, even for Gaussian (Smart Gaussian uses it for edge preservation)
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

            // 3. Thickness Pass
            if (thicknessMat != null)
            {
                props.SetFloat("_ContributionScale", runtimeThicknessContribution);
                props.SetFloat("_SizeScale", runtimeRenderParticleScale); // Sync size scale for thickness
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
                    for(int i=0; i<runtimeThicknessBlurIterations; i++)
                    {
                        fluidCmd.Blit(thicknessTexID, tempID, thicknessBlurMat, 0);
                        fluidCmd.Blit(tempID, thicknessTexID, thicknessBlurMat, 1);
                    }
                    fluidCmd.ReleaseTemporaryRT(tempID);
                }
                fluidCmd.SetGlobalTexture("_FluidThicknessTexture", thicknessTexID);
            }

            // 4. Normal Pass（与深度 RT 同尺寸，避免与 effectiveDownsample 不一致）
             if (normalMat != null && depthMat != null && fluidDepthW > 0 && fluidDepthH > 0)
             {
                 normalMat.SetFloat("_NormalStrength", normalStrength);
                 fluidCmd.GetTemporaryRT(normalTexID, fluidDepthW, fluidDepthH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
                 
                 // Explicitly Blit from Depth to Normal (sets _MainTex to depthTexID)
                 fluidCmd.Blit(depthTexID, normalTexID, normalMat);
                 
                 fluidCmd.SetGlobalTexture("_FluidNormalTexture", normalTexID);
             }

             // 5. Output Selection
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
                 compositeMat.SetColor("_Color", fluidColor);
                 compositeMat.SetFloat("_Absorption", absorption);
                 compositeMat.SetFloat("_Smoothness", smoothness);
                 compositeMat.SetFloat("_Specular", specular);
                 compositeMat.SetFloat("_ThicknessCutoff", thicknessCutoff);
                 compositeMat.SetFloat("_RefractionStrength", refractionStrength);
                 fluidCmd.Blit(bgTexID, BuiltinRenderTextureType.CameraTarget, compositeMat);
             }
        }
    }

    void OnDestroy()
    {
        if (mainCam != null && fluidCmd != null)
        {
            mainCam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
            fluidCmd.Release();
        }

        Release(bufX); Release(bufV); Release(bufC0); Release(bufC1); Release(bufC2); Release(bufGridMassI); Release(bufGridMomX); Release(bufGridMomY); Release(bufGridMomZ);
        Release(particlesBuffer);
        Release(boatSpheresBuffer);
    }

    void Release(ComputeBuffer b) { if (b != null) b.Release(); }

    void FillInitial(Vector3[] xInit, Vector3[] vInit)
    {
        Vector3 sMin, sMax; GetSpawnBounds(out sMin, out sMax);
        Vector3 size = sMax - sMin;
        float maxSide = Mathf.Max(1e-6f, Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
        int baseN = Mathf.Max(1, Mathf.RoundToInt(Mathf.Pow(Mathf.Max(1, particleCount), 1f / 3f)));
        int nx = Mathf.Max(1, Mathf.RoundToInt(baseN * (size.x / maxSide)));
        int ny = Mathf.Max(1, Mathf.RoundToInt(baseN * (size.y / maxSide)));
        int nz = Mathf.Max(1, Mathf.RoundToInt(baseN * (size.z / maxSide)));
        int target = Mathf.Max(1, particleCount);
        int prod = Mathf.Max(1, nx * ny * nz);
        int guard = 10000;
        while (prod != target && guard-- > 0)
        {
            if (prod > target)
            {
                if (nx >= ny && nx >= nz && nx > 1) nx--;
                else if (ny >= nx && ny >= nz && ny > 1) ny--;
                else if (nz > 1) nz--;
            }
            else
            {
                if (size.x >= size.y && size.x >= size.z) nx++;
                else if (size.y >= size.x && size.y >= size.z) ny++;
                else nz++;
            }
            prod = Mathf.Max(1, nx * ny * nz);
        }
        Vector3 step = new Vector3(size.x / nx, size.y / ny, size.z / nz);
        int idx = 0;

        if (spawnMode == SpawnMode.PureRandom)
        {
            for (int i = 0; i < particleCount; i++)
            {
                Vector3 p = new Vector3(
                    Mathf.Lerp(sMin.x, sMax.x, Random.value),
                    Mathf.Lerp(sMin.y, sMax.y, Random.value),
                    Mathf.Lerp(sMin.z, sMax.z, Random.value)
                );
                xInit[i] = p;
                vInit[i] = Vector3.zero;
            }
        }
        else
        {
            // Grid Random (Stratified)
            for (int iz = 0; iz < nz && idx < particleCount; iz++)
            for (int iy = 0; iy < ny && idx < particleCount; iy++)
            for (int ix = 0; ix < nx && idx < particleCount; ix++)
            {
                // Jitter covers the full cell if initialJitter is 1.0
                Vector3 jitter = new Vector3(
                    (Random.value - 0.5f) * step.x * initialJitter, 
                    (Random.value - 0.5f) * step.y * initialJitter, 
                    (Random.value - 0.5f) * step.z * initialJitter
                );
                Vector3 p = sMin + new Vector3((ix + 0.5f) * step.x, (iy + 0.5f) * step.y, (iz + 0.5f) * step.z) + jitter;
                xInit[idx] = p;
                vInit[idx] = Vector3.zero;
                idx++;
            }
            
            // Shuffle to break alignment artifacts
            for (int i = 0; i < idx; i++) {
                int rnd = Random.Range(i, idx);
                Vector3 tempX = xInit[i]; xInit[i] = xInit[rnd]; xInit[rnd] = tempX;
                // vInit is all zero anyway
            }
        }
        
        float spawnVol = Mathf.Max(1e-6f, size.x * size.y * size.z);
        particleMass = restDensity * (spawnVol / Mathf.Max(1, particleCount)) * initialMassScale;
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
            Vector3 center = (bmin + bmax) * 0.5f;
            float zc = cam.WorldToViewportPoint(center).z;
            minZ = Mathf.Max(0.3f, zc - 10f);
            maxZ = zc + 30f;
        }
        float margin = Mathf.Max(clipMargin, (bmax - bmin).magnitude * 0.1f);
        float near = Mathf.Max(0.2f, minZ - margin);
        float far = Mathf.Max(near + 10f, maxZ + margin);
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
        baseGridSmooth = gridSmooth;
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
        int thickDsAdd = 2 - runtimeQualityLevel;
        int blurMinus = 2 - runtimeQualityLevel;

        runtimeTargetDepthHeight = Mathf.Max(0, baseTargetDepthHeight - (2 - runtimeQualityLevel) * 120);
        runtimeDepthDownsample = Mathf.Clamp(baseDepthDownsample + depthDsAdd, 1, 4);
        runtimeThicknessDownsample = Mathf.Clamp(baseThicknessDownsample + thickDsAdd, 1, 5);
        runtimeBlurIterations = Mathf.Clamp(baseBlurIterations - blurMinus, 0, 10);
        runtimeThicknessBlurIterations = Mathf.Clamp(baseThicknessBlurIterations - (2 - runtimeQualityLevel), 0, 5);
        runtimeMaxSubsteps = Mathf.Clamp(baseMaxSubsteps - (2 - runtimeQualityLevel), 1, 8);
        runtimeFixedTimeStep = Mathf.Clamp(baseFixedTimeStep * (1f + (2 - runtimeQualityLevel) * 0.08f), 0.0025f, 0.02f);
        runtimeGridSmooth = Mathf.Clamp(baseGridSmooth + (2 - runtimeQualityLevel) * 0.05f, 0f, 1f);
        runtimeRenderParticleScale = Mathf.Clamp(baseRenderParticleScale + (2 - runtimeQualityLevel) * 0.04f, 1f, 2f);

        if (logChange)
        {
            Debug.Log($"MPM Adaptive Quality -> L{runtimeQualityLevel} fps~{(1f / Mathf.Max(frameTimeEma, 1e-4f)):F1}, depthDS={runtimeDepthDownsample}, thickDS={runtimeThicknessDownsample}, substeps={runtimeMaxSubsteps}");
        }
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
        // 避免自动标定把粒径压得过小导致液面明显发透明。
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

    void LogStatsIfNeeded()
    {
        int f = Time.frameCount;
        if ((f % Mathf.Max(1, debugLogStrideFrames)) != 0) return;
        if (bufV == null || bufX == null) return;
        int count = Mathf.Clamp(debugSampleCount, 1, particleCount);
        int start = Mathf.Clamp(particleCount / 2 - count / 2, 0, Mathf.Max(0, particleCount - count));
        var vel = new Vector3[count];
        var pos = new Vector3[count];
        try { bufV.GetData(vel, 0, start, count); bufX.GetData(pos, 0, start, count); } catch { return; }
        float sum = 0f;
        float maxSp = 0f;
        int moving = 0;
        for (int i = 0; i < vel.Length; i++)
        {
            float s = vel[i].magnitude;
            sum += s;
            if (s > maxSp) maxSp = s;
            if (s > 0.01f) moving++;
        }
        float avg = sum / Mathf.Max(1, vel.Length);
        Debug.Log($"MPM Stats frame={f} avgSpeed={avg:F3} maxSpeed={maxSp:F3} moving={moving}/{vel.Length} y[{pos[0].y:F3}-{pos[pos.Length-1].y:F3}]");
    }

    void OnGUI()
    {
        if (showDepthDebug && debugDepthMat != null)
        {
            if (Event.current.type.Equals(EventType.Repaint))
            {
                Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, debugDepthMat);
            }
        }
        else if (showThicknessDebug && debugThicknessMat != null)
        {
            if (Event.current.type.Equals(EventType.Repaint))
            {
                 Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, debugThicknessMat);
             }
         }
         else if (showNormalDebug && debugNormalMat != null)
         {
             if (Event.current.type.Equals(EventType.Repaint))
             {
                 Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, debugNormalMat);
             }
         }
    }
}
