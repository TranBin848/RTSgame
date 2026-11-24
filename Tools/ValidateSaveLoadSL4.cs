using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RTS.DOTS;
using RTS.SaveLoad;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateSaveLoadSL4
{
    const string Folder = "Outputs/SaveLoad/SL4";
    static GameSceneCompositionRoot Root => UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
    static SaveContentCatalog Content => Resources.Load<SaveContentCatalog>("ProceduralSaveCatalog");
    static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    static void Report(string name, object value) { Directory.CreateDirectory(Folder); File.WriteAllText(Path.Combine(Folder, name), JsonConvert.SerializeObject(value, Formatting.Indented)); }
    public static string Prepare()
    {
        Check(Root != null && Root.IsReady, "Source scene must be ready.");
        Application.runInBackground = true; Time.timeScale = 0;
        var bridge = Root.GetComponent<SwarmGameBridge>(); var em = bridge.Gameplay.EntityManager;
        bridge.Gameplay.AddResources(37, -11, 23);
        var hall = UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var resource = UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None).First();
        bridge.UnregisterResource(resource); UnityEngine.Object.Destroy(resource.gameObject);
        Check(bridge.SpawnGameplay(Content.ResolvePrefab("storagepit"), new Vector3(8, 8), Quaternion.identity) != null, "New structure fixture failed.");
        var hallBody = em.GetComponentData<CombatBody>(hall.DotsEntity);
        Check(bridge.Simulation.TryResolveSpawnPosition(hallBody.Position + new float2(7, 0), .25f, out var position), "Enemy position failed.");
        var enemy = bridge.Simulation.SpawnEnemy(position, EnemySpawnSettings.Default);
        var body = em.GetComponentData<CombatBody>(enemy); body.Health = 73; em.SetComponentData(enemy, body);
        var soldier = UnityEngine.Object.FindFirstObjectByType<WarriorUnit>();
        if (soldier != null) bridge.Gameplay.Attack(soldier.DotsEntity, enemy);
        Root.GetComponent<GameManager>().RestoreLoadedSelection(new[] { (Unit)hall });
        Check(Root.SaveCapture.RequestCapture(1), "Source capture refused.");
        return "Prepared wallet/resource tombstone/new building/native target/selection fixture.";
    }
    public static string SaveAndMenu()
    {
        var service = Root.SaveCapture;
        Check(!service.IsPending && service.LastSnapshot != null, service.LastError ?? "Source pending.");
        var snapshot = service.LastSnapshot;
        MatchLoadBootstrap.ValidateSL4(snapshot, Content);
        var bytes = new SaveCodec(service.Catalog).Encode(snapshot);
        Directory.CreateDirectory(Folder); File.WriteAllBytes(Path.Combine(Folder, "Source.json"), bytes);
        Report("SourceSummary.json", new { seed = snapshot.Map.Seed, objects = snapshot.Objects.Length,
            resources = snapshot.Objects.Count(x => x.Resource != null), enemies = snapshot.Objects.Count(x => x.Enemy != null),
            tombstones = snapshot.Map.Placements.Count(x => x.ObjectId == 0), snapshot.Player.Wallet, snapshot.NextSaveId });
        SceneManager.LoadScene("MainMenu");
        return "Saved detached source fixture; unloaded source match to MainMenu.";
    }
    public static string Load()
    {
        Check(SceneManager.GetActiveScene().name == "MainMenu", "Load must begin at menu.");
        var bytes = File.ReadAllBytes(Path.Combine(Folder, "Source.json"));
        // Corrupt/config-incompatible input must never tear down the menu.
        bool rejected = false;
        var bad = (byte[])bytes.Clone(); bad[bad.Length / 2] ^= 1;
        try { MatchLoadBootstrap.LoadFromMenu(bad, Content); } catch (SaveValidationException) { rejected = true; }
        Check(rejected && SceneManager.GetActiveScene().name == "MainMenu" && !MatchLoadBootstrap.IsLoading, "Bad file mutated menu.");
        MatchLoadBootstrap.LoadFromMenu(bytes, Content);
        return "Preflight corruption guard PASS; fresh-scene load requested.";
    }
    public static string RequestRecapture()
    {
        Check(Root != null && Root.IsReady, Root != null ? Root.LastError : MatchLoadBootstrap.LastError);
        Check(MatchLoadBootstrap.LastLoadSucceeded && !MatchLoadBootstrap.IsLoading, "Load operation not finalized.");
        Check(Time.timeScale == 0 && Root.GetComponent<SwarmGameBridge>().Simulation.Paused, "Loaded match was not held paused.");
        Check(Root.SaveCapture.RequestCapture(2), "Restored capture refused.");
        return "Loaded/Ready/paused; canonical comparison requested.";
    }
    public static string Verify()
    {
        var service = Root.SaveCapture;
        Check(!service.IsPending && service.LastSnapshot != null, service.LastError ?? "Restored pending.");
        var codec = new SaveCodec(service.Catalog);
        var original = codec.Decode(File.ReadAllBytes(Path.Combine(Folder, "Source.json")));
        var restored = service.LastSnapshot;
        var a = JObject.FromObject(original); var b = JObject.FromObject(restored);
        foreach (string key in new[] { "SaveId", "CapturedUtc", "Generation" }) { a.Remove(key); b.Remove(key); }
        bool equal = JToken.DeepEquals(a, b);
        if (!equal) { Report("Mismatch.json", new { expected = a, actual = b }); throw new Exception("SL4 canonical field equivalence failed; see Mismatch.json."); }
        var bridge = Root.GetComponent<SwarmGameBridge>();
        Check(bridge.Simulation.RenderData.Length == original.Objects.Count(x => x.Enemy != null), "Paused native render cache missing.");
        Check(original.Map.Placements.Count(x => x.ObjectId == 0) == 1 && restored.Objects.Count(x => x.Resource != null) == original.Objects.Count(x => x.Resource != null), "Lost resource was respawned.");
        long next = bridge.Simulation.NextSaveId;
        var enemy = restored.Objects.First(x => x.Enemy != null);
        Check(bridge.Simulation.TryResolveSpawnPosition(new float2(enemy.Body.Position.X + 1, enemy.Body.Position.Y), .25f, out var position), "Postload spawn fixture position failed.");
        var created = bridge.Simulation.SpawnEnemy(position, EnemySpawnSettings.Default);
        long newId = bridge.Gameplay.EntityManager.GetComponentData<SaveIdentity>(created).Value;
        Check(newId == next && !restored.Objects.Any(x => x.Id == newId), "SaveId reused after load.");
        var report = new { passed = true, seed = original.Map.Seed, objects = original.Objects.Length, canonicalFieldsEqual = equal,
            allRelationsRemapped = true, walletUnchanged = true, noStartingGrant = true, resourceTombstonePreserved = true,
            nativeVisibleWhilePaused = true, nextIdContinues = true, newId, paused = Time.timeScale == 0, WorldCount = Unity.Entities.World.All.Count };
        Report("Result-" + original.Map.Seed + ".json", report);
        File.WriteAllBytes(Path.Combine(Folder, "Restored.json"), codec.Encode(restored));
        return JsonConvert.SerializeObject(report);
    }
}
