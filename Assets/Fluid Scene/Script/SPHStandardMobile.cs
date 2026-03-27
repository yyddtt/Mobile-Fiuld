using UnityEngine;
using UnityEngine.Rendering;

public class SPHStandardMobile : MonoBehaviour
{
    public enum RenderMode { Fluid, GridParticles }
    public enum DepthSmoothMode { None, CurvatureFlow, Bilateral, Gaussian }
    [Header("Simulation")]
    public int particleCount = 7000;
    public Vector3 boundsMin = new Vector3(0,0,0);
    public Vector3 boundsMax = new Vector3(15,10,5);
    [HideInInspector] public int gridResolution = 32;
    public float neighbourRadius = 0.35f;
    [HideInInspector] public float particleMass = 1.0f;
    public float restDensity = 1000.0f;
    public float viscosity = 0.08f;
    [HideInInspector] public float eosGamma = 7.0f;
    [HideInInspector] public float soundSpeed = 25.0f;
    public Vector3 gravity = new Vector3(0,-9.8f,0);
    [HideInInspector] public float maxSpeed = 12.0f;
    [HideInInspector] public float boundaryDamping = 0.5f;
    [HideInInspector] public float boundaryDampingZ = 0.6f;
    [HideInInspector] public float boundaryMaxBounceSpeedZ = 3.5f;
    [HideInInspector] public float xsphC = 0.2f;
    public float particleSize = 0.18f;
    public bool runSimulation = true;
    [HideInInspector] public bool enableLowParticleCountTuning = true;
    public float minSpeed = 0.015f;
    
    public Vector3 spawnMin = new Vector3(4,2,1);
    public Vector3 spawnMax = new Vector3(10,6,4);
    [HideInInspector] public bool autoCalibrateMass = true;
    [HideInInspector] public bool autoNeighbourRadius = true;
    [HideInInspector] public float initialJitter = 0.05f;
    public bool enableSubstepping = true;
    public float fixedTimeStep = 0.005f;
    public int maxSubsteps = 4;
    public RenderMode renderMode = RenderMode.Fluid;
    [Header("Fluid Composite")]
    public Color waterTint = new Color(0.6f,0.8f,1f,1f);
    public float waterOpacity = 0.6f;
    public float refractStrength = 0.02f;
    public float fresnelPower = 4f;
    public float waterSoftDepth = 0.08f;
    public float minDepthVisibility = 0.1f;
    public float edgeBoost = 1.2f;
    public float edgeWidth = 0.009f;
    public float alphaFloor = 0.08f;
    public bool useBlurredDepthForNormals = true;
    [HideInInspector] public bool simulateInLateUpdate = true;
    [Header("Physics Preset")]
    public bool realisticWaterPreset = true;
    public bool sweepFlowPreset = true;
    public float tintMix = 0.5f;
    public float envStrength = 0.6f;
    public float specularStrength = 0.8f;
    public float highlightClamp = 0.93f;
    public float absorption = 0.8f;
    public float reflectionThicknessSuppress = 1.0f;
    public float refractionThicknessSuppress = 0.35f;
    public bool enableReflection = true;
    public bool enableRefraction = true;
    public bool useBackground = true;
    public float backgroundWeight = 1f;
    public bool sharpEdgesPreset = true;
    [HideInInspector] public float highQualityDepthScale = 1.0f;
    [Header("Adaptive/Power")]
    [HideInInspector] public int targetFrameRate = 60;
    [Header("Camera")]
    public bool autoCameraClipTuning = true;
    public float clipMargin = 5f;
    [Header("Frame Stride (Advanced)")]
    [HideInInspector] public int thicknessUpdateStride = 1;
    [HideInInspector] public int thicknessNormalsUpdateStride = 1;
    [HideInInspector] public int depthNormalsUpdateStride = 1;

    ComputeShader cs;
    int kClearGrid;
    int kBuildGrid;
    int kDensity;
    int kMain;

    ComputeBuffer bufX;
    ComputeBuffer bufV;
    ComputeBuffer bufRho;
    ComputeBuffer bufCellHead;
    ComputeBuffer bufNextIndex;
    ComputeBuffer bufImpulses;
    ComputeBuffer bufObstacles;

    struct Particle { public Vector3 position; public Vector4 color; }
    ComputeBuffer particlesBuffer;
    Material gridParticleMat;

    Material fluidMat;
    Material depthMat;
    Material thicknessMat;
    Material thicknessBlurMat;
    Material thicknessNormalsMat;
    Material thicknessAdditiveMat;
    Material depthNormalsMat;
    Material depthGaussianBlurMat;
    Material depthBilateralBlurMat;
    Material depthCurvatureFlowMat;
    Mesh sphereMesh;
    Bounds drawBounds;
    RenderTexture particleDepthRT;
    RenderTexture particleDepthBackRT;
    RenderTexture particleDepthTempRT;
    RenderTexture particleBlurredDepthRT;
    RenderTexture depthNormalRT;
    RenderTexture particleThicknessRT;
    RenderTexture particleThicknessTempRT;
    RenderTexture particleBlurredThicknessRT;
    RenderTexture thicknessNormalRT;
    CommandBuffer depthCB;
    CommandBuffer thicknessCB;
    public bool enableDepthPass = true;
    [HideInInspector] public Vector2Int depthRTSize = new Vector2Int(256, 256);
    public bool showDepthPreview = true;
    public bool enableDepthBlur = true;
    public bool useBilateralDepthBlur = true;
    public bool showBlurredDepthPreview = true;
    [HideInInspector] public Vector4 depthGaussianWeights = new Vector4(0.15f, 0.4f, 0.15f, 0.15f);
    public float depthSigmaSpatial = 2.8f;
    public float depthSigmaRange = 0.035f;
    [Header("Depth Smoothing")]
    public DepthSmoothMode depthSmoothMode = DepthSmoothMode.Bilateral;
    [HideInInspector] public int curvatureFlowIterations = 8;
    [HideInInspector] public float curvatureFlowLambda = 0.2f;
    [HideInInspector] public float curvatureFlowSigmaRange = 0.03f;
    public bool depthDebugToScreen = false;
    [Range(0,2)] public int depthDebugMode = 0; // 0=Depth,1=White,2=ID gradient
    public bool enableThicknessPass = true;
    public bool thicknessAdditive = true;
    public float thicknessContributionScale = 0.035f;
    [HideInInspector] public float thicknessThreshold = 0.005f;
    public float thicknessScale = 1.3f;
    public bool showThicknessPreview = true;
    public bool enableThicknessBlur = true;
    public bool showBlurredThicknessPreview = true;
    [Header("Normals")]
    public bool enableThicknessNormals = true;
    public float thicknessNormalStrength = 1.3f;
    public bool showThicknessNormalsPreview = true;
    [Header("Preview/Debug")]
    public float previewScale = 1.0f;
    public float compositeThicknessScale = 1.0f;
    public float compositeThicknessGamma = 1.0f;
    public float compositeThicknessMax = 8.0f;
    public float compositeThicknessExposure = 0.80f;
    public float thicknessTopBias = 0.5f;
    [HideInInspector] public Vector4 thicknessBlurWeights = new Vector4(0.18f, 0.52f, 0.18f, 0.12f);
    public float depthEdgeStrength = 1.6f;
    public float depthEdgeThreshold = 0.0025f;
    public float fresnelAlphaBase = 0.6f;
    public float fresnelAlphaWeight = 1.0f;
    public float refractionClampPixels = 0.7f;
    public float refractionEdgeSuppress = 0.7f;
    public bool showDepthNormalsPreview = false;
    
