using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public static class EnforcePluginImportSettings
{
    [MenuItem("Tools/Plugins/Enforce Import Settings")]
    public static void EnforceAll()
    {
        var guids = AssetDatabase.FindAssets("t:PluginImporter", new[] { "Assets" });
        int changed = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsEditorPlugin(path)) continue;
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null) continue;
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);
            foreach (BuildTarget t in Enum.GetValues(typeof(BuildTarget))) importer.SetCompatibleWithPlatform(t, false);
            importer.SaveAndReimport();
            changed++;
        }
        if (changed > 0) EditorUtility.DisplayDialog("Plugins", $"Updated {changed} native plugin(s).", "OK");
    }

    static bool IsEditorPlugin(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var isEditorPath = path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("\\Editor\\", StringComparison.OrdinalIgnoreCase) >= 0;
        switch (ext)
        {
            case ".a":
            case ".so":
            case ".dll":
            case ".lib":
            case ".aar":
                return isEditorPath;
            default:
                return false;
        }
    }
}
