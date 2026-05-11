using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 统一流体演示 UI：算法切换、暂停/单步/重置、时间缩放、小船与粒子显示。
/// 面板默认贴在屏幕右上角，避免与左侧 RuntimePerfHUD（OnGUI）重叠。
/// </summary>
public class FluidAppShell : MonoBehaviour
{
    public MPMFluidMobile mpm;
    public SPHStandardMobile sph;
    public FluidBoat boat;
    public SimpleBoatUI boatUi;
    public GameObject boatRoot;

    [Tooltip("首次进入场景时是否使用 MPM（否则为 SPH）")]
    public bool startWithMpm = true;
    [Tooltip("首次进入场景时是否显示小船。关闭后需手动点 UI 的“小船”开关才显示。")]
    public bool startWithBoat = false;

    [Header("布局")]
    [Tooltip("控制 dock 距屏幕右边缘的像素（参考分辨率 1080 宽下）")]
    public float dockMarginRight = 18f;
    [Tooltip("控制 dock 距屏幕上边缘的像素")]
    public float dockMarginTop = 20f;
    [Tooltip("dock 宽度")]
    public float dockWidth = 322f;

    [Header("内置 UI（留空则在运行时生成）")]
    public Canvas rootCanvas;

    bool wantPaused;
    bool boatDesiredVisible = false;
    bool particlesDesiredVisible;

    Text statusLine;
    Button btnPause;
    Button btnStep;
    Toggle toggleMpm;
    Toggle toggleSph;
    Toggle toggleBoat;
    Toggle toggleParticles;
    Text timeScaleLabel;
    RectTransform dockRt;

