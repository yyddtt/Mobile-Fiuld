using UnityEngine;

public class ObservationBox : MonoBehaviour
{
    public SPHStandardMobile fluid;
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
        if (fluid == null) return margin;
        float drawSize = Mathf.Max(fluid.particleSize, fluid.neighbourRadius * 0.9f);
        return margin + drawSize * 0.5f;
    }
    
    public void SyncBoundsFromWalls()
    {
        if (!syncBoundsFromWalls || fluid == null) return;
        if (leftWall == null || rightWall == null || backWall == null) return;
        var pad = Padding();
        var minX = leftWall.position.x + thickness * 0.5f + pad;
        var maxX = rightWall.position.x - thickness * 0.5f - pad;
        var minZ = backWall.position.z + thickness * 0.5f + pad;
        var minY = bottomWall != null ? bottomWall.position.y + thickness * 0.5f + pad : fluid.boundsMin.y;
        var bmin = new Vector3(minX, minY, minZ);
        var bmax = new Vector3(maxX, fluid.boundsMax.y, fluid.boundsMax.z);
        fluid.boundsMin = bmin;
        fluid.boundsMax = bmax;
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
        if (fluid == null) return;
        if (wallsRoot == null)
        {
            var existing = transform.Find("ObservationBoxWalls");
            wallsRoot = existing != null ? existing : new GameObject("ObservationBoxWalls").transform;
            wallsRoot.SetParent(transform, false);
        }
        var min = fluid.boundsMin;
        var max = fluid.boundsMax;
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
