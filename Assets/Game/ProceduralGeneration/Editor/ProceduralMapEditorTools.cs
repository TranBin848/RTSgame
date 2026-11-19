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
    const string SettingsPath = "Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset";

    [MenuItem("RTS/Procedural Generation/Create or Refresh Gameplay Scene")]
    public static void CreateOrRefreshGameplayScene()
    {
        EnsureSettingsFolder();
        ProceduralMapSettings settings = AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ProceduralMapSettings>();
            var settingsData = new SerializedObject(settings);
            settingsData.FindProperty("m_Preset").enumValueIndex = (int)RTS.ProceduralGeneration.MapLayoutPreset.StrategicRegions;
            settingsData.ApplyModifiedPropertiesWithoutUndo();
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
        var legacySpawners = Object.FindObjectsByType<ResourceSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        var spawnerReferences = serialized.FindProperty("m_LegacySpawners");
        spawnerReferences.arraySize = legacySpawners.Length;
        for (int i = 0; i < legacySpawners.Length; i++) spawnerReferences.GetArrayElementAtIndex(i).objectReferenceValue = legacySpawners[i];
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

        var manager = Object.FindFirstObjectByType<GameManager>();
        if (manager == null) throw new System.InvalidOperationException("Procedural scene requires GameManager.");
        var composition = manager.GetComponent<GameSceneCompositionRoot>();
        if (composition == null) composition = manager.gameObject.AddComponent<GameSceneCompositionRoot>();
        var compositionData = new SerializedObject(composition);
        const string researchPath = "Assets/Game/DOTS/EconomyTools.asset";
        var research = AssetDatabase.LoadAssetAtPath<EconomyResearchDefinition>(researchPath);
        if (research == null)
        {
            research = ScriptableObject.CreateInstance<EconomyResearchDefinition>();
            AssetDatabase.CreateAsset(research, researchPath);
        }
        compositionData.FindProperty("m_ResearchDefinition").objectReferenceValue = research;
        compositionData.FindProperty("m_Game").objectReferenceValue = manager;
        compositionData.FindProperty("m_Camera").objectReferenceValue = Object.FindFirstObjectByType<CameraController>();
        compositionData.FindProperty("m_Map").objectReferenceValue = map;
        compositionData.FindProperty("m_Generator").objectReferenceValue = authoring;
        compositionData.FindProperty("m_Fog").objectReferenceValue = Object.FindFirstObjectByType<FogOfWarManager>();
        compositionData.FindProperty("m_DayNight").objectReferenceValue = Object.FindFirstObjectByType<DayNightCycleManager>();
        compositionData.FindProperty("m_Waves").objectReferenceValue = Object.FindFirstObjectByType<EnemyWaveManager>();
        compositionData.ApplyModifiedPropertiesWithoutUndo();
        ConfigureResearchPanel(manager, composition);
        ConfigureMapPresentation(composition,authoring);
        MatchSessionEditorTools.Configure(composition);

        foreach (var spawner in legacySpawners) spawner.enabled = true; // Authoring suppresses only these configured spawners.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScene);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"[ProceduralMap] Scene created: {TargetScene}. Open it and press Play to generate the configured seed.");
    }

    public static void ConfigureResearchPanel(GameManager manager, GameSceneCompositionRoot composition)
    {
        var gameData = new SerializedObject(manager);
        var hud = gameData.FindProperty("m_HUDCanvas").objectReferenceValue as GameObject;
        if (hud == null) throw new System.InvalidOperationException("Research UI requires the gameplay HUD.");
        var ids = new[] { "economy_tools", "economy_cargo", "military_archery", "military_training", "expansion_tower", "expansion_fortify" };
        ResearchPanel first = null;
        for (int index = 0; index < ids.Length; index++)
        {
        Transform parent = index == 0 ? hud.transform : first.transform;
        string nodeName = index == 0 ? "ResearchPanel" : ids[index];
        var existing = parent.Find(nodeName);
        GameObject panel = existing != null ? existing.gameObject : new GameObject(nodeName,
            typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(ResearchPanel));
        panel.transform.SetParent(parent, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = index == 0 ? new Vector2(0.5f, 1) : new Vector2(0, 1);
        rect.pivot = new Vector2(index == 0 ? 0.5f : 0, 1);
        rect.anchoredPosition = index == 0 ? new Vector2(-310, -75) : new Vector2((index / 2) * 310, -(index % 2) * 105);
        rect.sizeDelta = new Vector2(300, 100);
        panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.12f, 0.16f, 0.95f);
        var textChild = panel.transform.Find("Label");
        var labelObject = textChild != null ? textChild.gameObject : new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        labelObject.transform.SetParent(panel.transform, false);
        var labelRect = (RectTransform)labelObject.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8, 8); labelRect.offsetMax = new Vector2(-8, -8);
        var label = labelObject.GetComponent<UnityEngine.UI.Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 13; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
        label.text = "Research tools — select TownHall";
        var data = new SerializedObject(panel.GetComponent<ResearchPanel>());
        data.FindProperty("m_Button").objectReferenceValue = panel.GetComponent<UnityEngine.UI.Button>();
        data.FindProperty("m_TechnologyId").stringValue = ids[index];
        data.FindProperty("m_Label").objectReferenceValue = label; data.ApplyModifiedPropertiesWithoutUndo();
        if (index == 0) first = panel.GetComponent<ResearchPanel>();
        }
        var rootData = new SerializedObject(composition); rootData.FindProperty("m_ResearchPanel").objectReferenceValue = first;
        rootData.ApplyModifiedPropertiesWithoutUndo();
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

    public static void ConfigureMapPresentation(GameSceneCompositionRoot root,ProceduralMapAuthoring authoring)
    {
        var presentation=authoring.GetComponent<ProceduralMapPresentation>();
        if(presentation==null)presentation=authoring.gameObject.AddComponent<ProceduralMapPresentation>();
        var data=new SerializedObject(presentation);
        const string art="Assets/Game/Images/Tiny Swords/";
        data.FindProperty("m_BushTexture").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"Terrain/Decorations/Bushes/Bushe1.png");
        data.FindProperty("m_RockTexture").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"Terrain/Decorations/Rocks/Rock1.png");
        data.FindProperty("m_WatchpostTexture").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"Buildings/Black Buildings/Tower.png");
        data.FindProperty("m_WorldMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/TerrainObjects/Tree.prefab").GetComponent<SpriteRenderer>().sharedMaterial;
        data.ApplyModifiedPropertiesWithoutUndo();
        var refs=new SerializedObject(root);
        refs.FindProperty("m_MapPresentation").objectReferenceValue=presentation;
        refs.FindProperty("m_Minimap").objectReferenceValue=Object.FindFirstObjectByType<MinimapFogUI>();
        refs.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(presentation);EditorUtility.SetDirty(root);
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
