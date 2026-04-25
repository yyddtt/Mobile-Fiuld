using UnityEngine;
using UnityEngine.Rendering;

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
    public int targetDepthHeight = 420;
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

    MaterialPropertyBlock props;
    
    Camera mainCam;

    Mesh sphereMesh;
    Bounds drawBounds;

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

        var gph = Shader.Find("Instanced/GridParticleMobile");
        if (gph != null)
        {
            gridParticleMat = new Material(gph);
            gridParticleMat.enableInstancing = true;
            gridParticleMat.SetFloat("_size", particleSize);
        }

        var dph = Shader.Find("Instanced/GridParticleDepth");
        if (dph != null)
        {
            depthMat = new Material(dph);
            depthMat.enableInstancing = true;
        }

        var debugShader = Shader.Find("Fluid/DebugDepth");
        if (debugShader != null)
        {
            debugDepthMat = new Material(debugShader);
        }

        var blurShader = Shader.Find("SSFR/DepthBilateral");
        if (blurShader != null) blurMat = new Material(blurShader);

        var gaussShader = Shader.Find("SSFR/DepthGaussianSmart");
        if (gaussShader != null) gaussianMat = new Material(gaussShader);

        var thShader = Shader.Find("Instanced/GridParticleThickness");
        if (thShader != null)
        {
            thicknessMat = new Material(thShader);
            thicknessMat.enableInstancing = true;
        }

        var thBlurShader = Shader.Find("SSFR/ThicknessBlur");
        if (thBlurShader != null) thicknessBlurMat = new Material(thBlurShader);

        var debugThShader = Shader.Find("Fluid/DebugThickness");
        if (debugThShader != null) debugThicknessMat = new Material(debugThShader);

        var normShader = Shader.Find("SSFR/FluidNormals");
        if (normShader != null) normalMat = new Material(normShader);

        var debugNormShader = Shader.Find("Fluid/DebugNormal");
        if (debugNormShader != null) debugNormalMat = new Material(debugNormShader);

        var compShader = Shader.Find("SSFR/FluidComposite");
        if (compShader != null) compositeMat = new Material(compShader);

        sphereMesh = CreateSphereMesh();
        props = new MaterialPropertyBlock();
        drawBounds = new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin + Vector3.one * 2f);

        if (depthMat != null) depthMat.SetBuffer("_particlesBuffer", particlesBuffer);
        if (thicknessMat != null) thicknessMat.SetBuffer("_particlesBuffer", particlesBuffer);

        if (targetFrameRate > 0) Application.targetFrameRate = targetFrameRate;

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
            
            mainCam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
        }
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
        if (!runSimulation) { Draw(); return; }
        float dtFrame = Mathf.Clamp(Time.deltaTime, 1e-4f, 0.05f);
        int steps = enableSubstepping ? Mathf.Clamp(Mathf.CeilToInt(dtFrame / fixedTimeStep), 1, maxSubsteps) : 1;
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
        int gridCount = gridResolution * gridResolution * gridResolution;
        int groupsGrid = (gridCount + 127) / 128;
        int groupsParticle = (particleCount + 127) / 128;

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
        cs.SetFloat("gridSmoothWeight", gridSmooth);
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
        props.SetFloat("_size", particleSize);
        props.SetFloat("_SizeScale", renderParticleScale);
        props.SetFloat("_AnisotropyScale", anisotropyScale);
        props.SetFloat("_MaxAnisotropy", maxAnisotropy);
        props.SetBuffer("_particlesBuffer", particlesBuffer);
        
        // GLOBAL BUFFER SETTING (Critical for CommandBuffer Instancing)
        Shader.SetGlobalBuffer("_particlesBuffer", particlesBuffer);
        Shader.SetGlobalFloat("_SizeScale", renderParticleScale);
        Shader.SetGlobalFloat("_size", particleSize);
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
                RenderTextureFormat depthFmt = SelectSingleChannelFloatFormat();
                
                // Adaptive Downsampling: Ensure consistent blur radius across different screen DPIs
                // If targetDepthHeight > 0, we calculate downsample to match that height roughly.
                int effectiveDownsample = depthDownsample;
                if (targetDepthHeight > 0)
                {
                    float scale = (float)Screen.height / (float)targetDepthHeight;
                    effectiveDownsample = Mathf.Max(depthDownsample, Mathf.RoundToInt(scale));
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

                if (currentBlurMat != null && blurIterations > 0)
                {
                    currentBlurMat.SetFloat("_SigmaSpatial", blurSigmaSpatial);
                    // Always pass SigmaRange now, even for Gaussian (Smart Gaussian uses it for edge preservation)
                    currentBlurMat.SetFloat("_SigmaRange", blurSigmaRange);
                    currentBlurMat.SetInt("_FilterRadius", blurRadius);

                    int tempDepthID = Shader.PropertyToID("_FluidDepthTemp");
                    fluidCmd.GetTemporaryRT(tempDepthID, dw, dh, 0, FilterMode.Bilinear, depthFmt);

                    for (int i = 0; i < blurIterations; i++)
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
                props.SetFloat("_ContributionScale", thicknessContribution);
                props.SetFloat("_SizeScale", renderParticleScale); // Sync size scale for thickness
                int w = Screen.width / thicknessDownsample;
                int h = Screen.height / thicknessDownsample;
                RenderTextureFormat thickFmt = SelectSingleChannelFloatFormat();
                
                fluidCmd.GetTemporaryRT(thicknessTexID, w, h, 0, FilterMode.Bilinear, thickFmt);
                fluidCmd.SetRenderTarget(thicknessTexID);
                fluidCmd.ClearRenderTarget(false, true, Color.black);
                fluidCmd.DrawMeshInstancedProcedural(sphereMesh, 0, thicknessMat, 0, particleCount, props);

                if (thicknessBlurMat != null && thicknessBlurIterations > 0)
                {
                    thicknessBlurMat.SetInt("_FilterRadius", thicknessBlurRadius);
                    int tempID = Shader.PropertyToID("_FluidThicknessTemp");
                    fluidCmd.GetTemporaryRT(tempID, w, h, 0, FilterMode.Bilinear, thickFmt);
                    for(int i=0; i<thicknessBlurIterations; i++)
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
        if (mainCam != null)
        {
            if (fluidCmd != null) { mainCam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd); fluidCmd.Release(); }
        }

        Release(bufX); Release(bufV); Release(bufC0); Release(bufC1); Release(bufC2); Release(bufGridMassI); Release(bufGridMomX); Release(bufGridMomY); Release(bufGridMomZ);
        Release(particlesBuffer);
        Release(boatSpheresBuffer);
    }

    RenderTextureFormat SelectSingleChannelFloatFormat()
    {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf)) return RenderTextureFormat.RHalf;
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat)) return RenderTextureFormat.RFloat;
        return RenderTextureFormat.R8;
    }

    void ApplySharpEdgesPreset()
    {
        // no-op after render pipeline removal
    }

    void ApplyRealisticWaterPreset()
    {
        viscosity = 0.02f;
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

    Mesh CreateSphereMesh()
    {
        int segments = Application.isMobilePlatform ? 16 : 24;
        int rings = Application.isMobilePlatform ? 12 : 16;
        var verts = new Vector3[(rings + 1) * (segments + 1)];
        var normals = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];
        int vi = 0;
        for (int r = 0; r <= rings; r++)
        {
            float v = (float)r / rings;
            float phi = v * Mathf.PI;
            for (int s = 0; s <= segments; s++)
            {
                float u = (float)s / segments;
                float theta = u * Mathf.PI * 2f;
                float x = Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = Mathf.Cos(phi);
                float z = Mathf.Sin(phi) * Mathf.Sin(theta);
                var p = new Vector3(x, y, z);
                verts[vi] = p;
                normals[vi] = p.normalized;
                uvs[vi] = new Vector2(u, v);
                vi++;
            }
        }
        int[] tris = new int[rings * segments * 6];
        int ti = 0;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int a = r * (segments + 1) + s;
                int b = a + segments + 1;
                int c = a + 1;
                int d = b + 1;
                tris[ti++] = a;
                tris[ti++] = b;
                tris[ti++] = c;
                tris[ti++] = c;
                tris[ti++] = b;
                tris[ti++] = d;
            }
        }
        var mesh = new Mesh();
        mesh.name = "ProceduralSphere";
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
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