    public float depthNormalStrength = 11.0f;
    public float depthNormalWeight = 0.25f;
    public Color absorptionColor = new Color(0.8f,0.6f,0.5f,1f);
    public bool enableStir = false;
    public Transform stirTransform;
    public float stirRadius = 0.6f;
    public float stirStrength = 50f;
    public Transform[] obstacleTransforms;
    public float obstacleDefaultRadius = 0.5f;
    public float obstaclePushStrength = 40f;
    public float obstacleDamping = 0.6f;
    public Transform barrierTransform;
    public bool enableBarrierMove = true;
    public bool requireSelectionToMove = true;
    public LayerMask selectableMask = ~0;
    public float vorticityEps = 0.35f;
    public float surfaceTension = 0.0f;
    public float freeSurfaceDamping = 0.0f;
    public float freeSurfaceThreshold = 0.75f;
    
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

    void Start()
    {
        cs = Resources.Load<ComputeShader>("Shader/Compute Shader/SPH/Mobile/sph_standard_mobile");
        if (cs == null) { enabled = false; return; }
        kClearGrid = cs.FindKernel("ClearGrid");
        kBuildGrid = cs.FindKernel("BuildGrid");
        kDensity = cs.FindKernel("ComputeDensity");
        kMain = cs.FindKernel("SPHMain");

        int gridCount = gridResolution * gridResolution * gridResolution;
        bufX = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufV = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufRho = new ComputeBuffer(particleCount, sizeof(float));
        bufCellHead = new ComputeBuffer(gridCount, sizeof(int));
        bufNextIndex = new ComputeBuffer(particleCount, sizeof(int));
        particlesBuffer = new ComputeBuffer(particleCount, sizeof(float) * (3 + 4));
        bufImpulses = new ComputeBuffer(Mathf.Max(1, maxImpulses), sizeof(float) * 4);
        bufImpulses.SetData(new Vector4[1] { Vector4.zero });
        bufObstacles = new ComputeBuffer(Mathf.Max(1, obstacleTransforms != null ? obstacleTransforms.Length : 1), sizeof(float) * 4);

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
            int nx = Mathf.Max(1, Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f)));
            int ny = nx;
            int nz = nx;
            Vector3 step = new Vector3(size.x / nx, size.y / ny, size.z / nz);
            if (autoCalibrateMass)
            {
                float cellVol = Mathf.Max(step.x * step.y * step.z, 1e-6f);
                particleMass = restDensity * cellVol;
            }
            if (autoNeighbourRadius)
            {
                float maxStep = Mathf.Max(step.x, Mathf.Max(step.y, step.z));
                neighbourRadius = Mathf.Max(neighbourRadius, maxStep * 2.0f);
            }
        }

        var fsh = Shader.Find("Instanced/FluidCompositeMobile");
        if (fsh == null) { enabled = false; return; }
        fluidMat = new Material(fsh);
        fluidMat.enableInstancing = true;
        fluidMat.SetFloat("_size", particleSize);
        fluidMat.SetColor("_TintColor", waterTint);
        fluidMat.SetFloat("_Opacity", waterOpacity);
        fluidMat.SetFloat("_RefractStrength", refractStrength);
        fluidMat.SetFloat("_FresnelPower", fresnelPower);
        fluidMat.SetFloat("_SoftDepth", waterSoftDepth);
        fluidMat.SetFloat("_MinDepthVisibility", minDepthVisibility);
        fluidMat.SetFloat("_EdgeBoost", edgeBoost);
        fluidMat.SetFloat("_EdgeWidth", edgeWidth);
        fluidMat.SetFloat("_AlphaFloor", alphaFloor);
        fluidMat.SetFloat("_TintMix", tintMix);
        fluidMat.SetFloat("_EnvStrength", envStrength);
        fluidMat.SetFloat("_SpecularStrength", specularStrength);
        fluidMat.SetFloat("_HighlightClamp", highlightClamp);
        fluidMat.SetFloat("_Absorption", absorption);
        fluidMat.SetFloat("_ReflectionThicknessSuppress", reflectionThicknessSuppress);
        fluidMat.SetFloat("_RefractionThicknessSuppress", refractionThicknessSuppress);
        fluidMat.SetFloat("_EnableReflection", enableReflection ? 1f : 0f);
        fluidMat.SetFloat("_EnableRefraction", enableRefraction ? 1f : 0f);
        fluidMat.SetFloat("_UseBackground", useBackground ? 1f : 0f);
        fluidMat.SetFloat("_BackgroundWeight", backgroundWeight);
        var gph = Shader.Find("Instanced/GridParticleMobile");
        if (gph != null)
        {
            gridParticleMat = new Material(gph);
            gridParticleMat.enableInstancing = true;
            gridParticleMat.SetFloat("_size", particleSize);
        }

        sphereMesh = CreateSphereMesh();
        drawBounds = new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin + Vector3.one * 2f);
        int rtW = Mathf.RoundToInt(depthRTSize.x * Mathf.Max(0.5f, highQualityDepthScale));
        int rtH = Mathf.RoundToInt(depthRTSize.y * Mathf.Max(0.5f, highQualityDepthScale));

        var cam = Camera.main;
        if (cam != null) cam.depthTextureMode |= DepthTextureMode.Depth;
        if (cam != null && autoCameraClipTuning) ApplyCameraClipTuning(cam);
        if (enableDepthPass)
        {
            var dsh = Shader.Find("Instanced/ParticleDepthMobile");
            if (dsh != null)
            {
                depthMat = new Material(dsh);
                depthMat.enableInstancing = true;
                depthMat.SetFloat("_size", particleSize);
                var dColorFmt = SelectSingleChannelFloatFormat();
                particleDepthRT = new RenderTexture(rtW, rtH, 0, dColorFmt);
                particleDepthRT.name = "ParticleDepthRT";
                particleDepthRT.filterMode = FilterMode.Point;
                particleDepthRT.Create();
                particleDepthBackRT = new RenderTexture(rtW, rtH, 0, dColorFmt);
                particleDepthBackRT.name = "ParticleDepthBackRT";
                particleDepthBackRT.filterMode = FilterMode.Point;
                particleDepthBackRT.Create();
                if (enableDepthBlur)
                {
                    var gsh = Shader.Find("Hidden/DepthGaussianBlur");
                    var bsh = Shader.Find("Hidden/DepthBilateral");
                    if (gsh != null)
                    {
                        depthGaussianBlurMat = new Material(gsh);
                        depthGaussianBlurMat.SetVector("_Weights", depthGaussianWeights);
                    }
                    if (bsh != null)
                    {
                        depthBilateralBlurMat = new Material(bsh);
                        depthBilateralBlurMat.SetFloat("_SigmaSpatial", depthSigmaSpatial);
                        depthBilateralBlurMat.SetFloat("_SigmaRange", depthSigmaRange);
                    }
                    var dFmt = SelectSingleChannelFloatFormat();
                    particleDepthTempRT = new RenderTexture(rtW, rtH, 0, dFmt);
                    particleDepthTempRT.name = "ParticleDepthTempRT";
                    particleDepthTempRT.filterMode = FilterMode.Bilinear;
                    particleDepthTempRT.Create();
                    particleBlurredDepthRT = new RenderTexture(rtW, rtH, 0, dFmt);
                    particleBlurredDepthRT.name = "BlurredDepthRT";
                    particleBlurredDepthRT.filterMode = FilterMode.Bilinear;
                    particleBlurredDepthRT.Create();
                    var cfsh = Shader.Find("Hidden/DepthCurvatureFlow");
                    if (cfsh != null)
                    {
                        depthCurvatureFlowMat = new Material(cfsh);
                    }
                }
                depthCB = new CommandBuffer();
                depthCB.name = "SPH Particle Depth Pass";
                if (cam != null) cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, depthCB);
            }
        }
        if (enableThicknessPass)
        {
            var tsh = Shader.Find("Hidden/ParticleThickness");
            var tash = Shader.Find("Instanced/ParticleThicknessAdditiveMobile");
            if (tsh != null)
            {
                thicknessMat = new Material(tsh);
                thicknessMat.SetFloat("_Threshold", thicknessThreshold);
                thicknessMat.SetFloat("_Scale", thicknessScale);
                var tFmt = SelectSingleChannelFloatFormat();
                particleThicknessRT = new RenderTexture(rtW, rtH, 0, tFmt);
                particleThicknessRT.name = "ParticleThicknessRT";
                particleThicknessRT.filterMode = FilterMode.Point;
                particleThicknessRT.Create();
            }
            if (tash != null)
            {
                thicknessAdditiveMat = new Material(tash);
                thicknessAdditiveMat.enableInstancing = true;
                thicknessAdditiveMat.SetFloat("_size", particleSize);
                thicknessAdditiveMat.SetFloat("_ContributionScale", thicknessContributionScale);
                if (thicknessCB == null)
                {
                    thicknessCB = new CommandBuffer();
                    thicknessCB.name = "SPH Particle Thickness Additive Pass";
                    if (cam != null) cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, thicknessCB);
                }
            }
        }
        if (enableThicknessBlur)
        {
            var bsh = Shader.Find("Hidden/ThicknessBlur");
            if (bsh != null)
            {
                thicknessBlurMat = new Material(bsh);
                thicknessBlurMat.SetVector("_Weights", thicknessBlurWeights);
                var tFmt = SelectSingleChannelFloatFormat();
                particleThicknessTempRT = new RenderTexture(rtW, rtH, 0, tFmt);
                particleThicknessTempRT.name = "ParticleThicknessTempRT";
                particleThicknessTempRT.filterMode = FilterMode.Bilinear;
                particleThicknessTempRT.Create();
                particleBlurredThicknessRT = new RenderTexture(rtW, rtH, 0, tFmt);
                particleBlurredThicknessRT.name = "BlurredThicknessRT";
                particleBlurredThicknessRT.filterMode = FilterMode.Bilinear;
                particleBlurredThicknessRT.Create();
            }
        }
        if (enableThicknessNormals)
        {
            var nsh = Shader.Find("Hidden/ThicknessNormals");
            if (nsh != null)
            {
                thicknessNormalsMat = new Material(nsh);
                thicknessNormalsMat.SetFloat("_NormalStrength", thicknessNormalStrength);
                thicknessNormalRT = new RenderTexture(rtW, rtH, 0, RenderTextureFormat.ARGB32);
                thicknessNormalRT.name = "ThicknessNormalRT";
                thicknessNormalRT.filterMode = FilterMode.Bilinear;
                thicknessNormalRT.Create();
            }
        }
        {
            var dnh = Shader.Find("Hidden/DepthNormals");
            if (dnh != null && enableDepthPass)
            {
                depthNormalsMat = new Material(dnh);
                depthNormalsMat.SetFloat("_NormalStrength", depthNormalStrength);
                depthNormalRT = new RenderTexture(rtW, rtH, 0, RenderTextureFormat.ARGB32);
                depthNormalRT.name = "DepthNormalRT";
                depthNormalRT.filterMode = FilterMode.Bilinear;
                depthNormalRT.Create();
            }
        }
        if (enableLowParticleCountTuning && particleCount <= 8000)
        {
            maxSubsteps = Mathf.Max(maxSubsteps, 6);
            fixedTimeStep = Mathf.Min(fixedTimeStep, 0.0045f);
            viscosity = Mathf.Max(viscosity, 0.15f);
            xsphC = Mathf.Clamp(xsphC, 0.35f, 0.55f);
            boundaryDamping = Mathf.Clamp(boundaryDamping, 0.75f, 0.95f);
        }
        if (particleCount >= 8000)
        {
            ApplyMediumParticleStability();
        }
        if (realisticWaterPreset)
        {
            ApplyRealisticWaterPreset();
        }
        if (sweepFlowPreset)
        {
            ApplySweepFlowPreset();
        }
        if (targetFrameRate > 0) Application.targetFrameRate = targetFrameRate;
        if (sharpEdgesPreset) ApplySharpEdgesPreset();
        if (Application.isMobilePlatform)
        {
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 4);
            if (QualitySettings.shadows == ShadowQuality.Disable) QualitySettings.shadows = ShadowQuality.HardOnly;
        }
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
        if (!runSimulation) { Draw(); return; }
        float dtFrame = Mathf.Clamp(Time.deltaTime, 1e-4f, 0.05f);
        int steps = enableSubstepping ? Mathf.Clamp(Mathf.CeilToInt(dtFrame / fixedTimeStep), 1, maxSubsteps) : 1;
        float dtStep = dtFrame / steps;
        for (int s = 0; s < steps; s++)
        {
            SimulateStep(dtStep);
        }
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

    void SimulateStep(float dt)
    {
        float hConst = Mathf.Max(neighbourRadius, 1e-3f);
        float poly6 = 315f / (64f * Mathf.PI * Mathf.Pow(hConst, 9f));
        float spiky = 45f / (Mathf.PI * Mathf.Pow(hConst, 6f));
        float visc = spiky;
        float eosKVal = restDensity * soundSpeed * soundSpeed / Mathf.Max(eosGamma, 1e-3f);
        cs.SetInt("n_grid", gridResolution);
        cs.SetInt("particle_num", particleCount);
        cs.SetVector("boundsMin", boundsMin);
        cs.SetVector("boundsMax", boundsMax);
        cs.SetBuffer(kClearGrid, "cellHead", bufCellHead);
        int groupsGrid = (gridResolution * gridResolution * gridResolution + 127) / 128;
        cs.Dispatch(kClearGrid, groupsGrid, 1, 1);

        cs.SetBuffer(kBuildGrid, "x", bufX);
        cs.SetBuffer(kBuildGrid, "cellHead", bufCellHead);
        cs.SetBuffer(kBuildGrid, "nextIndex", bufNextIndex);
        int groupsBuild = (particleCount + 127) / 128;
        cs.Dispatch(kBuildGrid, groupsBuild, 1, 1);

        cs.SetInt("n_grid", gridResolution);
        cs.SetInt("particle_num", particleCount);
        cs.SetVector("boundsMin", boundsMin);
        cs.SetVector("boundsMax", boundsMax);
        cs.SetFloat("restDensity", restDensity);
        cs.SetFloat("particleMass", particleMass);
        cs.SetFloat("neighbourRadius", neighbourRadius);
        cs.SetFloat("poly6_const", poly6);
        cs.SetFloat("spiky_const", spiky);
        cs.SetFloat("visc_const", visc);
        cs.SetFloat("eosK", eosKVal);
        cs.SetBuffer(kDensity, "x", bufX);
        cs.SetBuffer(kDensity, "rho", bufRho);
        cs.SetBuffer(kDensity, "cellHead", bufCellHead);
        cs.SetBuffer(kDensity, "nextIndex", bufNextIndex);
        int groupsMain = (particleCount + 127) / 128;
        cs.Dispatch(kDensity, groupsMain, 1, 1);

        cs.SetInt("n_grid", gridResolution);
        cs.SetInt("particle_num", particleCount);
        cs.SetVector("boundsMin", boundsMin);
        cs.SetVector("boundsMax", boundsMax);
        cs.SetVector("gravity", gravity);
        cs.SetFloat("dt", dt);
        cs.SetFloat("restDensity", restDensity);
        cs.SetFloat("particleMass", particleMass);
        cs.SetFloat("neighbourRadius", neighbourRadius);
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
        cs.SetVector("swirlAxis", swirlAxis);
        cs.SetFloat("impulseNormalCoeff", Mathf.Max(0f, impulseNormalCoeff));
        cs.SetFloat("impulseTangentialCoeff", Mathf.Max(0f, impulseTangentialCoeff));
        cs.SetFloat("poly6_const", poly6);
        cs.SetFloat("spiky_const", spiky);
        cs.SetFloat("visc_const", visc);
        cs.SetFloat("eosK", eosKVal);
        cs.SetBuffer(kMain, "x", bufX);
        cs.SetBuffer(kMain, "v", bufV);
        cs.SetBuffer(kMain, "rho", bufRho);
        cs.SetBuffer(kMain, "cellHead", bufCellHead);
        cs.SetBuffer(kMain, "nextIndex", bufNextIndex);
        cs.SetBuffer(kMain, "_particlesBuffer", particlesBuffer);
        int iCount = 0;
        float iRad = 0f;
        float iStr = 0f;
        float dynImpulseRadius = 0f;
        float speedFactor = 1f + Mathf.Clamp(stirSpeed * stirSpeedBoost, 0f, 5f);
        float boostFromRadius = 1f;
        if (enableStir && stirTransform != null)
        {
            Vector3 p = ClampToBounds(stirTransform.position);
            int desiredCount = Mathf.Clamp(Mathf.Min(16, maxImpulses), 1, maxImpulses);
            if (bufImpulses != null && bufImpulses.count < desiredCount)
            {
                bufImpulses.Release();
                bufImpulses = new ComputeBuffer(desiredCount, sizeof(float) * 4);
            }
            var arr = new Vector4[desiredCount];
            int wr = 0;
            arr[wr++] = new Vector4(p.x, p.y, p.z, 1f);
            float offs = Mathf.Max(0.01f, stirRadius * 0.35f);
            Vector3 p1 = ClampToBounds(new Vector3(p.x + offs, p.y, p.z));
            Vector3 p2 = ClampToBounds(new Vector3(p.x - offs, p.y, p.z));
            Vector3 p3 = ClampToBounds(new Vector3(p.x, p.y, p.z + offs));
            Vector3 p4 = ClampToBounds(new Vector3(p.x, p.y, p.z - offs));
            if (wr < desiredCount) arr[wr++] = new Vector4(p1.x, p1.y, p1.z, 1f);
            if (wr < desiredCount) arr[wr++] = new Vector4(p2.x, p2.y, p2.z, 1f);
            if (wr < desiredCount) arr[wr++] = new Vector4(p3.x, p3.y, p3.z, 1f);
            if (wr < desiredCount) arr[wr++] = new Vector4(p4.x, p4.y, p4.z, 1f);
            Vector3 axis = (stirTransform.parent != null ? stirTransform.parent.up : Vector3.up);
            Vector3 pa = ClampToBounds(p + axis * offs);
            Vector3 pb = ClampToBounds(p - axis * offs);
            if (wr < desiredCount) arr[wr++] = new Vector4(pa.x, pa.y, pa.z, 1f);
            if (wr < desiredCount) arr[wr++] = new Vector4(pb.x, pb.y, pb.z, 1f);
            if (wr < desiredCount)
            {
                Vector3 pc = ClampToBounds(p + axis * (offs * 2f));
                arr[wr++] = new Vector4(pc.x, pc.y, pc.z, 1f);
            }
            Vector3 dirXZ = (stirDirXZ.sqrMagnitude < 1e-6f) ? Vector3.forward : stirDirXZ.normalized;
            Vector3 pf = ClampToBounds(p + dirXZ * offs);
            Vector3 pbk = ClampToBounds(p - dirXZ * offs);
            if (wr < desiredCount) arr[wr++] = new Vector4(pf.x, pf.y, pf.z, 1f);
            if (wr < desiredCount) arr[wr++] = new Vector4(pbk.x, pbk.y, pbk.z, 1f);
            int ringN = 6;
            for (int ri = 0; ri < ringN && wr < desiredCount; ri++)
            {
                float ang = (Mathf.PI * 2f / ringN) * ri;
                Vector3 pr = new Vector3(p.x + Mathf.Cos(ang) * offs, p.y, p.z + Mathf.Sin(ang) * offs);
                pr = ClampToBounds(pr);
                arr[wr++] = new Vector4(pr.x, pr.y, pr.z, 1f);
            }
            bufImpulses.SetData(arr);
            iCount = wr;
            iRad = Mathf.Max(0.001f, stirRadius);
            Transform stickPivot = stirTransform != null && stirTransform.parent != null ? stirTransform.parent : stirTransform;
            float angFactor = 1f;
            if (stickPivot != null)
            {
                if (!stickRotInitialized)
                {
                    lastStickRot = stickPivot.rotation;
                    stickRotInitialized = true;
                    stickAngularSpeed = 0f;
                }
                else
                {
                    Quaternion dq = stickPivot.rotation * Quaternion.Inverse(lastStickRot);
                    float angDeg; Vector3 ax;
                    dq.ToAngleAxis(out angDeg, out ax);
                    float angRad = Mathf.Deg2Rad * Mathf.Min(angDeg, 180f);
                    float align = Mathf.Abs(Vector3.Dot(ax.normalized, stickPivot.up.normalized));
                    float s = angRad / Mathf.Max(dt, 1e-4f);
                    stickAngularSpeed = Mathf.Lerp(stickAngularSpeed, s * align, 0.65f);
                    lastStickRot = stickPivot.rotation;
                }
                angFactor = 1f + Mathf.Clamp(stickAngularSpeed * stirAngularBoost, 0f, 6f);
            }
            iStr = stirStrength * Mathf.Max(1.5f, stirStrengthFactor * stirStrengthScale) * speedFactor * angFactor;
        }
        cs.SetInt("impulseCount", iCount);
        cs.SetFloat("impulseRadius", Mathf.Max(iRad, dynImpulseRadius));
        cs.SetFloat("impulseStrength", iStr);
        cs.SetBuffer(kMain, "impulses", bufImpulses);
        int obstCountBase = obstacleTransforms != null ? obstacleTransforms.Length : 0;
        int chainN = 0;
        Transform stickRoot = stirTransform != null && stirTransform.parent != null ? stirTransform.parent : stirTransform;
        Vector4[] chain = null;
        if (enableStir && stirTransform != null)
        {
            int segN = Mathf.Max(3, stirSegments);
            chain = new Vector4[segN];
            CapsuleCollider cap = stickRoot != null ? stickRoot.GetComponent<CapsuleCollider>() : null;
            MeshRenderer mr = stickRoot != null ? stickRoot.GetComponent<MeshRenderer>() : null;
            if (cap != null && stirUseCapsuleSegments)
            {
                int dir = cap.direction;
                Vector3 axis = (dir == 0) ? stickRoot.right : (dir == 1 ? stickRoot.up : stickRoot.forward);
                axis = axis.normalized;
                Vector3 cWorld = stickRoot.TransformPoint(cap.center);
                float sx = stickRoot.lossyScale.x;
                float sy = stickRoot.lossyScale.y;
                float sz = stickRoot.lossyScale.z;
                float sAxis = (dir == 0) ? sx : (dir == 1 ? sy : sz);
                float sRad = (dir == 0) ? Mathf.Max(sy, sz) : (dir == 1 ? Mathf.Max(sx, sz) : Mathf.Max(sx, sy));
                float heightWorld = Mathf.Max(0.001f, cap.height * sAxis);
                float radWorld = Mathf.Max(0.001f, cap.radius * sRad);
                Vector3 start = cWorld - axis * Mathf.Max(0.0f, heightWorld * 0.5f - radWorld);
                Vector3 end = cWorld + axis * Mathf.Max(0.0f, heightWorld * 0.5f - radWorld);
                for (int si = 0; si < segN; si++)
                {
                    float tSeg = segN == 1 ? 0.5f : (float)si / (float)(segN - 1);
                    Vector3 pi = Vector3.Lerp(start, end, tSeg);
                    Vector3 pc = ClampToBounds(pi);
                    chain[si] = new Vector4(pc.x, pc.y, pc.z, radWorld);
                }
                chainN = segN;
                dynImpulseRadius = Mathf.Max(dynImpulseRadius, radWorld * impulseRadiusScaleFromCapsule);
                boostFromRadius = Mathf.Clamp(smallStickRadiusRef / Mathf.Max(radWorld, 1e-3f), 1f, smallStickBoostMax);
            }
            else if (mr != null)
            {
                Vector3 axis = stickRoot.up.normalized;
                Bounds b = mr.bounds;
                Vector3 cWorld = b.center;
                float heightWorld = Mathf.Max(0.001f, Vector3.Dot(b.size, new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z))));
                float radWorld = Mathf.Max(0.001f, Mathf.Max(b.extents.x, b.extents.z));
                Vector3 start = cWorld - axis * Mathf.Max(0.0f, heightWorld * 0.5f - radWorld);
                Vector3 end = cWorld + axis * Mathf.Max(0.0f, heightWorld * 0.5f - radWorld);
                for (int si = 0; si < segN; si++)
                {
                    float tSeg = segN == 1 ? 0.5f : (float)si / (float)(segN - 1);
                    Vector3 pi = Vector3.Lerp(start, end, tSeg);
                    Vector3 pc = ClampToBounds(pi);
                    chain[si] = new Vector4(pc.x, pc.y, pc.z, radWorld);
                }
                chainN = segN;
                dynImpulseRadius = Mathf.Max(dynImpulseRadius, radWorld * impulseRadiusScaleFromCapsule);
                boostFromRadius = Mathf.Clamp(smallStickRadiusRef / Mathf.Max(radWorld, 1e-3f), 1f, smallStickBoostMax);
            }
            else
            {
                float r = Mathf.Max(0.001f, stirRadius);
                Vector3 pc = ClampToBounds(stirTransform.position);
                chain[0] = new Vector4(pc.x, pc.y, pc.z, r);
                chainN = 1;
                dynImpulseRadius = Mathf.Max(dynImpulseRadius, r * impulseRadiusScaleFromCapsule);
            }
        }
        int obstCount = obstCountBase + chainN;
        int allocCount = Mathf.Max(1, obstCount);
        if (bufObstacles == null || bufObstacles.count != allocCount)
        {
            if (bufObstacles != null) bufObstacles.Release();
            bufObstacles = new ComputeBuffer(allocCount, sizeof(float) * 4);
        }
        var obs = new Vector4[allocCount];
        if (obstCountBase > 0)
        {
            for (int oi = 0; oi < obstCountBase; oi++)
            {
                Transform t = obstacleTransforms[oi];
                if (t == null) { obs[oi] = Vector4.zero; continue; }
                Vector3 op = ClampToBounds(t.position);
                float r = obstacleDefaultRadius;
                var sc2 = t.GetComponent<SphereCollider>();
                if (sc2 != null) r = Mathf.Max(0.001f, sc2.radius * Mathf.Max(t.lossyScale.x, Mathf.Max(t.lossyScale.y, t.lossyScale.z)));
                obs[oi] = new Vector4(op.x, op.y, op.z, r);
            }
        }
        if (chainN > 0 && chain != null)
        {
            for (int ci = 0; ci < chainN; ci++) obs[obstCountBase + ci] = chain[ci];
        }
        if (allocCount == 1 && obstCount == 0) obs[0] = Vector4.zero;
        bufObstacles.SetData(obs);
        cs.SetInt("obstacleCount", obstCount);
        float angFactorOb = 1f + Mathf.Clamp(stickAngularSpeed * stirAngularBoost, 0f, 6f);
        cs.SetFloat("obstaclePushStrength", obstaclePushStrength * Mathf.Max(1f, speedFactor * angFactorOb * obstaclePushSpeedScale * boostFromRadius));
        cs.SetFloat("obstacleDamping", obstacleDamping);
        float tanScale = 1f + Mathf.Clamp(stickAngularSpeed * 1.5f, 0f, 8f);
        cs.SetFloat("obstacleTangentialStrength", obstacleTangentialStrength * tanScale);
        cs.SetFloat("obstacleFriction", obstacleFriction);
        if (bufObstacles != null) cs.SetBuffer(kMain, "obstacles", bufObstacles);
        if (enableStir && stirTransform != null)
        {
            Vector3 axDyn = (stickRoot != null ? stickRoot.up : stirTransform.up);
            cs.SetVector("swirlAxis", axDyn);
        }
        dynImpulseRadius = Mathf.Max(dynImpulseRadius, minImpulseRadius);
        iStr *= Mathf.Max(1f, boostFromRadius);
        cs.Dispatch(kMain, groupsMain, 1, 1);
    }

    void Draw()
    {
        if (sphereMesh == null) return;
        drawBounds.center = (boundsMin + boundsMax) * 0.5f;
        drawBounds.size = boundsMax - boundsMin + Vector3.one * 2f;
        if (renderMode == RenderMode.GridParticles && gridParticleMat != null)
        {
            float drawSize = particleSize;
            gridParticleMat.SetFloat("_size", drawSize);
            gridParticleMat.SetBuffer("_particlesBuffer", particlesBuffer);
            Graphics.DrawMeshInstancedProcedural(sphereMesh, 0, gridParticleMat, drawBounds, particleCount);
            return;
        }
        // 延后流体合成到所有贴图准备完成之后
        if (enableDepthPass && depthMat != null && particleDepthRT != null && depthCB != null)
        {
            float drawSize = Mathf.Max(particleSize, neighbourRadius * 0.60f);
            depthMat.SetFloat("_size", drawSize);
            depthMat.SetBuffer("_particlesBuffer", particlesBuffer);
            depthMat.SetFloat("_DebugMode", depthDebugMode);
            depthMat.SetFloat("_ParticleCount", Mathf.Max(1, particleCount));
            depthCB.Clear();
            depthCB.SetRenderTarget(particleDepthRT);
            depthCB.SetViewport(new Rect(0, 0, particleDepthRT.width, particleDepthRT.height));
            depthCB.ClearRenderTarget(false, true, Color.white);
            depthCB.DrawMeshInstancedProcedural(sphereMesh, 0, depthMat, 0, particleCount, null);
            depthCB.SetRenderTarget(particleDepthBackRT);
            depthCB.SetViewport(new Rect(0, 0, particleDepthBackRT.width, particleDepthBackRT.height));
            depthCB.ClearRenderTarget(false, true, Color.black);
            depthCB.DrawMeshInstancedProcedural(sphereMesh, 0, depthMat, 1, particleCount, null);
            if (enableDepthBlur && particleDepthTempRT != null && particleBlurredDepthRT != null)
            {
                switch (depthSmoothMode)
                {
                    case DepthSmoothMode.CurvatureFlow:
                        if (depthCurvatureFlowMat != null)
                        {
                            depthCurvatureFlowMat.SetFloat("_Lambda", curvatureFlowLambda);
                            depthCurvatureFlowMat.SetFloat("_SigmaRange", curvatureFlowSigmaRange);
                            RenderTexture src = particleDepthRT;
                            RenderTexture dst = particleDepthTempRT;
                            int iters = Mathf.Max(1, curvatureFlowIterations);
                            for (int i = 0; i < iters - 1; i++)
                            {
                                depthCurvatureFlowMat.SetTexture("_DepthTex", src);
                                Graphics.Blit(src, dst, depthCurvatureFlowMat);
                                var tmp = src; src = dst; dst = tmp;
                            }
                            depthCurvatureFlowMat.SetTexture("_DepthTex", src);
                            Graphics.Blit(src, particleBlurredDepthRT, depthCurvatureFlowMat);
                        }
                        else
                        {
                            Graphics.Blit(particleDepthRT, particleBlurredDepthRT);
                        }
                        break;
                    case DepthSmoothMode.Bilateral:
                        if (depthBilateralBlurMat != null)
                        {
                            depthBilateralBlurMat.SetTexture("_DepthTex", particleDepthRT);
                            Graphics.Blit(particleDepthRT, particleDepthTempRT, depthBilateralBlurMat, 0);
                            depthBilateralBlurMat.SetTexture("_DepthTex", particleDepthTempRT);
                            Graphics.Blit(particleDepthTempRT, particleBlurredDepthRT, depthBilateralBlurMat, 1);
                        }
                        else
                        {
                            Graphics.Blit(particleDepthRT, particleBlurredDepthRT);
                        }
                        break;
                    case DepthSmoothMode.Gaussian:
                        if (depthGaussianBlurMat != null)
                        {
                            depthGaussianBlurMat.SetTexture("_DepthTex", particleDepthRT);
                            Graphics.Blit(particleDepthRT, particleDepthTempRT, depthGaussianBlurMat, 0);
                            depthGaussianBlurMat.SetTexture("_DepthTex", particleDepthTempRT);
                            Graphics.Blit(particleDepthTempRT, particleBlurredDepthRT, depthGaussianBlurMat, 1);
                        }
                        else
                        {
                            Graphics.Blit(particleDepthRT, particleBlurredDepthRT);
                        }
                        break;
                    default:
                        Graphics.Blit(particleDepthRT, particleBlurredDepthRT);
                        break;
                }
            }
        }
        if (enableThicknessPass && particleThicknessRT != null)
        {
            bool doThicknessThisFrame = (Time.frameCount % Mathf.Max(thicknessUpdateStride, 1)) == 0;
            if (thicknessAdditive && thicknessAdditiveMat != null && thicknessCB != null && doThicknessThisFrame)
            {
                float drawSize = Mathf.Max(particleSize, neighbourRadius * 0.8f);
                thicknessAdditiveMat.SetFloat("_size", drawSize);
                thicknessAdditiveMat.SetFloat("_ContributionScale", thicknessContributionScale);
                thicknessAdditiveMat.SetBuffer("_particlesBuffer", particlesBuffer);
                thicknessCB.Clear();
                thicknessCB.SetRenderTarget(particleThicknessRT);
                thicknessCB.SetViewport(new Rect(0, 0, particleThicknessRT.width, particleThicknessRT.height));
                thicknessCB.ClearRenderTarget(false, true, Color.black);
                thicknessCB.DrawMeshInstancedProcedural(sphereMesh, 0, thicknessAdditiveMat, 0, particleCount, null);
            }
            else if (thicknessMat != null && particleDepthRT != null && doThicknessThisFrame)
            {
                var depthSrc = (enableDepthBlur && particleBlurredDepthRT != null) ? particleBlurredDepthRT : particleDepthRT;
                thicknessMat.SetTexture("_DepthTex", depthSrc);
                thicknessMat.SetFloat("_Threshold", thicknessThreshold);
                thicknessMat.SetFloat("_Scale", thicknessScale);
                Graphics.Blit(depthSrc, particleThicknessRT, thicknessMat);
            }
        }
        if (enableThicknessBlur && thicknessBlurMat != null && particleThicknessRT != null && particleThicknessTempRT != null && particleBlurredThicknessRT != null)
        {
            bool doThicknessBlurThisFrame = (Time.frameCount % Mathf.Max(thicknessUpdateStride, 1)) == 0;
            if (doThicknessBlurThisFrame)
            {
                Graphics.Blit(particleThicknessRT, particleThicknessTempRT, thicknessBlurMat, 0);
                Graphics.Blit(particleThicknessTempRT, particleBlurredThicknessRT, thicknessBlurMat, 1);
            }
        }
        if (enableThicknessNormals && thicknessNormalsMat != null && particleBlurredThicknessRT != null && thicknessNormalRT != null)
        {
            bool doThicknessNormalsThisFrame = (Time.frameCount % Mathf.Max(thicknessNormalsUpdateStride, 1)) == 0;
            if (doThicknessNormalsThisFrame)
            {
                thicknessNormalsMat.SetFloat("_NormalStrength", thicknessNormalStrength);
                thicknessNormalsMat.SetTexture("_ThicknessTex", particleBlurredThicknessRT);
                Graphics.Blit(particleBlurredThicknessRT, thicknessNormalRT, thicknessNormalsMat);
            }
        }
        if (depthNormalsMat != null && depthNormalRT != null)
        {
            bool doDepthNormalsThisFrame = (Time.frameCount % Mathf.Max(depthNormalsUpdateStride, 1)) == 0;
            var depthSrcForNormals = (useBlurredDepthForNormals && enableDepthBlur && particleBlurredDepthRT != null) ? particleBlurredDepthRT : particleDepthRT;
            if (doDepthNormalsThisFrame && depthSrcForNormals != null)
            {
                depthNormalsMat.SetFloat("_NormalStrength", depthNormalStrength);
                depthNormalsMat.SetTexture("_DepthTex", depthSrcForNormals);
                Graphics.Blit(depthSrcForNormals, depthNormalRT, depthNormalsMat);
            }
        }
        if (depthDebugToScreen && depthMat != null)
        {
            float drawSize = Mathf.Max(particleSize, neighbourRadius * 0.9f);
            depthMat.SetFloat("_size", drawSize);
            depthMat.SetBuffer("_particlesBuffer", particlesBuffer);
            depthMat.SetFloat("_DebugMode", depthDebugMode);
            depthMat.SetFloat("_ParticleCount", Mathf.Max(1, particleCount));
            Graphics.DrawMeshInstancedProcedural(sphereMesh, 0, depthMat, drawBounds, particleCount);
        }

        if (fluidMat != null && particleBlurredThicknessRT != null && thicknessNormalRT != null)
        {
            float drawSize = Mathf.Max(particleSize, neighbourRadius * 0.65f);
            fluidMat.SetFloat("_size", drawSize);
            fluidMat.SetBuffer("_particlesBuffer", particlesBuffer);
            fluidMat.SetColor("_TintColor", waterTint);
            fluidMat.SetFloat("_Opacity", waterOpacity);
            fluidMat.SetFloat("_RefractStrength", refractStrength);
            fluidMat.SetFloat("_FresnelPower", fresnelPower);
            fluidMat.SetFloat("_SoftDepth", waterSoftDepth);
            fluidMat.SetFloat("_MinDepthVisibility", minDepthVisibility);
            fluidMat.SetFloat("_EdgeBoost", edgeBoost);
            fluidMat.SetFloat("_EdgeWidth", edgeWidth);
            fluidMat.SetFloat("_AlphaFloor", alphaFloor);
            fluidMat.SetFloat("_TintMix", tintMix);
            fluidMat.SetFloat("_EnvStrength", envStrength);
            fluidMat.SetFloat("_SpecularStrength", specularStrength);
            fluidMat.SetFloat("_HighlightClamp", highlightClamp);
            fluidMat.SetColor("_AbsorptionColor", absorptionColor);
            fluidMat.SetFloat("_ReflectionThicknessSuppress", reflectionThicknessSuppress);
            fluidMat.SetFloat("_RefractionThicknessSuppress", refractionThicknessSuppress);
            fluidMat.SetFloat("_EnableReflection", enableReflection ? 1f : 0f);
            fluidMat.SetFloat("_EnableRefraction", enableRefraction ? 1f : 0f);
            fluidMat.SetFloat("_UseBackground", useBackground ? 1f : 0f);
            fluidMat.SetFloat("_BackgroundWeight", backgroundWeight);
            fluidMat.SetFloat("_ThicknessScale", compositeThicknessScale);
            fluidMat.SetFloat("_ThicknessGamma", compositeThicknessGamma);
            fluidMat.SetFloat("_ThicknessMax", compositeThicknessMax);
            fluidMat.SetFloat("_ThicknessExposure", compositeThicknessExposure);
            fluidMat.SetFloat("_ThicknessTopBias", thicknessTopBias);
            fluidMat.SetFloat("_DepthEdgeStrength", depthEdgeStrength);
            fluidMat.SetFloat("_DepthEdgeThreshold", depthEdgeThreshold);
            fluidMat.SetFloat("_FresnelAlphaBase", fresnelAlphaBase);
            fluidMat.SetFloat("_FresnelAlphaWeight", fresnelAlphaWeight);
            fluidMat.SetFloat("_RefractionClampPixels", refractionClampPixels);
            fluidMat.SetFloat("_RefractionEdgeSuppress", refractionEdgeSuppress);
            fluidMat.SetTexture("_BlurredThicknessTex", particleBlurredThicknessRT);
            fluidMat.SetTexture("_ThicknessNormalsTex", thicknessNormalRT);
            if (particleDepthRT != null)
            {
                fluidMat.SetTexture("_SSDepthTex", particleDepthRT);
            }
            if (depthNormalRT != null)
            {
                fluidMat.SetTexture("_DepthNormalsTex", depthNormalRT);
                fluidMat.SetFloat("_DepthNormalWeight", depthNormalWeight);
            }
            Graphics.DrawMeshInstancedProcedural(sphereMesh, 0, fluidMat, drawBounds, particleCount);
        }
    }

    void OnGUI()
    {
        int w = Mathf.RoundToInt(depthRTSize.x * Mathf.Max(0.1f, previewScale));
        int h = Mathf.RoundToInt(depthRTSize.y * Mathf.Max(0.1f, previewScale));
        if (showDepthPreview && particleDepthRT != null)
        {
            GUI.DrawTexture(new Rect(10, 10, w, h), particleDepthRT, ScaleMode.StretchToFill, false);
        }
        if (showBlurredDepthPreview && particleBlurredDepthRT != null)
        {
            GUI.DrawTexture(new Rect(20 + w, 10, w, h), particleBlurredDepthRT, ScaleMode.StretchToFill, false);
        }
        if (showDepthNormalsPreview && depthNormalRT != null)
        {
            GUI.DrawTexture(new Rect(30 + 2*w, 10, w, h), depthNormalRT, ScaleMode.StretchToFill, false);
        }
        if (showThicknessPreview && particleThicknessRT != null)
        {
            GUI.DrawTexture(new Rect(10, 20 + h, w, h), particleThicknessRT, ScaleMode.StretchToFill, false);
        }
        if (showBlurredThicknessPreview && particleBlurredThicknessRT != null)
        {
            GUI.DrawTexture(new Rect(10, 30 + 2*h, w, h), particleBlurredThicknessRT, ScaleMode.StretchToFill, false);
        }
        if (showThicknessNormalsPreview && thicknessNormalRT != null)
        {
            GUI.DrawTexture(new Rect(10, 40 + 3*h, w, h), thicknessNormalRT, ScaleMode.StretchToFill, false);
        }
        int btnW = 132;
        int btnH = 54;
        int pad = 10;
        if (Application.isMobilePlatform)
        {
            Rect r1 = new Rect(pad, Screen.height - btnH - pad, btnW, btnH);
            Rect r2 = new Rect(pad + btnW + 8, Screen.height - btnH - pad, btnW, btnH);
            if (GUI.Button(r1, "重新开始")) ResetSimulation();
            string toggleLabel = runSimulation ? "暂停" : "继续";
            if (GUI.Button(r2, toggleLabel))
            {
                if (runSimulation) Pause(); else Resume();
            }
        }
    }

    void OnDestroy()
    {
        Release(bufX); Release(bufV); Release(bufRho); Release(bufCellHead); Release(bufNextIndex); Release(particlesBuffer); Release(bufImpulses); Release(bufObstacles);
        var cam = Camera.main;
        if (depthCB != null)
        {
            if (cam != null) cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, depthCB);
            depthCB.Release();
            depthCB = null;
        }
        if (thicknessCB != null)
        {
            if (cam != null) cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, thicknessCB);
            thicknessCB.Release();
            thicknessCB = null;
        }
        if (particleDepthRT != null)
        {
            particleDepthRT.Release();
            particleDepthRT = null;
        }
        if (particleDepthBackRT != null)
        {
            particleDepthBackRT.Release();
            particleDepthBackRT = null;
        }
        if (particleDepthTempRT != null)
        {
            particleDepthTempRT.Release();
            particleDepthTempRT = null;
        }
        if (particleBlurredDepthRT != null)
        {
            particleBlurredDepthRT.Release();
            particleBlurredDepthRT = null;
        }
        if (particleThicknessRT != null)
        {
            particleThicknessRT.Release();
            particleThicknessRT = null;
        }
        if (particleThicknessTempRT != null)
        {
            particleThicknessTempRT.Release();
            particleThicknessTempRT = null;
        }
        if (particleBlurredThicknessRT != null)
        {
            particleBlurredThicknessRT.Release();
            particleBlurredThicknessRT = null;
        }
        if (thicknessNormalRT != null)
        {
            thicknessNormalRT.Release();
            thicknessNormalRT = null;
        }
    }
    
    RenderTextureFormat SelectSingleChannelFloatFormat()
    {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf)) return RenderTextureFormat.RHalf;
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat)) return RenderTextureFormat.RFloat;
        return RenderTextureFormat.R8;
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
    
    

    
    

    
    

    void ApplySharpEdgesPreset()
    {
        useBlurredDepthForNormals = true;
        depthSmoothMode = DepthSmoothMode.Bilateral;
        depthSigmaSpatial = Mathf.Clamp(2.4f, 2.0f, 4.0f);
        depthSigmaRange = Mathf.Clamp(0.020f, 0.010f, 0.05f);
        waterSoftDepth = Mathf.Clamp(0.035f, 0.015f, 0.08f);
        minDepthVisibility = Mathf.Clamp(0.22f, 0.06f, 0.30f);
        alphaFloor = Mathf.Clamp(0.26f, 0.10f, 0.40f);
        edgeBoost = Mathf.Clamp(1.55f, 0.8f, 2.3f);
        edgeWidth = Mathf.Clamp(0.0075f, 0.005f, 0.02f);
        depthEdgeStrength = Mathf.Clamp(2.2f, 0.8f, 2.6f);
        depthEdgeThreshold = Mathf.Clamp(0.0015f, 0.0008f, 0.006f);
        refractionClampPixels = Mathf.Clamp(0.45f, 0.3f, 1.2f);
        refractionEdgeSuppress = Mathf.Clamp(0.95f, 0.6f, 0.98f);
        compositeThicknessExposure = Mathf.Clamp(1.10f, 0.6f, 1.3f);
        compositeThicknessGamma = Mathf.Clamp(0.80f, 0.6f, 1.4f);
        compositeThicknessMax = Mathf.Clamp(9.0f, 6.0f, 24.0f);
        thicknessTopBias = Mathf.Clamp(0.65f, 0.2f, 0.9f);
        depthNormalStrength = Mathf.Clamp(22.0f, 10.0f, 26.0f);
        depthNormalWeight = Mathf.Clamp(0.45f, 0.15f, 0.55f);
        fresnelAlphaBase = Mathf.Clamp(0.72f, 0.4f, 0.9f);
        fresnelAlphaWeight = Mathf.Clamp(1.50f, 0.6f, 2.0f);
        absorption = Mathf.Clamp(1.35f, 0.6f, 2.2f);
        absorptionColor = new Color(0.58f, 0.76f, 1.0f, 1.0f);
    }
    
    void ApplyMediumParticleStability()
    {
        maxSubsteps = Mathf.Max(maxSubsteps, 14);
        fixedTimeStep = Mathf.Clamp(fixedTimeStep, 0.0033f, 0.0039f);
        viscosity = Mathf.Max(viscosity, 0.12f);
        xsphC = Mathf.Clamp(xsphC, 0.30f, 0.36f);
        boundaryDamping = Mathf.Clamp(boundaryDamping, 0.82f, 0.88f);
        soundSpeed = Mathf.Clamp(soundSpeed, 24f, 30f);
        maxSpeed = Mathf.Min(maxSpeed, 12.5f);
        initialJitter = Mathf.Min(initialJitter, 0.015f);
        minSpeed = 0.003f;
    }
    
    void ApplyRealisticWaterPreset()
    {
        enableLowParticleCountTuning = false;
        viscosity = 0.08f;
        xsphC = 0.20f;
        boundaryDamping = 0.65f;
        boundaryDampingZ = 0.60f;
        boundaryMaxBounceSpeedZ = 3.5f;
        minSpeed = 0.015f;
        soundSpeed = 20f;
        maxSpeed = Mathf.Min(maxSpeed, 10.0f);
    }
    void ApplySweepFlowPreset()
    {
        vorticityEps = Mathf.Clamp(0.65f, 0.35f, 1.2f);
        obstacleTangentialStrength = 85f;
        obstaclePushStrength = 52f;
        obstacleFriction = 0.12f;
        stirStrengthScale = 2.6f;
        stirAngularBoost = 3.5f;
        impulseNormalCoeff = 1.15f;
        impulseTangentialCoeff = 1.35f;
        freeSurfaceDamping = 0.0f;
        freeSurfaceThreshold = 0.70f;
        minImpulseRadius = Mathf.Max(minImpulseRadius, 0.9f);
        highQualityDepthScale = Mathf.Clamp(1.5f, 1.0f, 2.0f);
        depthNormalStrength = Mathf.Clamp(24.0f, 10.0f, 28.0f);
        depthNormalWeight = Mathf.Clamp(0.50f, 0.15f, 0.65f);
        thicknessNormalStrength = Mathf.Clamp(1.6f, 1.0f, 2.2f);
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
        runSimulation = true;
    }

    public void Pause()
    {
        runSimulation = false;
    }

    public void Resume()
    {
        runSimulation = true;
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
        int nx = Mathf.Max(1, Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f)));
        int ny = nx;
        int nz = nx;
        Vector3 step = new Vector3(size.x / nx, size.y / ny, size.z / nz);
        int idx = 0;
        for (int iz = 0; iz < nz && idx < particleCount; iz++)
        for (int iy = 0; iy < ny && idx < particleCount; iy++)
        for (int ix = 0; ix < nx && idx < particleCount; ix++)
        {
            Vector3 jitter = new Vector3((Random.value - 0.5f) * step.x, (Random.value - 0.5f) * step.y, (Random.value - 0.5f) * step.z) * initialJitter;
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
        float sum = 0f;
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
                        Vector3 d = p - center;
                        d.y = 0f;
                        if (d.sqrMagnitude <= radius * radius)
                        {
                            sum += p.y;
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
        if (n > 0) level = sum / n;
        return n > 0;
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
        float sum = 0f;
        int n = 0;
        float r2 = radius * radius;
        for (int i = 0; i < cpuXCache.Length; i++)
        {
            Vector3 p = cpuXCache[i];
            Vector3 d = p - center; d.y = 0f;
            if (d.sqrMagnitude <= r2)
            {
                sum += p.y;
                n++;
            }
        }
        if (n > 0) level = sum / n;
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
    
    // Removed buoyant block runtime helpers; use BuoyantBlockFloat component and menu creator instead.

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
}
