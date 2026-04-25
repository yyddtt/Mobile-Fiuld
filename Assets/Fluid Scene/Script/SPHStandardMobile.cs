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
    [Tooltip("越大越「糖浆」；想轻盈可保持偏低并与 XSPH 配合。")]
    public float viscosity = 0.055f;
    [HideInInspector] public float eosGamma = 7.0f;
    [HideInInspector] public float soundSpeed = 25.0f;
    public Vector3 gravity = new Vector3(0,-9.8f,0);
    [HideInInspector] public float maxSpeed = 12.0f;
    [HideInInspector] public float boundaryDamping = 0.5f;
    [HideInInspector] public float boundaryDampingZ = 0.6f;
    [HideInInspector] public float boundaryMaxBounceSpeedZ = 3.5f;
    [HideInInspector] public float xsphC = 0.14f;
    [Tooltip("SSFR 椭球基准半径（世界单位）。SPH 排列较规则，可略小于 MPM；过大则屏上 splat 显胖、细节糊成一片。建议约 0.08–0.11。")]
    public float particleSize = 0.102f;
    public bool runSimulation = true;
    [HideInInspector] public bool enableLowParticleCountTuning = true;
    public float minSpeed = 0.015f;
    
    [Tooltip("初始粒子生成盒最小角，坐标必须在 boundsMin～boundsMax 内；若越界会被钳制，x 偏小且 boundsMin.x 较大时会整团贴在左墙。")]
    public Vector3 spawnMin = new Vector3(4,2,1);
    [Tooltip("初始粒子生成盒最大角，须大于 spawnMin 且落在边界盒内。")]
    public Vector3 spawnMax = new Vector3(10,6,4);
    [HideInInspector] public bool autoCalibrateMass = true;
    [HideInInspector] public bool autoNeighbourRadius = true;
    [HideInInspector] public float initialJitter = 0.05f;
    public bool enableSubstepping = true;
    public float fixedTimeStep = 0.004f;
    public int maxSubsteps = 6;
    public RenderMode renderMode = RenderMode.Fluid;
    [HideInInspector] public bool simulateInLateUpdate = true;
    [Header("Physics Preset")]
    public bool realisticWaterPreset = true;
    public bool sweepFlowPreset = true;
    [Tooltip("开启时仅保留历史名称；渲染参数请直接在下方 SSFR 区块调节（与 MPM 一致）。不再强制覆盖模糊/染色。")]
    public bool sharpEdgesPreset = false;
    [Header("Adaptive/Power")]
    [HideInInspector] public int targetFrameRate = 60;
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
    [Range(0.01f, 0.5f)] public float thicknessContribution = 0.056f;
    [Range(0, 5)] public int thicknessBlurIterations = 2;
    [Range(1, 20)] public int thicknessBlurRadius = 5;
    [Range(1, 5)] public int thicknessDownsample = 2;
    [Header("Normals")]
    [Tooltip("传给 SSFR/FluidNormals；过大易显颗粒波纹，略低更「整片水面」。")]
    [Range(0.1f, 10f)] public float normalStrength = 0.86f;
    [Header("Anisotropy (grid particles & fluid splats)")]
    [Tooltip("乘在 particleSize 上（着色器内 splat 尺寸）。MPM 常需 1.4+ 填洞；SPH 可降到约 1.15–1.28 以保留轮廓细节。")]
    [Range(1f, 2f)] public float renderParticleScale = 1.38f;
    [Range(0f, 5f)] public float anisotropyScale = 0.38f;
    [Range(1f, 10f)] public float maxAnisotropy = 4f;
    [Header("Depth filtering")]
    public DepthFilterType filterType = DepthFilterType.Gaussian;
    public enum DepthFilterType { Bilateral, Gaussian }
    [Tooltip("深度 RT 目标高度；略提高可减轻锯齿（SPH 可略锐于 MPM）。0 则仅用 depthDownsample。")]
    public int targetDepthHeight = 520;
    [Range(1, 4)] public int depthDownsample = 2;
    [Range(0, 10)] public int blurIterations = 2;
    [Range(0.1f, 50f)] public float blurSigmaSpatial = 6.8f;
    [Range(0.01f, 5f)] public float blurSigmaRange = 2.35f;
    [Range(1, 20)] public int blurRadius = 6;

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
    public float obstaclePushStrength = 40f;
    public float obstacleDamping = 0.6f;
    public Transform barrierTransform;
    public bool enableBarrierMove = true;
    public bool requireSelectionToMove = true;
    public LayerMask selectableMask = ~0;
    [Tooltip("涡量约束强度；>0.2 时显式 WCSPH 极易抖。扫流预设已改为上限夹紧。")]
    public float vorticityEps = 0.08f;
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

    [Tooltip("首帧渲染前在 GPU 上预跑的子步数，缓解初始密度未建立导致的「悬在空中」与首帧乱溅。")]
    [Range(0, 48)]
    public int simulationWarmupSteps = 12;

