using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using RTS.ProceduralGeneration;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

public static class ValidateFourFrontiers
{
    const string Folder = "Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers";
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static string Baseline()
    {
        var lines = new List<string>();
        foreach(var preset in new[]{MapLayoutPreset.LegacyMainland,MapLayoutPreset.StrategicRegions})
        foreach(int seed in new[]{0,1,1543})
        {
            Check(new ProceduralMapGenerator().TryGenerate(new MapGenerationSettingsData{Preset=preset,Seed=seed},out var map,out var error),error);
            lines.Add(preset+" seed="+seed+" attempt="+map.Attempt+" sha256="+Hash(map));
        }
        File.WriteAllLines(Folder+"/baseline-after.txt",lines);
        Check(File.ReadAllLines(Folder+"/baseline-before.txt").SequenceEqual(lines),"Legacy/strategic baseline changed.");
        return "PASS: six Unity v1/v3 baseline hashes unchanged.";
    }
    public static string Sweep() => SweepRange(0,1000);
    public static string SweepRange(int start, int count)
    {
        var samples=new List<object>(); var failures=new List<string>(); var times=new List<double>();var retries=new int[8];
        var generator=new ProceduralMapGenerator();
        for(int seed=start;seed<start+count;seed++)
        {
            var settings=new MapGenerationSettingsData{Preset=MapLayoutPreset.FourFrontiers,Seed=seed};
            var timer=Stopwatch.StartNew(); bool ok=generator.TryGenerate(settings,out var map,out var error);timer.Stop();times.Add(timer.Elapsed.TotalMilliseconds);
            if(!ok){failures.Add(seed+": "+error);samples.Add(new{seed,ok,error,milliseconds=timer.Elapsed.TotalMilliseconds});continue;}
            retries[map.Attempt]++;
            Check(generator.TryGenerate(settings,out var repeat,out error),error);
            string hash=Hash(map);Check(Hash(repeat)==hash,"Nondeterministic seed "+seed);
            var report=ProceduralMapGenerator.ValidateSnapshot(settings,map);Check(report.IsValid,string.Join(" | ",report.Errors));
            samples.Add(new{seed,ok,attempt=map.Attempt,hash,milliseconds=timer.Elapsed.TotalMilliseconds,
                area=seed<=1?Enumerable.Range(0,5).Select(id=>map.Layout.RegionIds.Count(x=>x==id)).ToArray():null,
                centers=map.Layout.Regions,resourceMaxPath=map.Validation.MaxResourcePathDistance});
            if(seed==0||seed==1||seed==1543||seed<10&&map.Attempt>0)Preview(map);
        }
        times.Sort();
        var result=new{count,failures,retries,median=times[count/2-1],p95=times[(count*95/100)-1],max=times.Last(),runtime="Unity Editor Core; excludes Tilemap/prefabs/navigation",samples};
        File.WriteAllText(Folder+"/unity-sweep-"+start+"-"+count+".json",JsonConvert.SerializeObject(result,Formatting.Indented));
        foreach(int seed in new[]{1543,-1,int.MinValue})
        {var settings=new MapGenerationSettingsData{Preset=MapLayoutPreset.FourFrontiers,Seed=seed};Check(generator.TryGenerate(settings,out var map,out var error),error);Preview(map);}
        Check(failures.Count==0,"Seed failures retained in unity-sweep.json");
        return JsonConvert.SerializeObject(new{count,failures=failures.Count,retries,median=times[count/2-1],p95=times[(count*95/100)-1],max=times.Last()});
    }
    static void Preview(MapData map)
    {
        var texture=ProceduralMapPreviewWindow.Render(map);
        File.WriteAllBytes(Folder+"/preview-"+map.Seed+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
    }
    public static string BakeAndActivate()
    {
        Check(!EditorApplication.isPlaying,"Stop Play first.");
        const string settingsPath="Assets/Game/ProceduralGeneration/Settings/FourFrontiersMapSettings.asset";
        var asset=AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>(settingsPath);
        if(asset==null)
        {
            Check(AssetDatabase.CopyAsset("Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset",settingsPath),"Copy settings failed.");
            asset=AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>(settingsPath);
            var serialized=new SerializedObject(asset);serialized.FindProperty("m_Preset").enumValueIndex=2;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var data=asset.ToData();
        Check(new ProceduralMapGenerator().TryGenerate(data,out var map,out var error),error);
        var fallback=FourFrontiersFallback.Capture(data,map);string json=JsonUtility.ToJson(fallback);
        Check(JsonUtility.FromJson<FourFrontiersFallback>(json).Restore(data).Validation.IsValid,"Fallback roundtrip failed.");
        File.WriteAllText("Assets/Game/ProceduralGeneration/Resources/FourFrontiersFallback.json",json);
        AssetDatabase.Refresh();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ProceduralGame.unity");
        var authoring=UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var so=new SerializedObject(authoring);so.FindProperty("m_Settings").objectReferenceValue=asset;so.ApplyModifiedProperties();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Activated separate FourFrontiers settings; v3 settings retained. Fallback baked and validated.";
    }
    public static string FallbackChecks()
    {
        var data=new MapGenerationSettingsData{Preset=MapLayoutPreset.FourFrontiers,Seed=1543};
        string json=Resources.Load<TextAsset>("FourFrontiersFallback").text;
        var fallback=JsonUtility.FromJson<FourFrontiersFallback>(json);var map=fallback.Restore(data);
        Check(map.RequestedSeed==1543&&map.Seed==1&&map.UsedFallback,"Provenance mismatch.");
        fallback.Terrain[0]^=1;Check(map.Terrain[0]!=fallback.Terrain[0],"Fallback arrays aliased.");
        int rejected=0;
        foreach(string fixture in new[]{"version","budget","gateway","settings","pad"})
        {
            fallback=JsonUtility.FromJson<FourFrontiersFallback>(json);var requested=data.Clone();
            if(fixture=="version")fallback.Version=3;
            if(fixture=="budget")fallback.Resources=fallback.Resources.Skip(1).ToArray();
            if(fixture=="gateway")fallback.Gateways[1]=fallback.Gateways[0];
            if(fixture=="settings")requested.TotalTreeCount++;
            if(fixture=="pad")fallback.Terrain[(fallback.Settings.Height/2)*fallback.Settings.Width+fallback.Settings.Width/2]=0;
            try{fallback.Restore(requested);}catch(InvalidOperationException){rejected++;}catch(ArgumentException){rejected++;}
        }
        Check(rejected==5,"Fallback corruption accepted.");return "PASS fallback provenance, detached arrays, five invalid fixtures rejected.";
    }
    public static string Hash(MapData map)
    {
        using(var stream=new MemoryStream())
        {
            using(var w=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
            {
                w.Write(map.Width);w.Write(map.Height);w.Write(map.Seed);w.Write(map.Attempt);w.Write(map.GeneratorVersion);w.Write(map.Terrain);w.Write(map.Reserved);
                if(map.Layout!=null)
                {
                    w.Write(map.Layout.RegionIds);w.Write(map.Layout.Routes);
                    foreach(var r in map.Layout.Regions){w.Write(r.X);w.Write(r.Y);w.Write(r.Radius);w.Write((byte)r.Role);}
                    foreach(var c in map.Layout.Connections){w.Write(c.From);w.Write(c.To);}
                    foreach(var g in map.Layout.Gateways){w.Write(g.X);w.Write(g.Y);w.Write(g.Region);}
                    var l=map.Layout.Landmark;w.Write(l.X);w.Write(l.Y);w.Write(l.Radius);w.Write(l.Region);
                }
                foreach(var r in map.Resources){w.Write((byte)r.Kind);w.Write(r.X);w.Write(r.Y);w.Write(r.VariantId);w.Write(r.ClusterId);w.Write(r.IsStartingResource);}
            }
            using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","");
        }
    }
}
