using UnityEngine;

public class RuntimePerfHUD : MonoBehaviour
{
    public bool show = true;
    public int fontSize = 28;
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);

    float deltaTime;
    float fps;
    float frameTimeMs;

    float startTime;
    float startBattery = -1f;
    float drainPerMinute;
    
    // Android Power Monitoring
    bool supportsAndroidBattery = false;
    AndroidJavaObject batteryManager;
    float currentMA = 0f;
    float averageMA = 0f;
    System.Collections.Generic.Queue<float> maHistory = new System.Collections.Generic.Queue<float>();
    int historySize = 10;
    float currentWatts = 0f;
    float lastBatteryUpdate;
    string debugBatteryInfo = "";

    // Baseline for Delta
    float baselineMA = 0f;
    bool hasBaseline = false;
    bool isCalibrating = false;
    string calibrationStatus = "";

    // UI Elements
    GUIStyle style;
    Texture2D bgTexture;
    Rect rect;
    MPMFluidMobile fluidSim;

    void Awake()
    {
        var instances = FindObjectsOfType<RuntimePerfHUD>();
        if (instances.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        startTime = Time.realtimeSinceStartup;
        startBattery = SystemInfo.batteryLevel;
        
        // Try init, but don't limit to just one method
        InitAndroidBattery();
        
        fluidSim = FindObjectOfType<MPMFluidMobile>();
    }

    void InitAndroidBattery()
    {
        if (Application.platform != RuntimePlatform.Android) 
        {
            debugBatteryInfo = "Not Android";
            return;
        }

        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity != null)
                {
                    batteryManager = activity.Call<AndroidJavaObject>("getSystemService", "batterymanager");
                    supportsAndroidBattery = batteryManager != null;
                    if(!supportsAndroidBattery) debugBatteryInfo = "Mgr Null";
                }
            }
        }
        catch (System.Exception e)
        {
            debugBatteryInfo = "Init Err";
            Debug.LogWarning("[RuntimePerfHUD] Failed to init Android BatteryManager: " + e.Message);
        }
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        if (deltaTime > 0f)
        {
            fps = 1f / deltaTime;
            frameTimeMs = deltaTime * 1000f;
        }

        // Update Battery Stats every 1 second
        if (Time.realtimeSinceStartup - lastBatteryUpdate > 1.0f)
        {
            lastBatteryUpdate = Time.realtimeSinceStartup;
            UpdateBatteryStats();
        }
    }

    void UpdateBatteryStats()
    {
        // 1. Standard Unity Battery Level
        if (startBattery >= 0f && SystemInfo.batteryLevel >= 0f)
        {
            float elapsedMinutes = (Time.realtimeSinceStartup - startTime) / 60f;
            if (elapsedMinutes > 0.1f)
            {
                float used = startBattery - SystemInfo.batteryLevel;
                if (used > 0f)
                {
                    drainPerMinute = used / elapsedMinutes * 100f;
                }
            }
        }
        
        // 2. Android Instantaneous Current
        if (Application.platform == RuntimePlatform.Android)
        {
            long microAmp = 0;
            bool success = false;
            string source = "";

            // Method A: BatteryManager API (Try multiple Property IDs)
            // 2: CURRENT_NOW, 3: CURRENT_AVERAGE, 1: STATUS (Sometimes mismapped)
            if (supportsAndroidBattery && batteryManager != null)
            {
                int[] propIds = { 2, 3 }; 
                foreach(int id in propIds)
                {
                    try 
                    {
                        int val = batteryManager.Call<int>("getIntProperty", id);
                        if (Mathf.Abs(val) > 100) // Valid if > 100 uA
                        {
                            microAmp = val;
                            success = true;
                            source = "API" + id;
                            break;
                        }
                    }
                    catch {}
                }
            }

            // Method B: System File (Fallback - Expanded Search)
            if (!success)
            {
                string[] paths = { 
                    "/sys/class/power_supply/battery/current_now",
                    "/sys/class/power_supply/battery/current_avg",
                    "/sys/class/power_supply/bms/current_now",
                    "/sys/class/power_supply/usb/current_now",
                    "/sys/class/power_supply/main/current_now"
                };

                foreach (var path in paths)
                {
                    if (ReadSystemFile(path, out long val))
                    {
                        if (Mathf.Abs(val) > 100)
                        {
                            microAmp = val;
                            success = true;
                            source = "File";
                            break;
                        }
                    }
                }
            }

            if (success)
            {
                debugBatteryInfo = source;
                // Normalizing: some devices return negative for discharge
                currentMA = Mathf.Abs(microAmp) / 1000f;
                
                // Heuristic Correction: 
                // Some files return mA directly instead of uA.
                // If value is < 10 (e.g. 3.5), it might be Amps? Unlikely.
                // If value is < 10000 (e.g. 800), it might be mA.
                // If value is > 10000 (e.g. 800000), it is uA.
                
                // If we calculated < 10mA, it's suspiciously low, maybe the raw value was already mA?
                // Example: raw 800 -> /1000 = 0.8mA (Wrong) -> should be 800mA
                if (currentMA < 10f && microAmp > 100)
                {
                    currentMA = microAmp; // Treat raw as mA
                    debugBatteryInfo += "-Raw";
                }

                // Moving Average Calculation
                maHistory.Enqueue(currentMA);
                if (maHistory.Count > historySize)
                {
                    maHistory.Dequeue();
                }
                
                float sum = 0f;
                foreach(var v in maHistory) sum += v;
                averageMA = sum / maHistory.Count;
                
                currentWatts = (averageMA / 1000f) * 3.8f;
            }
            else
            {
                if(string.IsNullOrEmpty(debugBatteryInfo) || !debugBatteryInfo.StartsWith("Fail")) 
                    debugBatteryInfo = "Fail: No Data";
            }
        }
    }

    bool ReadSystemFile(string path, out long result)
    {
        result = 0;
        try
        {
            if (System.IO.File.Exists(path))
            {
                string text = System.IO.File.ReadAllText(path);
                if (long.TryParse(text.Trim(), out long val))
                {
                    result = val;
                    return true;
                }
            }
        }
        catch {}
        return false;
    }

    void OnGUI()
    {
        if (!show)
        {
            return;
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = fontSize;
            style.normal.textColor = textColor;
        }

        if (bgTexture == null)
        {
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, backgroundColor);
            bgTexture.Apply();
        }

        if (rect.width <= 0f)
        {
            rect = new Rect(10f, 10f, 520f, 420f); // Increased height for Sim Toggle
        }

        GUI.DrawTexture(rect, bgTexture);

        float batteryPercent = SystemInfo.batteryLevel >= 0f ? SystemInfo.batteryLevel * 100f : -1f;
        string batteryStr = batteryPercent >= 0f ? batteryPercent.ToString("F0") + "%" : "N/A";
        string drainStr = drainPerMinute > 0f ? drainPerMinute.ToString("F2") + "%/min" : "-";
        string statusStr = SystemInfo.batteryStatus.ToString();
        
        string powerStr = "";
        string deltaStr = "";

        if (isCalibrating)
        {
             powerStr = "\n" + calibrationStatus;
             deltaStr = "";
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            if (currentMA > 1)
            {
                // Show Average as main metric, Instant as secondary
                powerStr = string.Format("\nAvg: {0:F0} mA (Inst: {1:F0})\nPower: ~{2:F2} W", averageMA, currentMA, currentWatts);
                
                if (hasBaseline)
                {
                    // Delta based on Average to be stable
                    float deltaMA = averageMA - baselineMA;
                    float deltaWatts = (deltaMA / 1000f) * 3.8f;
                    string sign = deltaMA >= 0 ? "+" : "";
                    deltaStr = string.Format("\nDelta (Avg): {0}{1:F0} mA (~{2:F2} W)", sign, deltaMA, deltaWatts);
                }
                else
                {
                    deltaStr = "\nDelta: N/A (Calibrate)";
                }
            }
            else
            {
                powerStr = string.Format("\nCurrent: N/A ({0})", debugBatteryInfo);
            }
        }
        else
        {
            powerStr = "\nCurrent: N/A (Not Android)";
        }

        string text = string.Format(
            "FPS: {0:F1}\nFrame: {1:F1} ms\nBattery: {2} ({3})\nStatus: {4}{5}{6}",
            fps,
            frameTimeMs,
            batteryStr,
            drainStr,
            statusStr,
            powerStr,
            deltaStr
        );

        GUI.Label(rect, text, style);

        // Calibration Button
        if (Application.platform == RuntimePlatform.Android && currentMA > 1)
        {
            float btnHeight = 50f;
            float margin = 10f;
            float startY = rect.y + rect.height - btnHeight - margin;

            if (isCalibrating)
            {
                // Disable input during calibration
                return;
            }
            
            // 1. Auto Calibrate Button (Replaces manual Set Baseline)
            string baseBtnText = hasBaseline ? string.Format("Recalibrate (Base: {0:F0} mA)", baselineMA) : "Auto Calibrate Fluid Power";
            if (GUI.Button(new Rect(rect.x + margin, startY, rect.width - margin * 2, btnHeight), baseBtnText))
            {
                StartCoroutine(AutoCalibrateRoutine());
            }

            // 2. Manual Toggle (Optional)
            if (fluidSim != null)
            {
                string simText = fluidSim.runSimulation ? "Stop Simulation" : "Start Simulation";
                if (GUI.Button(new Rect(rect.x + margin, startY - btnHeight - margin, rect.width - margin * 2, btnHeight), simText))
                {
                    fluidSim.runSimulation = !fluidSim.runSimulation;
                }
            }
        }
    }

    System.Collections.IEnumerator AutoCalibrateRoutine()
    {
        isCalibrating = true;
        hasBaseline = false;
        
        // Step 1: Stop Fluid (Sim AND Rendering)
        if (fluidSim != null) 
        {
            fluidSim.runSimulation = false;
            fluidSim.enableRendering = false;
        }
        calibrationStatus = "Calibrating: Stopping Fluid...";
        yield return new WaitForSeconds(1.0f);

        // Step 2: Wait for power to stabilize (4 seconds)
        for(int i=4; i>0; i--)
        {
            calibrationStatus = string.Format("Calibrating: Stabilizing... {0}s", i);
            yield return new WaitForSeconds(1.0f);
        }

        // Step 3: Sample Baseline (Average over 3 seconds)
        calibrationStatus = "Calibrating: Sampling Baseline...";
        float sampleSum = 0f;
        int sampleCount = 0;
        float sampleDuration = 3.0f;
        float elapsed = 0f;

        while(elapsed < sampleDuration)
        {
            sampleSum += currentMA;
            sampleCount++;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (sampleCount > 0)
        {
            baselineMA = sampleSum / sampleCount;
            hasBaseline = true;
        }

        // Step 4: Restart Fluid
        calibrationStatus = "Calibrating: Restarting Fluid...";
        if (fluidSim != null) 
        {
            fluidSim.runSimulation = true;
            fluidSim.enableRendering = true;
        }
        yield return new WaitForSeconds(1.0f); // Give it a moment to ramp up

        isCalibrating = false;
    }
}
