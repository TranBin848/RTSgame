using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RTS.SaveLoad;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public sealed class SaveContentCatalogEditor : IPreprocessBuildWithReport
{
    public const string CatalogPath = "Assets/Game/SaveLoad/Resources/ProceduralSaveCatalog.asset";
    const string ScenePath = "Assets/Scenes/ProceduralGame.unity";
    public int callbackOrder => 1000;
    static SaveContentCatalogEditor()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { RefreshFingerprint(); }
            catch (Exception error) { EditorApplication.isPlaying = false; Debug.LogError("Save catalog preflight failed: " + error.Message); }
        };
    }
    public void OnPreprocessBuild(BuildReport report) => RefreshFingerprint();
    [MenuItem("RTS/SaveLoad/Rebuild content catalog")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Build save catalog outside Play Mode.");
        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
        var catalog = AssetDatabase.LoadAssetAtPath<SaveContentCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SaveContentCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var definitions = new List<SaveContentCatalog.Definition>();
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x => x, StringComparer.Ordinal))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var capabilities = SaveContentCatalog.CapabilitiesFor(prefab);
            if (capabilities == 0) continue;
            string existingId = prefab.GetComponent<SaveDefinitionTag>()?.DefinitionId;
            string id = !string.IsNullOrEmpty(existingId) ? existingId : Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!SaveCatalog.ValidId(id)) throw new InvalidOperationException("Assign an ASCII definition ID for " + path);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var tag = contents.GetComponent<SaveDefinitionTag>();
                if (tag == null) tag = contents.AddComponent<SaveDefinitionTag>();
                if (tag.DefinitionId != id)
                {
                    tag.SetAuthoringId(id);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            int visual = prefab.GetComponent<EnemyRangerUnit>() != null ? 1 : prefab.GetComponent<EnemyUnit>() != null ? 0
                : prefab.GetComponent<Projectile>() != null ? (id == "arrow" ? 2 : id == "gnoll_bone" ? 3 : -1) : -1;
            definitions.Add(new SaveContentCatalog.Definition { Id = id, Prefab = prefab, Capabilities = capabilities, NativeVisualId = visual });
        }
        catalog.Definitions = definitions.ToArray();
        catalog.Actions = AssetDatabase.FindAssets("t:ActionSO", new[] { "Assets/Game" })
            .OrderBy(x => x, StringComparer.Ordinal).Select(guid => new SaveContentCatalog.ActionDefinition
            { Id = "action_" + guid, Action = AssetDatabase.LoadAssetAtPath<ActionSO>(AssetDatabase.GUIDToAssetPath(guid)) }).ToArray();
        var research = AssetDatabase.FindAssets("t:EconomyResearchDefinition", new[] { "Assets/Game" });
        if (research.Length != 1) throw new InvalidOperationException("Select the one ProceduralGame research definition explicitly.");
        catalog.Research = AssetDatabase.LoadAssetAtPath<EconomyResearchDefinition>(AssetDatabase.GUIDToAssetPath(research[0]));
        var clocks = AssetDatabase.FindAssets("t:DayNightCycleDefinition", new[] { "Assets/Game" });
        if (clocks.Length != 1) throw new InvalidOperationException("Select the one ProceduralGame clock definition explicitly.");
        catalog.Clock = AssetDatabase.LoadAssetAtPath<DayNightCycleDefinition>(AssetDatabase.GUIDToAssetPath(clocks[0]));
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        RefreshFingerprint();
    }
    public static string ComputeFingerprint()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<SaveContentCatalog>(CatalogPath);
        if (catalog == null) throw new InvalidOperationException("Run RTS/SaveLoad/Rebuild content catalog first.");
        var roots = catalog.Definitions.Select(x => AssetDatabase.GetAssetPath(x.Prefab))
            .Concat(catalog.Actions.Select(x => AssetDatabase.GetAssetPath(x.Action)))
            .Concat(new[] { ScenePath, AssetDatabase.GetAssetPath(catalog.Research), AssetDatabase.GetAssetPath(catalog.Clock) }).ToArray();
        var paths = new HashSet<string>(AssetDatabase.GetDependencies(roots, true), StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles("Assets/Game", "*", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                && (x.Replace('\\', '/').Contains("/Resources/") || x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || x.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))))
            paths.Add(path.Replace('\\', '/'));
        // Resource-loaded tactical/presentation assets are not necessarily scene dependencies.
        foreach (string dependency in AssetDatabase.GetDependencies(paths.Where(x => x.StartsWith("Assets/", StringComparison.Ordinal)).ToArray(), true)) paths.Add(dependency);
        foreach (string path in new[] { "Packages/manifest.json", "Packages/packages-lock.json", "ProjectSettings/ProjectVersion.txt",
            "ProjectSettings/ProjectSettings.asset", "ProjectSettings/GraphicsSettings.asset", "ProjectSettings/QualitySettings.asset", "ProjectSettings/TimeManager.asset", "ProjectSettings/TagManager.asset" }) paths.Add(path);
        paths.Remove(CatalogPath);
        var manifest = new StringBuilder("RTS.ProceduralSaveContent/1\n");
        foreach (string path in paths.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!File.Exists(path)) continue;
            manifest.Append(path).Append(':').Append(SaveCatalog.Hash(File.ReadAllBytes(path))).Append('\n');
            if (File.Exists(path + ".meta")) manifest.Append(SaveCatalog.Hash(File.ReadAllBytes(path + ".meta"))).Append('\n');
        }
        // Catalog values themselves participate; exclude only its baked hash to avoid self-reference.
        foreach (var item in catalog.Definitions.OrderBy(x => x.Id, StringComparer.Ordinal))
            manifest.Append(item.Id).Append(':').Append((int)item.Capabilities).Append(':').Append(item.NativeVisualId).Append(':').Append(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item.Prefab))).Append('\n');
        foreach (var item in catalog.Actions.OrderBy(x => x.Id, StringComparer.Ordinal))
            manifest.Append(item.Id).Append(':').Append(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item.Action))).Append('\n');
        return SaveCatalog.Hash(Encoding.UTF8.GetBytes(manifest.ToString()));
    }
    public static void RefreshFingerprint()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<SaveContentCatalog>(CatalogPath);
        if (catalog == null) throw new InvalidOperationException("Procedural save catalog is missing.");
        string hash = ComputeFingerprint();
        if (catalog.ContentHash != hash)
        { catalog.SetBakedHash(hash); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); }
        catalog.BuildRuntimeCatalog();
    }
}
