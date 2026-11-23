#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResearchTreeEditorTools
{
    [MenuItem("RTS/Progression/Configure Research Tree UI")]
    public static void ConfigureCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Configure the research tree outside Play Mode.");
        var root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        if (root == null) throw new InvalidOperationException("Open ProceduralGame with its scene composition root first.");
        Configure(root.GetComponent<GameManager>(), root);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
    }

    public static void Configure(GameManager game, GameSceneCompositionRoot root)
    {
        var data = new SerializedObject(game);
        var hud = data.FindProperty("m_HUDCanvas").objectReferenceValue as GameObject;
        if (hud == null) throw new InvalidOperationException("Research tree requires the existing HUD.");
        var legacy = hud.transform.Find("ResearchPanel");
        if (legacy != null) legacy.gameObject.SetActive(false);
        var existing = hud.transform.Find("ResearchTree");
        var treeObject = existing != null ? existing.gameObject : new GameObject("ResearchTree", typeof(RectTransform));
        treeObject.transform.SetParent(hud.transform, false);
        var rect = (RectTransform)treeObject.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var tree = treeObject.GetComponent<ResearchTreePanel>();
        if (tree == null) tree = treeObject.AddComponent<ResearchTreePanel>();
        var treeData = new SerializedObject(tree);
        const string art = "Assets/Game/Images/Tiny Swords/";
        var hallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Buildings/TownHall.prefab");
        if (hallPrefab == null) throw new InvalidOperationException("Missing TownHall prefab for the root icon.");
        var hallData = new SerializedObject(hallPrefab.GetComponent<TownHall>());
        treeData.FindProperty("m_RootIcon").objectReferenceValue = hallData.FindProperty("m_UnitIcon").objectReferenceValue
            ?? hallPrefab.GetComponentInChildren<SpriteRenderer>().sprite;
        SetIcon(treeData, "m_WorkerIcon", art + "Units/Blue Units/Pawn/Pawn_Icon.png");
        SetIcon(treeData, "m_CargoIcon", "Assets/Resources/Icons/WoodIcon.png");
        SetIcon(treeData, "m_ArcherIcon", art + "Units/Blue Units/Archer/Archer_Icon.png");
        SetIcon(treeData, "m_TrainingIcon", art + "Units/Blue Units/Warrior/Warriror_Icon.png");
        SetIcon(treeData, "m_TowerIcon", art + "Buildings/Blue Buildings/Tower/TowerIcon.png");
        SetIcon(treeData, "m_FortifyIcon", art + "Buildings/Blue Buildings/Tower/TowerIcon.png");
        treeData.ApplyModifiedPropertiesWithoutUndo();
        var rootData = new SerializedObject(root);
        rootData.FindProperty("m_ResearchTree").objectReferenceValue = tree;
        rootData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(root); EditorUtility.SetDirty(tree);
    }
    static void SetIcon(SerializedObject target, string field, string path)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) throw new InvalidOperationException("Missing research icon: " + path);
        target.FindProperty(field).objectReferenceValue = sprite;
    }
}
#endif
