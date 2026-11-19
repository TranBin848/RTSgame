// Unity MCP script-execute: S9Build.Run, outside Play Mode, saved scenes required.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class S9Build
{
    [Serializable] sealed class Evidence
    {
        public string result, unity, output;
        public string[] scenes;
        public ulong bytes;
        public double seconds;
        public int errors, warnings;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stop Play/compile before building.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes before build.");
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Windows build support is unavailable.");
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        if(scenes.Length==0||scenes[0]!="Assets/Scenes/MainMenu.unity"||!scenes.Contains("Assets/Scenes/ProceduralGame.unity")||!scenes.Contains("Assets/Scenes/SampleScene.unity"))
            throw new InvalidOperationException("Expected MainMenu first and both gameplay scenes enabled.");
        string folder=Path.GetFullPath(".utmp/s9-build-2026-11-17");Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=scenes,locationPathName=Path.Combine(folder,"RTSgame2026.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
        var s=report.summary;
        var evidence=new Evidence {result=s.result.ToString(),unity=Application.unityVersion,output=s.outputPath,scenes=scenes,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,errors=(int)s.totalErrors,warnings=(int)s.totalWarnings};
        string path="Docs/Modules/Progression/S9-Build-2026-11-17.json";File.WriteAllText(path,JsonUtility.ToJson(evidence,true));
        if(s.result!=BuildResult.Succeeded)throw new Exception("Player build failed; see "+path);
        return JsonUtility.ToJson(evidence,true);
    }
}
