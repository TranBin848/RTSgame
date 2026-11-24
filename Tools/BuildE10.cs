using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class E10Build
{
    public static string Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stop Play/compile before build.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scenes before build.");
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0 || scenes[0] != "Assets/Scenes/MainMenu.unity" || !scenes.Contains("Assets/Scenes/ProceduralGame.unity")
            || !scenes.Contains("Assets/Scenes/SampleScene.unity")) throw new Exception("Unexpected build scene contract.");
        string folder = Path.GetFullPath(".utmp/e10-release-2026-11-24");
        if (File.Exists(Path.Combine(folder, "RTSgame2026.exe"))) throw new Exception("Use a new artifact path; existing build retained.");
        Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes,
            locationPathName = Path.Combine(folder, "RTSgame2026.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        var s = report.summary;
        var evidence = new {
            result = s.result.ToString(), unity = Application.unityVersion, output = s.outputPath, scenes,
            bytes = s.totalSize, seconds = s.totalTime.TotalSeconds, errors = s.totalErrors, warnings = s.totalWarnings,
            options = "None", backend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(),
            quality = QualitySettings.names[QualitySettings.GetQualityLevel()], vsync = QualitySettings.vSyncCount
        };
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(evidence, Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory("Docs/Modules/GameExperience/Evidence/E10");
        File.WriteAllText("Docs/Modules/GameExperience/Evidence/E10/build.json", json);
        if (s.result != BuildResult.Succeeded) throw new Exception("Release build failed; inspect build.json.");
        return json;
    }
}
