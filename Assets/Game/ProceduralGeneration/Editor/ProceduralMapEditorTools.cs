#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProceduralMapEditorTools
{
    const string SourceScene = "Assets/Scenes/SampleScene.unity";
    const string TargetScene = "Assets/Scenes/ProceduralGame.unity";
    const string SettingsPath = "Assets/Game/ProceduralGeneration/Settings/DefaultProceduralMapSettings.asset";

    [MenuItem("RTS/Procedural Generation/Create or Refresh Gameplay Scene")]
    public static void CreateOrRefreshGameplayScene()
    {
        EnsureSettingsFolder();
        ProceduralMapSettings settings = AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ProceduralMapSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SettingsPath, ImportAssetOptions.ForceUpdate);
            settings = AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>(SettingsPath);
        }
        if (settings == null)
            throw new System.InvalidOperationException($"Cannot create or load map settings at {SettingsPath}.");

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene))
        {
            if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
                throw new System.InvalidOperationException($"Cannot copy {SourceScene} to {TargetScene}.");
        }

        Scene scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        TilemapManager map = Object.FindFirstObjectByType<TilemapManager>();
        TownHall townHall = Object.FindFirstObjectByType<TownHall>();
        UnityEngine.Tilemaps.Tilemap foamTilemap = GameObject.Find("Foam")?.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        ResourceSpawner legacySpawner = Object.FindFirstObjectByType<ResourceSpawner>(FindObjectsInactive.Include);
        if (map == null || townHall == null || foamTilemap == null)
            throw new System.InvalidOperationException("Procedural scene requires the TilemapManager, TownHall and Foam Tilemap from SampleScene.");

        ProceduralMapAuthoring authoring = Object.FindFirstObjectByType<ProceduralMapAuthoring>(FindObjectsInactive.Include);
        if (authoring == null)
        {
            var gameObject = new GameObject("ProceduralMapAuthoring");
            authoring = gameObject.AddComponent<ProceduralMapAuthoring>();
        }

        var serialized = new SerializedObject(authoring);
        serialized.FindProperty("m_Settings").objectReferenceValue = settings;
        serialized.FindProperty("m_TilemapManager").objectReferenceValue = map;
        serialized.FindProperty("m_MapCenter").objectReferenceValue = townHall.transform;
        serialized.FindProperty("m_GrassTile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>("Assets/Game/Tilemaps/RuleTile.asset");
        serialized.FindProperty("m_WaterTile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>("Assets/Game/Tilemaps/Tiles/Water Background color.asset");
        serialized.FindProperty("m_FoamTilemap").objectReferenceValue = foamTilemap;
        serialized.FindProperty("m_FoamTile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>("Assets/Game/Tilemaps/TilesAnimated/Foam.asset");
        serialized.FindProperty("m_TreePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/TerrainObjects/Tree.prefab");
        serialized.FindProperty("m_SheepPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/TerrainObjects/Sheep/Sheep.prefab");

        SerializedProperty gold = serialized.FindProperty("m_GoldPrefabs");
        gold.arraySize = 6;
        for (int index = 0; index < 6; index++)
            gold.GetArrayElementAtIndex(index).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Game/Prefabs/TerrainObjects/GoldStone/GoldStone{index + 1}.prefab");

        Transform resourceRoot = GameObject.Find("Procedural Resources")?.transform;
        if (resourceRoot == null)
        {
            var root = new GameObject("Procedural Resources");
            root.transform.SetParent(authoring.transform, false);
            resourceRoot = root.transform;
        }
        serialized.FindProperty("m_GeneratedResourceRoot").objectReferenceValue = resourceRoot;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(authoring);

        if (legacySpawner != null) legacySpawner.enabled = true; // Authoring disables it before Awake only in this scene.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScene);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"[ProceduralMap] Scene created: {TargetScene}. Open it and press Play to generate the configured seed.");
    }

    [MenuItem("RTS/Procedural Generation/Open Gameplay Scene")]
    public static void OpenGameplayScene()
    {
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene)) CreateOrRefreshGameplayScene();
        else EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
    }

    static void EnsureSettingsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Game/ProceduralGeneration/Settings"))
            AssetDatabase.CreateFolder("Assets/Game/ProceduralGeneration", "Settings");
    }

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(scene => scene.path != TargetScene))
            scenes.Add(new EditorBuildSettingsScene(TargetScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
