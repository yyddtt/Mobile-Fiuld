using UnityEngine;
using UnityEngine.Rendering;

public class AutoLighting : MonoBehaviour
{
    public SPHStandardMobile fluid;
    public Light directionalLight;
    public Light spotLight;
    public ReflectionProbe reflectionProbe;
    public bool autoCreateIfMissing = true;
    public Vector3 directionalEuler = new Vector3(50f, -30f, 0f);
    public float spotOffsetYFactor = 0.3f;
    public float spotOffsetZFactor = 0.6f;
    public float spotAngle = 70f;
    public float spotRange = 40f;
    public float probeMargin = 0.5f;
    public bool probeRealtime = true;
    public bool probeBoxProjection = true;
    public float directionalIntensity = 1.0f;
    public float spotIntensity = 4.0f;
    public float probeIntensity = 1.0f;
    public bool enableDirectLights = false;
    Transform root;

    void Start()
    {
        EnsureReferences();
        ApplyLayout();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                EnsureReferences();
                ApplyLayout();
            };
        }
    }
#endif

    void Update()
    {
        ApplyLayout();
    }

    void EnsureReferences()
    {
        if (fluid == null) fluid = GetComponent<SPHStandardMobile>();
        if (root == null)
        {
            var existing = transform.Find("AutoLighting");
            root = existing != null ? existing : new GameObject("AutoLighting").transform;
            root.SetParent(transform, false);
        }
        if (autoCreateIfMissing)
        {
            if (directionalLight == null)
            {
                var t = root.Find("Directional Light Auto");
                if (t == null)
                {
                    var go = new GameObject("Directional Light Auto");
                    t = go.transform;
                    t.SetParent(root, false);
                    directionalLight = go.AddComponent<Light>();
                    directionalLight.type = LightType.Directional;
                }
                else
                {
                    directionalLight = t.GetComponent<Light>();
                    if (directionalLight == null) directionalLight = t.gameObject.AddComponent<Light>();
                    directionalLight.type = LightType.Directional;
                }
            }
            if (spotLight == null)
            {
                var t = root.Find("Fake Spot Light Auto");
                if (t == null)
                {
                    var go = new GameObject("Fake Spot Light Auto");
                    t = go.transform;
                    t.SetParent(root, false);
                    spotLight = go.AddComponent<Light>();
                    spotLight.type = LightType.Spot;
                }
                else
                {
                    spotLight = t.GetComponent<Light>();
                    if (spotLight == null) spotLight = t.gameObject.AddComponent<Light>();
                    spotLight.type = LightType.Spot;
                }
            }
            if (reflectionProbe == null)
            {
                var t = root.Find("Reflection Probe Auto");
                if (t == null)
                {
                    var go = new GameObject("Reflection Probe Auto");
                    t = go.transform;
                    t.SetParent(root, false);
                    reflectionProbe = go.AddComponent<ReflectionProbe>();
                }
                else
                {
                    reflectionProbe = t.GetComponent<ReflectionProbe>();
                    if (reflectionProbe == null) reflectionProbe = t.gameObject.AddComponent<ReflectionProbe>();
                }
            }
        }
    }

    void ApplyLayout()
    {
        if (fluid == null) return;
        var min = fluid.boundsMin;
        var max = fluid.boundsMax;
        var center = (min + max) * 0.5f;
        var size = new Vector3(Mathf.Max(max.x - min.x, 0.001f), Mathf.Max(max.y - min.y, 0.001f), Mathf.Max(max.z - min.z, 0.001f));
        if (directionalLight != null)
        {
            directionalLight.transform.position = center + Vector3.up * size.y;
            directionalLight.transform.rotation = Quaternion.Euler(directionalEuler);
            directionalLight.intensity = enableDirectLights ? directionalIntensity : 0f;
            directionalLight.enabled = enableDirectLights;
            directionalLight.shadows = enableDirectLights ? LightShadows.Soft : LightShadows.None;
        }
        if (spotLight != null)
        {
            var pos = new Vector3(center.x, center.y + size.y * spotOffsetYFactor, center.z + size.z * spotOffsetZFactor);
            spotLight.transform.position = pos;
            var dir = center - pos;
            if (dir.sqrMagnitude > 1e-6f) spotLight.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            spotLight.spotAngle = spotAngle;
            spotLight.range = Mathf.Max(spotRange, size.magnitude);
            spotLight.intensity = enableDirectLights ? spotIntensity : 0f;
            spotLight.enabled = enableDirectLights;
            spotLight.shadows = enableDirectLights ? LightShadows.Soft : LightShadows.None;
        }
        if (reflectionProbe != null)
        {
            reflectionProbe.transform.position = center;
            reflectionProbe.refreshMode = probeRealtime ? ReflectionProbeRefreshMode.EveryFrame : ReflectionProbeRefreshMode.ViaScripting;
            reflectionProbe.boxProjection = probeBoxProjection;
            reflectionProbe.size = size + Vector3.one * (probeMargin * 2f);
            reflectionProbe.intensity = probeIntensity;
        }
    }
}
