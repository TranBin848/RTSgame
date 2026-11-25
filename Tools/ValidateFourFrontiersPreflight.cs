using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using RTS.SaveLoad;
using RTS.ProceduralGeneration;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateFourFrontiersPreflight
{
    public static string Run()
    {
        if(SceneManager.GetActiveScene().name!="MainMenu")throw new Exception("Menu required.");
        var content=Resources.Load<SaveContentCatalog>("ProceduralSaveCatalog");
        var codec=new SaveCodec(content.BuildRuntimeCatalog());
        var bytes=File.ReadAllBytes("Outputs/FourFrontiers/SL4-1/Source.json");
        var source=codec.Decode(bytes); MatchLoadBootstrap.ValidateSL4(source,content);
        var rows=new List<object>();
        foreach(string fault in new[]{"schema","content","version","role","connection","gateway","region-id","preset","starting-budget","regional-budget","provenance"})
        {
            var save=codec.Decode(bytes);
            if(fault=="schema")save.SchemaVersion=1;
            if(fault=="content")save.ContentFingerprint=new string('0',64);
            if(fault=="version")save.Map.GeneratorVersion=3;
            if(fault=="role")save.Map.Regions[4].Role=1;
            if(fault=="connection")save.Map.Connections[3]=save.Map.Connections[0];
            if(fault=="gateway")save.Map.Gateways[1]=save.Map.Gateways[0];
            if(fault=="region-id")save.Map.RegionIds[0]=5;
            if(fault=="preset")
            {var data=JsonUtility.FromJson<MapGenerationSettingsData>(save.Map.SettingsJson);data.Preset=MapLayoutPreset.StrategicRegions;save.Map.SettingsJson=JsonUtility.ToJson(data);}
            if(fault=="starting-budget")
            {int index=Array.FindIndex(save.Map.Placements,p=>p.IsStartingResource);save.Map.Placements[index].IsStartingResource=false;}
            if(fault=="regional-budget")
            {var p=save.Map.Placements.First(x=>!x.IsStartingResource);save.Map.RegionIds[p.Y*save.Map.Width+p.X]=0;}
            if(fault=="provenance")save.Map.RequestedSeed=1543;
            string error=null;
            try {MatchLoadBootstrap.LoadFromMenu(codec.Encode(save),content);}
            catch(SaveValidationException e){error=e.Message;}
            if(error==null||SceneManager.GetActiveScene().name!="MainMenu"||MatchLoadBootstrap.IsLoading)
                throw new Exception("Preflight changed menu or accepted fault: "+fault);
            rows.Add(new{fault,rejected=true,error,scene="MainMenu",pending=false});
        }
        string json=JsonConvert.SerializeObject(new{passed=true,count=rows.Count,rows},Formatting.Indented);
        File.WriteAllText("Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers/preflight.json",json);
        return json;
    }
}
