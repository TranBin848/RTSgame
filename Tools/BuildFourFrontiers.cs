using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildFourFrontiers
{
    public static string Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Editor must be idle.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scenes before build.");
        string directory = Path.GetFullPath(".utmp/four-frontiers-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(directory);
        var scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
        if (scenes[0] != "Assets/Scenes/MainMenu.unity") throw new Exception("Player must start in MainMenu.");
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = Path.Combine(directory, "RTSgame2026.exe"),
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        var s = result.summary;
        var report = new { result = s.result.ToString(), errors = s.totalErrors, warnings = s.totalWarnings, seconds = s.totalTime.TotalSeconds,
            output = s.outputPath, backend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(), unity = Application.unityVersion, cpu = SystemInfo.processorType, memoryMiB = SystemInfo.systemMemorySize, gpu = SystemInfo.graphicsDeviceName, quality = QualitySettings.names[QualitySettings.GetQualityLevel()], vsync = QualitySettings.vSyncCount };
        Directory.CreateDirectory("Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers");
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers/Build.json", json);
        if (s.result != BuildResult.Succeeded) throw new Exception("SL4 Windows build failed.");
        return json;
    }
}
