using UnityEngine;

public class ObservationBox : MonoBehaviour
{
    public SPHStandardMobile fluid;
    [Tooltip("与 MPM 共用场景时赋值；墙体与边界同步会跟随当前启用的流体组件。")]
    public MPMFluidMobile mpmFluid;
    public float thickness = 0.2f;
    public float margin = 0f;
    public Material wallMaterial;
    public bool generateOnStart = true;
    public bool addBottom = true;
    public bool addFront = false;
    public bool syncBoundsFromWalls = false;
    public float bottomExtra = 0.0f;
    public Material frontMaterial;
    Transform wallsRoot;
    Transform leftWall;
    Transform rightWall;
    Transform backWall;
    Transform bottomWall;
    Transform frontWall;
    
    float Padding()
    {
        if (TryGetActiveFluid(out var sph, out var mpm))
        {
            if (mpm != null)
                return margin + mpm.particleSize * 0.5f;
            if (sph != null)
            {
                float drawSize = Mathf.Max(sph.particleSize, sph.neighbourRadius * 0.9f);
                return margin + drawSize * 0.5f;
            }
        }
        return margin;
    }

    bool TryGetActiveFluid(out SPHStandardMobile sph, out MPMFluidMobile mpm)
    {
        sph = fluid;
        mpm = mpmFluid;
        if (mpm != null && mpm.enabled && (sph == null || !sph.enabled)) { sph = null; return mpm != null; }
        if (sph != null && sph.enabled) { mpm = null; return true; }
        if (mpm != null) { sph = null; return true; }
        if (sph != null) { mpm = null; return true; }
        return false;
    }
    
    public void SyncBoundsFromWalls()
    {
        if (!syncBoundsFromWalls) return;
        if (!TryGetActiveFluid(out var sph, out var mpm)) return;
        if (leftWall == null || rightWall == null || backWall == null) return;
        var pad = Padding();
        var minX = leftWall.position.x + thickness * 0.5f + pad;
        var maxX = rightWall.position.x - thickness * 0.5f - pad;
        var minZ = backWall.position.z + thickness * 0.5f + pad;
        float refMinY = mpm != null ? mpm.boundsMin.y : (sph != null ? sph.boundsMin.y : 0f);
        float refMaxY = mpm != null ? mpm.boundsMax.y : (sph != null ? sph.boundsMax.y : 0f);
        var minY = bottomWall != null ? bottomWall.position.y + thickness * 0.5f + pad : refMinY;
        var bmin = new Vector3(minX, minY, minZ);
        var bmax = new Vector3(maxX, refMaxY, mpm != null ? mpm.boundsMax.z : sph.boundsMax.z);
        if (mpm != null) { mpm.boundsMin = bmin; mpm.boundsMax = bmax; }
        if (sph != null) { sph.boundsMin = bmin; sph.boundsMax = bmax; }
    }

    void Start()
    {
        if (generateOnStart) GenerateWalls();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                GenerateWalls();
            };
        }
    }
#endif

    void GenerateWalls()
    {
        if (fluid == null) fluid = GetComponent<SPHStandardMobile>();
        if (mpmFluid == null) mpmFluid = GetComponent<MPMFluidMobile>();
        if (!TryGetActiveFluid(out var sph, out var mpm)) return;
        var min = mpm != null ? mpm.boundsMin : sph.boundsMin;
        var max = mpm != null ? mpm.boundsMax : sph.boundsMax;
        if (wallsRoot == null)
        {
            var existing = transform.Find("ObservationBoxWalls");
            wallsRoot = existing != null ? existing : new GameObject("ObservationBoxWalls").transform;
            wallsRoot.SetParent(transform, false);
        }
        var centerY = (min.y + max.y) * 0.5f;
        var centerZ = (min.z + max.z) * 0.5f;
        var centerX = (min.x + max.x) * 0.5f;
        var height = Mathf.Max(max.y - min.y, 0.001f) + margin * 2f;
        var depth = Mathf.Max(max.z - min.z, 0.001f) + margin * 2f;
        var width = Mathf.Max(max.x - min.x, 0.001f) + margin * 2f;
        var pad = Padding();
        leftWall = CreateOrUpdateWall("WallLeft",
            new Vector3(min.x - thickness * 0.5f - pad, centerY, centerZ),
            new Vector3(thickness, height + pad * 2f, depth + pad * 2f));
        rightWall = CreateOrUpdateWall("WallRight",
            new Vector3(max.x + thickness * 0.5f + pad, centerY, centerZ),
            new Vector3(thickness, height + pad * 2f, depth + pad * 2f));
        backWall = CreateOrUpdateWall("WallBack",
            new Vector3(centerX, centerY, min.z - thickness * 0.5f - pad),
            new Vector3(width + thickness * 2f + pad * 2f, height + pad * 2f, thickness));
        if (addBottom)
        {
            bottomWall = CreateOrUpdateWall("WallBottom",
                new Vector3(centerX, min.y - thickness * 0.5f - pad - bottomExtra, centerZ),
                new Vector3(width + thickness * 2f + pad * 2f, thickness, depth + pad * 2f));
        }
        if (addFront)
        {
            var mat = frontMaterial != null ? frontMaterial : CreateDefaultTransparentMaterial();
            frontWall = CreateOrUpdateWall("WallFront",
                new Vector3(centerX, centerY, max.z + thickness * 0.5f + pad),
                new Vector3(width + thickness * 2f + pad * 2f, height + pad * 2f, thickness),
                mat);
        }
    }

    Transform CreateOrUpdateWall(string name, Vector3 position, Vector3 size)
    {
        var t = wallsRoot != null ? wallsRoot.Find(name) : null;
        if (t == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            t = go.transform;
            t.SetParent(wallsRoot, false);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && wallMaterial != null) mr.sharedMaterial = wallMaterial;
        }
        t.position = position;
        t.localScale = size;
        return t;
    }

    Transform CreateOrUpdateWall(string name, Vector3 position, Vector3 size, Material materialOverride)
    {
        var t = wallsRoot != null ? wallsRoot.Find(name) : null;
        if (t == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            t = go.transform;
            t.SetParent(wallsRoot, false);
        }
        var mr = t.GetComponent<MeshRenderer>();
        if (mr != null && materialOverride != null) mr.sharedMaterial = materialOverride;
        t.position = position;
        t.localScale = size;
        return t;
    }

    Material CreateDefaultTransparentMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s != null)
        {
            var m = new Material(s);
            m.SetFloat("_Surface", 1f);
            var baseColor = m.GetColor("_BaseColor");
            baseColor.a = 0.2f;
            m.SetColor("_BaseColor", baseColor);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }
        s = Shader.Find("Standard");
        var mat = new Material(s);
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        var c = mat.color;
        c.a = 0.2f;
        mat.color = c;
        return mat;
    }

    void Update()
    {
        if (!syncBoundsFromWalls) GenerateWalls();
        SyncBoundsFromWalls();
    }
}