#if UNITY_EDITOR
    void OnValidate()
    {
        particleCount = Mathf.Clamp(particleCount, MinParticleCount, MaxParticleCountMobile);
        gridResolution = Mathf.Clamp(gridResolution, 16, 48);
        maxImpulses = Mathf.Max(1, maxImpulses);
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
        kMain = cs.FindKernel("SPHMain");

        int gridCount = gridResolution * gridResolution * gridResolution;
        bufX = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufV = new ComputeBuffer(particleCount, sizeof(float) * 3);
        bufRho = new ComputeBuffer(particleCount, sizeof(float));
        bufCellHead = new ComputeBuffer(gridCount, sizeof(int));
        bufNextIndex = new ComputeBuffer(particleCount, sizeof(int));
        particlesBuffer = new ComputeBuffer(particleCount, sizeof(float) * 12);
        int impulseCapacity = Mathf.Max(1, maxImpulses);
        if (enableStir)
            impulseCapacity = Mathf.Max(impulseCapacity, 16);
        bufImpulses = new ComputeBuffer(impulseCapacity, sizeof(float) * 4);
        bufImpulses.SetData(new Vector4[1] { Vector4.zero });
        bufObstacles = new ComputeBuffer(Mathf.Max(1, obstacleTransforms != null ? obstacleTransforms.Length : 1), sizeof(float) * 4);

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
        if (debugShader != null) debugDepthMat = new Material(debugShader);

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
            mainCam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
        }
        if (enableLowParticleCountTuning && particleCount < 8000)
        {
            maxSubsteps = Mathf.Max(maxSubsteps, 6);
            fixedTimeStep = Mathf.Min(fixedTimeStep, 0.0045f);
            viscosity = Mathf.Max(viscosity, 0.09f);
            xsphC = Mathf.Clamp(xsphC, 0.22f, 0.42f);
            boundaryDamping = Mathf.Clamp(boundaryDamping, 0.68f, 0.9f);
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
        if (sharpEdgesPreset) ApplySharpEdgesPreset();
        if (Application.isMobilePlatform)
        {
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 4);
            if (QualitySettings.shadows == ShadowQuality.Disable) QualitySettings.shadows = ShadowQuality.HardOnly;
        }

        WarmupSimulation();
    }

    void WarmupSimulation()
    {
        if (!runSimulation || simulationWarmupSteps <= 0 || cs == null) return;
        if (bufX == null || bufV == null || bufImpulses == null || bufObstacles == null || particlesBuffer == null)
            return;
        float dtW = Mathf.Min(fixedTimeStep * 0.45f, 0.0032f);
        for (int i = 0; i < simulationWarmupSteps; i++)
            SimulateStep(dtW);
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
        // 粗略 CFL：显式压力对 dt 敏感，步长过大易见整团弹跳/抖动
        float h = Mathf.Max(neighbourRadius, 0.01f);
        float c = Mathf.Max(soundSpeed, 8f);
        float dtCfl = 0.28f * h / c;
        if (enableSubstepping && dtStep > dtCfl && dtCfl > 1e-5f)
        {
            int need = Mathf.Clamp(Mathf.CeilToInt(dtFrame / dtCfl), 1, Mathf.Max(maxSubsteps * 2, maxSubsteps));
            steps = Mathf.Max(steps, need);
            steps = Mathf.Min(steps, Mathf.Max(maxSubsteps * 2, 12));
            dtStep = dtFrame / steps;
        }
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
        cs.SetBuffer(kMain, "x", bufX);
        cs.SetBuffer(kMain, "v", bufV);
        cs.SetBuffer(kMain, "rho", bufRho);
        cs.SetBuffer(kMain, "cellHead", bufCellHead);
        cs.SetBuffer(kMain, "nextIndex", bufNextIndex);
        cs.SetBuffer(kMain, "_particlesBuffer", particlesBuffer);
        cs.SetBuffer(kMain, "impulses", bufImpulses);
        cs.SetBuffer(kMain, "obstacles", bufObstacles);
    }

    void SimulateStep(float dt)
    {
        if (cs == null || bufX == null || bufV == null || bufRho == null || bufCellHead == null || bufNextIndex == null
            || bufImpulses == null || bufObstacles == null || particlesBuffer == null)
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
        cs.SetVector("swirlAxis", swirlAxis);
        cs.SetFloat("impulseNormalCoeff", Mathf.Max(0f, impulseNormalCoeff));
        cs.SetFloat("impulseTangentialCoeff", Mathf.Max(0f, impulseTangentialCoeff));
        int iCount = 0;
        float iRad = 0f;
        float iStr = 0f;
        float dynImpulseRadius = 0f;
        float speedFactor = 1f + Mathf.Clamp(stirSpeed * stirSpeedBoost, 0f, 5f);
        float boostFromRadius = 1f;
        bool impulsesBufferRebuilt = false;
        if (enableStir && stirTransform != null && bufImpulses != null)
        {
            Vector3 p = ClampToBounds(stirTransform.position);
            int safeImp = Mathf.Max(1, maxImpulses);
            int desiredCount = Mathf.Min(16, safeImp);
            if (bufImpulses.count < desiredCount)
            {
                bufImpulses.Release();
                bufImpulses = new ComputeBuffer(desiredCount, sizeof(float) * 4);
                impulsesBufferRebuilt = true;
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
        if (impulsesBufferRebuilt) cs.SetBuffer(kMain, "impulses", bufImpulses);
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
        bool obstaclesBufferRebuilt = false;
        if (bufObstacles == null || bufObstacles.count != allocCount)
        {
            if (bufObstacles != null) bufObstacles.Release();
            bufObstacles = new ComputeBuffer(allocCount, sizeof(float) * 4);
            obstaclesBufferRebuilt = true;
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
        if (obstaclesBufferRebuilt) cs.SetBuffer(kMain, "obstacles", bufObstacles);
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
        if (sphereMesh == null || particlesBuffer == null) return;
        drawBounds.center = (boundsMin + boundsMax) * 0.5f;
        drawBounds.size = boundsMax - boundsMin + Vector3.one * 2f;

        if (renderMode == RenderMode.GridParticles)
        {
            if (gridParticleMat == null) return;
            props.Clear();
            props.SetFloat("_size", particleSize);
            props.SetFloat("_SizeScale", renderParticleScale);
            props.SetFloat("_AnisotropyScale", anisotropyScale);
            props.SetFloat("_MaxAnisotropy", maxAnisotropy);
            props.SetBuffer("_particlesBuffer", particlesBuffer);
            Shader.SetGlobalBuffer("_particlesBuffer", particlesBuffer);
            Shader.SetGlobalFloat("_SizeScale", renderParticleScale);
            Shader.SetGlobalFloat("_size", particleSize);
            Shader.SetGlobalFloat("_AnisotropyScale", anisotropyScale);
            Shader.SetGlobalFloat("_MaxAnisotropy", maxAnisotropy);
            Graphics.DrawMeshInstancedProcedural(sphereMesh, 0, gridParticleMat, drawBounds, particleCount, props, ShadowCastingMode.On, true);
            return;
        }

        if (fluidCmd != null && !enableRendering)
        {
            fluidCmd.Clear();
            return;
        }

        props.Clear();
        props.SetFloat("_size", particleSize);
        props.SetFloat("_SizeScale", renderParticleScale);
        props.SetFloat("_AnisotropyScale", anisotropyScale);
        props.SetFloat("_MaxAnisotropy", maxAnisotropy);
        props.SetBuffer("_particlesBuffer", particlesBuffer);

        Shader.SetGlobalBuffer("_particlesBuffer", particlesBuffer);
        Shader.SetGlobalFloat("_SizeScale", renderParticleScale);
        Shader.SetGlobalFloat("_size", particleSize);
        Shader.SetGlobalFloat("_AnisotropyScale", anisotropyScale);
        Shader.SetGlobalFloat("_MaxAnisotropy", maxAnisotropy);

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
                RenderTextureFormat depthFmt = SelectSingleChannelFloatFormat();

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

                Material currentBlurMat = (filterType == DepthFilterType.Gaussian && gaussianMat != null) ? gaussianMat : blurMat;

                if (currentBlurMat != null && blurIterations > 0)
                {
                    currentBlurMat.SetFloat("_SigmaSpatial", blurSigmaSpatial);
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

            if (thicknessMat != null)
            {
                props.SetFloat("_ContributionScale", thicknessContribution);
                props.SetFloat("_SizeScale", renderParticleScale);
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
                    for (int i = 0; i < thicknessBlurIterations; i++)
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
        if (mainCam != null && fluidCmd != null)
        {
            mainCam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, fluidCmd);
            fluidCmd.Release();
            fluidCmd = null;
        }
        Release(bufX); Release(bufV); Release(bufRho); Release(bufCellHead); Release(bufNextIndex); Release(particlesBuffer); Release(bufImpulses); Release(bufObstacles);
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
        // 与 MPMFluidMobile 一致：不在此预设里改写 SSFR，避免与 MPM 的 Gaussian/高模糊/染色 完全两套逻辑。
    }
    
    void ApplyMediumParticleStability()
    {
        // 粒子多时仍保持可接受的子步数；过高子步 + 强边界阻尼易造成“一碰底就吸住”。
        maxSubsteps = Mathf.Clamp(Mathf.Max(maxSubsteps, 8), 8, 12);
        fixedTimeStep = Mathf.Clamp(fixedTimeStep, 0.0033f, 0.0042f);
        // 不强行抬高黏度，避免把「轻盈水」又拉回糖浆感；只做上限防止极端发散
        viscosity = Mathf.Min(viscosity, 0.095f);
        if (viscosity < 0.04f) viscosity = 0.04f;
        xsphC = Mathf.Clamp(xsphC, 0.08f, 0.22f);
        boundaryDamping = Mathf.Clamp(boundaryDamping, 0.55f, 0.72f);
        soundSpeed = Mathf.Clamp(soundSpeed, 26f, 38f);
        maxSpeed = Mathf.Min(maxSpeed, 12.5f);
        initialJitter = Mathf.Min(initialJitter, 0.015f);
        // 与着色器里“贴壁休眠”配合：过小会在稀疏邻域误判静止
        minSpeed = Mathf.Max(minSpeed, 0.012f);
        vorticityEps = Mathf.Min(vorticityEps, 0.14f);
    }
    
    void ApplyRealisticWaterPreset()
    {
        enableLowParticleCountTuning = false;
        viscosity = 0.055f;
        xsphC = 0.14f;
        boundaryDamping = 0.58f;
        boundaryDampingZ = 0.60f;
        boundaryMaxBounceSpeedZ = 3.5f;
        minSpeed = 0.015f;
        soundSpeed = Mathf.Max(soundSpeed, 24f);
        maxSpeed = Mathf.Min(maxSpeed, 10.0f);
    }
    void ApplySweepFlowPreset()
    {
        // 曾误写为 Clamp(0.65, …) 恒等于 0.65，涡量过强会导致整流体高频抖、跳。
        vorticityEps = Mathf.Clamp(vorticityEps, 0f, 0.16f);
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
        normalStrength = Mathf.Clamp(normalStrength, 0.85f, 1.6f);
        // 扫流预设只调物理/交互；不再抬高深度模糊，避免 SPH 被强制糊成「MPM 填洞」档。
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
