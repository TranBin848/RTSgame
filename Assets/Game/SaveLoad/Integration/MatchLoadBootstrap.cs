using System;
using System.Linq;
using RTS.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

// Application bootstrap owns detached data across scene load, never scene service references.
public static class MatchLoadBootstrap
{
    public static MatchSaveData Pending { get; private set; }
    public static bool IsLoading => Pending != null;
    public static string LastError { get; private set; }
    public static bool LastLoadSucceeded { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Pending = null; LastError = null; LastLoadSucceeded = false; }
    public static void ValidateSL4(MatchSaveData save, SaveContentCatalog content)
    {
        SaveValidator.Validate(save, content.BuildRuntimeCatalog());
        if (save.Player.ActiveTechnologyId != null || save.Objects.Any(x => x.Projectile != null
            || x.Worker != null && (x.Worker.Phase != 0 || x.Worker.Carried != 0 || x.Worker.ResumeResource != 0 || x.Worker.ResumeConstruction != 0
                || x.Worker.Node != 0 || x.Worker.Depot != 0 || x.Worker.Structure != 0 || x.Worker.Shelter != 0)
            || x.Resource?.Claims > 0 || x.Construction?.Active == true || x.Producer?.Queue.Length > 0))
            throw new SaveValidationException("SL4 cannot yet restore active work/cargo/training/research/projectiles. These states require SL5.");
        if (save.Clock.Phase != 0 || save.Clock.Day != 1 || save.Reports.CompletedNight != 0)
            throw new SaveValidationException("SL4 supports the first daytime only; later clock/wave/report restoration requires SL6.");
        if (content.Clock == null || save.Clock.Elapsed >= content.Clock.DayDuration) throw new SaveValidationException("Saved clock exceeds its first-day duration.");
        if (save.Objects.Any(x => x.Actor != null && x.Body.Health <= 0))
            throw new SaveValidationException("Dead actor presentation recovery requires SL5; SL4 will not replay historical death events.");
        var settings = JsonUtility.FromJson<RTS.ProceduralGeneration.MapGenerationSettingsData>(save.Map.SettingsJson);
        var settingsToken = Newtonsoft.Json.Linq.JObject.Parse(save.Map.SettingsJson);
        foreach (var token in settingsToken.Descendants().Where(x => x.Type == Newtonsoft.Json.Linq.JTokenType.Float))
        { double value = (double)token; if (double.IsNaN(value) || double.IsInfinity(value)) throw new SaveValidationException("Non-finite map settings."); }
        string settingsError = RTS.ProceduralGeneration.MapGenerationSettingsValidator.Validate(settings);
        if (settingsError != null || settings.Seed != save.Map.Seed || settings.Width != save.Map.Width || settings.Height != save.Map.Height)
            throw new SaveValidationException("Invalid saved generation settings: " + settingsError);
        foreach (var obj in save.Objects)
        {
            var prefab = content.ResolvePrefab(obj.DefinitionId);
            if (content.DefinitionFor(prefab) != obj.DefinitionId) throw new SaveValidationException("Invalid prefab resolver.");
            if (obj.Enemy != null && content.NativeDefinition(obj.Enemy.VisualId, false) != obj.DefinitionId)
                throw new SaveValidationException("Enemy definition/visual mismatch.");
            if (obj.Construction?.RequestId > 0)
            {
                var action = content.ResolveAction(obj.Construction.ActionId) as BuildActionSo;
                if (action == null || action.StructureUnitPrefab.gameObject != prefab || !obj.Construction.Finished)
                    throw new SaveValidationException("Construction definition/action mismatch.");
            }
        }
        if (save.Player.Wallet.Wood > int.MaxValue || save.Player.Wallet.Gold > int.MaxValue || save.Player.Wallet.Meat > int.MaxValue)
            throw new SaveValidationException("Wallet exceeds native integer range.");
    }
    public static void LoadFromMenu(byte[] encoded, SaveContentCatalog content)
    {
        if (IsLoading || SceneManager.GetActiveScene().name != "MainMenu")
            throw new InvalidOperationException("SL4 loads only from MainMenu. Live-match rollback is scheduled for SL6.");
        LastLoadSucceeded = false; LastError = null;
        try
        {
            var save = new SaveCodec(content.BuildRuntimeCatalog()).Decode(encoded);
            ValidateSL4(save, content);
            Pending = save;
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated && world.GetExistingSystemManaged<RTS.DOTS.SwarmSimulationSystem>() is { } combat) combat.Paused = true;
            SceneManager.LoadScene("ProceduralGame");
        }
        catch (Exception error) { Pending = null; LastError = error.Message; throw; }
    }
    public static void Complete() { Pending = null; LastLoadSucceeded = true; LastError = null; }
    public static void Fail(string error) { Pending = null; LastLoadSucceeded = false; LastError = error; }
}
