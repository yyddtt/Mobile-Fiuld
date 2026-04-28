using UnityEngine;

/// <summary>
///  SSFR/网格粒子在 MPM 与 SPH 中共享的网格与材质创建；避免两脚本重复维护同一套 Shader.Find 逻辑。
/// </summary>
public static class MobileSsfRenderShared
{
    public sealed class SsfMaterials
    {
        public Material gridParticle;
        public Material depth;
        public Material debugDepth;
        public Material blur;
        public Material gaussian;
        public Material thickness;
        public Material thicknessBlur;
        public Material debugThickness;
        public Material normal;
        public Material debugNormal;
        public Material composite;
    }

    public static RenderTextureFormat SelectSingleChannelFloatFormat()
    {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf)) return RenderTextureFormat.RHalf;
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat)) return RenderTextureFormat.RFloat;
        return RenderTextureFormat.R8;
    }

    public static Mesh CreateParticleSphereMesh()
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

    public static SsfMaterials CreateSsfMaterials(float particleSize, ComputeBuffer particlesBuffer)
    {
        var m = new SsfMaterials();

        var gph = Shader.Find("Instanced/GridParticleMobile");
        if (gph != null)
        {
            m.gridParticle = new Material(gph);
            m.gridParticle.enableInstancing = true;
            m.gridParticle.SetFloat("_size", particleSize);
        }

        var dph = Shader.Find("Instanced/GridParticleDepth");
        if (dph != null)
        {
            m.depth = new Material(dph);
            m.depth.enableInstancing = true;
        }

        var debugShader = Shader.Find("Fluid/DebugDepth");
        if (debugShader != null) m.debugDepth = new Material(debugShader);

        var blurShader = Shader.Find("SSFR/DepthBilateral");
        if (blurShader != null) m.blur = new Material(blurShader);

        var gaussShader = Shader.Find("SSFR/DepthGaussianSmart");
        if (gaussShader != null) m.gaussian = new Material(gaussShader);

        var thShader = Shader.Find("Instanced/GridParticleThickness");
        if (thShader != null)
        {
            m.thickness = new Material(thShader);
            m.thickness.enableInstancing = true;
        }

        var thBlurShader = Shader.Find("SSFR/ThicknessBlur");
        if (thBlurShader != null) m.thicknessBlur = new Material(thBlurShader);

        var debugThShader = Shader.Find("Fluid/DebugThickness");
        if (debugThShader != null) m.debugThickness = new Material(debugThShader);

        var normShader = Shader.Find("SSFR/FluidNormals");
        if (normShader != null) m.normal = new Material(normShader);

        var debugNormShader = Shader.Find("Fluid/DebugNormal");
        if (debugNormShader != null) m.debugNormal = new Material(debugNormShader);

        var compShader = Shader.Find("SSFR/FluidComposite");
        if (compShader != null) m.composite = new Material(compShader);

        if (m.depth != null) m.depth.SetBuffer("_particlesBuffer", particlesBuffer);
        if (m.thickness != null) m.thickness.SetBuffer("_particlesBuffer", particlesBuffer);
        return m;
    }
}
