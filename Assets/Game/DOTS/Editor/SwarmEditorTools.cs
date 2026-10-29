using System;
using System.IO;
using RTS.DOTS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SwarmEditorTools
{
    public const string DemoScene = "Assets/Scenes/SwarmSimulation.unity";
    public const string LibraryPath = "Assets/Game/DOTS/Resources/DOTS/SwarmVisualLibrary.asset";

    [MenuItem("RTS/DOTS/Generate swarm assets and scene")]
    public static void GenerateAssets()
    {
        // Do not discard unsaved work when invoked interactively.
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var library = AssetDatabase.LoadAssetAtPath<SwarmVisualLibrary>(LibraryPath);
        if (library == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath)); AssetDatabase.Refresh();
            library = ScriptableObject.CreateInstance<SwarmVisualLibrary>(); AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.Visuals = new[]
        {
            Visual("Skull", "Enemy/Skull/SkullIdle", "Enemy/Skull/SkullRun", "Enemy/Skull/SkullAttack", "Enemy/Skull/SkullDead"),
            Visual("Gnoll", "Enemy/Gnoll/GnollIdle", "Enemy/Gnoll/GnollWalk", "Enemy/Gnoll/GnollThrow", "Enemy/Gnoll/GnollDead"),
            new SwarmVisualLibrary.Visual { Name = "Arrow", Idle = PrefabSprite("Assets/Game/Prefabs/Objects/Arrow.prefab"), RotateWithVelocity = true },
            new SwarmVisualLibrary.Visual { Name = "Bone", Idle = Frames("Objects/GnollBone"), RotateWithVelocity = true }
        };
        EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
        const string materialPath = "Assets/Game/DOTS/Resources/DOTS/SwarmSprite.mat";
        var instancedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (instancedMaterial == null)
        {
            instancedMaterial = new Material(Shader.Find("RTS/DOTS/SwarmSprite")) { enableInstancing = true };
            AssetDatabase.CreateAsset(instancedMaterial, materialPath);
        }
        instancedMaterial.enableInstancing = true; EditorUtility.SetDirty(instancedMaterial); AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 38;
        camera.transform.position = new Vector3(0, 0, -10); camera.backgroundColor = new Color(0.055f, 0.09f, 0.11f);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.nearClipPlane = 0.1f; camera.farClipPlane = 100;
        cameraObject.AddComponent<AudioListener>();
        var controller = cameraObject.AddComponent<CameraController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("m_MaxZoom").floatValue = 80; serialized.FindProperty("m_MinZoom").floatValue = 5;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var demo = new GameObject("DOTS Swarm Simulation").AddComponent<SwarmDemoController>(); demo.Visuals = library;
        EditorSceneManager.SaveScene(scene, DemoScene);
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true));
        scenes.Add(new EditorBuildSettingsScene(DemoScene, true));
        foreach (var entry in EditorBuildSettings.scenes)
            if (entry.path != DemoScene && entry.path != "Assets/Scenes/SampleScene.unity") scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("DOTS visual library and SwarmSimulation scene generated.");
    }

    static SwarmVisualLibrary.Visual Visual(string name, string idle, string move, string attack, string dead)
        => new SwarmVisualLibrary.Visual { Name = name, Idle = Frames(idle), Move = Frames(move), Attack = Frames(attack), Dead = Frames(dead) };
    static Sprite[] Frames(string relative)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/" + relative + ".anim");
        if (clip == null) throw new InvalidOperationException("Missing sprite clip: " + relative);
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite") continue;
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            var frames = new Sprite[keys.Length];
            for (int i = 0; i < keys.Length; i++) frames[i] = keys[i].value as Sprite;
            return frames;
        }
        throw new InvalidOperationException("No sprite frames in clip: " + relative);
    }
    static Sprite[] PrefabSprite(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return new[] { prefab.GetComponent<SpriteRenderer>().sprite };
    }
    [MenuItem("RTS/DOTS/Open swarm simulation")]
    public static void OpenDemo()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(DemoScene);
    }
    [MenuItem("RTS/DOTS/Build swarm demo (Windows)")]
    public static void BuildDemo()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScene) == null) GenerateAssets();
        string directory = Path.GetFullPath(".utmp/swarm-build"); Directory.CreateDirectory(directory);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { DemoScene, "Assets/Scenes/SampleScene.unity" }, locationPathName = Path.Combine(directory, "SwarmDemo.exe"),
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("Swarm player build failed: " + report.summary.result);
        Debug.Log("Swarm player build succeeded: " + report.summary.outputPath);
    }
    public static void GenerateAndBuild()
    {
        GenerateAssets(); BuildDemo();
    }
}