    static Font BuiltinUiFont()
    {
        try
        {
            var f = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "PingFang SC", "Noto Sans CJK SC", "Arial" },
                32);
            if (f != null)
                return f;
        }
        catch { /* 忽略 */ }

        var builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (builtin != null)
            return builtin;

        try
        {
            return Font.CreateDynamicFontFromOSFont(new[] { "Arial" }, 28);
        }
        catch
        {
            return null;
        }
    }

    void Awake()
    {
        if (mpm == null) mpm = FindObjectOfType<MPMFluidMobile>();
        if (sph == null) sph = FindObjectOfType<SPHStandardMobile>();
        if (boat == null) boat = FindObjectOfType<FluidBoat>();
        if (boatUi == null) boatUi = FindObjectOfType<SimpleBoatUI>();
        if (boatRoot == null && boat != null) boatRoot = boat.gameObject;
        boatDesiredVisible = startWithBoat;

        if (mpm != null) wantPaused = !mpm.runSimulation;
        else if (sph != null) wantPaused = !sph.runSimulation;

        // 在 Awake 就先执行一次，避免 Start 前出现一帧小船可见。
        if (!boatDesiredVisible)
        {
            if (boat != null) boat.enabled = false;
            if (boatRoot != null) boatRoot.SetActive(false);
            if (boatUi != null) boatUi.enabled = false;
        }

        EnsureCanvas();
    }

    void Start()
    {
        SetAlgorithm(startWithMpm);
        ApplyPauseState();
        ApplyParticleToggle(particlesDesiredVisible);
        RefreshBoatUi();
        RefreshStatus();
    }

    void Update()
    {
        if (btnPause != null)
        {
            var t = btnPause.GetComponentInChildren<Text>();
            if (t != null) t.text = wantPaused ? "继续" : "暂停";
        }
        RefreshStatus();
    }

    void EnsureCanvas()
    {
        if (rootCanvas != null) return;

        EnsureEventSystem();

        var font = BuiltinUiFont();

        var go = new GameObject("FluidApp_UI");
        rootCanvas = go.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 80;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        var dock = new GameObject("ControlDock");
        dock.transform.SetParent(go.transform, false);
        dockRt = dock.AddComponent<RectTransform>();
        dockRt.anchorMin = Vector2.one;
        dockRt.anchorMax = Vector2.one;
        dockRt.pivot = Vector2.one;
        dockRt.anchoredPosition = new Vector2(-dockMarginRight, -dockMarginTop);
        dockRt.sizeDelta = new Vector2(dockWidth, 540f);

        var dockBg = dock.AddComponent<Image>();
        dockBg.color = new Color(0.055f, 0.075f, 0.11f, 0.93f);
        var sh = dock.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.55f);
        sh.effectDistance = new Vector2(4f, -4f);

        const float padX = 14f;
        float cy = -14f;
        const float gap = 6f;

        statusLine = AddDockStripText(dock.transform, "Status", "流体演示", 22, font, padX, ref cy, 36);
        statusLine.color = new Color(0.92f, 0.96f, 1f);
        var titleOut = statusLine.gameObject.AddComponent<Outline>();
        titleOut.effectColor = new Color(0f, 0f, 0f, 0.55f);
        titleOut.effectDistance = new Vector2(1f, -1f);

        cy -= 2f;
        AddDockStripText(dock.transform, "Sec1", "求解器", 17, font, padX, ref cy, 22).color = new Color(0.62f, 0.72f, 0.88f);

        var tgGo = new GameObject("SolverToggleGroup");
        tgGo.transform.SetParent(dock.transform, false);
        var tgRt = tgGo.AddComponent<RectTransform>();
        PlaceDockStrip(tgRt, padX, ref cy, 38f);
        var toggleGroup = tgGo.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = false;

        toggleMpm = CreateToggle(tgGo.transform, "MPM", "MPM", font, new Vector2(6, 0), new Vector2(132, 32));
        toggleSph = CreateToggle(tgGo.transform, "SPH", "SPH", font, new Vector2(144, 0), new Vector2(132, 32));
        toggleMpm.group = toggleGroup;
        toggleSph.group = toggleGroup;
        toggleMpm.onValueChanged.AddListener(v => { if (v) SetAlgorithm(true); });
        toggleSph.onValueChanged.AddListener(v => { if (v) SetAlgorithm(false); });
        toggleMpm.SetIsOnWithoutNotify(startWithMpm);
        toggleSph.SetIsOnWithoutNotify(!startWithMpm);

        cy -= gap;
        AddDockSeparator(dock.transform, padX, ref cy);
        cy -= 4f;

        btnPause = AddDockStripButton(dock.transform, "Pause", "暂停 / 继续", font, padX, ref cy, 40f);
        btnPause.onClick.AddListener(TogglePause);
        var btnReset = AddDockStripButton(dock.transform, "Reset", "重置流体", font, padX, ref cy, 40f);
        btnReset.onClick.AddListener(ResetSimulation);
        btnStep = AddDockStripButton(dock.transform, "Step", "单步模拟", font, padX, ref cy, 40f);
        btnStep.onClick.AddListener(StepOnce);

        cy -= 4f;
        AddDockSeparator(dock.transform, padX, ref cy);
        cy -= 4f;

        AddDockStripText(dock.transform, "Sec2", "选项", 17, font, padX, ref cy, 22).color = new Color(0.62f, 0.72f, 0.88f);

        var boatRow = new GameObject("BoatRow");
        boatRow.transform.SetParent(dock.transform, false);
        var boatRowRt = boatRow.AddComponent<RectTransform>();
        PlaceDockStrip(boatRowRt, padX, ref cy, 34f);
        toggleBoat = CreateToggle(boatRow.transform, "Boat", "小船", font, new Vector2(4, 0), new Vector2(dockWidth - padX * 2 - 8f, 30));
        toggleBoat.isOn = boatDesiredVisible;
        toggleBoat.onValueChanged.AddListener(v => { boatDesiredVisible = v; RefreshBoatUi(); });

        var partRow = new GameObject("ParticlesRow");
        partRow.transform.SetParent(dock.transform, false);
        var partRowRt = partRow.AddComponent<RectTransform>();
        PlaceDockStrip(partRowRt, padX, ref cy, 34f);
        toggleParticles = CreateToggle(partRow.transform, "Particles", "显示粒子点", font, new Vector2(4, 0), new Vector2(dockWidth - padX * 2 - 8f, 30));
        toggleParticles.isOn = particlesDesiredVisible;
        toggleParticles.onValueChanged.AddListener(ApplyParticleToggle);

        cy -= 4f;
        AddDockSeparator(dock.transform, padX, ref cy);
        cy -= 4f;

        timeScaleLabel = AddDockStripText(dock.transform, "TsLabel", "时间 ×1.0", 17, font, padX, ref cy, 24);
        timeScaleLabel.color = new Color(0.78f, 0.86f, 0.96f);

        var tsBar = new GameObject("TimeScaleBar");
        tsBar.transform.SetParent(dock.transform, false);
        var tsRt = tsBar.AddComponent<RectTransform>();
        PlaceDockStrip(tsRt, padX, ref cy, 38f);

        float gapB = 8f;
        float innerW = dockWidth - padX * 2;
        float bw = (innerW - gapB * 2f) / 3f;
        StylePrimaryButton(CreateBarButton(tsBar.transform, "TsSlow", "0.5×", font, 0, bw, 34)).onClick.AddListener(() => ApplyTimeScale(0.5f));
        StylePrimaryButton(CreateBarButton(tsBar.transform, "Ts1", "1×", font, bw + gapB, bw, 34)).onClick.AddListener(() => ApplyTimeScale(1f));
        StylePrimaryButton(CreateBarButton(tsBar.transform, "Ts2", "2×", font, 2f * (bw + gapB), bw, 34)).onClick.AddListener(() => ApplyTimeScale(2f));
        ApplyTimeScale(1f);

        if (boatUi != null && boatUi.joystickBackground == null)
        {
            var joyBg = new GameObject("Joystick_BG");
            joyBg.transform.SetParent(go.transform, false);
            var joyRt = joyBg.AddComponent<RectTransform>();
            joyRt.sizeDelta = new Vector2(176, 176);
            joyRt.anchorMin = new Vector2(0, 0);
            joyRt.anchorMax = new Vector2(0, 0);
            joyRt.pivot = new Vector2(0.5f, 0.5f);
            joyRt.anchoredPosition = new Vector2(148, 186);
            var joyIm = joyBg.AddComponent<Image>();
            joyIm.color = new Color(0.1f, 0.14f, 0.2f, 0.62f);
            joyIm.raycastTarget = true;
            var jsh = joyBg.AddComponent<Shadow>();
            jsh.effectColor = new Color(0f, 0f, 0f, 0.35f);
            jsh.effectDistance = new Vector2(2f, -2f);

            var handle = new GameObject("Joystick_Handle");
            handle.transform.SetParent(joyBg.transform, false);
            var hRt = handle.AddComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(78, 78);
            hRt.anchoredPosition = Vector2.zero;
            var hIm = handle.AddComponent<Image>();
            hIm.color = new Color(0.38f, 0.72f, 1f, 0.95f);

            boatUi.joystickBackground = joyRt;
            boatUi.joystickHandle = hRt;
        }
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    static void PlaceDockStrip(RectTransform rt, float padX, ref float cy, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(-padX * 2f, height);
        rt.anchoredPosition = new Vector2(0, cy);
        cy -= height + 6f;
    }

    static Text AddDockStripText(Transform parent, string name, string msg, int size, Font font, float padX, ref float cy, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(-padX * 2f, height);
        rt.anchoredPosition = new Vector2(0, cy);
        var t = go.AddComponent<Text>();
        if (font != null) t.font = font;
        t.text = msg;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleLeft;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        cy -= height + 4f;
        return t;
    }

    static void AddDockSeparator(Transform parent, float padX, ref float cy)
    {
        var go = new GameObject("Sep");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(-padX * 2f, 2f);
        rt.anchoredPosition = new Vector2(0, cy);
        var im = go.AddComponent<Image>();
        im.color = new Color(0.22f, 0.32f, 0.45f, 0.55f);
        cy -= 2f + 6f;
    }

    static Button AddDockStripButton(Transform parent, string name, string label, Font font, float padX, ref float cy, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(-padX * 2f, height);
        rt.anchoredPosition = new Vector2(0, cy);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.42f, 0.78f, 0.95f);
        var btn = go.AddComponent<Button>();
        StylePrimaryButton(btn);
        btn.targetGraphic = img;
        int fs = Mathf.Clamp(Mathf.RoundToInt(height * 0.42f), 15, 22);
        var tx = CreateText(go.transform, "Label", label, fs, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font);
        StretchFull(tx.rectTransform);
        cy -= height + 6f;
        return btn;
    }

    static Button StylePrimaryButton(Button btn)
    {
        var img = btn.GetComponent<Image>();
        if (img == null) img = btn.gameObject.AddComponent<Image>();
        var c = btn.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(0.95f, 0.98f, 1f, 1f);
        c.pressedColor = new Color(0.75f, 0.85f, 1f, 1f);
        c.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
        btn.colors = c;
        return btn;
    }

    static Button CreateBarButton(Transform parent, string name, string label, Font font, float x, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(w, h);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.14f, 0.38f, 0.72f, 0.95f);
        var btn = go.AddComponent<Button>();
        StylePrimaryButton(btn);
        int fs = Mathf.Clamp(Mathf.RoundToInt(h * 0.45f), 14, 20);
        var tx = CreateText(go.transform, "Label", label, fs, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font);
        StretchFull(tx.rectTransform);
        btn.targetGraphic = img;
        return btn;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        go.AddComponent<Image>();
        return go;
    }

    static Text CreateText(Transform parent, string name, string msg, int size, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 sz, Font font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sz;
        var t = go.AddComponent<Text>();
        if (font != null) t.font = font;
        t.text = msg;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    static Button CreateButtonRight(Transform parent, string name, string label, Font font, float marginFromRight, Vector2 sz)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-marginFromRight, 0);
        rt.sizeDelta = sz;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.38f, 0.68f, 0.92f);
        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.highlightedColor = new Color(0.35f, 0.6f, 1f, 1f);
        c.pressedColor = new Color(0.12f, 0.28f, 0.55f, 1f);
        btn.colors = c;
        var fontSize = Mathf.Clamp(Mathf.RoundToInt(sz.y * 0.45f), 14, 22);
        var tx = CreateText(go.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, font);
        StretchFull(tx.rectTransform);
        return btn;
    }

    static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 pos, Vector2 sz)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sz;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.82f, 0.92f);
        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.highlightedColor = new Color(0.35f, 0.6f, 1f, 1f);
        c.pressedColor = new Color(0.15f, 0.35f, 0.7f, 1f);
        btn.colors = c;

        var tx = CreateText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, font);
        StretchFull(tx.rectTransform);
        return btn;
    }

    static Toggle CreateToggle(Transform parent, string name, string label, Font font, Vector2 pos, Vector2 sz)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sz;

        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bRt = bg.AddComponent<RectTransform>();
        bRt.sizeDelta = new Vector2(28, 28);
        bRt.anchorMin = new Vector2(0, 0.5f);
        bRt.anchorMax = new Vector2(0, 0.5f);
        bRt.pivot = new Vector2(0, 0.5f);
        bRt.anchoredPosition = new Vector2(0, 0);
        var bIm = bg.AddComponent<Image>();
        bIm.color = new Color(0.18f, 0.22f, 0.3f, 0.95f);

        var check = new GameObject("Checkmark");
        check.transform.SetParent(bg.transform, false);
        var cRt = check.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.1f, 0.1f);
        cRt.anchorMax = new Vector2(0.9f, 0.9f);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;
        var cIm = check.AddComponent<Image>();
        cIm.color = new Color(0.4f, 0.85f, 0.95f, 1f);

        var t = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 0), new Vector2(sz.x - 36, sz.y), font);
        t.color = new Color(0.85f, 0.9f, 1f);

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bIm;
        toggle.graphic = cIm;
        return toggle;
    }

    void ApplyTimeScale(float t)
    {
        t = Mathf.Clamp(t, 0.25f, 3f);
        Time.timeScale = t;
        if (timeScaleLabel != null)
            timeScaleLabel.text = $"时间 ×{t:0.##}";
    }

    public void SetAlgorithm(bool useMpm)
    {
        if (mpm != null) mpm.enabled = useMpm;
        if (sph != null) sph.enabled = !useMpm;
        if (toggleMpm != null) toggleMpm.SetIsOnWithoutNotify(useMpm);
        if (toggleSph != null) toggleSph.SetIsOnWithoutNotify(!useMpm);
        ApplyPauseState();
        RefreshBoatUi();
        RefreshStatus();
    }

    void TogglePause()
    {
        wantPaused = !wantPaused;
        ApplyPauseState();
    }

    void ApplyPauseState()
    {
        bool run = !wantPaused;
        if (mpm != null) mpm.runSimulation = run;
        if (sph != null) sph.runSimulation = run;
    }

    void ResetSimulation()
    {
        wantPaused = true;
        ApplyPauseState();
        if (boat != null) boat.NotifyFluidReset();
        if (mpm != null && mpm.enabled) mpm.ResetSimulation();
        if (sph != null && sph.enabled) sph.ResetSimulation();
        wantPaused = false;
        ApplyPauseState();
    }

    void StepOnce()
    {
        wantPaused = true;
        ApplyPauseState();
        if (mpm != null && mpm.enabled) mpm.StepOnce();
        if (sph != null && sph.enabled) sph.StepOnce();
    }

    void ApplyParticleToggle(bool on)
    {
        particlesDesiredVisible = on;
        if (mpm != null) mpm.renderParticles = on;
        if (sph != null) sph.renderParticles = on;
    }

    void RefreshBoatUi()
    {
        bool mpmOn = mpm != null && mpm.enabled;
        bool sphOn = sph != null && sph.enabled;
        if (toggleBoat != null)
            toggleBoat.interactable = mpmOn || sphOn;

        bool showBoat = (mpmOn || sphOn) && boatDesiredVisible;
        if (boat != null) boat.enabled = showBoat;
        if (boatRoot != null) boatRoot.SetActive(showBoat);
        if (boatUi != null) boatUi.enabled = showBoat;
        if (showBoat && boat != null)
        {
            if (boat.fluid == null) boat.fluid = mpm != null ? mpm : FindObjectOfType<MPMFluidMobile>();
            if (boat.sphFluid == null) boat.sphFluid = sph != null ? sph : FindObjectOfType<SPHStandardMobile>();
            boat.SnapToActiveFluidStart(true);
        }
    }

    void RefreshStatus()
    {
        if (statusLine == null) return;
        string algo = (mpm != null && mpm.enabled) ? "MPM" : "SPH";
        string state = wantPaused ? "已暂停" : "运行中";
        statusLine.text = $"{algo}  ·  {state}";
    }
}
