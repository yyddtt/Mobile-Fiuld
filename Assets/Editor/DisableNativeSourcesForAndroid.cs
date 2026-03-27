using UnityEditor;
using UnityEngine;
using System;

public static class DisableNativeSourcesForAndroid
{
    [MenuItem("Tools/Plugins/Disable Native Sources For Runtime")]
    public static void DisableAll()
    {
        var root = "Assets";
        var guids = AssetDatabase.FindAssets("", new[] { root });
        int changed = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsNativeSourceOrLib(path)) continue;

            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null) continue;

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);
            foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
            {
                importer.SetCompatibleWithPlatform(target, false);
            }
            importer.SaveAndReimport();
            changed++;
        }
        EditorUtility.DisplayDialog("Done", $"Disabled {changed} native source(s) for runtime platforms.", "OK");
    }

    static bool IsNativeSourceOrLib(string path)
    {
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        switch (ext)
        {
            case ".cpp":
            case ".c":
            case ".mm":
            case ".m":
            case ".h":
            case ".hpp":
                return true;
            default:
                return false;
        }
    }
}
