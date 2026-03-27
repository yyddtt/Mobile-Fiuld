using UnityEditor;
using UnityEngine;

public static class BuoyantBlockCreator
{
    [MenuItem("Tools/Fluid/Create Buoyant Block")]
    public static void CreateBuoyantBlock()
    {
        var fluid = Object.FindObjectOfType<SPHStandardMobile>();
        if (fluid == null)
        {
            EditorUtility.DisplayDialog("Create Buoyant Block", "No SPHStandardMobile found in scene.", "OK");
            return;
        }
        var existingBlocks = Object.FindObjectsOfType<BuoyantBlockFloat>();
        if (existingBlocks != null && existingBlocks.Length > 0)
        {
            Selection.activeGameObject = existingBlocks[0].gameObject;
            return;
        }
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BuoyantBlock";
        var bb = go.AddComponent<BuoyantBlockFloat>();
        bb.fluid = fluid;
        bb.size = new Vector3(0.8f, 0.5f, 0.8f);
        bb.density = 90f;
        bb.buoyancyCoeff = fluid.restDensity * 9.81f;
        bb.sampleStride = 3;
        bb.requireLocalPresence = true;
        bb.dragCd = 1.1f;
        bb.lateralDragScale = 1.6f;
        bb.flowSampleRadius = 1.6f;
        bb.buoyancyScale = 1.3f;
        Vector3 center = (fluid.boundsMin + fluid.boundsMax) * 0.5f;
        float waterY = (fluid.spawnMin.y + fluid.spawnMax.y) * 0.5f;
        float neutralY = waterY - bb.size.y * (bb.density / fluid.restDensity) + bb.size.y * 0.5f;
        float y = Mathf.Clamp(neutralY, fluid.boundsMin.y + bb.size.y * 0.5f + 0.02f, fluid.boundsMax.y - bb.size.y * 0.5f - 0.02f);
        go.transform.position = new Vector3(center.x, y, center.z);
        go.transform.localScale = bb.size;
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }
    
    [MenuItem("Tools/Fluid/Cleanup Buoyant Blocks")]
    public static void CleanupBuoyantBlocks()
    {
        var blocks = Object.FindObjectsOfType<BuoyantBlockFloat>();
        if (blocks == null || blocks.Length <= 1) return;
        var keep = blocks[0];
        for (int i = 1; i < blocks.Length; i++)
        {
            var go = blocks[i].gameObject;
            if (go != keep.gameObject)
            {
                Object.DestroyImmediate(go);
            }
        }
        Selection.activeGameObject = keep.gameObject;
        EditorGUIUtility.PingObject(keep.gameObject);
    }
}
